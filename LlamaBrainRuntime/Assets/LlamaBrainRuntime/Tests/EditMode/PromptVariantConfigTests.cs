#nullable enable
#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using LlamaBrain.Persona;
using LlamaBrain.Runtime.Core;

namespace LlamaBrain.Tests.EditMode
{
  [TestFixture]
  [Category("Domain")]
  public class PromptVariantConfigTests
  {
    private PersonaConfig? _config;

    [SetUp]
    public void SetUp()
    {
      _config = ScriptableObject.CreateInstance<PersonaConfig>();
      _config.PersonaId = "test_variant_001";
      _config.Name = "Test Variant Config";
      _config.Description = "Test config for variant testing";
      _config.SystemPrompt = "Default prompt";
    }

    [TearDown]
    public void TearDown()
    {
      if (_config != null)
      {
        Object.DestroyImmediate(_config);
      }
    }

    [Test]
    public void PromptVariantConfig_Serialization_RoundTrips()
    {
      // Arrange: Create variants
      _config!.SystemPromptVariants = new System.Collections.Generic.List<PromptVariantConfig>
      {
        new PromptVariantConfig
        {
          VariantName = "VariantA",
          SystemPrompt = "You are variant A.",
          TrafficPercentage = 50f,
          IsActive = true
        },
        new PromptVariantConfig
        {
          VariantName = "VariantB",
          SystemPrompt = "You are variant B.",
          TrafficPercentage = 50f,
          IsActive = true
        }
      };

      // Act: Serialize and deserialize (Unity does this via JSON internally)
      var json = JsonUtility.ToJson(_config);
      var deserialized = ScriptableObject.CreateInstance<PersonaConfig>();
      JsonUtility.FromJsonOverwrite(json, deserialized);

      try
      {
        // Assert: Variants should round-trip correctly
        Assert.IsNotNull(deserialized.SystemPromptVariants, "Variants should not be null after deserialization");
        Assert.AreEqual(2, deserialized.SystemPromptVariants.Count, "Should have 2 variants");

        var variantA = deserialized.SystemPromptVariants[0];
        Assert.AreEqual("VariantA", variantA.VariantName);
        Assert.AreEqual("You are variant A.", variantA.SystemPrompt);
        Assert.AreEqual(50f, variantA.TrafficPercentage);
        Assert.IsTrue(variantA.IsActive);

        var variantB = deserialized.SystemPromptVariants[1];
        Assert.AreEqual("VariantB", variantB.VariantName);
        Assert.AreEqual("You are variant B.", variantB.SystemPrompt);
        Assert.AreEqual(50f, variantB.TrafficPercentage);
        Assert.IsTrue(variantB.IsActive);
      }
      finally
      {
        Object.DestroyImmediate(deserialized);
      }
    }

    [Test]
    public void PromptVariantConfig_TrafficSum_Validates()
    {
      // Arrange: Create variants that sum to 100%
      _config!.SystemPromptVariants = new System.Collections.Generic.List<PromptVariantConfig>
      {
        new PromptVariantConfig
        {
          VariantName = "VariantA",
          SystemPrompt = "You are variant A.",
          TrafficPercentage = 30f,
          IsActive = true
        },
        new PromptVariantConfig
        {
          VariantName = "VariantB",
          SystemPrompt = "You are variant B.",
          TrafficPercentage = 70f,
          IsActive = true
        }
      };

      // Act: Validate traffic sum
      var errors = _config.ValidateVariantTraffic();

      // Assert: Should have no errors (sums to 100%)
      Assert.IsEmpty(errors, "Traffic sum validation should pass for 30% + 70% = 100%");
    }

    [Test]
    public void PromptVariantConfig_TrafficSum_TooLow_ReturnsError()
    {
      // Arrange: Create variants that sum to less than 100%
      _config!.SystemPromptVariants = new System.Collections.Generic.List<PromptVariantConfig>
      {
        new PromptVariantConfig
        {
          VariantName = "VariantA",
          SystemPrompt = "You are variant A.",
          TrafficPercentage = 30f,
          IsActive = true
        },
        new PromptVariantConfig
        {
          VariantName = "VariantB",
          SystemPrompt = "You are variant B.",
          TrafficPercentage = 50f,
          IsActive = true
        }
      };

      // Act: Validate traffic sum
      var errors = _config.ValidateVariantTraffic();

      // Assert: Should have error (sums to 80%, not 100%)
      Assert.IsNotEmpty(errors, "Traffic sum validation should fail for 30% + 50% = 80%");
      Assert.That(errors[0], Does.Contain("80").IgnoreCase, "Error should mention actual sum (80%)");
    }

