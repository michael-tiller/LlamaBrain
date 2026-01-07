using NUnit.Framework;
using Newtonsoft.Json;
using LlamaBrain.Core.StructuredInput.Schemas;
using System.Collections.Generic;

namespace LlamaBrain.Tests.StructuredInput
{
    /// <summary>
    /// Tests for ValidationRequirements and ConstraintAuthority schema classes.
    /// Feature 23.2: Validation requirements and authority boundaries.
    /// </summary>
    [TestFixture]
    public class ValidationRequirementsTests
    {
        #region ValidationRequirements Tests

        [Test]
        public void ValidationRequirements_DefaultValues_AllNull()
        {
            var validation = new ValidationRequirements();

            Assert.That(validation.MinLength, Is.Null);
            Assert.That(validation.MaxLength, Is.Null);
            Assert.That(validation.RequiredKeywords, Is.Null);
            Assert.That(validation.ForbiddenKeywords, Is.Null);
            Assert.That(validation.Format, Is.Null);
            Assert.That(validation.MustBeQuestion, Is.Null);
            Assert.That(validation.MustNotBeQuestion, Is.Null);
        }

        [Test]
        public void ValidationRequirements_SetLengthLimits_PreservesValues()
        {
            var validation = new ValidationRequirements
            {
                MinLength = 10,
                MaxLength = 100
            };

            Assert.That(validation.MinLength, Is.EqualTo(10));
            Assert.That(validation.MaxLength, Is.EqualTo(100));
        }

        [Test]
        public void ValidationRequirements_SetKeywords_PreservesValues()
        {
            var validation = new ValidationRequirements
            {
                RequiredKeywords = new List<string> { "hello", "world" },
                ForbiddenKeywords = new List<string> { "goodbye", "cruel" }
            };

            Assert.That(validation.RequiredKeywords, Has.Count.EqualTo(2));
            Assert.That(validation.RequiredKeywords, Contains.Item("hello"));
            Assert.That(validation.ForbiddenKeywords, Has.Count.EqualTo(2));
            Assert.That(validation.ForbiddenKeywords, Contains.Item("goodbye"));
        }

        [Test]
        public void ValidationRequirements_JsonRoundTrip_PreservesAllFields()
        {
            var original = new ValidationRequirements
            {
                MinLength = 5,
                MaxLength = 200,
                RequiredKeywords = new List<string> { "welcome" },
                ForbiddenKeywords = new List<string> { "profanity" },
                Format = "single_sentence",
                MustBeQuestion = false,
                MustNotBeQuestion = true
            };

            var json = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<ValidationRequirements>(json);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.MinLength, Is.EqualTo(5));
            Assert.That(restored.MaxLength, Is.EqualTo(200));
            Assert.That(restored.RequiredKeywords, Contains.Item("welcome"));
            Assert.That(restored.ForbiddenKeywords, Contains.Item("profanity"));
            Assert.That(restored.Format, Is.EqualTo("single_sentence"));
            Assert.That(restored.MustBeQuestion, Is.False);
            Assert.That(restored.MustNotBeQuestion, Is.True);
        }

        [Test]
        public void ValidationRequirements_NullValues_OmittedFromJson()
        {
            var validation = new ValidationRequirements
            {
                MinLength = 10
                // All other fields left null
            };

            var json = JsonConvert.SerializeObject(validation);

            Assert.That(json, Does.Contain("minLength"));
            Assert.That(json, Does.Not.Contain("maxLength"));
            Assert.That(json, Does.Not.Contain("requiredKeywords"));
            Assert.That(json, Does.Not.Contain("forbiddenKeywords"));
        }

        #endregion

        #region ConstraintAuthority Tests

        [Test]
        public void ConstraintAuthority_DefaultValues_SystemSourceAndOverridable()
        {
            var authority = new ConstraintAuthority();

            Assert.That(authority.Source, Is.EqualTo(ConstraintSource.System));
            Assert.That(authority.SourceId, Is.Null);
            Assert.That(authority.Priority, Is.EqualTo(0));
            Assert.That(authority.IsOverridable, Is.True);
            Assert.That(authority.SetAt, Is.Null);
            Assert.That(authority.ExpiresAt, Is.Null);
        }

        [Test]
        public void ConstraintAuthority_SetAllFields_PreservesValues()
        {
            var authority = new ConstraintAuthority
            {
                Source = ConstraintSource.Quest,
                SourceId = "quest_001",
                Priority = 10,
                IsOverridable = false,
                SetAt = 100.5f,
                ExpiresAt = 500.0f
            };

            Assert.That(authority.Source, Is.EqualTo(ConstraintSource.Quest));
            Assert.That(authority.SourceId, Is.EqualTo("quest_001"));
            Assert.That(authority.Priority, Is.EqualTo(10));
            Assert.That(authority.IsOverridable, Is.False);
            Assert.That(authority.SetAt, Is.EqualTo(100.5f));
            Assert.That(authority.ExpiresAt, Is.EqualTo(500.0f));
        }

