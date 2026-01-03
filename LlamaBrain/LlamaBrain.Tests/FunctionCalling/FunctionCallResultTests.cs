using NUnit.Framework;
using LlamaBrain.Core.FunctionCalling;

namespace LlamaBrain.Tests.FunctionCalling
{
    /// <summary>
    /// Tests for FunctionCallResult - result of executing a function call.
    /// </summary>
    public class FunctionCallResultTests
    {
        #region SuccessResult Factory Tests

        [Test]
        public void SuccessResult_SetsSuccessToTrue()
        {
            // Act
            var result = FunctionCallResult.SuccessResult("value");

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void SuccessResult_SetsResult()
        {
            // Arrange
            var value = new { data = "test", count = 42 };

            // Act
            var result = FunctionCallResult.SuccessResult(value);

            // Assert
            Assert.That(result.Result, Is.EqualTo(value));
        }

        [Test]
        public void SuccessResult_NullResult_IsAllowed()
        {
            // Act
            var result = FunctionCallResult.SuccessResult(null);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Result, Is.Null);
        }

        [Test]
        public void SuccessResult_SetsCallId()
        {
            // Act
            var result = FunctionCallResult.SuccessResult("value", "call_123");

            // Assert
            Assert.That(result.CallId, Is.EqualTo("call_123"));
        }

        [Test]
        public void SuccessResult_NullCallId_IsAllowed()
        {
            // Act
            var result = FunctionCallResult.SuccessResult("value", null);

            // Assert
            Assert.That(result.CallId, Is.Null);
        }

        [Test]
        public void SuccessResult_ErrorMessageIsNull()
        {
            // Act
            var result = FunctionCallResult.SuccessResult("value");

            // Assert
            Assert.That(result.ErrorMessage, Is.Null);
        }

        [Test]
        public void SuccessResult_WithVariousResultTypes()
        {
            // String
            var stringResult = FunctionCallResult.SuccessResult("hello");
            Assert.That(stringResult.Result, Is.EqualTo("hello"));

            // Integer
            var intResult = FunctionCallResult.SuccessResult(42);
            Assert.That(intResult.Result, Is.EqualTo(42));

            // List
            var listResult = FunctionCallResult.SuccessResult(new[] { 1, 2, 3 });
            Assert.That(listResult.Result, Is.EqualTo(new[] { 1, 2, 3 }));

            // Complex object
            var obj = new { name = "test", values = new[] { 1, 2 } };
            var objResult = FunctionCallResult.SuccessResult(obj);
            Assert.That(objResult.Result, Is.EqualTo(obj));
        }

        #endregion

        #region FailureResult Factory Tests

        [Test]
        public void FailureResult_SetsSuccessToFalse()
        {
            // Act
            var result = FunctionCallResult.FailureResult("Error message");

            // Assert
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void FailureResult_SetsErrorMessage()
        {
            // Act
            var result = FunctionCallResult.FailureResult("Something went wrong");

            // Assert
            Assert.That(result.ErrorMessage, Is.EqualTo("Something went wrong"));
        }

        [Test]
        public void FailureResult_SetsCallId()
        {
            // Act
            var result = FunctionCallResult.FailureResult("Error", "call_456");

            // Assert
            Assert.That(result.CallId, Is.EqualTo("call_456"));
        }

        [Test]
        public void FailureResult_NullCallId_IsAllowed()
        {
            // Act
            var result = FunctionCallResult.FailureResult("Error", null);

            // Assert
            Assert.That(result.CallId, Is.Null);
        }

        [Test]
        public void FailureResult_ResultIsNull()
        {
            // Act
            var result = FunctionCallResult.FailureResult("Error");

            // Assert
            Assert.That(result.Result, Is.Null);
        }

        [Test]
        public void FailureResult_EmptyErrorMessage_IsAllowed()
        {
            // Act
            var result = FunctionCallResult.FailureResult("");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo(""));
        }

        #endregion

        #region ToString Tests

        [Test]
        public void ToString_SuccessResult_ShowsSuccess()
        {
            // Arrange
            var result = FunctionCallResult.SuccessResult("data");

            // Act
            var str = result.ToString();

            // Assert
            Assert.That(str, Does.Contain("Success"));
        }

        [Test]
        public void ToString_SuccessResultWithCallId_ShowsCallId()
        {
            // Arrange
            var result = FunctionCallResult.SuccessResult("data", "my_id");

            // Act
            var str = result.ToString();

            // Assert
            Assert.That(str, Does.Contain("my_id"));
        }

        [Test]
        public void ToString_SuccessResultWithoutCallId_DoesNotShowId()
        {
            // Arrange
            var result = FunctionCallResult.SuccessResult("data");

            // Act
            var str = result.ToString();

            // Assert
            Assert.That(str, Does.Not.Contain("ID:"));
        }

        [Test]
        public void ToString_FailureResult_ShowsFailed()
        {
            // Arrange
            var result = FunctionCallResult.FailureResult("Something broke");

            // Act
            var str = result.ToString();

            // Assert
            Assert.That(str, Does.Contain("Failed"));
        }

        [Test]
        public void ToString_FailureResult_ShowsErrorMessage()
        {
            // Arrange
            var result = FunctionCallResult.FailureResult("Something broke");

            // Act
            var str = result.ToString();

            // Assert
            Assert.That(str, Does.Contain("Something broke"));
        }

        [Test]
        public void ToString_FailureResultWithCallId_ShowsCallId()
        {
            // Arrange
            var result = FunctionCallResult.FailureResult("Error", "error_call");

            // Act
            var str = result.ToString();

            // Assert
            Assert.That(str, Does.Contain("error_call"));
        }

        #endregion

        #region Default Values Tests

        [Test]
        public void DefaultConstructor_SuccessIsFalse()
        {
            // Act
            var result = new FunctionCallResult();

            // Assert
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void DefaultConstructor_ResultIsNull()
        {
            // Act
            var result = new FunctionCallResult();

            // Assert
            Assert.That(result.Result, Is.Null);
        }

        [Test]
        public void DefaultConstructor_ErrorMessageIsNull()
        {
            // Act
            var result = new FunctionCallResult();

            // Assert
            Assert.That(result.ErrorMessage, Is.Null);
        }

        [Test]
        public void DefaultConstructor_CallIdIsNull()
        {
            // Act
            var result = new FunctionCallResult();

            // Assert
            Assert.That(result.CallId, Is.Null);
        }

        #endregion

        #region Property Setter Tests

        [Test]
        public void Properties_CanBeSet()
        {
            // Arrange
            var result = new FunctionCallResult();

            // Act
            result.Success = true;
            result.Result = "my result";
            result.ErrorMessage = "my error";
            result.CallId = "my_id";

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Result, Is.EqualTo("my result"));
            Assert.That(result.ErrorMessage, Is.EqualTo("my error"));
            Assert.That(result.CallId, Is.EqualTo("my_id"));
        }

        #endregion
    }
}
