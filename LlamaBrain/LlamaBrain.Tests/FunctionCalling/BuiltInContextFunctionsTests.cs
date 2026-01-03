using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.FunctionCalling;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.FunctionCalling
{
    /// <summary>
    /// Tests for BuiltInContextFunctions - pre-registered context access functions.
    /// </summary>
    public class BuiltInContextFunctionsTests
    {
        private FunctionCallDispatcher _dispatcher = null!;

        [SetUp]
        public void SetUp()
        {
            _dispatcher = new FunctionCallDispatcher();
        }

        #region RegisterAll Tests

        [Test]
        public void RegisterAll_RegistersAllExpectedFunctions()
        {
            // Arrange
            var snapshot = CreateMinimalSnapshot();

            // Act
            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Assert
            var functionNames = _dispatcher.GetRegisteredFunctionNames();
            Assert.That(functionNames, Does.Contain("get_memories"));
            Assert.That(functionNames, Does.Contain("get_beliefs"));
            Assert.That(functionNames, Does.Contain("get_constraints"));
            Assert.That(functionNames, Does.Contain("get_dialogue_history"));
            Assert.That(functionNames, Does.Contain("get_world_state"));
            Assert.That(functionNames, Does.Contain("get_canonical_facts"));
        }

        [Test]
        public void RegisterAll_AllFunctionsHaveDescriptions()
        {
            // Arrange
            var snapshot = CreateMinimalSnapshot();

            // Act
            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Assert
            var metadata = _dispatcher.GetAllFunctionMetadata();
            foreach (var m in metadata)
            {
                Assert.That(m.Description, Is.Not.Empty, $"Function {m.FunctionName} should have a description");
            }
        }

        [Test]
        public void RegisterAll_AllFunctionsHaveParameterSchemas()
        {
            // Arrange
            var snapshot = CreateMinimalSnapshot();

            // Act
            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Assert
            var metadata = _dispatcher.GetAllFunctionMetadata();
            foreach (var m in metadata)
            {
                Assert.That(m.ParameterSchema, Is.Not.Null.And.Not.Empty, $"Function {m.FunctionName} should have a parameter schema");
            }
        }

        #endregion

        #region get_memories Tests

        [Test]
        public void GetMemories_ReturnsEpisodicMemories()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithEpisodicMemories(new[] { "Memory 1", "Memory 2", "Memory 3" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_memories"));

            // Assert
            Assert.That(result.Success, Is.True);
            var memories = result.Result as IEnumerable<object>;
            Assert.That(memories, Is.Not.Null);
            Assert.That(memories!.Count(), Is.EqualTo(3));
        }

        [Test]
        public void GetMemories_WithLimit_RespectsLimit()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithEpisodicMemories(new[] { "Memory 1", "Memory 2", "Memory 3", "Memory 4", "Memory 5" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_memories", new Dictionary<string, object>
            {
                { "limit", 2 }
            }));

            // Assert
            Assert.That(result.Success, Is.True);
            var memories = result.Result as IEnumerable<object>;
            Assert.That(memories!.Count(), Is.EqualTo(2));
        }

        [Test]
        public void GetMemories_EmptyMemories_ReturnsEmptyList()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_memories"));

            // Assert
            Assert.That(result.Success, Is.True);
            var memories = result.Result as IEnumerable<object>;
            Assert.That(memories, Is.Not.Null);
            Assert.That(memories!.Count(), Is.EqualTo(0));
        }

        [Test]
        public void GetMemories_DefaultLimit_Is10()
        {
            // Arrange
            var manyMemories = Enumerable.Range(1, 20).Select(i => $"Memory {i}").ToArray();
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithEpisodicMemories(manyMemories)
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_memories"));

            // Assert
            var memories = result.Result as IEnumerable<object>;
            Assert.That(memories!.Count(), Is.EqualTo(10));
        }

        [Test]
        public void GetMemories_PreservesCallId()
        {
            // Arrange
            var snapshot = CreateMinimalSnapshot();
            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_memories", null, "test_call_id"));

            // Assert
            Assert.That(result.CallId, Is.EqualTo("test_call_id"));
        }

        [Test]
        public void GetMemories_ReturnsExpectedStructure()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithEpisodicMemories(new[] { "Test memory content" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_memories"));

            // Assert
            var memories = result.Result as IEnumerable<object>;
            var firstMemory = memories!.First() as Dictionary<string, object>;
            Assert.That(firstMemory, Is.Not.Null);
            Assert.That(firstMemory!.ContainsKey("content"), Is.True);
            Assert.That(firstMemory.ContainsKey("recency"), Is.True);
            Assert.That(firstMemory.ContainsKey("importance"), Is.True);
            Assert.That(firstMemory["content"], Is.EqualTo("Test memory content"));
        }

        #endregion

        #region get_beliefs Tests

        [Test]
        public void GetBeliefs_ReturnsBeliefs()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithBeliefs(new[] { "I trust the player", "The world is dangerous" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_beliefs"));

            // Assert
            Assert.That(result.Success, Is.True);
            var beliefs = result.Result as IEnumerable<object>;
            Assert.That(beliefs!.Count(), Is.EqualTo(2));
        }

        [Test]
        public void GetBeliefs_WithLimit_RespectsLimit()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithBeliefs(new[] { "Belief 1", "Belief 2", "Belief 3", "Belief 4" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_beliefs", new Dictionary<string, object>
            {
                { "limit", 2 }
            }));

            // Assert
            var beliefs = result.Result as IEnumerable<object>;
            Assert.That(beliefs!.Count(), Is.EqualTo(2));
        }

        [Test]
        public void GetBeliefs_EmptyBeliefs_ReturnsEmptyList()
        {
            // Arrange
            var snapshot = CreateMinimalSnapshot();
            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_beliefs"));

            // Assert
            Assert.That(result.Success, Is.True);
            var beliefs = result.Result as IEnumerable<object>;
            Assert.That(beliefs!.Count(), Is.EqualTo(0));
        }

        [Test]
        public void GetBeliefs_ReturnsExpectedStructure()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithBeliefs(new[] { "Test belief content" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_beliefs"));

            // Assert
            var beliefs = result.Result as IEnumerable<object>;
            var firstBelief = beliefs!.First() as Dictionary<string, object>;
            Assert.That(firstBelief, Is.Not.Null);
            Assert.That(firstBelief!.ContainsKey("id"), Is.True);
            Assert.That(firstBelief.ContainsKey("content"), Is.True);
            Assert.That(firstBelief.ContainsKey("confidence"), Is.True);
            Assert.That(firstBelief.ContainsKey("sentiment"), Is.True);
            Assert.That(firstBelief["content"], Is.EqualTo("Test belief content"));
        }

        #endregion

        #region get_constraints Tests

        [Test]
        public void GetConstraints_ReturnsConstraintsByType()
        {
            // Arrange
            var constraints = new ConstraintSet();
            constraints.Add(Constraint.Prohibition("p1", "No secrets", "Do not reveal secrets"));
            constraints.Add(Constraint.Requirement("r1", "Be polite", "Always be polite"));
            constraints.Add(Constraint.Permission("perm1", "Use magic", "May use magic"));

            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithConstraints(constraints)
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_constraints"));

            // Assert
            Assert.That(result.Success, Is.True);
            var constraintResult = result.Result as Dictionary<string, object>;
            Assert.That(constraintResult, Is.Not.Null);
            Assert.That(constraintResult!.ContainsKey("prohibitions"), Is.True);
            Assert.That(constraintResult.ContainsKey("requirements"), Is.True);
            Assert.That(constraintResult.ContainsKey("permissions"), Is.True);
        }

        [Test]
        public void GetConstraints_ExtractsPromptInjections()
        {
            // Arrange
            var constraints = new ConstraintSet();
            constraints.Add(Constraint.Prohibition("p1", "No secrets", "Do not reveal secrets"));

            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithConstraints(constraints)
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_constraints"));

            // Assert
            var constraintResult = result.Result as Dictionary<string, object>;
            var prohibitions = constraintResult!["prohibitions"] as List<string>;
            Assert.That(prohibitions, Does.Contain("Do not reveal secrets"));
        }

        [Test]
        public void GetConstraints_EmptyConstraints_ReturnsEmptyLists()
        {
            // Arrange
            var snapshot = CreateMinimalSnapshot();
            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_constraints"));

            // Assert
            Assert.That(result.Success, Is.True);
            var constraintResult = result.Result as Dictionary<string, object>;
            var prohibitions = constraintResult!["prohibitions"] as List<string>;
            var requirements = constraintResult["requirements"] as List<string>;
            var permissions = constraintResult["permissions"] as List<string>;
            Assert.That(prohibitions, Is.Empty);
            Assert.That(requirements, Is.Empty);
            Assert.That(permissions, Is.Empty);
        }

        [Test]
        public void GetConstraints_FiltersEmptyPromptInjections()
        {
            // Arrange
            var constraints = new ConstraintSet();
            constraints.Add(new Constraint
            {
                Id = "test",
                Type = ConstraintType.Prohibition,
                PromptInjection = ""  // Empty prompt injection
            });
            constraints.Add(new Constraint
            {
                Id = "test2",
                Type = ConstraintType.Prohibition,
                PromptInjection = null  // Null prompt injection
            });
            constraints.Add(new Constraint
            {
                Id = "test3",
                Type = ConstraintType.Prohibition,
                PromptInjection = "Valid prohibition"
            });

            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithConstraints(constraints)
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_constraints"));

            // Assert
            var constraintResult = result.Result as Dictionary<string, object>;
            var prohibitions = constraintResult!["prohibitions"] as List<string>;
            Assert.That(prohibitions!.Count, Is.EqualTo(1));
            Assert.That(prohibitions, Does.Contain("Valid prohibition"));
        }

        #endregion

        #region get_dialogue_history Tests

        [Test]
        public void GetDialogueHistory_ReturnsDialogueEntries()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithDialogueHistory(new[] { "Player: Hello", "NPC: Greetings" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_dialogue_history"));

            // Assert
            Assert.That(result.Success, Is.True);
            var history = result.Result as IEnumerable<object>;
            Assert.That(history!.Count(), Is.EqualTo(2));
        }

        [Test]
        public void GetDialogueHistory_ParsesSpeakerAndText()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithDialogueHistory(new[] { "Player: Hello there!" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_dialogue_history"));

            // Assert
            var history = result.Result as IEnumerable<object>;
            var firstEntry = history!.First() as Dictionary<string, object>;
            Assert.That(firstEntry!["speaker"], Is.EqualTo("Player"));
            Assert.That(firstEntry["text"], Is.EqualTo("Hello there!"));
        }

        [Test]
        public void GetDialogueHistory_WithLimit_TakesLastN()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithDialogueHistory(new[]
                {
                    "Player: First",
                    "NPC: Second",
                    "Player: Third",
                    "NPC: Fourth"
                })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_dialogue_history", new Dictionary<string, object>
            {
                { "limit", 2 }
            }));

            // Assert
            var history = (result.Result as IEnumerable<object>)!.ToList();
            Assert.That(history.Count, Is.EqualTo(2));
            var firstEntry = history[0] as Dictionary<string, object>;
            var secondEntry = history[1] as Dictionary<string, object>;
            // TakeLast returns the last 2 entries
            Assert.That(firstEntry!["speaker"], Is.EqualTo("Player"));
            Assert.That(firstEntry["text"], Is.EqualTo("Third"));
            Assert.That(secondEntry!["speaker"], Is.EqualTo("NPC"));
            Assert.That(secondEntry["text"], Is.EqualTo("Fourth"));
        }

        [Test]
        public void GetDialogueHistory_NoColonInLine_UsesUnknownSpeaker()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithDialogueHistory(new[] { "Some malformed line without speaker" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_dialogue_history"));

            // Assert
            var history = result.Result as IEnumerable<object>;
            var firstEntry = history!.First() as Dictionary<string, object>;
            Assert.That(firstEntry!["speaker"], Is.EqualTo("Unknown"));
            Assert.That(firstEntry["text"], Is.EqualTo("Some malformed line without speaker"));
        }

        [Test]
        public void GetDialogueHistory_EmptyHistory_ReturnsEmptyList()
        {
            // Arrange
            var snapshot = CreateMinimalSnapshot();
            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_dialogue_history"));

            // Assert
            var history = result.Result as IEnumerable<object>;
            Assert.That(history!.Count(), Is.EqualTo(0));
        }

        #endregion

        #region get_world_state Tests

        [Test]
        public void GetWorldState_ReturnsWorldStateEntries()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithWorldState(new[] { "door=open", "chest=locked" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_world_state"));

            // Assert
            Assert.That(result.Success, Is.True);
            var state = result.Result as IEnumerable<object>;
            Assert.That(state!.Count(), Is.EqualTo(2));
        }

        [Test]
        public void GetWorldState_ParsesKeyValuePairs()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithWorldState(new[] { "player_health=100" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_world_state"));

            // Assert
            var state = result.Result as IEnumerable<object>;
            var firstEntry = state!.First() as Dictionary<string, object>;
            Assert.That(firstEntry!["key"], Is.EqualTo("player_health"));
            Assert.That(firstEntry["value"], Is.EqualTo("100"));
        }

        [Test]
        public void GetWorldState_WithSpecificKeys_FiltersResults()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithWorldState(new[] { "door=open", "chest=locked", "window=closed" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_world_state", new Dictionary<string, object>
            {
                { "keys", new List<object> { "door", "window" } }
            }));

            // Assert
            var state = (result.Result as IEnumerable<object>)!.ToList();
            Assert.That(state.Count, Is.EqualTo(2));
            var keys = state.Select(s => ((Dictionary<string, object>)s)["key"].ToString()).ToList();
            Assert.That(keys, Does.Contain("door"));
            Assert.That(keys, Does.Contain("window"));
            Assert.That(keys, Does.Not.Contain("chest"));
        }

        [Test]
        public void GetWorldState_KeyFilteringIsCaseInsensitive()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithWorldState(new[] { "Door=open" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_world_state", new Dictionary<string, object>
            {
                { "keys", new List<object> { "door" } }  // lowercase
            }));

            // Assert
            var state = (result.Result as IEnumerable<object>)!.ToList();
            Assert.That(state.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetWorldState_NoEqualsSign_UsesFullLineAsKey()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithWorldState(new[] { "some_state_without_value" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_world_state"));

            // Assert
            var state = result.Result as IEnumerable<object>;
            var firstEntry = state!.First() as Dictionary<string, object>;
            Assert.That(firstEntry!["key"], Is.EqualTo("some_state_without_value"));
            Assert.That(firstEntry["value"], Is.EqualTo(""));
        }

        [Test]
        public void GetWorldState_EmptyState_ReturnsEmptyList()
        {
            // Arrange
            var snapshot = CreateMinimalSnapshot();
            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_world_state"));

            // Assert
            var state = result.Result as IEnumerable<object>;
            Assert.That(state!.Count(), Is.EqualTo(0));
        }

        [Test]
        public void GetWorldState_MultipleEqualsInValue_PreservesValue()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithWorldState(new[] { "equation=2+2=4" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_world_state"));

            // Assert
            var state = result.Result as IEnumerable<object>;
            var firstEntry = state!.First() as Dictionary<string, object>;
            Assert.That(firstEntry!["key"], Is.EqualTo("equation"));
            Assert.That(firstEntry["value"], Is.EqualTo("2+2=4"));
        }

        #endregion

        #region get_canonical_facts Tests

        [Test]
        public void GetCanonicalFacts_ReturnsCanonicalFacts()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithCanonicalFacts(new[] { "The king is Arthur", "The castle is Camelot" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_canonical_facts"));

            // Assert
            Assert.That(result.Success, Is.True);
            var facts = result.Result as IEnumerable<object>;
            Assert.That(facts!.Count(), Is.EqualTo(2));
        }

        [Test]
        public void GetCanonicalFacts_ReturnsExpectedStructure()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithCanonicalFacts(new[] { "The king is Arthur" })
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_canonical_facts"));

            // Assert
            var facts = result.Result as IEnumerable<object>;
            var firstFact = facts!.First() as Dictionary<string, object>;
            Assert.That(firstFact!.ContainsKey("fact"), Is.True);
            Assert.That(firstFact.ContainsKey("authority"), Is.True);
            Assert.That(firstFact["fact"], Is.EqualTo("The king is Arthur"));
            Assert.That(firstFact["authority"], Is.EqualTo("canonical"));
        }

        [Test]
        public void GetCanonicalFacts_EmptyFacts_ReturnsEmptyList()
        {
            // Arrange
            var snapshot = CreateMinimalSnapshot();
            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_canonical_facts"));

            // Assert
            var facts = result.Result as IEnumerable<object>;
            Assert.That(facts!.Count(), Is.EqualTo(0));
        }

        #endregion

        #region Argument Parsing Tests

        [Test]
        public void Functions_HandleStringArguments()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithEpisodicMemories(Enumerable.Range(1, 20).Select(i => $"Memory {i}").ToArray())
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act - limit passed as string
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_memories", new Dictionary<string, object>
            {
                { "limit", "5" }  // String instead of int
            }));

            // Assert
            var memories = result.Result as IEnumerable<object>;
            Assert.That(memories!.Count(), Is.EqualTo(5));
        }

        [Test]
        public void Functions_HandleMissingArguments_UseDefaults()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Test")
                .WithEpisodicMemories(Enumerable.Range(1, 20).Select(i => $"Memory {i}").ToArray())
                .Build();

            BuiltInContextFunctions.RegisterAll(_dispatcher, snapshot);

            // Act - no arguments provided
            var result = _dispatcher.DispatchCall(FunctionCall.Create("get_memories"));

            // Assert - should use default limit of 10
            var memories = result.Result as IEnumerable<object>;
            Assert.That(memories!.Count(), Is.EqualTo(10));
        }

        #endregion

        #region Helper Methods

        private static StateSnapshot CreateMinimalSnapshot()
        {
            return new StateSnapshotBuilder()
                .WithSystemPrompt("Test system prompt")
                .WithPlayerInput("Test player input")
                .Build();
        }

        #endregion
    }
}
