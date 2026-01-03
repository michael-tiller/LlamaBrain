using LlamaBrain.Core.Inference;
using LlamaBrain.Core.StructuredInput.Schemas;

namespace LlamaBrain.Core.StructuredInput
{
    /// <summary>
    /// Interface for providers that build structured context from state snapshots.
    /// Mirrors IStructuredOutputProvider pattern for consistency.
    /// </summary>
    public interface IStructuredContextProvider
    {
        /// <summary>
        /// Checks whether this provider supports the specified format.
        /// </summary>
        /// <param name="format">The structured context format to check.</param>
        /// <returns>True if the format is supported, false otherwise.</returns>
        bool SupportsFormat(StructuredContextFormat format);

        /// <summary>
        /// Builds structured context from a state snapshot.
        /// </summary>
        /// <param name="snapshot">The state snapshot containing context data.</param>
        /// <returns>A structured context schema ready for serialization.</returns>
        ContextJsonSchema BuildContext(StateSnapshot snapshot);

        /// <summary>
        /// Validates that a context schema is well-formed.
        /// </summary>
        /// <param name="context">The context schema to validate.</param>
        /// <param name="error">The error message if validation fails, null otherwise.</param>
        /// <returns>True if the context is valid, false otherwise.</returns>
        bool ValidateContext(ContextJsonSchema context, out string? error);
    }
}
