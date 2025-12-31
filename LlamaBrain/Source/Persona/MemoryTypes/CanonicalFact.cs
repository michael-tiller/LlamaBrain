using System;
using System.Collections.Generic;

namespace LlamaBrain.Persona.MemoryTypes
{
  /// <summary>
  /// Immutable world truths defined by game designers.
  /// Cannot be modified or contradicted by any runtime source.
  /// Examples: "The king's name is Arthur", "Dragons breathe fire"
  /// </summary>
  [Serializable]
  public class CanonicalFact : MemoryEntry
  {
    /// <summary>
    /// The fact content.
    /// </summary>
    public string Fact { get; }

    /// <summary>
    /// Alias for Id - the unique identifier for this canonical fact.
    /// </summary>
    public string FactId => Id ?? "";

    /// <summary>
    /// Keywords that, if found in output, indicate a potential contradiction.
    /// Used by the validation gate to detect violations.
    /// </summary>
    public List<string>? ContradictionKeywords { get; set; }

    /// <summary>
    /// Optional domain/category for the fact (e.g., "world", "character", "lore").
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Canonical facts have the highest authority.
    /// </summary>
    public override MemoryAuthority Authority => MemoryAuthority.Canonical;

    /// <summary>
    /// Returns the fact content.
    /// </summary>
    public override string Content => Fact;

    /// <summary>
    /// Creates a new canonical fact.
    /// </summary>
    /// <param name="fact">The immutable fact content.</param>
    /// <param name="domain">Optional domain/category.</param>
    /// <exception cref="ArgumentNullException">Thrown when fact is null</exception>
    public CanonicalFact(string fact, string? domain = null)
    {
      Fact = fact ?? throw new ArgumentNullException(nameof(fact));
      Domain = domain;
      Source = MutationSource.Designer; // Only designers can create canonical facts
    }

    /// <summary>
    /// Creates a canonical fact with a specific ID.
    /// </summary>
    /// <param name="id">The unique identifier for the fact</param>
    /// <param name="fact">The immutable fact content</param>
    /// <param name="domain">Optional domain/category</param>
    /// <returns>A new CanonicalFact with the specified ID</returns>
    public static CanonicalFact Create(string id, string fact, string? domain = null)
    {
      return new CanonicalFact(fact, domain) { Id = id };
    }
  }
}
