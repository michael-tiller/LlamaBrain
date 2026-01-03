using System;
using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core.FunctionCalling;

namespace LlamaBrain.Tests.FunctionCalling
{
    /// <summary>
    /// Tests for FunctionCallDispatcher - command dispatch pattern for function calls.
    /// </summary>
    public class FunctionCallDispatcherTests
    {
        private FunctionCallDispatcher _dispatcher = null!;

        [SetUp]
        public void SetUp()
        {
            _dispatcher = new FunctionCallDispatcher();
        }

        #region Registration Tests

        [Test]
        public void RegisterFunction_ValidHandler_RegistersSuccessfully()
        {
            // Arrange
            var called = false;
            Func<FunctionCall, FunctionCallResult> handler = call =>
            {
                called = true;
                return FunctionCallResult.SuccessResult("result");
            };

            // Act
            _dispatcher.RegisterFunction("test_function", handler);

            // Assert
            Assert.That(_dispatcher.IsFunctionRegistered("test_function"), Is.True);

            // Verify the handler works
            var result = _dispatcher.DispatchCall(FunctionCall.Create("test_function"));
            Assert.That(called, Is.True);
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void RegisterFunction_WithMetadata_StoresMetadataCorrectly()
        {
            // Arrange
            var description = "A test function";
            var schema = @"{ ""type"": ""object"" }";

            // Act
            _dispatcher.RegisterFunction(
                "test_function",
                call => FunctionCallResult.SuccessResult(null),
                description,
                schema);

            // Assert
            var metadata = _dispatcher.GetFunctionMetadata("test_function");
            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata!.FunctionName, Is.EqualTo("test_function"));
            Assert.That(metadata.Description, Is.EqualTo(description));
            Assert.That(metadata.ParameterSchema, Is.EqualTo(schema));
        }

