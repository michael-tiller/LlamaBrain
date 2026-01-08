using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using LlamaBrain.Utilities;

namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// Schema version identifier for backward compatibility tracking.
    /// </summary>
    public readonly struct SchemaVersion : IEquatable<SchemaVersion>, IComparable<SchemaVersion>
    {
        /// <summary>
        /// Major version - breaking changes to schema structure.
        /// </summary>
        public int Major { get; }

        /// <summary>
        /// Minor version - backward-compatible additions.
        /// </summary>
        public int Minor { get; }

        /// <summary>
        /// Patch version - backward-compatible fixes.
        /// </summary>
        public int Patch { get; }

        /// <summary>
        /// Current schema version for ParsedOutput.
        /// </summary>
        public static readonly SchemaVersion Current = new SchemaVersion(1, 2, 0);

        /// <summary>
        /// Initial schema version (v1.0.0).
        /// </summary>
        public static readonly SchemaVersion V1_0_0 = new SchemaVersion(1, 0, 0);

        /// <summary>
        /// Version 1.1.0 - Added schemaVersion field and function call enhancements.
        /// </summary>
        public static readonly SchemaVersion V1_1_0 = new SchemaVersion(1, 1, 0);

        /// <summary>
        /// Version 1.2.0 - Added complex intent parameters, relationships, partial context support.
        /// F13.3: Parameters changed from Dictionary&lt;string, string&gt; to Dictionary&lt;string, object&gt;
        /// F23.2: Added RelationshipEntry with affinity/trust/familiarity, authority validation
        /// F23.5: Made context sections nullable for partial context support
        /// </summary>
        public static readonly SchemaVersion V1_2_0 = new SchemaVersion(1, 2, 0);

        /// <summary>
        /// Creates a new schema version.
        /// </summary>
        /// <param name="major">Major version number</param>
        /// <param name="minor">Minor version number</param>
        /// <param name="patch">Patch version number</param>
        public SchemaVersion(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        /// <summary>
        /// Parses a version string like "1.0.0" into a SchemaVersion.
        /// </summary>
        /// <param name="versionString">Version string in format "major.minor.patch"</param>
        /// <returns>Parsed SchemaVersion</returns>
        /// <exception cref="FormatException">If the version string is invalid</exception>
        public static SchemaVersion Parse(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
            {
                throw new FormatException("Version string cannot be null or empty");
            }

            var parts = versionString.Split('.');
            if (parts.Length != 3)
            {
                throw new FormatException($"Version string must be in format 'major.minor.patch', got: {versionString}");
            }

            if (!int.TryParse(parts[0], out var major) ||
                !int.TryParse(parts[1], out var minor) ||
                !int.TryParse(parts[2], out var patch))
            {
                throw new FormatException($"Version components must be integers: {versionString}");
            }

            return new SchemaVersion(major, minor, patch);
        }

        /// <summary>
        /// Tries to parse a version string.
        /// </summary>
        /// <param name="versionString">Version string to parse</param>
        /// <param name="version">Parsed version if successful</param>
        /// <returns>True if parsing succeeded</returns>
        public static bool TryParse(string versionString, out SchemaVersion version)
        {
            try
            {
                version = Parse(versionString);
                return true;
            }
            catch (FormatException)
            {
                version = default;
                return false;
            }
        }

        /// <summary>
        /// Checks if this version is compatible with another version.
        /// Backward compatibility: same major version, this version >= other version.
        /// </summary>
        /// <param name="other">Version to check compatibility with</param>
        /// <returns>True if this version can read data from the other version</returns>
        public bool IsBackwardCompatibleWith(SchemaVersion other)
        {
            // Same major version means backward compatible
            // Current version must be >= other version to read its data
            return Major == other.Major && CompareTo(other) >= 0;
        }

        /// <summary>
        /// Checks if this version requires migration from another version.
        /// </summary>
        /// <param name="other">Version to compare</param>
        /// <returns>True if migration may be needed</returns>
        public bool RequiresMigrationFrom(SchemaVersion other)
        {
            // Migration needed if versions differ but are compatible
            return IsBackwardCompatibleWith(other) && !Equals(other);
        }

        /// <summary>
        /// Returns a string representation of the schema version.
        /// </summary>
        /// <returns>A version string in the format "major.minor.patch".</returns>
        public override string ToString() => $"{Major}.{Minor}.{Patch}";

        /// <summary>
        /// Determines whether this instance equals another SchemaVersion.
        /// </summary>
        /// <param name="other">The SchemaVersion to compare with this instance.</param>
        /// <returns>True if the versions are equal; otherwise, false.</returns>
        public bool Equals(SchemaVersion other)
        {
            return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
        }

        /// <summary>
        /// Determines whether this instance equals the specified object.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns>True if obj is a SchemaVersion and equal to this instance; otherwise, false.</returns>
        public override bool Equals(object? obj)
        {
            return obj is SchemaVersion other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A hash code for the current SchemaVersion.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, Patch);
        }

        /// <summary>
        /// Compares this instance with another SchemaVersion.
        /// </summary>
        /// <param name="other">The SchemaVersion to compare with this instance.</param>
        /// <returns>A value indicating the relative order of the versions being compared.</returns>
        public int CompareTo(SchemaVersion other)
        {
            var majorCompare = Major.CompareTo(other.Major);
            if (majorCompare != 0) return majorCompare;

            var minorCompare = Minor.CompareTo(other.Minor);
            if (minorCompare != 0) return minorCompare;

            return Patch.CompareTo(other.Patch);
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="left">The first SchemaVersion to compare.</param>
        /// <param name="right">The second SchemaVersion to compare.</param>
        /// <returns>True if the versions are equal; otherwise, false.</returns>
        public static bool operator ==(SchemaVersion left, SchemaVersion right) => left.Equals(right);

        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="left">The first SchemaVersion to compare.</param>
        /// <param name="right">The second SchemaVersion to compare.</param>
        /// <returns>True if the versions are not equal; otherwise, false.</returns>
        public static bool operator !=(SchemaVersion left, SchemaVersion right) => !left.Equals(right);

        /// <summary>
        /// Less than operator.
        /// </summary>
        /// <param name="left">The first SchemaVersion to compare.</param>
        /// <param name="right">The second SchemaVersion to compare.</param>
        /// <returns>True if left is less than right; otherwise, false.</returns>
        public static bool operator <(SchemaVersion left, SchemaVersion right) => left.CompareTo(right) < 0;

        /// <summary>
        /// Greater than operator.
        /// </summary>
        /// <param name="left">The first SchemaVersion to compare.</param>
        /// <param name="right">The second SchemaVersion to compare.</param>
        /// <returns>True if left is greater than right; otherwise, false.</returns>
        public static bool operator >(SchemaVersion left, SchemaVersion right) => left.CompareTo(right) > 0;

        /// <summary>
        /// Less than or equal operator.
        /// </summary>
        /// <param name="left">The first SchemaVersion to compare.</param>
        /// <param name="right">The second SchemaVersion to compare.</param>
        /// <returns>True if left is less than or equal to right; otherwise, false.</returns>
        public static bool operator <=(SchemaVersion left, SchemaVersion right) => left.CompareTo(right) <= 0;

        /// <summary>
        /// Greater than or equal operator.
        /// </summary>
        /// <param name="left">The first SchemaVersion to compare.</param>
        /// <param name="right">The second SchemaVersion to compare.</param>
        /// <returns>True if left is greater than or equal to right; otherwise, false.</returns>
        public static bool operator >=(SchemaVersion left, SchemaVersion right) => left.CompareTo(right) >= 0;
    }

    /// <summary>
    /// Manages schema versioning for structured output schemas.
    /// Provides version detection, migration support, and backward compatibility checking.
    /// </summary>
    public static class SchemaVersionManager
    {
        /// <summary>
        /// Version history with migration notes.
        /// </summary>
        private static readonly Dictionary<SchemaVersion, string> VersionHistory = new Dictionary<SchemaVersion, string>
        {
            { SchemaVersion.V1_0_0, "Initial schema version with dialogueText, proposedMutations, worldIntents, functionCalls" },
            { SchemaVersion.V1_1_0, "Added schemaVersion field for version tracking" },
            { SchemaVersion.V1_2_0, "F13.3: Complex intent parameters (Dictionary<string, object>). F23.2: RelationshipEntry with affinity/trust/familiarity, authority validation. F23.5: Nullable context sections for partial context support." }
        };

        /// <summary>
        /// Detects the schema version from a JSON response.
        /// Returns V1_0_0 if no version field is present (legacy format).
        /// </summary>
        /// <param name="json">JSON string to check</param>
        /// <returns>Detected schema version</returns>
        public static SchemaVersion DetectVersion(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return SchemaVersion.V1_0_0;
            }

            try
            {
                var jObject = JObject.Parse(json);
                var versionToken = jObject["schemaVersion"];

                if (versionToken == null)
                {
                    // No version field = legacy v1.0.0
                    return SchemaVersion.V1_0_0;
                }

                var versionString = versionToken.ToString();
                if (SchemaVersion.TryParse(versionString, out var version))
                {
                    return version;
                }

                // Invalid version format, assume v1.0.0
                Logger.Warn($"[SchemaVersionManager] Invalid schema version format: {versionString}, defaulting to v1.0.0");
                return SchemaVersion.V1_0_0;
            }
            catch (Exception)
            {
                // Not valid JSON, assume v1.0.0
                return SchemaVersion.V1_0_0;
            }
        }

        /// <summary>
        /// Checks if the current schema version can read data from the detected version.
        /// </summary>
        /// <param name="detectedVersion">Version detected from the data</param>
        /// <returns>True if the data can be read</returns>
        public static bool CanRead(SchemaVersion detectedVersion)
        {
            return SchemaVersion.Current.IsBackwardCompatibleWith(detectedVersion);
        }

        /// <summary>
        /// Gets the migration notes for a version.
        /// </summary>
        /// <param name="version">Version to get notes for</param>
        /// <returns>Migration notes or null if not found</returns>
        public static string? GetVersionNotes(SchemaVersion version)
        {
            return VersionHistory.TryGetValue(version, out var notes) ? notes : null;
        }

        /// <summary>
        /// Gets all known schema versions in order.
        /// </summary>
        /// <returns>List of schema versions</returns>
        public static IReadOnlyList<SchemaVersion> GetAllVersions()
        {
            return VersionHistory.Keys.OrderBy(v => v).ToList();
        }

        /// <summary>
        /// Adds the schemaVersion field to a JSON schema string.
        /// </summary>
        /// <param name="schema">Original schema without version</param>
        /// <returns>Schema with schemaVersion field added</returns>
        public static string AddVersionToSchema(string schema)
        {
            if (string.IsNullOrWhiteSpace(schema))
            {
                return schema;
            }

            try
            {
                var jObject = JObject.Parse(schema);
                var properties = jObject["properties"] as JObject;

                if (properties == null)
                {
                    return schema; // Not a valid object schema
                }

                // Check if schemaVersion already exists
                if (properties.ContainsKey("schemaVersion"))
                {
                    return schema;
                }

                // Add schemaVersion as first property
                var versionProperty = new JObject
                {
                    ["type"] = "string",
                    ["description"] = $"Schema version for backward compatibility. Current: {SchemaVersion.Current}",
                    ["const"] = SchemaVersion.Current.ToString()
                };

                // Create new properties with schemaVersion first
                var newProperties = new JObject { ["schemaVersion"] = versionProperty };
                foreach (var prop in properties)
                {
                    newProperties[prop.Key] = prop.Value;
                }
                jObject["properties"] = newProperties;

                return jObject.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SchemaVersionManager] Failed to add version to schema: {ex.Message}");
                return schema;
            }
        }
    }
}
