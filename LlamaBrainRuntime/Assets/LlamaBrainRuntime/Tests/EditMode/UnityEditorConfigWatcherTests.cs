#nullable enable
#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using LlamaBrain.Editor.Config;
using LlamaBrain.Runtime.Core;
using System.Collections.Generic;
using System.IO;

namespace LlamaBrain.Tests.EditMode
{
  [TestFixture]
  [Category("Integration")]
  public class UnityEditorConfigWatcherTests
  {
    private UnityEditorConfigWatcher? _watcher;
    private List<string> _changedAssets = new List<string>();
    private string _testAssetFolder = "Assets/TestConfigs";

    [SetUp]
    public void SetUp()
    {
      _watcher = UnityEditorConfigWatcher.Instance;
      _changedAssets.Clear();
      _watcher.OnConfigChanged += OnConfigChanged;

      // Create test asset folder
      if (!AssetDatabase.IsValidFolder(_testAssetFolder))
      {
        AssetDatabase.CreateFolder("Assets", "TestConfigs");
      }
    }

    [TearDown]
    public void TearDown()
    {
      if (_watcher != null)
      {
        _watcher.StopWatching();
        _watcher.OnConfigChanged -= OnConfigChanged;
      }

      _changedAssets.Clear();

      // Clean up test assets
      if (AssetDatabase.IsValidFolder(_testAssetFolder))
      {
        AssetDatabase.DeleteAsset(_testAssetFolder);
      }

      AssetDatabase.Refresh();
    }

    private void OnConfigChanged(string assetPath)
    {
      _changedAssets.Add(assetPath);
    }

    [Test]
    public void StartWatching_SetsIsWatchingTrue()
    {
      // Arrange
      Assert.IsFalse(_watcher!.IsWatching, "Watcher should not be watching initially");

      // Act
      _watcher.StartWatching();

      // Assert
      Assert.IsTrue(_watcher.IsWatching, "Watcher should be watching after StartWatching()");
    }

    [Test]
    public void StopWatching_SetsIsWatchingFalse()
    {
      // Arrange
      _watcher!.StartWatching();
      Assert.IsTrue(_watcher.IsWatching);

      // Act
      _watcher.StopWatching();

      // Assert
      Assert.IsFalse(_watcher.IsWatching, "Watcher should stop watching after StopWatching()");
    }

    [Test]
    public void AssetModified_PersonaConfig_FiresEvent()
    {
      // Arrange
      _watcher!.StartWatching();

      // Create a PersonaConfig asset
      var config = ScriptableObject.CreateInstance<PersonaConfig>();
      string assetPath = $"{_testAssetFolder}/TestPersonaConfig.asset";
      AssetDatabase.CreateAsset(config, assetPath);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      _changedAssets.Clear(); // Clear any changes from creation

      // Act: Modify the asset
      config.Name = "Modified Wizard";
      EditorUtility.SetDirty(config);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      // Wait for debounce + processing
      System.Threading.Thread.Sleep(150); // 100ms debounce + 50ms buffer
      EditorApplication.Step(); // Trigger EditorApplication.update

      // Assert
      Assert.That(_changedAssets.Count, Is.GreaterThan(0),
        "Config change event should fire for PersonaConfig modification");
      Assert.That(_changedAssets, Contains.Item(assetPath),
        $"Changed assets should contain {assetPath}");
    }

    [Test]
    public void AssetModified_BrainSettings_FiresEvent()
    {
      // Arrange
      _watcher!.StartWatching();

      // Create a BrainSettings asset
      var settings = ScriptableObject.CreateInstance<BrainSettings>();
      string assetPath = $"{_testAssetFolder}/TestBrainSettings.asset";
      AssetDatabase.CreateAsset(settings, assetPath);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      _changedAssets.Clear(); // Clear any changes from creation

      // Act: Modify the asset
      settings.Temperature = 0.5f;
      EditorUtility.SetDirty(settings);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      // Wait for debounce + processing
      System.Threading.Thread.Sleep(150);
      EditorApplication.Step();

      // Assert
      Assert.That(_changedAssets.Count, Is.GreaterThan(0),
        "Config change event should fire for BrainSettings modification");
      Assert.That(_changedAssets, Contains.Item(assetPath),
        $"Changed assets should contain {assetPath}");
    }

    [Test]
    public void AssetModified_UnrelatedAsset_DoesNotFireEvent()
    {
      // Arrange
      _watcher!.StartWatching();

      // Create a non-config asset (just a regular ScriptableObject)
      var unrelatedAsset = ScriptableObject.CreateInstance<ScriptableObject>();
      string assetPath = $"{_testAssetFolder}/UnrelatedAsset.asset";
      AssetDatabase.CreateAsset(unrelatedAsset, assetPath);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      _changedAssets.Clear();

      // Act: Modify the unrelated asset
      EditorUtility.SetDirty(unrelatedAsset);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      // Wait for debounce + processing
      System.Threading.Thread.Sleep(150);
      EditorApplication.Step();

      // Assert
      Assert.That(_changedAssets, Does.Not.Contain(assetPath),
        "Unrelated asset changes should not fire config change events");
    }

    [Test]
    public void MultipleChanges_Debounced_FiresOnce()
    {
      // Arrange
      _watcher!.StartWatching();

      var config = ScriptableObject.CreateInstance<PersonaConfig>();
      string assetPath = $"{_testAssetFolder}/DebouncedConfig.asset";
      AssetDatabase.CreateAsset(config, assetPath);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      _changedAssets.Clear();

      // Act: Make multiple rapid changes (within debounce window)
      config.Name = "Change 1";
      EditorUtility.SetDirty(config);
      AssetDatabase.SaveAssets();

      System.Threading.Thread.Sleep(30); // Within debounce window

      config.Name = "Change 2";
      EditorUtility.SetDirty(config);
      AssetDatabase.SaveAssets();

      System.Threading.Thread.Sleep(30); // Within debounce window

      config.Name = "Change 3";
      EditorUtility.SetDirty(config);
      AssetDatabase.SaveAssets();

      AssetDatabase.Refresh();

      // Wait for debounce to complete
      System.Threading.Thread.Sleep(150);
      EditorApplication.Step();

      // Assert: Should fire event only once after debounce
      int occurrences = 0;
      foreach (var changed in _changedAssets)
      {
        if (changed == assetPath)
          occurrences++;
      }

      Assert.That(occurrences, Is.EqualTo(1),
        "Debouncing should group multiple rapid changes into a single event");
    }

    [Test]
    public void WatcherNotStarted_NoEventsF fired()
    {
      // Arrange: Don't start watching
      Assert.IsFalse(_watcher!.IsWatching);

      var config = ScriptableObject.CreateInstance<PersonaConfig>();
      string assetPath = $"{_testAssetFolder}/UnwatchedConfig.asset";
      AssetDatabase.CreateAsset(config, assetPath);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      _changedAssets.Clear();

      // Act: Modify asset while not watching
      config.Name = "Modified";
      EditorUtility.SetDirty(config);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      System.Threading.Thread.Sleep(150);
      EditorApplication.Step();

      // Assert
      Assert.That(_changedAssets, Is.Empty,
        "No events should fire when watcher is not started");
    }

    [Test]
    public void Singleton_ReturnsConsistentInstance()
    {
      // Act
      var instance1 = UnityEditorConfigWatcher.Instance;
      var instance2 = UnityEditorConfigWatcher.Instance;

      // Assert
      Assert.AreSame(instance1, instance2,
        "Instance property should return the same singleton instance");
    }
  }
}
#endif
