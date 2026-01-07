using System.Collections;
using System.Collections.Generic;
using LlamaBrain.Core.StructuredOutput;
using LlamaBrain.Core.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LlamaBrain.Tests.StructuredOutput
{
    /// <summary>
    /// Tests for complex intent parameters (F13.3).
    /// Validates support for nested objects and arrays in intent parameters.
    /// </summary>
    [TestFixture]
    public class ComplexIntentParametersTests
    {
        #region StructuredIntent Complex Parameters

        [Test]
        public void StructuredIntent_Parameters_SupportsStringValues()
        {
            var intent = new StructuredIntent
            {
                IntentType = "give_item",
                Parameters = new Dictionary<string, object>
                {
                    { "itemId", "sword_001" },
                    { "reason", "quest_reward" }
                }
            };

            Assert.That(intent.Parameters["itemId"], Is.EqualTo("sword_001"));
            Assert.That(intent.Parameters["reason"], Is.EqualTo("quest_reward"));
        }

        [Test]
        public void StructuredIntent_Parameters_SupportsNumericValues()
        {
            var intent = new StructuredIntent
            {
                IntentType = "give_item",
                Parameters = new Dictionary<string, object>
                {
                    { "quantity", 5 },
                    { "quality", 0.75f }
                }
            };

            Assert.That(intent.Parameters["quantity"], Is.EqualTo(5));
            Assert.That(intent.Parameters["quality"], Is.EqualTo(0.75f));
        }

        [Test]
        public void StructuredIntent_Parameters_SupportsArrayValues()
        {
            var intent = new StructuredIntent
            {
                IntentType = "give_items",
                Parameters = new Dictionary<string, object>
                {
                    { "itemIds", new[] { "sword_001", "shield_002", "potion_003" } }
                }
            };

            var itemIds = intent.Parameters["itemIds"] as string[];
            Assert.That(itemIds, Is.Not.Null);
            Assert.That(itemIds!, Has.Length.EqualTo(3));
            Assert.That(itemIds![0], Is.EqualTo("sword_001"));
        }

        [Test]
        public void StructuredIntent_Parameters_SupportsNestedObjects()
        {
            var intent = new StructuredIntent
            {
                IntentType = "move_to",
                Parameters = new Dictionary<string, object>
                {
                    { "destination", new Dictionary<string, object>
                        {
                            { "x", 100.5f },
                            { "y", 0f },
                            { "z", -50.25f }
                        }
                    },
                    { "speed", "walk" }
                }
            };

            var destination = intent.Parameters["destination"] as Dictionary<string, object>;
            Assert.That(destination, Is.Not.Null);
            Assert.That(destination!["x"], Is.EqualTo(100.5f));
        }

        [Test]
        public void StructuredIntent_Parameters_SerializesToJson()
        {
            var intent = new StructuredIntent
            {
                IntentType = "complex_action",
                Target = "player",
                Priority = 5,
                Parameters = new Dictionary<string, object>
                {
                    { "items", new[] { "item1", "item2" } },
                    { "options", new Dictionary<string, object>
                        {
                            { "verbose", true },
                            { "timeout", 30 }
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(intent);
            var deserialized = JsonConvert.DeserializeObject<StructuredIntent>(json);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.IntentType, Is.EqualTo("complex_action"));
            Assert.That(deserialized.Parameters, Has.Count.EqualTo(2));
        }

        [Test]
        public void StructuredIntent_ToWorldIntent_PreservesComplexParameters()
        {
            var structuredIntent = new StructuredIntent
            {
                IntentType = "give_item",
                Target = "player",
                Priority = 3,
                Parameters = new Dictionary<string, object>
                {
                    { "itemId", "sword_001" },
                    { "quantity", 1 }
                }
            };

            var worldIntent = structuredIntent.ToWorldIntent();

            Assert.That(worldIntent.Parameters, Has.Count.EqualTo(2));
            Assert.That(worldIntent.Parameters["itemId"], Is.EqualTo("sword_001"));
            Assert.That(worldIntent.Parameters["quantity"], Is.EqualTo(1));
        }

        #endregion

        #region WorldIntent Complex Parameters

        [Test]
        public void WorldIntent_Parameters_SupportsObjectValues()
        {
            var intent = WorldIntent.Create("patrol", "guard_post", 1);
            intent.Parameters = new Dictionary<string, object>
            {
                { "route", new[] { "point_a", "point_b", "point_c" } },
                { "loopCount", 3 }
            };

            Assert.That(intent.Parameters["loopCount"], Is.EqualTo(3));
            var route = intent.Parameters["route"] as string[];
            Assert.That(route, Is.Not.Null);
            Assert.That(route, Has.Length.EqualTo(3));
        }

        #endregion

        #region Validation Tests

        [Test]
        public void ValidateIntent_WithComplexParameters_Passes()
        {
            var intent = new StructuredIntent
            {
                IntentType = "complex_action",
                Priority = 0,
                Parameters = new Dictionary<string, object>
                {
                    { "nestedData", new Dictionary<string, object> { { "key", "value" } } },
                    { "arrayData", new[] { 1, 2, 3 } }
                }
            };

            var result = StructuredSchemaValidator.ValidateIntent(intent);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateIntent_WithNullParameters_Fails()
        {
            var intent = new StructuredIntent
            {
                IntentType = "action",
                Parameters = null!
            };

            var result = StructuredSchemaValidator.ValidateIntent(intent);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("parameters"));
        }

        #endregion

        #region Typed Parameter Helper Tests

        [Test]
        public void GiveItemParameters_ExtractsFromDictionary()
        {
            var parameters = new Dictionary<string, object>
            {
                { "itemId", "sword_001" },
                { "quantity", 5 },
                { "condition", "new" }
            };

            var typed = IntentParameterExtensions.GetGiveItemParameters(parameters);

            Assert.That(typed, Is.Not.Null);
            Assert.That(typed!.ItemId, Is.EqualTo("sword_001"));
            Assert.That(typed.Quantity, Is.EqualTo(5));
            Assert.That(typed.Condition, Is.EqualTo("new"));
        }

        [Test]
        public void GiveItemParameters_HandlesDefaultQuantity()
        {
            var parameters = new Dictionary<string, object>
            {
                { "itemId", "potion_001" }
            };

            var typed = IntentParameterExtensions.GetGiveItemParameters(parameters);

            Assert.That(typed, Is.Not.Null);
            Assert.That(typed!.ItemId, Is.EqualTo("potion_001"));
            Assert.That(typed.Quantity, Is.EqualTo(1)); // Default
        }

        [Test]
        public void GiveItemParameters_ReturnsNullForMissingItemId()
        {
            var parameters = new Dictionary<string, object>
            {
                { "quantity", 5 }
            };

            var typed = IntentParameterExtensions.GetGiveItemParameters(parameters);

            Assert.That(typed, Is.Null);
        }

        [Test]
        public void MoveToParameters_ExtractsFromDictionary()
        {
            var parameters = new Dictionary<string, object>
            {
                { "location", "tavern" },
                { "speed", "run" },
                { "pathType", "direct" }
            };

            var typed = IntentParameterExtensions.GetMoveToParameters(parameters);

            Assert.That(typed, Is.Not.Null);
            Assert.That(typed!.Location, Is.EqualTo("tavern"));
            Assert.That(typed.Speed, Is.EqualTo("run"));
            Assert.That(typed.PathType, Is.EqualTo("direct"));
        }

        [Test]
        public void MoveToParameters_HandlesDefaults()
        {
            var parameters = new Dictionary<string, object>
            {
                { "location", "market" }
            };

            var typed = IntentParameterExtensions.GetMoveToParameters(parameters);

            Assert.That(typed, Is.Not.Null);
            Assert.That(typed!.Speed, Is.EqualTo("walk")); // Default
            Assert.That(typed.PathType, Is.EqualTo("pathfind")); // Default
        }

        [Test]
        public void InteractParameters_ExtractsFromDictionary()
        {
            var parameters = new Dictionary<string, object>
            {
                { "targetEntity", "npc_blacksmith" },
                { "interactionType", "trade" },
                { "duration", 30.0f }
            };

            var typed = IntentParameterExtensions.GetInteractParameters(parameters);

            Assert.That(typed, Is.Not.Null);
            Assert.That(typed!.TargetEntity, Is.EqualTo("npc_blacksmith"));
            Assert.That(typed.InteractionType, Is.EqualTo("trade"));
            Assert.That(typed.Duration, Is.EqualTo(30.0f));
        }

        [Test]
        public void GetValue_ExtractsTypedValueFromDictionary()
        {
            var parameters = new Dictionary<string, object>
            {
                { "count", 42 },
                { "name", "test" },
                { "enabled", true }
            };

            Assert.That(IntentParameterExtensions.GetValue<int>(parameters, "count"), Is.EqualTo(42));
            Assert.That(IntentParameterExtensions.GetValue<string>(parameters, "name"), Is.EqualTo("test"));
            Assert.That(IntentParameterExtensions.GetValue<bool>(parameters, "enabled"), Is.True);
        }

        [Test]
        public void GetValue_ReturnsDefaultForMissingKey()
        {
            var parameters = new Dictionary<string, object>();

            Assert.That(IntentParameterExtensions.GetValue<int>(parameters, "missing", 99), Is.EqualTo(99));
            Assert.That(IntentParameterExtensions.GetValue<string>(parameters, "missing", "default"), Is.EqualTo("default"));
        }

        #endregion

        #region JSON Schema Tests

        [Test]
        public void ParsedOutputSchema_Parameters_AllowsAnyType()
        {
            // The schema should have additionalProperties: true for parameters
            var schema = JsonSchemaBuilder.ParsedOutputSchema;

            Assert.That(schema, Does.Contain("\"additionalProperties\": true").Or.Contain("\"additionalProperties\":true"));
        }

        #endregion

        #region GetArray Collection Type Tests

        [Test]
        public void GetArray_WithTypedArray_ReturnsDirectly()
        {
            var parameters = new Dictionary<string, object>
            {
                { "items", new string[] { "a", "b", "c" } }
            };

            var result = IntentParameterExtensions.GetArray<string>(parameters, "items");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result![0], Is.EqualTo("a"));
        }

        [Test]
        public void GetArray_WithObjectArray_ConvertsElements()
        {
            var parameters = new Dictionary<string, object>
            {
                { "numbers", new object[] { 1, 2, 3 } }
            };

            var result = IntentParameterExtensions.GetArray<int>(parameters, "numbers");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result![0], Is.EqualTo(1));
        }

        [Test]
        public void GetArray_WithListOfT_ConvertsToArray()
        {
            var parameters = new Dictionary<string, object>
            {
                { "items", new List<string> { "x", "y", "z" } }
            };

            var result = IntentParameterExtensions.GetArray<string>(parameters, "items");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result![1], Is.EqualTo("y"));
        }

        [Test]
        public void GetArray_WithListOfObject_ConvertsElements()
        {
            var parameters = new Dictionary<string, object>
            {
                { "ids", new List<object> { 10L, 20L, 30L } }
            };

            var result = IntentParameterExtensions.GetArray<long>(parameters, "ids");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result![2], Is.EqualTo(30L));
        }

        [Test]
        public void GetArray_WithJArray_ConvertsElements()
        {
            var jArray = new JArray { "one", "two", "three" };
            var parameters = new Dictionary<string, object>
            {
                { "words", jArray }
            };

            var result = IntentParameterExtensions.GetArray<string>(parameters, "words");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result![0], Is.EqualTo("one"));
        }

        [Test]
        public void GetArray_WithJArrayOfIntegers_ConvertsElements()
        {
            var jArray = new JArray { 100, 200, 300 };
            var parameters = new Dictionary<string, object>
            {
                { "values", jArray }
            };

            var result = IntentParameterExtensions.GetArray<int>(parameters, "values");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result![1], Is.EqualTo(200));
        }

        [Test]
        public void GetArray_WithJToken_ConvertsArrayToken()
        {
            var json = "[\"alpha\", \"beta\", \"gamma\"]";
            var jToken = JToken.Parse(json);
            var parameters = new Dictionary<string, object>
            {
                { "letters", jToken }
            };

            var result = IntentParameterExtensions.GetArray<string>(parameters, "letters");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result![2], Is.EqualTo("gamma"));
        }

        [Test]
        public void GetArray_WithArrayList_ConvertsElements()
        {
            var arrayList = new ArrayList { "first", "second", "third" };
            var parameters = new Dictionary<string, object>
            {
                { "items", arrayList }
            };

            var result = IntentParameterExtensions.GetArray<string>(parameters, "items");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result![0], Is.EqualTo("first"));
        }

        [Test]
        public void GetArray_WithMissingKey_ReturnsNull()
        {
            var parameters = new Dictionary<string, object>();

            var result = IntentParameterExtensions.GetArray<string>(parameters, "nonexistent");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetArray_WithNullValue_ReturnsNull()
        {
            var parameters = new Dictionary<string, object>
            {
                { "items", null! }
            };

            var result = IntentParameterExtensions.GetArray<string>(parameters, "items");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetArray_WithNullParameters_ReturnsNull()
        {
            var result = IntentParameterExtensions.GetArray<string>(null, "items");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetArray_WithInconvertibleElements_ReturnsNull()
        {
            var parameters = new Dictionary<string, object>
            {
                { "items", new object[] { "not", "integers" } }
            };

            // Attempting to convert strings to integers should fail
            var result = IntentParameterExtensions.GetArray<int>(parameters, "items");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetArray_FromJsonDeserialized_HandlesListCorrectly()
        {
            // Simulate JSON deserialization which produces List<object> or JArray
            var json = @"{""items"":[""a"",""b"",""c""]}";
            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            var result = IntentParameterExtensions.GetArray<string>(deserialized, "items");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result![0], Is.EqualTo("a"));
        }

        [Test]
        public void GetArray_FromJsonDeserializedIntegers_HandlesConversion()
        {
            var json = @"{""numbers"":[1,2,3,4,5]}";
            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            var result = IntentParameterExtensions.GetArray<int>(deserialized, "numbers");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(5));
            Assert.That(result![4], Is.EqualTo(5));
        }

        [Test]
        public void GetArray_WithJArrayContainingJValues_ConvertsCorrectly()
        {
            var jArray = JArray.Parse("[10.5, 20.5, 30.5]");
            var parameters = new Dictionary<string, object>
            {
                { "floats", jArray }
            };

            var result = IntentParameterExtensions.GetArray<float>(parameters, "floats");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result![0], Is.EqualTo(10.5f).Within(0.01f));
        }

        [Test]
        public void GetArray_WithIEnumerable_ConvertsToArray()
        {
            IEnumerable<int> enumerable = new int[] { 7, 8, 9 };
            var parameters = new Dictionary<string, object>
            {
                { "nums", enumerable }
            };

            var result = IntentParameterExtensions.GetArray<int>(parameters, "nums");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result![0], Is.EqualTo(7));
        }

        #endregion
    }
}
