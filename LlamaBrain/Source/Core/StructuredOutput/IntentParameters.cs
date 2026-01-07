using System;
using System.Collections.Generic;

namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// Typed parameters for give_item intent.
    /// </summary>
    [Serializable]
    public sealed class GiveItemParameters
    {
        /// <summary>
        /// The ID of the item to give.
        /// </summary>
        public string ItemId { get; set; } = "";

        /// <summary>
        /// The quantity to give (default: 1).
        /// </summary>
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// The condition of the item (e.g., "new", "worn", "damaged").
        /// </summary>
        public string? Condition { get; set; }
    }

    /// <summary>
    /// Typed parameters for move_to intent.
    /// </summary>
    [Serializable]
    public sealed class MoveToParameters
    {
        /// <summary>
        /// The destination location.
        /// </summary>
        public string Location { get; set; } = "";

        /// <summary>
        /// Movement speed (e.g., "walk", "run", "sneak"). Default: "walk".
        /// </summary>
        public string Speed { get; set; } = "walk";

        /// <summary>
        /// Path type (e.g., "direct", "pathfind", "follow_road"). Default: "pathfind".
        /// </summary>
        public string PathType { get; set; } = "pathfind";
    }

    /// <summary>
    /// Typed parameters for interact intent.
    /// </summary>
    [Serializable]
    public sealed class InteractParameters
    {
        /// <summary>
        /// The target entity to interact with.
        /// </summary>
        public string TargetEntity { get; set; } = "";

        /// <summary>
        /// The type of interaction (e.g., "talk", "trade", "attack").
        /// </summary>
        public string InteractionType { get; set; } = "";

        /// <summary>
        /// Duration of the interaction in seconds (optional).
        /// </summary>
        public float? Duration { get; set; }
    }

    /// <summary>
    /// Extension methods for extracting typed parameters from intent parameter dictionaries.
    /// </summary>
    public static class IntentParameterExtensions
    {
        /// <summary>
        /// Extracts GiveItemParameters from a parameter dictionary.
        /// </summary>
        /// <param name="parameters">The parameters dictionary.</param>
        /// <returns>Typed parameters, or null if required fields are missing.</returns>
        public static GiveItemParameters? GetGiveItemParameters(Dictionary<string, object>? parameters)
        {
            if (parameters == null)
                return null;

            var itemId = GetValue<string>(parameters, "itemId");
            if (string.IsNullOrEmpty(itemId))
                return null;

            return new GiveItemParameters
            {
                ItemId = itemId,
                Quantity = GetValue(parameters, "quantity", 1),
                Condition = GetValue<string>(parameters, "condition")
            };
        }

        /// <summary>
        /// Extracts MoveToParameters from a parameter dictionary.
        /// </summary>
        /// <param name="parameters">The parameters dictionary.</param>
        /// <returns>Typed parameters, or null if required fields are missing.</returns>
        public static MoveToParameters? GetMoveToParameters(Dictionary<string, object>? parameters)
        {
            if (parameters == null)
                return null;

            var location = GetValue<string>(parameters, "location");
            if (string.IsNullOrEmpty(location))
                return null;

            return new MoveToParameters
            {
                Location = location,
                Speed = GetValue(parameters, "speed", "walk"),
                PathType = GetValue(parameters, "pathType", "pathfind")
            };
        }

        /// <summary>
        /// Extracts InteractParameters from a parameter dictionary.
        /// </summary>
        /// <param name="parameters">The parameters dictionary.</param>
        /// <returns>Typed parameters, or null if required fields are missing.</returns>
        public static InteractParameters? GetInteractParameters(Dictionary<string, object>? parameters)
        {
            if (parameters == null)
                return null;

            var targetEntity = GetValue<string>(parameters, "targetEntity");
            if (string.IsNullOrEmpty(targetEntity))
                return null;

            return new InteractParameters
            {
                TargetEntity = targetEntity,
                InteractionType = GetValue(parameters, "interactionType", ""),
                Duration = GetValue<float?>(parameters, "duration")
            };
        }

        /// <summary>
        /// Gets a typed value from a parameter dictionary.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="parameters">The parameters dictionary.</param>
        /// <param name="key">The key to look up.</param>
        /// <param name="defaultValue">Default value if key is missing or conversion fails.</param>
        /// <returns>The converted value or default.</returns>
        public static T GetValue<T>(Dictionary<string, object>? parameters, string key, T defaultValue = default!)
        {
            if (parameters == null || !parameters.TryGetValue(key, out var value) || value == null)
                return defaultValue;

            // Direct type match
            if (value is T typedValue)
                return typedValue;

            // Try conversion
            try
            {
                var targetType = typeof(T);
                var underlyingType = Nullable.GetUnderlyingType(targetType);

                if (underlyingType != null)
                {
                    // Handle nullable types
                    return (T)Convert.ChangeType(value, underlyingType);
                }

                return (T)Convert.ChangeType(value, targetType);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Gets an array value from a parameter dictionary.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="parameters">The parameters dictionary.</param>
        /// <param name="key">The key to look up.</param>
        /// <returns>The array or null if not found.</returns>
        public static T[]? GetArray<T>(Dictionary<string, object>? parameters, string key)
        {
            if (parameters == null || !parameters.TryGetValue(key, out var value) || value == null)
                return null;

            if (value is T[] typedArray)
                return typedArray;

            if (value is object[] objArray)
            {
                try
                {
                    var result = new T[objArray.Length];
                    for (int i = 0; i < objArray.Length; i++)
                    {
                        result[i] = (T)Convert.ChangeType(objArray[i], typeof(T));
                    }
                    return result;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a nested dictionary from a parameter dictionary.
        /// </summary>
        /// <param name="parameters">The parameters dictionary.</param>
        /// <param name="key">The key to look up.</param>
        /// <returns>The nested dictionary or null if not found.</returns>
        public static Dictionary<string, object>? GetNested(Dictionary<string, object>? parameters, string key)
        {
            if (parameters == null || !parameters.TryGetValue(key, out var value) || value == null)
                return null;

            return value as Dictionary<string, object>;
        }
    }
}
