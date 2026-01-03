using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core.FunctionCalling;

namespace LlamaBrain.Tests.FunctionCalling
{
    /// <summary>
    /// Tests for FunctionCall model - function call request from LLM.
    /// </summary>
    public class FunctionCallTests
    {
        #region Create Factory Tests

        [Test]
        public void Create_WithFunctionName_SetsFunctionName()
        {
            // Act
            var call = FunctionCall.Create("test_function");

            // Assert
            Assert.That(call.FunctionName, Is.EqualTo("test_function"));
        }

        [Test]
        public void Create_WithNullArguments_CreatesEmptyDictionary()
        {
            // Act
            var call = FunctionCall.Create("test_function", null);

            // Assert
            Assert.That(call.Arguments, Is.Not.Null);
            Assert.That(call.Arguments, Is.Empty);
        }

        [Test]
        public void Create_WithArguments_SetsArguments()
        {
            // Arrange
            var args = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 }
            };

            // Act
            var call = FunctionCall.Create("test_function", args);

            // Assert
            Assert.That(call.Arguments.Count, Is.EqualTo(2));
            Assert.That(call.Arguments["key1"], Is.EqualTo("value1"));
            Assert.That(call.Arguments["key2"], Is.EqualTo(42));
        }

        [Test]
        public void Create_WithCallId_SetsCallId()
        {
            // Act
            var call = FunctionCall.Create("test_function", null, "my_call_id");

            // Assert
            Assert.That(call.CallId, Is.EqualTo("my_call_id"));
        }

        [Test]
        public void Create_WithNullCallId_CallIdIsNull()
        {
            // Act
            var call = FunctionCall.Create("test_function", null, null);

            // Assert
            Assert.That(call.CallId, Is.Null);
        }

        [Test]
        public void Create_WithAllParameters_SetsAllCorrectly()
        {
            // Arrange
            var args = new Dictionary<string, object> { { "limit", 5 } };

            // Act
            var call = FunctionCall.Create("get_memories", args, "call_123");

            // Assert
            Assert.That(call.FunctionName, Is.EqualTo("get_memories"));
            Assert.That(call.Arguments["limit"], Is.EqualTo(5));
            Assert.That(call.CallId, Is.EqualTo("call_123"));
        }

        #endregion

        #region GetArgumentString Tests

        [Test]
        public void GetArgumentString_ExistingKey_ReturnsValue()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "name", "John" }
            });

            // Act
            var result = call.GetArgumentString("name");

            // Assert
            Assert.That(result, Is.EqualTo("John"));
        }

        [Test]
        public void GetArgumentString_MissingKey_ReturnsDefaultValue()
        {
            // Arrange
            var call = FunctionCall.Create("test");

            // Act
            var result = call.GetArgumentString("missing", "default_value");

            // Assert
            Assert.That(result, Is.EqualTo("default_value"));
        }

        [Test]
        public void GetArgumentString_MissingKeyNoDefault_ReturnsEmptyString()
        {
            // Arrange
            var call = FunctionCall.Create("test");

            // Act
            var result = call.GetArgumentString("missing");

            // Assert
            Assert.That(result, Is.EqualTo(""));
        }

        [Test]
        public void GetArgumentString_NullValue_ReturnsDefault()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "nullKey", null! }
            });

            // Act
            var result = call.GetArgumentString("nullKey", "fallback");

            // Assert
            Assert.That(result, Is.EqualTo("fallback"));
        }

        [Test]
        public void GetArgumentString_IntegerValue_ConvertsToString()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "number", 42 }
            });

            // Act
            var result = call.GetArgumentString("number");

            // Assert
            Assert.That(result, Is.EqualTo("42"));
        }

        [Test]
        public void GetArgumentString_BooleanValue_ConvertsToString()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "flag", true }
            });

            // Act
            var result = call.GetArgumentString("flag");

            // Assert
            Assert.That(result, Is.EqualTo("True"));
        }

        #endregion

        #region GetArgumentInt Tests

        [Test]
        public void GetArgumentInt_IntValue_ReturnsValue()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "count", 42 }
            });

            // Act
            var result = call.GetArgumentInt("count");

            // Assert
            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void GetArgumentInt_StringValue_ParsesValue()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "count", "123" }
            });

            // Act
            var result = call.GetArgumentInt("count");

            // Assert
            Assert.That(result, Is.EqualTo(123));
        }

        [Test]
        public void GetArgumentInt_InvalidString_ReturnsDefault()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "count", "not_a_number" }
            });

            // Act
            var result = call.GetArgumentInt("count", 99);

            // Assert
            Assert.That(result, Is.EqualTo(99));
        }

        [Test]
        public void GetArgumentInt_MissingKey_ReturnsDefault()
        {
            // Arrange
            var call = FunctionCall.Create("test");

            // Act
            var result = call.GetArgumentInt("missing", 50);

            // Assert
            Assert.That(result, Is.EqualTo(50));
        }

        [Test]
        public void GetArgumentInt_MissingKeyNoDefault_ReturnsZero()
        {
            // Arrange
            var call = FunctionCall.Create("test");

            // Act
            var result = call.GetArgumentInt("missing");

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void GetArgumentInt_DoubleValue_ConvertsToInt()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "value", 42.7 }
            });

            // Act
            var result = call.GetArgumentInt("value");

            // Assert
            // Double.ToString() gives "42.7", which can't be parsed as int
            Assert.That(result, Is.EqualTo(0)); // Falls back to default
        }

        [Test]
        public void GetArgumentInt_NegativeValue_Works()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "value", -10 }
            });

            // Act
            var result = call.GetArgumentInt("value");

            // Assert
            Assert.That(result, Is.EqualTo(-10));
        }

        #endregion

        #region GetArgumentBool Tests

        [Test]
        public void GetArgumentBool_TrueValue_ReturnsTrue()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "enabled", true }
            });

            // Act
            var result = call.GetArgumentBool("enabled");

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void GetArgumentBool_FalseValue_ReturnsFalse()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "enabled", false }
            });

            // Act
            var result = call.GetArgumentBool("enabled");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void GetArgumentBool_StringTrue_ParsesTrue()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "enabled", "true" }
            });

            // Act
            var result = call.GetArgumentBool("enabled");

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void GetArgumentBool_StringFalse_ParsesFalse()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "enabled", "false" }
            });

            // Act
            var result = call.GetArgumentBool("enabled");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void GetArgumentBool_InvalidString_ReturnsDefault()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "enabled", "yes" }  // Not parseable as bool
            });

            // Act
            var result = call.GetArgumentBool("enabled", true);

            // Assert
            Assert.That(result, Is.True); // Returns default
        }

        [Test]
        public void GetArgumentBool_MissingKey_ReturnsDefault()
        {
            // Arrange
            var call = FunctionCall.Create("test");

            // Act
            var result = call.GetArgumentBool("missing", true);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void GetArgumentBool_MissingKeyNoDefault_ReturnsFalse()
        {
            // Arrange
            var call = FunctionCall.Create("test");

            // Act
            var result = call.GetArgumentBool("missing");

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region GetArgumentDouble Tests

        [Test]
        public void GetArgumentDouble_DoubleValue_ReturnsValue()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "rate", 0.75 }
            });

            // Act
            var result = call.GetArgumentDouble("rate");

            // Assert
            Assert.That(result, Is.EqualTo(0.75));
        }

        [Test]
        public void GetArgumentDouble_FloatValue_ReturnsValue()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "rate", 0.5f }
            });

            // Act
            var result = call.GetArgumentDouble("rate");

            // Assert
            Assert.That(result, Is.EqualTo(0.5).Within(0.001));
        }

        [Test]
        public void GetArgumentDouble_StringValue_ParsesValue()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "rate", "0.25" }
            });

            // Act
            var result = call.GetArgumentDouble("rate");

            // Assert
            Assert.That(result, Is.EqualTo(0.25));
        }

        [Test]
        public void GetArgumentDouble_InvalidString_ReturnsDefault()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "rate", "not_a_number" }
            });

            // Act
            var result = call.GetArgumentDouble("rate", 1.0);

            // Assert
            Assert.That(result, Is.EqualTo(1.0));
        }

        [Test]
        public void GetArgumentDouble_MissingKey_ReturnsDefault()
        {
            // Arrange
            var call = FunctionCall.Create("test");

            // Act
            var result = call.GetArgumentDouble("missing", 3.14);

            // Assert
            Assert.That(result, Is.EqualTo(3.14));
        }

        [Test]
        public void GetArgumentDouble_MissingKeyNoDefault_ReturnsZero()
        {
            // Arrange
            var call = FunctionCall.Create("test");

            // Act
            var result = call.GetArgumentDouble("missing");

            // Assert
            Assert.That(result, Is.EqualTo(0.0));
        }

        [Test]
        public void GetArgumentDouble_IntegerValue_ConvertsToDouble()
        {
            // Arrange
            var call = FunctionCall.Create("test", new Dictionary<string, object>
            {
                { "value", 42 }
            });

            // Act
            var result = call.GetArgumentDouble("value");

            // Assert
            Assert.That(result, Is.EqualTo(42.0));
        }

        #endregion

        #region ToString Tests

        [Test]
        public void ToString_ReturnsExpectedFormat()
        {
            // Arrange
            var call = FunctionCall.Create("get_memories", new Dictionary<string, object>
            {
                { "limit", 10 },
                { "filter", "recent" }
            });

            // Act
            var result = call.ToString();

            // Assert
            Assert.That(result, Does.Contain("get_memories"));
            Assert.That(result, Does.Contain("2 args"));
        }

        [Test]
        public void ToString_NoArgs_ShowsZeroArgs()
        {
            // Arrange
            var call = FunctionCall.Create("simple_function");

            // Act
            var result = call.ToString();

            // Assert
            Assert.That(result, Does.Contain("simple_function"));
            Assert.That(result, Does.Contain("0 args"));
        }

        #endregion

        #region Default Values Tests

        [Test]
        public void DefaultConstructor_HasEmptyFunctionName()
        {
            // Act
            var call = new FunctionCall();

            // Assert
            Assert.That(call.FunctionName, Is.EqualTo(""));
        }

        [Test]
        public void DefaultConstructor_HasEmptyArguments()
        {
            // Act
            var call = new FunctionCall();

            // Assert
            Assert.That(call.Arguments, Is.Not.Null);
            Assert.That(call.Arguments, Is.Empty);
        }

        [Test]
        public void DefaultConstructor_HasNullCallId()
        {
            // Act
            var call = new FunctionCall();

            // Assert
            Assert.That(call.CallId, Is.Null);
        }

        #endregion
    }
}
