using System;
using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;
using LlamaBrain.Core.Validation;

namespace LlamaBrain.Tests.Memory
{
  [TestFixture]
  public class MemoryMutationControllerTests
  {
    private MemoryMutationController? _controller;
    private AuthoritativeMemorySystem? _memorySystem;

    [SetUp]
    public void SetUp()
    {
      _controller = new MemoryMutationController();
      _memorySystem = new AuthoritativeMemorySystem();
    }

    #region Constructor and Configuration Tests

    [Test]
    public void Constructor_Default_CreatesController()
    {
      var controller = new MemoryMutationController();
      Assert.That(controller, Is.Not.Null);
      Assert.That(controller.Statistics, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithConfig_UsesConfig()
    {
      var config = new MutationControllerConfig
      {
        DefaultEpisodicSignificance = 0.8f,
        EnableLogging = false
      };

      var controller = new MemoryMutationController(config);
      Assert.That(controller, Is.Not.Null);
    }

    [Test]
    public void Statistics_InitialState_AllZeros()
    {
      var stats = _controller!.Statistics;

      Assert.That(stats.TotalAttempted, Is.EqualTo(0));
      Assert.That(stats.TotalSucceeded, Is.EqualTo(0));
      Assert.That(stats.TotalFailed, Is.EqualTo(0));
      Assert.That(stats.EpisodicAppended, Is.EqualTo(0));
      Assert.That(stats.BeliefsTransformed, Is.EqualTo(0));
      Assert.That(stats.RelationshipsTransformed, Is.EqualTo(0));
      Assert.That(stats.IntentsEmitted, Is.EqualTo(0));
    }

    #endregion

    #region ExecuteMutations - Gate Result Tests

    [Test]
    public void ExecuteMutations_NullGateResult_ThrowsArgumentNullException()
    {
      Assert.Throws<ArgumentNullException>(() =>
        _controller!.ExecuteMutations(null!, _memorySystem!));
    }

    [Test]
    public void ExecuteMutations_NullMemorySystem_ThrowsArgumentNullException()
    {
      var gateResult = GateResult.Pass(ParsedOutput.Dialogue("Hello", "Hello"));
      Assert.Throws<ArgumentNullException>(() =>
        _controller!.ExecuteMutations(gateResult, null!));
    }

    [Test]
    public void ExecuteMutations_FailedGateResult_ReturnsEmptyBatch()
    {
      var gateResult = GateResult.Fail(ValidationFailure.ProhibitionViolated("Test"));

      var result = _controller!.ExecuteMutations(gateResult, _memorySystem!);

      Assert.That(result.TotalAttempted, Is.EqualTo(0));
      Assert.That(result.AllSucceeded, Is.True);
    }

    [Test]
    public void ExecuteMutations_PassedGateResultNoMutations_ReturnsEmptyBatch()
    {
      var output = ParsedOutput.Dialogue("Hello", "Hello");
      var gateResult = GateResult.Pass(output);

      var result = _controller!.ExecuteMutations(gateResult, _memorySystem!);

      Assert.That(result.TotalAttempted, Is.EqualTo(0));
      Assert.That(result.EmittedIntents.Count, Is.EqualTo(0));
    }

    #endregion

    #region AppendEpisodic Tests

    [Test]
    public void ExecuteSingleMutation_AppendEpisodic_Success()
    {
      var mutation = ProposedMutation.AppendEpisodic("Player said hello");

      var result = _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(result.Success, Is.True);
      Assert.That(result.Mutation, Is.EqualTo(mutation));
      Assert.That(result.AffectedEntry, Is.Not.Null);
      Assert.That(_memorySystem!.GetRecentMemories(10).GetEnumerator().MoveNext(), Is.True);
    }

    [Test]
    public void ExecuteSingleMutation_AppendEpisodic_EmptyContent_Fails()
    {
      var mutation = new ProposedMutation
      {
        Type = MutationType.AppendEpisodic,
        Content = ""
      };

      var result = _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("empty"));
    }

    [Test]
    public void ExecuteSingleMutation_AppendEpisodic_WithSourceText_IncreasesSignificance()
    {
      var mutation = ProposedMutation.AppendEpisodic("Important info", "Player revealed secret");

      var result = _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(result.Success, Is.True);
      var entry = result.AffectedEntry as EpisodicMemoryEntry;
      Assert.That(entry, Is.Not.Null);
      Assert.That(entry!.Significance, Is.GreaterThan(0.5f));
    }

    [Test]
    public void ExecuteSingleMutation_AppendEpisodic_UpdatesStatistics()
    {
      var mutation = ProposedMutation.AppendEpisodic("Test memory");

      _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(_controller!.Statistics.EpisodicAppended, Is.EqualTo(1));
      Assert.That(_controller!.Statistics.TotalSucceeded, Is.EqualTo(1));
    }

    #endregion

    #region TransformBelief Tests

    [Test]
    public void ExecuteSingleMutation_TransformBelief_Success()
    {
      var mutation = ProposedMutation.TransformBelief("player_friendly", "Player is friendly", 0.8f);

      var result = _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(result.Success, Is.True);
      Assert.That(result.AffectedEntry, Is.Not.Null);

      var belief = _memorySystem!.GetBelief("player_friendly");
      Assert.That(belief, Is.Not.Null);
      Assert.That(belief!.BeliefContent, Is.EqualTo("Player is friendly"));
      Assert.That(belief.Confidence, Is.EqualTo(0.8f));
    }

    [Test]
    public void ExecuteSingleMutation_TransformBelief_NoTarget_Fails()
    {
      var mutation = new ProposedMutation
      {
        Type = MutationType.TransformBelief,
        Target = null,
        Content = "Some belief"
      };

      var result = _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("target"));
    }

