using System;
using LlamaBrain.Core.StructuredOutput;
using NUnit.Framework;

namespace LlamaBrain.Tests.StructuredOutput
{
    /// <summary>
    /// Tests for SchemaVersion and SchemaVersionManager.
    /// </summary>
    public class SchemaVersionTests
    {
        #region SchemaVersion Tests

        [Test]
        public void SchemaVersion_Constructor_SetsProperties()
        {
            // Arrange & Act
            var version = new SchemaVersion(1, 2, 3);

            // Assert
            Assert.That(version.Major, Is.EqualTo(1));
            Assert.That(version.Minor, Is.EqualTo(2));
            Assert.That(version.Patch, Is.EqualTo(3));
        }

        [TestCase("1.0.0", 1, 0, 0)]
        [TestCase("1.2.3", 1, 2, 3)]
        [TestCase("10.20.30", 10, 20, 30)]
        [TestCase("0.0.1", 0, 0, 1)]
        public void SchemaVersion_Parse_ValidVersionStrings(string versionString, int major, int minor, int patch)
        {
            // Act
            var version = SchemaVersion.Parse(versionString);

            // Assert
            Assert.That(version.Major, Is.EqualTo(major));
            Assert.That(version.Minor, Is.EqualTo(minor));
            Assert.That(version.Patch, Is.EqualTo(patch));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("1.0")]
        [TestCase("1")]
        [TestCase("1.0.0.0")]
        [TestCase("a.b.c")]
        [TestCase("1.0.a")]
        public void SchemaVersion_Parse_InvalidVersionStrings_ThrowsFormatException(string versionString)
        {
            // Act & Assert
            Assert.Throws<FormatException>(() => SchemaVersion.Parse(versionString));
        }

        [Test]
        public void SchemaVersion_TryParse_ValidVersion_ReturnsTrue()
        {
            // Act
            var success = SchemaVersion.TryParse("1.2.3", out var version);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(version.Major, Is.EqualTo(1));
            Assert.That(version.Minor, Is.EqualTo(2));
            Assert.That(version.Patch, Is.EqualTo(3));
        }

        [Test]
        public void SchemaVersion_TryParse_InvalidVersion_ReturnsFalse()
        {
            // Act
            var success = SchemaVersion.TryParse("invalid", out var version);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(version, Is.EqualTo(default(SchemaVersion)));
        }

        [Test]
        public void SchemaVersion_ToString_FormatsCorrectly()
        {
            // Arrange
            var version = new SchemaVersion(1, 2, 3);

            // Act
            var result = version.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("1.2.3"));
        }

        [Test]
        public void SchemaVersion_Equals_SameVersion_ReturnsTrue()
        {
            // Arrange
            var v1 = new SchemaVersion(1, 2, 3);
            var v2 = new SchemaVersion(1, 2, 3);

            // Assert
            Assert.That(v1, Is.EqualTo(v2));
            Assert.That(v1 == v2, Is.True);
            Assert.That(v1 != v2, Is.False);
        }

        [Test]
        public void SchemaVersion_Equals_DifferentVersion_ReturnsFalse()
        {
            // Arrange
            var v1 = new SchemaVersion(1, 2, 3);
            var v2 = new SchemaVersion(1, 2, 4);

            // Assert
            Assert.That(v1, Is.Not.EqualTo(v2));
            Assert.That(v1 == v2, Is.False);
            Assert.That(v1 != v2, Is.True);
        }

        [TestCase(1, 0, 0, 1, 0, 0, 0)]  // Equal
        [TestCase(1, 0, 0, 0, 9, 9, 1)]  // Major diff
        [TestCase(1, 1, 0, 1, 0, 0, 1)]  // Minor diff
        [TestCase(1, 0, 1, 1, 0, 0, 1)]  // Patch diff
        [TestCase(1, 0, 0, 1, 1, 0, -1)] // v1 < v2
        public void SchemaVersion_CompareTo_ReturnsExpectedResult(
            int m1, int n1, int p1, int m2, int n2, int p2, int expected)
        {
            // Arrange
            var v1 = new SchemaVersion(m1, n1, p1);
            var v2 = new SchemaVersion(m2, n2, p2);

            // Act
            var result = v1.CompareTo(v2);

            // Assert
            Assert.That(Math.Sign(result), Is.EqualTo(expected));
        }

        [Test]
        public void SchemaVersion_ComparisonOperators_WorkCorrectly()
        {
            // Arrange
            var v1_0_0 = new SchemaVersion(1, 0, 0);
            var v1_1_0 = new SchemaVersion(1, 1, 0);
            var v2_0_0 = new SchemaVersion(2, 0, 0);

            // Assert
            Assert.That(v1_0_0 < v1_1_0, Is.True);
            Assert.That(v1_1_0 < v2_0_0, Is.True);
            Assert.That(v2_0_0 > v1_1_0, Is.True);
            var v1_0_0_copy = new SchemaVersion(1, 0, 0);
            Assert.That(v1_0_0 <= v1_0_0_copy, Is.True);
            Assert.That(v1_1_0 >= v1_0_0, Is.True);
        }