        [Test]
        public void RegisterFunction_NullFunctionName_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _dispatcher.RegisterFunction(null!, call => FunctionCallResult.SuccessResult(null)));
        }

        [Test]
        public void RegisterFunction_EmptyFunctionName_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _dispatcher.RegisterFunction("", call => FunctionCallResult.SuccessResult(null)));
        }

        [Test]
        public void RegisterFunction_WhitespaceFunctionName_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _dispatcher.RegisterFunction("   ", call => FunctionCallResult.SuccessResult(null)));
        }

        [Test]
        public void RegisterFunction_NullHandler_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _dispatcher.RegisterFunction("test_function", null!));
        }

        [Test]
        public void RegisterFunction_OverwritesExistingHandler()
        {
            // Arrange
            var firstCalled = false;
            var secondCalled = false;

            _dispatcher.RegisterFunction("test_function", call =>
            {
                firstCalled = true;
                return FunctionCallResult.SuccessResult("first");
            });

            // Act
            _dispatcher.RegisterFunction("test_function", call =>
            {
                secondCalled = true;
                return FunctionCallResult.SuccessResult("second");
            });

            _dispatcher.DispatchCall(FunctionCall.Create("test_function"));

            // Assert
            Assert.That(firstCalled, Is.False);
            Assert.That(secondCalled, Is.True);
        }

        #endregion

        #region Unregister Tests

        [Test]
        public void UnregisterFunction_ExistingFunction_ReturnsTrue()
        {
            // Arrange
            _dispatcher.RegisterFunction("test_function", call => FunctionCallResult.SuccessResult(null));

            // Act
            var result = _dispatcher.UnregisterFunction("test_function");

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_dispatcher.IsFunctionRegistered("test_function"), Is.False);
        }

        [Test]
        public void UnregisterFunction_NonExistingFunction_ReturnsFalse()
        {
            // Act
            var result = _dispatcher.UnregisterFunction("nonexistent");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void UnregisterFunction_NullName_ReturnsFalse()
        {
            // Act
            var result = _dispatcher.UnregisterFunction(null!);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void UnregisterFunction_EmptyName_ReturnsFalse()
        {
            // Act
            var result = _dispatcher.UnregisterFunction("");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void UnregisterFunction_RemovesMetadataAsWell()
        {
            // Arrange
            _dispatcher.RegisterFunction("test_function", call => FunctionCallResult.SuccessResult(null), "Description");

            // Act
            _dispatcher.UnregisterFunction("test_function");

            // Assert
            Assert.That(_dispatcher.GetFunctionMetadata("test_function"), Is.Null);
        }

        #endregion

        #region Case Insensitivity Tests

        [Test]
        public void RegisterFunction_CaseInsensitive_SameFunction()
        {
            // Arrange
            _dispatcher.RegisterFunction("Test_Function", call => FunctionCallResult.SuccessResult("result"));

            // Act & Assert
            Assert.That(_dispatcher.IsFunctionRegistered("test_function"), Is.True);
            Assert.That(_dispatcher.IsFunctionRegistered("TEST_FUNCTION"), Is.True);
            Assert.That(_dispatcher.IsFunctionRegistered("Test_Function"), Is.True);
        }

        [Test]
        public void DispatchCall_CaseInsensitive_FindsFunction()
        {
            // Arrange
            _dispatcher.RegisterFunction("Get_Memories", call => FunctionCallResult.SuccessResult("memories"));

            // Act
            var result1 = _dispatcher.DispatchCall(FunctionCall.Create("get_memories"));
            var result2 = _dispatcher.DispatchCall(FunctionCall.Create("GET_MEMORIES"));
            var result3 = _dispatcher.DispatchCall(FunctionCall.Create("Get_Memories"));

            // Assert
            Assert.That(result1.Success, Is.True);
            Assert.That(result2.Success, Is.True);
            Assert.That(result3.Success, Is.True);
        }

        [Test]
        public void GetFunctionMetadata_CaseInsensitive_ReturnsMetadata()
        {
            // Arrange
            _dispatcher.RegisterFunction("MyFunction", call => FunctionCallResult.SuccessResult(null), "Description");

            // Act
            var metadata1 = _dispatcher.GetFunctionMetadata("myfunction");
            var metadata2 = _dispatcher.GetFunctionMetadata("MYFUNCTION");
            var metadata3 = _dispatcher.GetFunctionMetadata("MyFunction");

            // Assert
            Assert.That(metadata1, Is.Not.Null);
            Assert.That(metadata2, Is.Not.Null);
            Assert.That(metadata3, Is.Not.Null);
            Assert.That(metadata1!.FunctionName, Is.EqualTo("MyFunction")); // Preserves original casing
        }

        #endregion

        #region Dispatch Tests

        [Test]
        public void DispatchCall_RegisteredFunction_ExecutesHandler()
        {
            // Arrange
            var receivedCall = (FunctionCall?)null;
            _dispatcher.RegisterFunction("process_data", call =>
            {
                receivedCall = call;
                return FunctionCallResult.SuccessResult("processed");
            });

            var functionCall = FunctionCall.Create("process_data", new Dictionary<string, object>
            {
                { "input", "test_data" }
            }, "call_123");

            // Act
            var result = _dispatcher.DispatchCall(functionCall);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Result, Is.EqualTo("processed"));
            Assert.That(receivedCall, Is.Not.Null);
            Assert.That(receivedCall!.GetArgumentString("input"), Is.EqualTo("test_data"));
        }

        [Test]
        public void DispatchCall_UnregisteredFunction_ReturnsFailure()
        {
            // Arrange
            var functionCall = FunctionCall.Create("unknown_function");

            // Act
            var result = _dispatcher.DispatchCall(functionCall);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("unknown_function"));
            Assert.That(result.ErrorMessage, Does.Contain("not registered"));
        }

        [Test]
        public void DispatchCall_UnregisteredFunction_ListsAvailableFunctions()
        {
            // Arrange
            _dispatcher.RegisterFunction("func_a", call => FunctionCallResult.SuccessResult(null));
            _dispatcher.RegisterFunction("func_b", call => FunctionCallResult.SuccessResult(null));

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("unknown"));

            // Assert
            Assert.That(result.ErrorMessage, Does.Contain("func_a"));
            Assert.That(result.ErrorMessage, Does.Contain("func_b"));
        }

        [Test]
        public void DispatchCall_NullFunctionCall_ReturnsFailure()
        {
            // Act
            var result = _dispatcher.DispatchCall(null!);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("cannot be null"));
        }

        [Test]
        public void DispatchCall_NullFunctionName_ReturnsFailure()
        {
            // Arrange
            var functionCall = new FunctionCall { FunctionName = null! };

            // Act
            var result = _dispatcher.DispatchCall(functionCall);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("null or empty"));
        }

        [Test]
        public void DispatchCall_EmptyFunctionName_ReturnsFailure()
        {
            // Arrange
            var functionCall = FunctionCall.Create("");

            // Act
            var result = _dispatcher.DispatchCall(functionCall);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("null or empty"));
        }

        [Test]
        public void DispatchCall_PreservesCallId()
        {
            // Arrange
            _dispatcher.RegisterFunction("test", call => FunctionCallResult.SuccessResult("result", call.CallId));

            var functionCall = FunctionCall.Create("test", null, "my_call_id_123");

            // Act
            var result = _dispatcher.DispatchCall(functionCall);

            // Assert
            Assert.That(result.CallId, Is.EqualTo("my_call_id_123"));
        }

        [Test]
        public void DispatchCall_HandlerThrowsException_ReturnsFailure()
        {
            // Arrange
            _dispatcher.RegisterFunction("throw_error", call =>
            {
                throw new InvalidOperationException("Test exception");
            });

            // Act
            var result = _dispatcher.DispatchCall(FunctionCall.Create("throw_error"));

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("throw_error"));
            Assert.That(result.ErrorMessage, Does.Contain("Test exception"));
        }

        [Test]
        public void DispatchCall_HandlerThrowsException_PreservesCallId()
        {
            // Arrange
            _dispatcher.RegisterFunction("throw_error", call =>
            {
                throw new Exception("Error");
            });

            var functionCall = FunctionCall.Create("throw_error", null, "error_call_id");

            // Act
            var result = _dispatcher.DispatchCall(functionCall);

            // Assert
            Assert.That(result.CallId, Is.EqualTo("error_call_id"));
        }

        #endregion

        #region DispatchCalls (Multiple) Tests

        [Test]
        public void DispatchCalls_MultipleCalls_ReturnsAllResults()
        {
            // Arrange
            _dispatcher.RegisterFunction("add", call =>
            {
                var a = call.GetArgumentInt("a");
                var b = call.GetArgumentInt("b");
                return FunctionCallResult.SuccessResult(a + b, call.CallId);
            });

            var calls = new[]
            {
                FunctionCall.Create("add", new Dictionary<string, object> { { "a", 1 }, { "b", 2 } }, "call_1"),
                FunctionCall.Create("add", new Dictionary<string, object> { { "a", 3 }, { "b", 4 } }, "call_2"),
                FunctionCall.Create("add", new Dictionary<string, object> { { "a", 5 }, { "b", 6 } }, "call_3")
            };

            // Act
            var results = _dispatcher.DispatchCalls(calls);

            // Assert
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0].Result, Is.EqualTo(3));
            Assert.That(results[1].Result, Is.EqualTo(7));
            Assert.That(results[2].Result, Is.EqualTo(11));
        }

        [Test]
        public void DispatchCalls_EmptyList_ReturnsEmptyList()
        {
            // Act
            var results = _dispatcher.DispatchCalls(new List<FunctionCall>());

            // Assert
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void DispatchCalls_MixedSuccessAndFailure_ReturnsAllResults()
        {
            // Arrange
            _dispatcher.RegisterFunction("succeed", call => FunctionCallResult.SuccessResult("ok"));

            var calls = new[]
            {
                FunctionCall.Create("succeed"),
                FunctionCall.Create("unknown"),
                FunctionCall.Create("succeed")
            };

            // Act
            var results = _dispatcher.DispatchCalls(calls);

            // Assert
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0].Success, Is.True);
            Assert.That(results[1].Success, Is.False);
            Assert.That(results[2].Success, Is.True);
        }

        #endregion

        #region Introspection Tests

        [Test]
        public void GetRegisteredFunctionNames_ReturnsAllNames()
        {
            // Arrange
            _dispatcher.RegisterFunction("func_c", call => FunctionCallResult.SuccessResult(null));
            _dispatcher.RegisterFunction("func_a", call => FunctionCallResult.SuccessResult(null));
            _dispatcher.RegisterFunction("func_b", call => FunctionCallResult.SuccessResult(null));

            // Act
            var names = _dispatcher.GetRegisteredFunctionNames();

            // Assert
            Assert.That(names.Count, Is.EqualTo(3));
            Assert.That(names, Does.Contain("func_a"));
            Assert.That(names, Does.Contain("func_b"));
            Assert.That(names, Does.Contain("func_c"));
        }

        [Test]
        public void GetRegisteredFunctionNames_ReturnsSortedList()
        {
            // Arrange
            _dispatcher.RegisterFunction("zebra", call => FunctionCallResult.SuccessResult(null));
            _dispatcher.RegisterFunction("apple", call => FunctionCallResult.SuccessResult(null));
            _dispatcher.RegisterFunction("mango", call => FunctionCallResult.SuccessResult(null));

            // Act
            var names = _dispatcher.GetRegisteredFunctionNames();

            // Assert
            Assert.That(names[0], Is.EqualTo("apple"));
            Assert.That(names[1], Is.EqualTo("mango"));
            Assert.That(names[2], Is.EqualTo("zebra"));
        }

        [Test]
        public void GetRegisteredFunctionNames_EmptyDispatcher_ReturnsEmptyList()
        {
            // Act
            var names = _dispatcher.GetRegisteredFunctionNames();

            // Assert
            Assert.That(names, Is.Empty);
        }

        [Test]
        public void GetAllFunctionMetadata_ReturnsAllMetadata()
        {
            // Arrange
            _dispatcher.RegisterFunction("func_1", call => FunctionCallResult.SuccessResult(null), "Description 1");
            _dispatcher.RegisterFunction("func_2", call => FunctionCallResult.SuccessResult(null), "Description 2");

            // Act
            var metadata = _dispatcher.GetAllFunctionMetadata();

            // Assert
            Assert.That(metadata.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetAllFunctionMetadata_ReturnsSortedByName()
        {
            // Arrange
            _dispatcher.RegisterFunction("zebra", call => FunctionCallResult.SuccessResult(null));
            _dispatcher.RegisterFunction("apple", call => FunctionCallResult.SuccessResult(null));

            // Act
            var metadata = _dispatcher.GetAllFunctionMetadata();

            // Assert
            Assert.That(metadata[0].FunctionName, Is.EqualTo("apple"));
            Assert.That(metadata[1].FunctionName, Is.EqualTo("zebra"));
        }

        [Test]
        public void IsFunctionRegistered_RegisteredFunction_ReturnsTrue()
        {
            // Arrange
            _dispatcher.RegisterFunction("test", call => FunctionCallResult.SuccessResult(null));

            // Act & Assert
            Assert.That(_dispatcher.IsFunctionRegistered("test"), Is.True);
        }

        [Test]
        public void IsFunctionRegistered_UnregisteredFunction_ReturnsFalse()
        {
            // Act & Assert
            Assert.That(_dispatcher.IsFunctionRegistered("unknown"), Is.False);
        }

        [Test]
        public void IsFunctionRegistered_NullName_ReturnsFalse()
        {
            // Act & Assert
            Assert.That(_dispatcher.IsFunctionRegistered(null!), Is.False);
        }

        [Test]
        public void IsFunctionRegistered_EmptyName_ReturnsFalse()
        {
            // Act & Assert
            Assert.That(_dispatcher.IsFunctionRegistered(""), Is.False);
        }

        [Test]
        public void GetFunctionMetadata_UnregisteredFunction_ReturnsNull()
        {
            // Act
            var metadata = _dispatcher.GetFunctionMetadata("unknown");

            // Assert
            Assert.That(metadata, Is.Null);
        }

        [Test]
        public void GetFunctionMetadata_NullName_ReturnsNull()
        {
            // Act
            var metadata = _dispatcher.GetFunctionMetadata(null!);

            // Assert
            Assert.That(metadata, Is.Null);
        }

        [Test]
        public void GetFunctionMetadata_EmptyName_ReturnsNull()
        {
            // Act
            var metadata = _dispatcher.GetFunctionMetadata("");

            // Assert
            Assert.That(metadata, Is.Null);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_RemovesAllFunctions()
        {
            // Arrange
            _dispatcher.RegisterFunction("func_1", call => FunctionCallResult.SuccessResult(null));
            _dispatcher.RegisterFunction("func_2", call => FunctionCallResult.SuccessResult(null));
            _dispatcher.RegisterFunction("func_3", call => FunctionCallResult.SuccessResult(null));

            // Act
            _dispatcher.Clear();

            // Assert
            Assert.That(_dispatcher.GetRegisteredFunctionNames(), Is.Empty);
            Assert.That(_dispatcher.IsFunctionRegistered("func_1"), Is.False);
            Assert.That(_dispatcher.IsFunctionRegistered("func_2"), Is.False);
            Assert.That(_dispatcher.IsFunctionRegistered("func_3"), Is.False);
        }

        [Test]
        public void Clear_RemovesAllMetadata()
        {
            // Arrange
            _dispatcher.RegisterFunction("test", call => FunctionCallResult.SuccessResult(null), "Description");

            // Act
            _dispatcher.Clear();

            // Assert
            Assert.That(_dispatcher.GetAllFunctionMetadata(), Is.Empty);
            Assert.That(_dispatcher.GetFunctionMetadata("test"), Is.Null);
        }

        [Test]
        public void Clear_EmptyDispatcher_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _dispatcher.Clear());
        }

        #endregion
    }
}