    [Test]
    public void ExecuteSingleMutation_TransformBelief_EmptyContent_Fails()
    {
      var mutation = ProposedMutation.TransformBelief("test_belief", "");

      var result = _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("empty"));
    }

    [Test]
    public void ExecuteSingleMutation_TransformBelief_CanonicalFact_Fails()
    {
      _memorySystem!.AddCanonicalFact("king_name", "The king's name is Arthur");

      var mutation = ProposedMutation.TransformBelief("king_name", "The king's name is Bob");

      var result = _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("canonical"));
      Assert.That(_controller!.Statistics.CanonicalMutationAttempts, Is.EqualTo(1));
    }

    [Test]
    public void ExecuteSingleMutation_TransformBelief_UpdatesStatistics()
    {
      var mutation = ProposedMutation.TransformBelief("test_belief", "Test content");

      _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(_controller!.Statistics.BeliefsTransformed, Is.EqualTo(1));
    }

    #endregion

    #region TransformRelationship Tests

    [Test]
    public void ExecuteSingleMutation_TransformRelationship_Success()
    {
      var mutation = ProposedMutation.TransformRelationship("player", "We are friends now");

      var result = _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(result.Success, Is.True);

      var belief = _memorySystem!.GetBelief("relationship_player");
      Assert.That(belief, Is.Not.Null);
      Assert.That(belief!.BeliefContent, Is.EqualTo("We are friends now"));
      Assert.That(belief.BeliefType, Is.EqualTo(BeliefType.Relationship));
    }

    [Test]
    public void ExecuteSingleMutation_TransformRelationship_NoTarget_Fails()
    {
      var mutation = new ProposedMutation
      {
        Type = MutationType.TransformRelationship,
        Target = null,
        Content = "Some relationship"
      };

      var result = _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("target"));
    }

    [Test]
    public void ExecuteSingleMutation_TransformRelationship_UpdatesStatistics()
    {
      var mutation = ProposedMutation.TransformRelationship("player", "Neutral relationship");

      _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(_controller!.Statistics.RelationshipsTransformed, Is.EqualTo(1));
    }

    #endregion

    #region EmitWorldIntent Tests

    [Test]
    public void ExecuteSingleMutation_EmitWorldIntent_Success()
    {
      var mutation = ProposedMutation.EmitWorldIntent("follow_player", "Starting to follow");
      WorldIntent? receivedIntent = null;
      _controller!.OnWorldIntentEmitted += (sender, e) => receivedIntent = e.Intent;

      var result = _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(result.Success, Is.True);
      Assert.That(receivedIntent, Is.Not.Null);
      Assert.That(receivedIntent!.IntentType, Is.EqualTo("follow_player"));
    }

    [Test]
    public void ExecuteSingleMutation_EmitWorldIntent_NoIntentType_Fails()
    {
      var mutation = new ProposedMutation
      {
        Type = MutationType.EmitWorldIntent,
        Target = null,
        Content = "Some content"
      };

      var result = _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("intent type"));
    }

    [Test]
    public void ExecuteSingleMutation_EmitWorldIntent_IncludesContentAsParameter()
    {
      var mutation = ProposedMutation.EmitWorldIntent("give_item", "gold_coin");
      WorldIntent? receivedIntent = null;
      _controller!.OnWorldIntentEmitted += (sender, e) => receivedIntent = e.Intent;

      _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(receivedIntent!.Parameters.ContainsKey("content"), Is.True);
      Assert.That(receivedIntent.Parameters["content"], Is.EqualTo("gold_coin"));
    }

    [Test]
    public void ExecuteSingleMutation_EmitWorldIntent_IncludesNpcId()
    {
      var mutation = ProposedMutation.EmitWorldIntent("follow_player", "");
      string? receivedNpcId = null;
      _controller!.OnWorldIntentEmitted += (sender, e) => receivedNpcId = e.NpcId;

      _controller!.ExecuteSingleMutation(mutation, _memorySystem!, "test_npc");

      Assert.That(receivedNpcId, Is.EqualTo("test_npc"));
    }

    #endregion

    #region Batch Execution Tests

    [Test]
    public void ExecuteMutations_MultipleMutations_AllExecuted()
    {
      var output = ParsedOutput.Dialogue("Hello", "Hello")
        .WithMutation(ProposedMutation.AppendEpisodic("Memory 1"))
        .WithMutation(ProposedMutation.TransformBelief("belief1", "Belief content"))
        .WithMutation(ProposedMutation.TransformRelationship("player", "Friend"));

      var gateResult = GateResult.Pass(output);

      var result = _controller!.ExecuteMutations(gateResult, _memorySystem!);

      Assert.That(result.TotalAttempted, Is.EqualTo(3));
      Assert.That(result.SuccessCount, Is.EqualTo(3));
      Assert.That(result.AllSucceeded, Is.True);
    }

    [Test]
    public void ExecuteMutations_WithIntents_IntentsEmitted()
    {
      var output = ParsedOutput.Dialogue("Hello", "Hello")
        .WithIntent(WorldIntent.Create("follow_player", "player"))
        .WithIntent(WorldIntent.Create("give_item", "sword"));

      var gateResult = GateResult.Pass(output);
      var emittedIntents = new List<WorldIntent>();
      _controller!.OnWorldIntentEmitted += (sender, e) => emittedIntents.Add(e.Intent);

      var result = _controller!.ExecuteMutations(gateResult, _memorySystem!);

      Assert.That(result.EmittedIntents.Count, Is.EqualTo(2));
      Assert.That(emittedIntents.Count, Is.EqualTo(2));
      Assert.That(_controller.Statistics.IntentsEmitted, Is.EqualTo(2));
    }

    [Test]
    public void ExecuteMutations_MixedSuccessAndFailure_ReportsCorrectCounts()
    {
      var output = ParsedOutput.Dialogue("Hello", "Hello")
        .WithMutation(ProposedMutation.AppendEpisodic("Valid memory"))
        .WithMutation(ProposedMutation.TransformBelief("", "Invalid - no target"));

      var gateResult = GateResult.Pass(output);

      var result = _controller!.ExecuteMutations(gateResult, _memorySystem!);

      Assert.That(result.TotalAttempted, Is.EqualTo(2));
      Assert.That(result.SuccessCount, Is.EqualTo(1));
      Assert.That(result.FailureCount, Is.EqualTo(1));
      Assert.That(result.AllSucceeded, Is.False);
    }

    #endregion

    #region ValidateMutation Tests

    [Test]
    public void ValidateMutation_ValidEpisodic_ReturnsNull()
    {
      var mutation = ProposedMutation.AppendEpisodic("Valid content");

      var error = _controller!.ValidateMutation(mutation, _memorySystem!);

      Assert.That(error, Is.Null);
    }

    [Test]
    public void ValidateMutation_EmptyEpisodic_ReturnsError()
    {
      var mutation = ProposedMutation.AppendEpisodic("");

      var error = _controller!.ValidateMutation(mutation, _memorySystem!);

      Assert.That(error, Is.Not.Null);
      Assert.That(error, Does.Contain("empty"));
    }

    [Test]
    public void ValidateMutation_CanonicalFact_ReturnsError()
    {
      _memorySystem!.AddCanonicalFact("protected_fact", "This cannot be modified");

      var mutation = ProposedMutation.TransformBelief("protected_fact", "New value");

      var error = _controller!.ValidateMutation(mutation, _memorySystem!);

      Assert.That(error, Is.Not.Null);
      Assert.That(error, Does.Contain("canonical"));
    }

    [Test]
    public void ValidateMutation_NullMutation_ReturnsError()
    {
      var error = _controller!.ValidateMutation(null!, _memorySystem!);

      Assert.That(error, Is.Not.Null);
      Assert.That(error, Does.Contain("null"));
    }

    #endregion

    #region Statistics and Reset Tests

    [Test]
    public void ResetStatistics_ClearsAllCounters()
    {
      // Execute some mutations to populate statistics
      _controller!.ExecuteSingleMutation(ProposedMutation.AppendEpisodic("Test"), _memorySystem!);
      _controller!.ExecuteSingleMutation(ProposedMutation.TransformBelief("b1", "test"), _memorySystem!);

      _controller!.ResetStatistics();

      Assert.That(_controller.Statistics.TotalAttempted, Is.EqualTo(0));
      Assert.That(_controller.Statistics.TotalSucceeded, Is.EqualTo(0));
      Assert.That(_controller.Statistics.EpisodicAppended, Is.EqualTo(0));
      Assert.That(_controller.Statistics.BeliefsTransformed, Is.EqualTo(0));
    }

    [Test]
    public void Statistics_SuccessRate_CalculatesCorrectly()
    {
      _controller!.ExecuteSingleMutation(ProposedMutation.AppendEpisodic("Test1"), _memorySystem!);
      _controller!.ExecuteSingleMutation(ProposedMutation.AppendEpisodic("Test2"), _memorySystem!);
      _controller!.ExecuteSingleMutation(ProposedMutation.TransformBelief("", "Invalid"), _memorySystem!);

      var stats = _controller.Statistics;

      Assert.That(stats.TotalAttempted, Is.EqualTo(3));
      Assert.That(stats.TotalSucceeded, Is.EqualTo(2));
      Assert.That(stats.SuccessRate, Is.EqualTo(200f / 3f).Within(0.01f));
    }

    #endregion

    #region Logging Tests

    [Test]
    public void OnLog_WhenSet_ReceivesLogMessages()
    {
      var logs = new List<string>();
      _controller!.OnLog = msg => logs.Add(msg);

      _controller!.ExecuteSingleMutation(ProposedMutation.AppendEpisodic("Test"), _memorySystem!);

      Assert.That(logs.Count, Is.GreaterThan(0));
      Assert.That(logs.Exists(l => l.Contains("MutationController")), Is.True);
    }

    [Test]
    public void OnLog_WhenDisabledInConfig_NoLogs()
    {
      var config = new MutationControllerConfig { EnableLogging = false };
      var controller = new MemoryMutationController(config);
      var logs = new List<string>();
      controller.OnLog = msg => logs.Add(msg);

      controller.ExecuteSingleMutation(ProposedMutation.AppendEpisodic("Test"), _memorySystem!);

      Assert.That(logs.Count, Is.EqualTo(0));
    }

    #endregion

    #region Exception Handling Tests

    [Test]
    public void ExecuteSingleMutation_UnknownType_ReturnsError()
    {
      var mutation = new ProposedMutation
      {
        Type = (MutationType)999, // Invalid type
        Content = "Test"
      };

      var result = _controller!.ExecuteSingleMutation(mutation, _memorySystem!);

      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("Unknown"));
    }

    #endregion

    #region MutationExecutionResult Tests

    [Test]
    public void MutationExecutionResult_Succeeded_HasCorrectProperties()
    {
      var mutation = ProposedMutation.AppendEpisodic("Test");
      var result = MutationExecutionResult.Succeeded(mutation);

      Assert.That(result.Success, Is.True);
      Assert.That(result.Mutation, Is.EqualTo(mutation));
      Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void MutationExecutionResult_Failed_HasCorrectProperties()
    {
      var mutation = ProposedMutation.AppendEpisodic("Test");
      var result = MutationExecutionResult.Failed(mutation, "Test error");

      Assert.That(result.Success, Is.False);
      Assert.That(result.Mutation, Is.EqualTo(mutation));
      Assert.That(result.ErrorMessage, Is.EqualTo("Test error"));
    }

    [Test]
    public void MutationExecutionResult_ToString_IncludesStatus()
    {
      var successResult = MutationExecutionResult.Succeeded(ProposedMutation.AppendEpisodic("Test"));
      var failResult = MutationExecutionResult.Failed(ProposedMutation.AppendEpisodic("Test"), "Error");

      Assert.That(successResult.ToString(), Does.Contain("OK"));
      Assert.That(failResult.ToString(), Does.Contain("FAILED"));
    }

    #endregion

    #region MutationBatchResult Tests

    [Test]
    public void MutationBatchResult_Properties_CalculateCorrectly()
    {
      var batch = new MutationBatchResult();
      batch.Results.Add(MutationExecutionResult.Succeeded(ProposedMutation.AppendEpisodic("1")));
      batch.Results.Add(MutationExecutionResult.Succeeded(ProposedMutation.AppendEpisodic("2")));
      batch.Results.Add(MutationExecutionResult.Failed(ProposedMutation.AppendEpisodic("3"), "Error"));

      Assert.That(batch.TotalAttempted, Is.EqualTo(3));
      Assert.That(batch.SuccessCount, Is.EqualTo(2));
      Assert.That(batch.FailureCount, Is.EqualTo(1));
      Assert.That(batch.AllSucceeded, Is.False);
    }

    [Test]
    public void MutationBatchResult_Failures_ReturnsOnlyFailedResults()
    {
      var batch = new MutationBatchResult();
      batch.Results.Add(MutationExecutionResult.Succeeded(ProposedMutation.AppendEpisodic("1")));
      batch.Results.Add(MutationExecutionResult.Failed(ProposedMutation.AppendEpisodic("2"), "Error"));

      var failures = new List<MutationExecutionResult>(batch.Failures);

      Assert.That(failures.Count, Is.EqualTo(1));
      Assert.That(failures[0].Success, Is.False);
    }

    #endregion

    #region MutationStatistics Tests

    [Test]
    public void MutationStatistics_ToString_IncludesAllMetrics()
    {
      _controller!.ExecuteSingleMutation(ProposedMutation.AppendEpisodic("Test"), _memorySystem!);

      var str = _controller.Statistics.ToString();

      Assert.That(str, Does.Contain("Mutations"));
      Assert.That(str, Does.Contain("Episodic"));
      Assert.That(str, Does.Contain("Beliefs"));
    }

    #endregion
  }
}