        [Test]
        public void SchemaVersion_IsBackwardCompatibleWith_SameMajor_ReturnsTrue()
        {
            // Arrange
            var current = new SchemaVersion(1, 1, 0);
            var older = new SchemaVersion(1, 0, 0);

            // Act & Assert
            Assert.That(current.IsBackwardCompatibleWith(older), Is.True);
            Assert.That(older.IsBackwardCompatibleWith(current), Is.False); // Older cannot read newer
        }

        [Test]
        public void SchemaVersion_IsBackwardCompatibleWith_DifferentMajor_ReturnsFalse()
        {
            // Arrange
            var v1 = new SchemaVersion(1, 0, 0);
            var v2 = new SchemaVersion(2, 0, 0);

            // Act & Assert
            Assert.That(v1.IsBackwardCompatibleWith(v2), Is.False);
            Assert.That(v2.IsBackwardCompatibleWith(v1), Is.False);
        }

        [Test]
        public void SchemaVersion_RequiresMigrationFrom_SameVersion_ReturnsFalse()
        {
            // Arrange
            var v1 = new SchemaVersion(1, 0, 0);
            var v2 = new SchemaVersion(1, 0, 0);

            // Act & Assert
            Assert.That(v1.RequiresMigrationFrom(v2), Is.False);
        }

        [Test]
        public void SchemaVersion_RequiresMigrationFrom_CompatibleDifferentVersion_ReturnsTrue()
        {
            // Arrange
            var current = new SchemaVersion(1, 1, 0);
            var older = new SchemaVersion(1, 0, 0);

            // Act & Assert
            Assert.That(current.RequiresMigrationFrom(older), Is.True);
        }

        [Test]
        public void SchemaVersion_Current_IsLatestVersion()
        {
            // Assert
            Assert.That(SchemaVersion.Current, Is.EqualTo(SchemaVersion.V1_2_0));
        }

        #endregion

        #region SchemaVersionManager Tests

        [Test]
        public void SchemaVersionManager_DetectVersion_NoVersionField_ReturnsV1_0_0()
        {
            // Arrange
            var json = @"{""dialogueText"": ""Hello""}";

            // Act
            var version = SchemaVersionManager.DetectVersion(json);

            // Assert
            Assert.That(version, Is.EqualTo(SchemaVersion.V1_0_0));
        }

        [Test]
        public void SchemaVersionManager_DetectVersion_WithVersionField_ReturnsDetectedVersion()
        {
            // Arrange
            var json = @"{""schemaVersion"": ""1.1.0"", ""dialogueText"": ""Hello""}";

            // Act
            var version = SchemaVersionManager.DetectVersion(json);

            // Assert
            Assert.That(version, Is.EqualTo(SchemaVersion.V1_1_0));
        }

        [Test]
        public void SchemaVersionManager_DetectVersion_InvalidJson_ReturnsV1_0_0()
        {
            // Arrange
            var json = "not valid json";

            // Act
            var version = SchemaVersionManager.DetectVersion(json);

            // Assert
            Assert.That(version, Is.EqualTo(SchemaVersion.V1_0_0));
        }

        [Test]
        public void SchemaVersionManager_DetectVersion_NullOrEmpty_ReturnsV1_0_0()
        {
            // Act & Assert
            Assert.That(SchemaVersionManager.DetectVersion(null!), Is.EqualTo(SchemaVersion.V1_0_0));
            Assert.That(SchemaVersionManager.DetectVersion(""), Is.EqualTo(SchemaVersion.V1_0_0));
            Assert.That(SchemaVersionManager.DetectVersion("  "), Is.EqualTo(SchemaVersion.V1_0_0));
        }

        [Test]
        public void SchemaVersionManager_CanRead_CompatibleVersion_ReturnsTrue()
        {
            // Arrange
            var legacyVersion = SchemaVersion.V1_0_0;

            // Act
            var canRead = SchemaVersionManager.CanRead(legacyVersion);

            // Assert
            Assert.That(canRead, Is.True);
        }

        [Test]
        public void SchemaVersionManager_CanRead_IncompatibleVersion_ReturnsFalse()
        {
            // Arrange
            var futureVersion = new SchemaVersion(2, 0, 0);

            // Act
            var canRead = SchemaVersionManager.CanRead(futureVersion);

            // Assert
            Assert.That(canRead, Is.False);
        }

        [Test]
        public void SchemaVersionManager_GetVersionNotes_KnownVersion_ReturnsNotes()
        {
            // Act
            var notes = SchemaVersionManager.GetVersionNotes(SchemaVersion.V1_0_0);

            // Assert
            Assert.That(notes, Is.Not.Null);
            Assert.That(notes, Does.Contain("dialogueText"));
        }

        [Test]
        public void SchemaVersionManager_GetVersionNotes_UnknownVersion_ReturnsNull()
        {
            // Arrange
            var unknownVersion = new SchemaVersion(99, 0, 0);

            // Act
            var notes = SchemaVersionManager.GetVersionNotes(unknownVersion);

            // Assert
            Assert.That(notes, Is.Null);
        }

