using System;
using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core.FunctionCalling;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Validation;

namespace LlamaBrain.Tests.FunctionCalling
{
    /// <summary>
    /// Tests for FunctionCallExecutor - executes function calls from parsed output.
    /// </summary>
    public class FunctionCallExecutorTests
    {
        private FunctionCallDispatcher _dispatcher = null!;
        private StateSnapshot _snapshot = null!;

        [SetUp]
        public void SetUp()
        {
            _dispatcher = new FunctionCallDispatcher();
            _snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test system prompt")
                .WithPlayerInput("Test input")
                .Build();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_NullDispatcher_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FunctionCallExecutor(null!, _snapshot));
        }

        [Test]
        public void Constructor_NullSnapshot_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FunctionCallExecutor(_dispatcher, null!));
        }

        [Test]
        public void Constructor_ValidArguments_CreatesExecutor()
        {
            // Act
            var executor = new FunctionCallExecutor(_dispatcher, _snapshot);

            // Assert
            Assert.That(executor, Is.Not.Null);
        }

        [Test]
        public void Constructor_NullMemorySystem_IsOptional()
        {
            // Act
            var executor = new FunctionCallExecutor(_dispatcher, _snapshot, null);

            // Assert
            Assert.That(executor, Is.Not.Null);
        }

        #endregion

        #region Execute Single Call Tests

        [Test]
        public void Execute_RegisteredFunction_ReturnsSuccessResult()
        {
            // Arrange
            _dispatcher.RegisterFunction("test_func", call => FunctionCallResult.SuccessResult("result", call.CallId));
            var executor = new FunctionCallExecutor(_dispatcher, _snapshot);
            var functionCall = FunctionCall.Create("test_func");

            // Act
            var result = executor.Execute(functionCall);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Result, Is.EqualTo("result"));
        }

        [Test]
        public void Execute_UnregisteredFunction_ReturnsFailureResult()
        {
            // Arrange
            var executor = new FunctionCallExecutor(_dispatcher, _snapshot);
            var functionCall = FunctionCall.Create("unknown_func");

            // Act
            var result = executor.Execute(functionCall);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("not registered"));
        }

        [Test]
        public void Execute_PassesArgumentsToHandler()
        {
            // Arrange
            Dictionary<string, object>? receivedArgs = null;
            _dispatcher.RegisterFunction("with_args", call =>
            {
                receivedArgs = call.Arguments;
                return FunctionCallResult.SuccessResult(null);
            });

            var executor = new FunctionCallExecutor(_dispatcher, _snapshot);
            var functionCall = FunctionCall.Create("with_args", new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 }
            });

            // Act
            executor.Execute(functionCall);

            // Assert
            Assert.That(receivedArgs, Is.Not.Null);
            Assert.That(receivedArgs!["key1"], Is.EqualTo("value1"));
            Assert.That(receivedArgs["key2"], Is.EqualTo(42));
        }

        #endregion

        #region ExecuteAll Tests

        [Test]
        public void ExecuteAll_NullParsedOutput_ReturnsEmptyDictionary()
        {
            // Arrange
            var executor = new FunctionCallExecutor(_dispatcher, _snapshot);

            // Act
            var results = executor.ExecuteAll(null!);

            // Assert
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void ExecuteAll_NoFunctionCalls_ReturnsEmptyDictionary()
        {
            // Arrange
            var executor = new FunctionCallExecutor(_dispatcher, _snapshot);
            var parsedOutput = ParsedOutput.Dialogue("Hello", "Hello");

            // Act
            var results = executor.ExecuteAll(parsedOutput);

            // Assert
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void ExecuteAll_NullFunctionCallsList_ReturnsEmptyDictionary()
        {
            // Arrange
            var executor = new FunctionCallExecutor(_dispatcher, _snapshot);
            var parsedOutput = ParsedOutput.Dialogue("Hello", "Hello");
            parsedOutput.FunctionCalls = null!;

            // Act
            var results = executor.ExecuteAll(parsedOutput);

            // Assert
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void ExecuteAll_SingleFunctionCall_ExecutesAndReturnsResult()
        {
            // Arrange
            _dispatcher.RegisterFunction("get_data", call => FunctionCallResult.SuccessResult("data_result", call.CallId));

            var executor = new FunctionCallExecutor(_dispatcher, _snapshot);
            var parsedOutput = ParsedOutput.Dialogue("Hello", "Hello")
                .WithFunctionCall(FunctionCall.Create("get_data", null, "call_1"));

            // Act
            var results = executor.ExecuteAll(parsedOutput);

            // Assert
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.ContainsKey("call_1"), Is.True);
            Assert.That(results["call_1"].Success, Is.True);
            Assert.That(results["call_1"].Result, Is.EqualTo("data_result"));
        }

        [Test]
        public void ExecuteAll_MultipleFunctionCalls_ExecutesAllAndReturnsResults()
        {
            // Arrange
            _dispatcher.RegisterFunction("func_a", call => FunctionCallResult.SuccessResult("result_a", call.CallId));
            _dispatcher.RegisterFunction("func_b", call => FunctionCallResult.SuccessResult("result_b", call.CallId));

            var executor = new FunctionCallExecutor(_dispatcher, _snapshot);
            var parsedOutput = ParsedOutput.Dialogue("Hello", "Hello")
                .WithFunctionCall(FunctionCall.Create("func_a", null, "call_a"))
                .WithFunctionCall(FunctionCall.Create("func_b", null, "call_b"));

            // Act
            var results = executor.ExecuteAll(parsedOutput);

            // Assert
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results["call_a"].Result, Is.EqualTo("result_a"));
            Assert.That(results["call_b"].Result, Is.EqualTo("result_b"));
        }

        [Test]
        public void ExecuteAll_NoCallId_UsesFunctionNameAsKey()
        {
            // Arrange
            _dispatcher.RegisterFunction("my_function", call => FunctionCallResult.SuccessResult("result"));

            var executor = new FunctionCallExecutor(_dispatcher, _snapshot);
            var parsedOutput = ParsedOutput.Dialogue("Hello", "Hello")
                .WithFunctionCall(FunctionCall.Create("my_function")); // No call ID

            // Act
            var results = executor.ExecuteAll(parsedOutput);

            // Assert
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.ContainsKey("my_function"), Is.True);
        }

        [Test]
        public void ExecuteAll_MixedSuccessAndFailure_ReturnsAllResults()
        {
            // Arrange
            _dispatcher.RegisterFunction("succeed", call => FunctionCallResult.SuccessResult("ok", call.CallId));
            // "fail" is not registered

            var executor = new FunctionCallExecutor(_dispatcher, _snapshot);
            var parsedOutput = ParsedOutput.Dialogue("Hello", "Hello")
                .WithFunctionCall(FunctionCall.Create("succeed", null, "success_call"))
                .WithFunctionCall(FunctionCall.Create("fail", null, "fail_call"));

            // Act
            var results = executor.ExecuteAll(parsedOutput);

            // Assert
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results["success_call"].Success, Is.True);
            Assert.That(results["fail_call"].Success, Is.False);
        }

        [Test]
        public void ExecuteAll_DuplicateCallIds_LastResultWins()
        {
            // Arrange
            var callCount = 0;
            _dispatcher.RegisterFunction("counter", call =>
            {
                callCount++;
                return FunctionCallResult.SuccessResult($"call_{callCount}", call.CallId);
            });

            var executor = new FunctionCallExecutor(_dispatcher, _snapshot);
            var parsedOutput = ParsedOutput.Dialogue("Hello", "Hello")
                .WithFunctionCall(FunctionCall.Create("counter", null, "same_id"))
                .WithFunctionCall(FunctionCall.Create("counter", null, "same_id"));

            // Act
            var results = executor.ExecuteAll(parsedOutput);

            // Assert
            Assert.That(results.Count, Is.EqualTo(1)); // Dictionary overwrites duplicates
            Assert.That(callCount, Is.EqualTo(2)); // Both calls were executed
            Assert.That(results["same_id"].Result, Is.EqualTo("call_2")); // Last result
        }

        #endregion

        #region CreateWithBuiltIns Factory Tests

        [Test]
        public void CreateWithBuiltIns_ReturnsConfiguredExecutor()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test prompt")
                .WithPlayerInput("Test input")
                .WithCanonicalFacts(new[] { "Fact 1", "Fact 2" })
                .Build();

            // Act
            var executor = FunctionCallExecutor.CreateWithBuiltIns(snapshot);

            // Assert
            Assert.That(executor, Is.Not.Null);
        }

        [Test]
        public void CreateWithBuiltIns_RegistersGetMemoriesFunction()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test prompt")
                .WithPlayerInput("Test input")
                .WithEpisodicMemories(new[] { "Memory 1", "Memory 2" })
                .Build();

            // Act
            var executor = FunctionCallExecutor.CreateWithBuiltIns(snapshot);
            var result = executor.Execute(FunctionCall.Create("get_memories"));

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Result, Is.Not.Null);
        }

        [Test]
        public void CreateWithBuiltIns_RegistersGetBeliefsFunction()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test prompt")
                .WithPlayerInput("Test input")
                .WithBeliefs(new[] { "Belief 1" })
                .Build();

            // Act
            var executor = FunctionCallExecutor.CreateWithBuiltIns(snapshot);
            var result = executor.Execute(FunctionCall.Create("get_beliefs"));

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void CreateWithBuiltIns_RegistersGetConstraintsFunction()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test prompt")
                .WithPlayerInput("Test input")
                .Build();

            // Act
            var executor = FunctionCallExecutor.CreateWithBuiltIns(snapshot);
            var result = executor.Execute(FunctionCall.Create("get_constraints"));

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void CreateWithBuiltIns_RegistersGetDialogueHistoryFunction()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test prompt")
                .WithPlayerInput("Test input")
                .WithDialogueHistory(new[] { "Player: Hello", "NPC: Hi there" })
                .Build();

            // Act
            var executor = FunctionCallExecutor.CreateWithBuiltIns(snapshot);
            var result = executor.Execute(FunctionCall.Create("get_dialogue_history"));

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void CreateWithBuiltIns_RegistersGetWorldStateFunction()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test prompt")
                .WithPlayerInput("Test input")
                .WithWorldState(new[] { "door=open" })
                .Build();

            // Act
            var executor = FunctionCallExecutor.CreateWithBuiltIns(snapshot);
            var result = executor.Execute(FunctionCall.Create("get_world_state"));

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void CreateWithBuiltIns_RegistersGetCanonicalFactsFunction()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test prompt")
                .WithPlayerInput("Test input")
                .WithCanonicalFacts(new[] { "The king is Arthur" })
                .Build();

            // Act
            var executor = FunctionCallExecutor.CreateWithBuiltIns(snapshot);
            var result = executor.Execute(FunctionCall.Create("get_canonical_facts"));

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void CreateWithBuiltIns_WithNullMemorySystem_StillWorks()
        {
            // Arrange
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test prompt")
                .WithPlayerInput("Test input")
                .Build();

            // Act
            var executor = FunctionCallExecutor.CreateWithBuiltIns(snapshot, null);

            // Assert
            Assert.That(executor, Is.Not.Null);
            var result = executor.Execute(FunctionCall.Create("get_memories"));
            Assert.That(result.Success, Is.True);
        }

        #endregion

        #region Integration with ParsedOutput Tests

        [Test]
        public void ExecuteAll_WithParsedOutputContainingDialogueAndFunctions_ExecutesFunctions()
        {
            // Arrange
            _dispatcher.RegisterFunction("get_info", call => FunctionCallResult.SuccessResult(new { info = "test_info" }, call.CallId));

            var executor = new FunctionCallExecutor(_dispatcher, _snapshot);
            var parsedOutput = new ParsedOutput
            {
                Success = true,
                DialogueText = "Let me look that up for you.",
                RawOutput = "test raw output"
            };
            parsedOutput.FunctionCalls.Add(FunctionCall.Create("get_info", null, "info_call"));

            // Act
            var results = executor.ExecuteAll(parsedOutput);

            // Assert
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results["info_call"].Success, Is.True);
        }

        [Test]
        public void ExecuteAll_FailedParsedOutput_StillExecutesFunctions()
        {
            // Arrange
            _dispatcher.RegisterFunction("fallback_func", call => FunctionCallResult.SuccessResult("fallback", call.CallId));

            var executor = new FunctionCallExecutor(_dispatcher, _snapshot);
            var parsedOutput = ParsedOutput.Failed("Parse error", "raw output");
            parsedOutput.FunctionCalls.Add(FunctionCall.Create("fallback_func", null, "fallback_call"));

            // Act
            var results = executor.ExecuteAll(parsedOutput);

            // Assert
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results["fallback_call"].Success, Is.True);
        }

        #endregion
    }
}
