using System;
using NUnit.Framework;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Tests.Memory
{
  /// <summary>
  /// Comprehensive tests for EpisodicMemoryEntry.
  /// </summary>
  [TestFixture]
  public class EpisodicMemoryTests
  {
    #region Constructor Tests

    [Test]
    public void Constructor_WithDescription_SetsProperties()
    {
      // Act
      var memory = new EpisodicMemoryEntry("Player said hello");

      // Assert
      Assert.That(memory.Description, Is.EqualTo("Player said hello"));
      Assert.That(memory.EpisodeType, Is.EqualTo(EpisodeType.Dialogue));
      Assert.That(memory.Strength, Is.EqualTo(1.0f));
      Assert.That(memory.Significance, Is.EqualTo(0.5f));
      Assert.That(memory.Source, Is.EqualTo(MutationSource.ValidatedOutput));
      Assert.That(memory.Authority, Is.EqualTo(MemoryAuthority.Episodic));
      Assert.That(memory.Content, Is.EqualTo("Player said hello"));
    }

    [Test]
    public void Constructor_WithDescriptionAndType_SetsEpisodeType()
    {
      // Act
      var memory = new EpisodicMemoryEntry("Test observation", EpisodeType.Observation);

      // Assert
      Assert.That(memory.Description, Is.EqualTo("Test observation"));
      Assert.That(memory.EpisodeType, Is.EqualTo(EpisodeType.Observation));
    }

    [Test]
    public void Constructor_WithNullDescription_ThrowsArgumentNullException()
    {
      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => new EpisodicMemoryEntry(null!));
    }

    [Test]
    public void Constructor_WithNullDescriptionAndType_ThrowsArgumentNullException()
    {
      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => new EpisodicMemoryEntry(null!, EpisodeType.Event));
    }

    #endregion

    #region Property Tests

    [Test]
    public void Content_ReturnsDescription()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test content");

      // Assert
      Assert.That(memory.Content, Is.EqualTo("Test content"));
      Assert.That(memory.Content, Is.EqualTo(memory.Description));
    }

    [Test]
    public void Participant_CanBeSet()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test");

      // Act
      memory.Participant = "Player";

      // Assert
      Assert.That(memory.Participant, Is.EqualTo("Player"));
    }

    [Test]
    public void Participant_CanBeNull()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Participant = "Player" };

      // Act
      memory.Participant = null;

      // Assert
      Assert.That(memory.Participant, Is.Null);
    }

    [Test]
    public void GameTime_CanBeSet()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test");

      // Act
      memory.GameTime = 123.45f;

      // Assert
      Assert.That(memory.GameTime, Is.EqualTo(123.45f));
    }

    [Test]
    public void Significance_CanBeSet()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test");

      // Act
      memory.Significance = 0.8f;

      // Assert
      Assert.That(memory.Significance, Is.EqualTo(0.8f));
    }

    [Test]
    public void Strength_CanBeSet()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test");

      // Act
      memory.Strength = 0.5f;

      // Assert
      Assert.That(memory.Strength, Is.EqualTo(0.5f));
    }

    [Test]
    public void EpisodeType_CanBeChanged()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test", EpisodeType.Dialogue);

      // Act
      memory.EpisodeType = EpisodeType.Event;

      // Assert
      Assert.That(memory.EpisodeType, Is.EqualTo(EpisodeType.Event));
    }

    #endregion

    #region IsActive Tests

    [Test]
    public void IsActive_WhenStrengthAboveThreshold_ReturnsTrue()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.11f };

      // Assert
      Assert.That(memory.IsActive, Is.True);
    }

    [Test]
    public void IsActive_WhenStrengthAtThreshold_ReturnsFalse()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.1f };

      // Assert
      Assert.That(memory.IsActive, Is.False);
    }

    [Test]
    public void IsActive_WhenStrengthBelowThreshold_ReturnsFalse()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.05f };

      // Assert
      Assert.That(memory.IsActive, Is.False);
    }

    [Test]
    public void IsActive_WhenStrengthIsZero_ReturnsFalse()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.0f };

      // Assert
      Assert.That(memory.IsActive, Is.False);
    }

    [Test]
    public void IsActive_WhenStrengthIsOne_ReturnsTrue()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 1.0f };

      // Assert
      Assert.That(memory.IsActive, Is.True);
    }

    #endregion

    #region ApplyDecay Tests

    [Test]
    public void ApplyDecay_ReducesStrength()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 1.0f, Significance = 0.0f };

      // Act
      memory.ApplyDecay(0.2f);

      // Assert
      Assert.That(memory.Strength, Is.EqualTo(0.8f).Within(0.001f));
    }

    [Test]
    public void ApplyDecay_WithHighSignificance_DecaysSlower()
    {
      // Arrange
      var normalMemory = new EpisodicMemoryEntry("Normal") { Strength = 1.0f, Significance = 0.0f };
      var significantMemory = new EpisodicMemoryEntry("Significant") { Strength = 1.0f, Significance = 1.0f };

      // Act
      normalMemory.ApplyDecay(0.2f);
      significantMemory.ApplyDecay(0.2f);

      // Assert
      // Normal: 1.0 - 0.2 = 0.8
      // Significant: 1.0 - (0.2 * (1.0 - 1.0 * 0.5)) = 1.0 - (0.2 * 0.5) = 1.0 - 0.1 = 0.9
      Assert.That(normalMemory.Strength, Is.EqualTo(0.8f).Within(0.001f));
      Assert.That(significantMemory.Strength, Is.EqualTo(0.9f).Within(0.001f));
      Assert.That(significantMemory.Strength, Is.GreaterThan(normalMemory.Strength));
    }

    [Test]
    public void ApplyDecay_WithMediumSignificance_DecaysModerately()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 1.0f, Significance = 0.5f };

      // Act
      memory.ApplyDecay(0.2f);

      // Assert
      // Adjusted decay: 0.2 * (1.0 - 0.5 * 0.5) = 0.2 * 0.75 = 0.15
      // Strength: 1.0 - 0.15 = 0.85
      Assert.That(memory.Strength, Is.EqualTo(0.85f).Within(0.001f));
    }

    [Test]
    public void ApplyDecay_CanReduceStrengthToZero()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.1f, Significance = 0.0f };

      // Act
      memory.ApplyDecay(0.2f);

      // Assert
      Assert.That(memory.Strength, Is.EqualTo(0.0f).Within(0.001f));
    }

    [Test]
    public void ApplyDecay_WithLargeDecayFactor_ClampsToZero()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.5f, Significance = 0.0f };

      // Act
      memory.ApplyDecay(1.0f);

      // Assert
      Assert.That(memory.Strength, Is.EqualTo(0.0f).Within(0.001f));
    }

    [Test]
    public void ApplyDecay_WithHighSignificanceAndLargeDecay_StillClampsToZero()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.3f, Significance = 1.0f };

      // Act
      memory.ApplyDecay(1.0f);

      // Assert
      // Adjusted decay: 1.0 * (1.0 - 1.0 * 0.5) = 0.5
      // Strength: 0.3 - 0.5 = -0.2, clamped to 0.0
      Assert.That(memory.Strength, Is.EqualTo(0.0f).Within(0.001f));
    }

    [Test]
    public void ApplyDecay_MultipleTimes_Accumulates()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 1.0f, Significance = 0.0f };

      // Act
      memory.ApplyDecay(0.2f);
      memory.ApplyDecay(0.2f);
      memory.ApplyDecay(0.2f);

      // Assert
      Assert.That(memory.Strength, Is.EqualTo(0.4f).Within(0.001f));
    }

    #endregion

    #region Reinforce Tests

    [Test]
    public void Reinforce_IncreasesStrength()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.5f };

      // Act
      memory.Reinforce(0.3f);

      // Assert
      Assert.That(memory.Strength, Is.EqualTo(0.8f).Within(0.001f));
    }

    [Test]
    public void Reinforce_WithDefaultAmount_IncreasesByPointTwo()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.5f };

      // Act
      memory.Reinforce();

      // Assert
      Assert.That(memory.Strength, Is.EqualTo(0.7f).Within(0.001f));
    }

    [Test]
    public void Reinforce_CapsAtOne()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.9f };

      // Act
      memory.Reinforce(0.5f);

      // Assert
      Assert.That(memory.Strength, Is.EqualTo(1.0f).Within(0.001f));
    }

    [Test]
    public void Reinforce_CallsMarkAccessed()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.5f };
      var originalAccessTime = memory.LastAccessedAt;
      
      // Small delay to ensure time difference
      System.Threading.Thread.Sleep(10);

      // Act
      memory.Reinforce(0.1f);

      // Assert
      Assert.That(memory.LastAccessedAt, Is.GreaterThan(originalAccessTime));
    }

    [Test]
    public void Reinforce_FromZero_IncreasesStrength()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.0f };

      // Act
      memory.Reinforce(0.3f);

      // Assert
      Assert.That(memory.Strength, Is.EqualTo(0.3f).Within(0.001f));
    }

    [Test]
    public void Reinforce_MultipleTimes_Accumulates()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.2f };

      // Act
      memory.Reinforce(0.2f);
      memory.Reinforce(0.2f);
      memory.Reinforce(0.2f);

      // Assert
      Assert.That(memory.Strength, Is.EqualTo(0.8f).Within(0.001f));
    }

    #endregion

    #region FromDialogue Tests

    [Test]
    public void FromDialogue_CreatesDialogueEntry()
    {
      // Act
      var memory = EpisodicMemoryEntry.FromDialogue("Player", "Hello there!", 0.7f);

      // Assert
      Assert.That(memory.EpisodeType, Is.EqualTo(EpisodeType.Dialogue));
      Assert.That(memory.Participant, Is.EqualTo("Player"));
      Assert.That(memory.Significance, Is.EqualTo(0.7f));
      Assert.That(memory.Description, Is.EqualTo("Player: Hello there!"));
      Assert.That(memory.Content, Contains.Substring("Player: Hello there!"));
    }

    [Test]
    public void FromDialogue_WithDefaultSignificance_UsesPointFive()
    {
      // Act
      var memory = EpisodicMemoryEntry.FromDialogue("Player", "Test");

      // Assert
      Assert.That(memory.Significance, Is.EqualTo(0.5f));
    }

    #endregion

    #region FromObservation Tests

    [Test]
    public void FromObservation_CreatesObservationEntry()
    {
      // Act
      var memory = EpisodicMemoryEntry.FromObservation("Player opened the door", 0.4f);

      // Assert
      Assert.That(memory.EpisodeType, Is.EqualTo(EpisodeType.Observation));
      Assert.That(memory.Description, Is.EqualTo("Player opened the door"));
      Assert.That(memory.Significance, Is.EqualTo(0.4f));
      Assert.That(memory.Participant, Is.Null);
    }

    [Test]
    public void FromObservation_WithDefaultSignificance_UsesPointThree()
    {
      // Act
      var memory = EpisodicMemoryEntry.FromObservation("Player moved");

      // Assert
      Assert.That(memory.Significance, Is.EqualTo(0.3f));
    }

    #endregion

    #region FromLearnedInfo Tests

    [Test]
    public void FromLearnedInfo_CreatesLearnedInfoEntry()
    {
      // Act
      var memory = EpisodicMemoryEntry.FromLearnedInfo("The king's name is Arthur", "Player", 0.8f);

      // Assert
      Assert.That(memory.EpisodeType, Is.EqualTo(EpisodeType.LearnedInfo));
      Assert.That(memory.Description, Is.EqualTo("The king's name is Arthur"));
      Assert.That(memory.Participant, Is.EqualTo("Player"));
      Assert.That(memory.Significance, Is.EqualTo(0.8f));
    }

    [Test]
    public void FromLearnedInfo_WithoutSource_SetsParticipantToNull()
    {
      // Act
      var memory = EpisodicMemoryEntry.FromLearnedInfo("Some information");

      // Assert
      Assert.That(memory.EpisodeType, Is.EqualTo(EpisodeType.LearnedInfo));
      Assert.That(memory.Participant, Is.Null);
      Assert.That(memory.Description, Is.EqualTo("Some information"));
    }

    [Test]
    public void FromLearnedInfo_WithDefaultSignificance_UsesPointSix()
    {
      // Act
      var memory = EpisodicMemoryEntry.FromLearnedInfo("Test info");

      // Assert
      Assert.That(memory.Significance, Is.EqualTo(0.6f));
    }

    [Test]
    public void FromLearnedInfo_WithSource_SetsParticipant()
    {
      // Act
      var memory = EpisodicMemoryEntry.FromLearnedInfo("Info", "Guard Captain");

      // Assert
      Assert.That(memory.Participant, Is.EqualTo("Guard Captain"));
    }

    #endregion

    #region EpisodeType Tests

    [Test]
    public void EpisodeType_AllTypes_CanBeSet()
    {
      // Test all episode types
      var dialogue = new EpisodicMemoryEntry("Test", EpisodeType.Dialogue);
      var observation = new EpisodicMemoryEntry("Test", EpisodeType.Observation);
      var thought = new EpisodicMemoryEntry("Test", EpisodeType.Thought);
      var evt = new EpisodicMemoryEntry("Test", EpisodeType.Event);
      var learnedInfo = new EpisodicMemoryEntry("Test", EpisodeType.LearnedInfo);

      // Assert
      Assert.That(dialogue.EpisodeType, Is.EqualTo(EpisodeType.Dialogue));
      Assert.That(observation.EpisodeType, Is.EqualTo(EpisodeType.Observation));
      Assert.That(thought.EpisodeType, Is.EqualTo(EpisodeType.Thought));
      Assert.That(evt.EpisodeType, Is.EqualTo(EpisodeType.Event));
      Assert.That(learnedInfo.EpisodeType, Is.EqualTo(EpisodeType.LearnedInfo));
    }

    #endregion

    #region Integration Tests

    [Test]
    public void Memory_Lifecycle_DecayAndReinforce()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Important conversation") { Strength = 1.0f, Significance = 0.7f };

      // Act - Apply decay multiple times
      memory.ApplyDecay(0.1f);
      memory.ApplyDecay(0.1f);
      memory.ApplyDecay(0.1f);

      // Assert - Should still be active due to high significance
      Assert.That(memory.IsActive, Is.True);
      Assert.That(memory.Strength, Is.GreaterThan(0.1f));

      // Act - Reinforce
      memory.Reinforce(0.2f);

      // Assert - Strength increased
      Assert.That(memory.Strength, Is.GreaterThan(0.5f));
      Assert.That(memory.IsActive, Is.True);
    }

    [Test]
    public void Memory_WithLowSignificance_DecaysFaster()
    {
      // Arrange
      var lowSig = new EpisodicMemoryEntry("Low") { Strength = 1.0f, Significance = 0.0f };
      var highSig = new EpisodicMemoryEntry("High") { Strength = 1.0f, Significance = 1.0f };

      // Act - Apply same decay multiple times
      for (int i = 0; i < 5; i++)
      {
        lowSig.ApplyDecay(0.1f);
        highSig.ApplyDecay(0.1f);
      }

      // Assert
      Assert.That(highSig.Strength, Is.GreaterThan(lowSig.Strength));
      Assert.That(lowSig.Strength, Is.LessThan(highSig.Strength));
    }

    #endregion
  }
}