        [Test]
        public void SchemaVersionManager_GetAllVersions_ReturnsOrderedVersions()
        {
            // Act
            var versions = SchemaVersionManager.GetAllVersions();

            // Assert
            Assert.That(versions, Is.Not.Empty);
            Assert.That(versions, Does.Contain(SchemaVersion.V1_0_0));
            Assert.That(versions, Does.Contain(SchemaVersion.V1_1_0));

            // Verify ordering
            for (int i = 1; i < versions.Count; i++)
            {
                Assert.That(versions[i - 1] < versions[i], Is.True);
            }
        }

        [Test]
        public void SchemaVersionManager_AddVersionToSchema_AddsVersionProperty()
        {
            // Arrange
            var schema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""dialogueText"": {
      ""type"": ""string""
    }
  }
}";

            // Act
            var versionedSchema = SchemaVersionManager.AddVersionToSchema(schema);

            // Assert
            Assert.That(versionedSchema, Does.Contain("schemaVersion"));
            Assert.That(versionedSchema, Does.Contain(SchemaVersion.Current.ToString()));
        }

        [Test]
        public void SchemaVersionManager_AddVersionToSchema_AlreadyHasVersion_ReturnsUnchanged()
        {
            // Arrange
            var schema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""schemaVersion"": {
      ""type"": ""string""
    },
    ""dialogueText"": {
      ""type"": ""string""
    }
  }
}";

            // Act
            var result = SchemaVersionManager.AddVersionToSchema(schema);

            // Assert
            Assert.That(result, Is.EqualTo(schema));
        }

        [Test]
        public void SchemaVersionManager_AddVersionToSchema_InvalidSchema_ReturnsOriginal()
        {
            // Arrange
            var invalidSchema = "not valid json";

            // Act
            var result = SchemaVersionManager.AddVersionToSchema(invalidSchema);

            // Assert
            Assert.That(result, Is.EqualTo(invalidSchema));
        }

        [Test]
        public void SchemaVersionManager_AddVersionToSchema_NullOrEmpty_ReturnsOriginal()
        {
            // Assert
            Assert.That(SchemaVersionManager.AddVersionToSchema(null!), Is.Null);
            Assert.That(SchemaVersionManager.AddVersionToSchema(""), Is.EqualTo(""));
        }

        #endregion

        #region JsonSchemaBuilder Version Integration Tests

        [Test]
        public void JsonSchemaBuilder_VersionedParsedOutputSchema_ContainsVersion()
        {
            // Act
            var schema = JsonSchemaBuilder.VersionedParsedOutputSchema;

            // Assert
            Assert.That(schema, Does.Contain("schemaVersion"));
            Assert.That(schema, Does.Contain(SchemaVersion.Current.ToString()));
        }

        [Test]
        public void JsonSchemaBuilder_BuildParsedOutputSchema_WithVersion_IncludesVersion()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildParsedOutputSchema(includeVersion: true);

            // Assert
            Assert.That(schema, Does.Contain("schemaVersion"));
        }

        [Test]
        public void JsonSchemaBuilder_BuildParsedOutputSchema_WithoutVersion_ExcludesVersion()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildParsedOutputSchema(includeVersion: false);

            // Assert
            Assert.That(schema, Does.Not.Contain("schemaVersion"));
        }

        [Test]
        public void JsonSchemaBuilder_GetCurrentVersion_ReturnsCurrentVersion()
        {
            // Act
            var version = JsonSchemaBuilder.GetCurrentVersion();

            // Assert
            Assert.That(version, Is.EqualTo(SchemaVersion.Current));
        }

        [Test]
        public void JsonSchemaBuilder_DetectSchemaVersion_DelegatesToManager()
        {
            // Arrange
            var json = @"{""schemaVersion"": ""1.0.0"", ""dialogueText"": ""Test""}";

            // Act
            var version = JsonSchemaBuilder.DetectSchemaVersion(json);

            // Assert
            Assert.That(version, Is.EqualTo(SchemaVersion.V1_0_0));
        }

        [Test]
        public void JsonSchemaBuilder_CanReadResponse_CompatibleResponse_ReturnsTrue()
        {
            // Arrange
            var json = @"{""dialogueText"": ""Hello""}"; // Legacy format without version

            // Act
            var canRead = JsonSchemaBuilder.CanReadResponse(json);

            // Assert
            Assert.That(canRead, Is.True);
        }

        [Test]
        public void JsonSchemaBuilder_BuildVersionedFromType_IncludesVersion()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildVersionedFromType<TestType>(includeVersion: true);

            // Assert
            Assert.That(schema, Does.Contain("schemaVersion"));
        }

        [Test]
        public void JsonSchemaBuilder_BuildVersionedFromType_ExcludesVersion_WhenDisabled()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildVersionedFromType<TestType>(includeVersion: false);

            // Assert
            Assert.That(schema, Does.Not.Contain("schemaVersion"));
        }

        // Test type for BuildVersionedFromType
        private class TestType
        {
            public string Name { get; set; } = "";
            public int Value { get; set; }
        }

        #endregion
    }
}