        [Test]
        public void ConstraintAuthority_JsonRoundTrip_PreservesAllFields()
        {
            var original = new ConstraintAuthority
            {
                Source = ConstraintSource.Designer,
                SourceId = "persona_blacksmith",
                Priority = 5,
                IsOverridable = true,
                SetAt = 0.0f,
                ExpiresAt = null
            };

            var json = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<ConstraintAuthority>(json);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.Source, Is.EqualTo(ConstraintSource.Designer));
            Assert.That(restored.SourceId, Is.EqualTo("persona_blacksmith"));
            Assert.That(restored.Priority, Is.EqualTo(5));
            Assert.That(restored.IsOverridable, Is.True);
            Assert.That(restored.SetAt, Is.EqualTo(0.0f));
            Assert.That(restored.ExpiresAt, Is.Null);
        }

        [Test]
        public void ConstraintAuthority_SourceSerializesAsString()
        {
            var authority = new ConstraintAuthority { Source = ConstraintSource.Npc };

            var json = JsonConvert.SerializeObject(authority);

            Assert.That(json, Does.Contain("\"Npc\""));
            Assert.That(json, Does.Not.Contain("\"2\""));
        }

        [Test]
        [TestCase(ConstraintSource.System, "System")]
        [TestCase(ConstraintSource.Designer, "Designer")]
        [TestCase(ConstraintSource.Npc, "Npc")]
        [TestCase(ConstraintSource.Player, "Player")]
        [TestCase(ConstraintSource.Quest, "Quest")]
        [TestCase(ConstraintSource.Environment, "Environment")]
        public void ConstraintSource_AllValues_SerializeCorrectly(ConstraintSource source, string expectedJson)
        {
            var authority = new ConstraintAuthority { Source = source };
            var json = JsonConvert.SerializeObject(authority);

            Assert.That(json, Does.Contain($"\"{expectedJson}\""));
        }

        #endregion

        #region ConstraintSection Integration Tests

        [Test]
        public void ConstraintSection_WithValidationAndAuthority_SerializesCorrectly()
        {
            var section = new ConstraintSection
            {
                Prohibitions = new List<string> { "No violence" },
                Requirements = new List<string> { "Be helpful" },
                Permissions = new List<string> { "May offer advice" },
                Validation = new ValidationRequirements
                {
                    MaxLength = 150,
                    ForbiddenKeywords = new List<string> { "profanity" }
                },
                Authority = new ConstraintAuthority
                {
                    Source = ConstraintSource.Designer,
                    SourceId = "npc_elder",
                    Priority = 3
                }
            };

            var json = JsonConvert.SerializeObject(section, Formatting.Indented);

            Assert.That(json, Does.Contain("prohibitions"));
            Assert.That(json, Does.Contain("validation"));
            Assert.That(json, Does.Contain("authority"));
            Assert.That(json, Does.Contain("maxLength"));
            Assert.That(json, Does.Contain("Designer"));
        }

        [Test]
        public void ConstraintSection_NullValidationAndAuthority_OmittedFromJson()
        {
            var section = new ConstraintSection
            {
                Prohibitions = new List<string> { "No violence" }
                // Validation and Authority left null
            };

            var json = JsonConvert.SerializeObject(section);

            Assert.That(json, Does.Contain("prohibitions"));
            Assert.That(json, Does.Not.Contain("validation"));
            Assert.That(json, Does.Not.Contain("authority"));
        }

        [Test]
        public void ConstraintSection_JsonRoundTrip_PreservesAllFields()
        {
            var original = new ConstraintSection
            {
                Prohibitions = new List<string> { "A", "B" },
                Requirements = new List<string> { "C" },
                Permissions = new List<string> { "D", "E", "F" },
                Validation = new ValidationRequirements
                {
                    MinLength = 20,
                    MaxLength = 100,
                    Format = "paragraph"
                },
                Authority = new ConstraintAuthority
                {
                    Source = ConstraintSource.Quest,
                    SourceId = "main_quest",
                    Priority = 100,
                    IsOverridable = false
                }
            };

            var json = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<ConstraintSection>(json);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.Prohibitions, Has.Count.EqualTo(2));
            Assert.That(restored.Requirements, Has.Count.EqualTo(1));
            Assert.That(restored.Permissions, Has.Count.EqualTo(3));
            Assert.That(restored.Validation, Is.Not.Null);
            Assert.That(restored.Validation!.MinLength, Is.EqualTo(20));
            Assert.That(restored.Authority, Is.Not.Null);
            Assert.That(restored.Authority!.Source, Is.EqualTo(ConstraintSource.Quest));
            Assert.That(restored.Authority.Priority, Is.EqualTo(100));
        }

        #endregion
    }
}
