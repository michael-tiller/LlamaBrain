using System.Collections.Generic;
using LlamaBrain.Core.StructuredOutput;
using LlamaBrain.Core.Validation;
using Newtonsoft.Json;
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
            Assert.That(itemIds, Has.Length.EqualTo(3));
            Assert.That(itemIds[0], Is.EqualTo("sword_001"));
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
            Assert.That(destination["x"], Is.EqualTo(100.5f));
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
    }
}
