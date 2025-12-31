using NUnit.Framework;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Expectancy
{
  /// <summary>
  /// Tests for the InteractionContext class.
  /// </summary>
  public class InteractionContextTests
  {
    #region Constructor Tests

    [Test]
    public void Constructor_DefaultValues_AreCorrect()
    {
      // Act
      var context = new InteractionContext();

      // Assert
      Assert.That(context.TriggerReason, Is.EqualTo(TriggerReason.PlayerUtterance)); // Default
      Assert.That(context.NpcId, Is.Null);
      Assert.That(context.TriggerId, Is.Null);
      Assert.That(context.PlayerInput, Is.Null);
      Assert.That(context.TriggerPrompt, Is.Null);
      Assert.That(context.GameTime, Is.EqualTo(0f));
      Assert.That(context.SceneName, Is.Null);
      Assert.That(context.InteractionCount, Is.EqualTo(0));
      Assert.That(context.Tags, Is.Null);
    }

    #endregion

    #region Factory Method Tests

    [Test]
    public void FromZoneTrigger_SetsCorrectValues()
    {
      // Act
      var context = InteractionContext.FromZoneTrigger(
        npcId: "guard-npc",
        triggerId: "entrance-zone",
        triggerPrompt: "Who goes there?",
        gameTime: 123.5f
      );

      // Assert
      Assert.That(context.TriggerReason, Is.EqualTo(TriggerReason.ZoneTrigger));
      Assert.That(context.NpcId, Is.EqualTo("guard-npc"));
      Assert.That(context.TriggerId, Is.EqualTo("entrance-zone"));
      Assert.That(context.TriggerPrompt, Is.EqualTo("Who goes there?"));
      Assert.That(context.GameTime, Is.EqualTo(123.5f));
    }

    [Test]
    public void FromZoneTrigger_DefaultGameTime_IsZero()
    {
      // Act
      var context = InteractionContext.FromZoneTrigger("npc", "trigger", "prompt");

      // Assert
      Assert.That(context.GameTime, Is.EqualTo(0f));
    }

    [Test]
    public void FromPlayerUtterance_SetsCorrectValues()
    {
      // Act
      var context = InteractionContext.FromPlayerUtterance(
        npcId: "merchant-npc",
        playerInput: "What do you have for sale?",
        gameTime: 456.7f
      );

      // Assert
      Assert.That(context.TriggerReason, Is.EqualTo(TriggerReason.PlayerUtterance));
      Assert.That(context.NpcId, Is.EqualTo("merchant-npc"));
      Assert.That(context.PlayerInput, Is.EqualTo("What do you have for sale?"));
      Assert.That(context.GameTime, Is.EqualTo(456.7f));
    }

    [Test]
    public void FromPlayerUtterance_DefaultGameTime_IsZero()
    {
      // Act
      var context = InteractionContext.FromPlayerUtterance("npc", "hello");

      // Assert
      Assert.That(context.GameTime, Is.EqualTo(0f));
    }

    #endregion

    #region Property Tests

    [Test]
    public void Properties_CanBeSetAndRead()
    {
      // Arrange
      var context = new InteractionContext();

      // Act
      context.TriggerReason = TriggerReason.QuestTrigger;
      context.NpcId = "quest-npc";
      context.TriggerId = "quest-zone";
      context.PlayerInput = "I'll help you";
      context.TriggerPrompt = "Please help me!";
      context.GameTime = 789.0f;
      context.SceneName = "Village";
      context.InteractionCount = 5;
      context.Tags = new[] { "quest-giver", "friendly" };

      // Assert
      Assert.That(context.TriggerReason, Is.EqualTo(TriggerReason.QuestTrigger));
      Assert.That(context.NpcId, Is.EqualTo("quest-npc"));
      Assert.That(context.TriggerId, Is.EqualTo("quest-zone"));
      Assert.That(context.PlayerInput, Is.EqualTo("I'll help you"));
      Assert.That(context.TriggerPrompt, Is.EqualTo("Please help me!"));
      Assert.That(context.GameTime, Is.EqualTo(789.0f));
      Assert.That(context.SceneName, Is.EqualTo("Village"));
      Assert.That(context.InteractionCount, Is.EqualTo(5));
      Assert.That(context.Tags, Is.EqualTo(new[] { "quest-giver", "friendly" }));
    }

    #endregion

    #region ToString Tests

    [Test]
    public void ToString_ReturnsFormattedString()
    {
      // Arrange
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.ZoneTrigger,
        NpcId = "test-npc",
        TriggerId = "test-trigger"
      };

      // Act
      var result = context.ToString();

      // Assert
      Assert.That(result, Contains.Substring("ZoneTrigger"));
      Assert.That(result, Contains.Substring("test-npc"));
      Assert.That(result, Contains.Substring("test-trigger"));
    }

    [Test]
    public void ToString_WithNullValues_DoesNotThrow()
    {
      // Arrange
      var context = new InteractionContext();

      // Act & Assert
      Assert.DoesNotThrow(() => context.ToString());
    }

    #endregion

    #region TriggerReason Enum Tests

    [Test]
    public void TriggerReason_AllValuesAreDefined()
    {
      // Assert
      Assert.That(System.Enum.IsDefined(typeof(TriggerReason), TriggerReason.PlayerUtterance), Is.True);
      Assert.That(System.Enum.IsDefined(typeof(TriggerReason), TriggerReason.ZoneTrigger), Is.True);
      Assert.That(System.Enum.IsDefined(typeof(TriggerReason), TriggerReason.TimeTrigger), Is.True);
      Assert.That(System.Enum.IsDefined(typeof(TriggerReason), TriggerReason.QuestTrigger), Is.True);
      Assert.That(System.Enum.IsDefined(typeof(TriggerReason), TriggerReason.NpcInteraction), Is.True);
      Assert.That(System.Enum.IsDefined(typeof(TriggerReason), TriggerReason.WorldEvent), Is.True);
      Assert.That(System.Enum.IsDefined(typeof(TriggerReason), TriggerReason.Custom), Is.True);
    }

    #endregion

    #region Serialization Tests

    [Test]
    public void Context_IsSerializable()
    {
      // Arrange
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.ZoneTrigger,
        NpcId = "test-npc",
        TriggerId = "test-trigger",
        TriggerPrompt = "Hello!",
        GameTime = 100f,
        SceneName = "TestScene",
        InteractionCount = 3,
        Tags = new[] { "tag1", "tag2" }
      };

      // Act - Serialize to JSON and back
      var json = Newtonsoft.Json.JsonConvert.SerializeObject(context);
      var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<InteractionContext>(json);

      // Assert
      Assert.That(deserialized, Is.Not.Null);
      Assert.That(deserialized!.TriggerReason, Is.EqualTo(context.TriggerReason));
      Assert.That(deserialized.NpcId, Is.EqualTo(context.NpcId));
      Assert.That(deserialized.TriggerId, Is.EqualTo(context.TriggerId));
      Assert.That(deserialized.TriggerPrompt, Is.EqualTo(context.TriggerPrompt));
      Assert.That(deserialized.GameTime, Is.EqualTo(context.GameTime));
      Assert.That(deserialized.SceneName, Is.EqualTo(context.SceneName));
      Assert.That(deserialized.InteractionCount, Is.EqualTo(context.InteractionCount));
      Assert.That(deserialized.Tags, Is.EqualTo(context.Tags));
    }

    #endregion
  }
}