    [Test]
    public void PromptVariantConfig_TrafficSum_TooHigh_ReturnsError()
    {
      // Arrange: Create variants that sum to more than 100%
      _config!.SystemPromptVariants = new System.Collections.Generic.List<PromptVariantConfig>
      {
        new PromptVariantConfig
        {
          VariantName = "VariantA",
          SystemPrompt = "You are variant A.",
          TrafficPercentage = 60f,
          IsActive = true
        },
        new PromptVariantConfig
        {
          VariantName = "VariantB",
          SystemPrompt = "You are variant B.",
          TrafficPercentage = 60f,
          IsActive = true
        }
      };

      // Act: Validate traffic sum
      var errors = _config.ValidateVariantTraffic();

      // Assert: Should have error (sums to 120%, not 100%)
      Assert.IsNotEmpty(errors, "Traffic sum validation should fail for 60% + 60% = 120%");
      Assert.That(errors[0], Does.Contain("120").IgnoreCase, "Error should mention actual sum (120%)");
    }

    [Test]
    public void PromptVariantConfig_InactiveVariants_NotIncludedInSum()
    {
      // Arrange: Create variants where inactive one doesn't count toward sum
      _config!.SystemPromptVariants = new System.Collections.Generic.List<PromptVariantConfig>
      {
        new PromptVariantConfig
        {
          VariantName = "Active",
          SystemPrompt = "You are active.",
          TrafficPercentage = 100f,
          IsActive = true
        },
        new PromptVariantConfig
        {
          VariantName = "Inactive",
          SystemPrompt = "You are inactive.",
          TrafficPercentage = 50f,
          IsActive = false // Inactive, should not count
        }
      };

      // Act: Validate traffic sum
      var errors = _config.ValidateVariantTraffic();

      // Assert: Should pass (only active variants count, 100%)
      Assert.IsEmpty(errors, "Inactive variants should not count toward traffic sum");
    }

    [Test]
    public void PromptVariantConfig_EmptyList_NoError()
    {
      // Arrange: Empty variant list
      _config!.SystemPromptVariants = new System.Collections.Generic.List<PromptVariantConfig>();

      // Act: Validate traffic sum
      var errors = _config.ValidateVariantTraffic();

      // Assert: Should have no errors (no variants = no validation needed)
      Assert.IsEmpty(errors, "Empty variant list should not produce errors");
    }

    [Test]
    public void PromptVariantConfig_NullList_NoError()
    {
      // Arrange: Null variant list
      _config!.SystemPromptVariants = null;

      // Act: Validate traffic sum
      var errors = _config.ValidateVariantTraffic();

      // Assert: Should have no errors (null list = no variants)
      Assert.IsEmpty(errors, "Null variant list should not produce errors");
    }

    [Test]
    public void PromptVariantConfig_DuplicateNames_ReturnsError()
    {
      // Arrange: Create variants with duplicate names
      _config!.SystemPromptVariants = new System.Collections.Generic.List<PromptVariantConfig>
      {
        new PromptVariantConfig
        {
          VariantName = "VariantA",
          SystemPrompt = "You are variant A v1.",
          TrafficPercentage = 50f,
          IsActive = true
        },
        new PromptVariantConfig
        {
          VariantName = "VariantA", // Duplicate name
          SystemPrompt = "You are variant A v2.",
          TrafficPercentage = 50f,
          IsActive = true
        }
      };

      // Act: Validate traffic sum
      var errors = _config.ValidateVariantTraffic();

      // Assert: Should have error about duplicate names
      Assert.IsNotEmpty(errors, "Duplicate variant names should produce error");
      Assert.That(errors[0], Does.Contain("duplicate").IgnoreCase, "Error should mention duplicates");
    }

    [Test]
    public void ToPromptVariants_ConvertsToLlamaBrainVariants()
    {
      // Arrange: Create Unity variants
      _config!.SystemPromptVariants = new System.Collections.Generic.List<PromptVariantConfig>
      {
        new PromptVariantConfig
        {
          VariantName = "VariantA",
          SystemPrompt = "You are variant A.",
          TrafficPercentage = 50f,
          IsActive = true
        },
        new PromptVariantConfig
        {
          VariantName = "VariantB",
          SystemPrompt = "You are variant B.",
          TrafficPercentage = 50f,
          IsActive = false
        }
      };

      // Act: Convert to LlamaBrain.Config.PromptVariant
      var variants = _config.ToPromptVariants();

      // Assert: Should convert correctly
      Assert.IsNotNull(variants, "Converted variants should not be null");
      Assert.AreEqual(2, variants.Count, "Should have 2 variants");

      var variantA = variants[0];
      Assert.AreEqual("VariantA", variantA.Name);
      Assert.AreEqual("You are variant A.", variantA.SystemPrompt);
      Assert.AreEqual(50f, variantA.TrafficPercentage);
      Assert.IsTrue(variantA.IsActive);

      var variantB = variants[1];
      Assert.AreEqual("VariantB", variantB.Name);
      Assert.AreEqual("You are variant B.", variantB.SystemPrompt);
      Assert.AreEqual(50f, variantB.TrafficPercentage);
      Assert.IsFalse(variantB.IsActive);
    }
  }
}
#endif
