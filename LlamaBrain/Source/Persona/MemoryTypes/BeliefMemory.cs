using System;

namespace LlamaBrain.Persona.MemoryTypes
{
  /// <summary>
  /// Type of belief or relationship memory.
  /// </summary>
  public enum BeliefType
  {
    /// <summary>Opinion about a person/entity.</summary>
    Opinion,
    /// <summary>Relationship status with someone.</summary>
    Relationship,
    /// <summary>Belief about a fact (may be wrong).</summary>
    Belief,
    /// <summary>Assumption made by the NPC.</summary>
    Assumption,
    /// <summary>Preference or like/dislike.</summary>
    Preference
  }

  /// <summary>
  /// NPC beliefs and relationship memories.
  /// These can be wrong and can change based on interactions.
  /// Examples: "I think the player is friendly", "I believe the treasure is in the cave"
  /// </summary>
  [Serializable]
  public class BeliefMemoryEntry : MemoryEntry
  {
    /// <summary>
    /// The subject of the belief (who/what it's about).
    /// </summary>
    public string Subject { get; }

    /// <summary>
    /// The belief content.
    /// </summary>
    public string BeliefContent { get; private set; }

    /// <summary>
    /// The type of belief.
    /// </summary>
    public BeliefType BeliefType { get; set; } = BeliefType.Opinion;

    /// <summary>
    /// Confidence level in this belief (0.0 = uncertain, 1.0 = certain).
    /// </summary>
    public float Confidence { get; set; } = 0.5f;

    /// <summary>
    /// Sentiment value (-1.0 = very negative, 0 = neutral, 1.0 = very positive).
    /// Used for relationship/opinion memories.
    /// </summary>
    public float Sentiment { get; set; } = 0f;

    /// <summary>
    /// Whether this belief has been contradicted by evidence.
    /// </summary>
    public bool IsContradicted { get; private set; }

    /// <summary>
    /// Evidence or reason for this belief.
    /// </summary>
    public string? Evidence { get; set; }

    /// <summary>
    /// Beliefs have the lowest authority (can be wrong).
    /// </summary>
    public override MemoryAuthority Authority => MemoryAuthority.Belief;

    /// <summary>
    /// Returns a formatted belief statement.
    /// </summary>
    public override string Content
    {
      get
      {
        string prefix;
        if (Confidence >= 0.8f)
          prefix = "I know that";
        else if (Confidence >= 0.5f)
          prefix = "I believe that";
        else if (Confidence >= 0.3f)
          prefix = "I think that";
        else
          prefix = "I'm not sure, but";
        
        return $"{prefix} {BeliefContent}";
      }
    }

    /// <summary>
    /// Creates a new belief memory entry.
    /// </summary>
    /// <param name="subject">Who/what the belief is about.</param>
    /// <param name="belief">The belief content.</param>
    /// <param name="beliefType">The type of belief.</param>
    /// <exception cref="ArgumentNullException">Thrown when subject or belief is null</exception>
    public BeliefMemoryEntry(string subject, string belief, BeliefType beliefType = BeliefType.Opinion)
    {
      Subject = subject ?? throw new ArgumentNullException(nameof(subject));
      BeliefContent = belief ?? throw new ArgumentNullException(nameof(belief));
      BeliefType = beliefType;
      Source = MutationSource.ValidatedOutput;
    }

    /// <summary>
    /// Updates this belief based on new evidence.
    /// </summary>
    /// <param name="newBelief">The updated belief content.</param>
    /// <param name="newConfidence">Updated confidence level.</param>
    /// <param name="evidence">Evidence for the change.</param>
    /// <param name="source">Source of the update.</param>
    /// <returns>A MutationResult indicating whether the update was successful</returns>
    public MutationResult UpdateBelief(string newBelief, float newConfidence, string? evidence, MutationSource source)
    {
      // Beliefs can be updated by ValidatedOutput or higher
      if (source < MutationSource.ValidatedOutput)
      {
        return MutationResult.Failed($"Source '{source}' lacks authority to modify beliefs. Required: ValidatedOutput or higher.");
      }

      BeliefContent = newBelief;
      Confidence = Math.Clamp(newConfidence, 0f, 1f);
      if (evidence != null) Evidence = evidence;

      return MutationResult.Succeeded(this);
    }

    /// <summary>
    /// Marks this belief as contradicted by higher-authority information.
    /// </summary>
    /// <param name="contradictingEvidence">What contradicted this belief.</param>
    public void MarkContradicted(string contradictingEvidence)
    {
      IsContradicted = true;
      Confidence = Math.Min(Confidence, 0.2f); // Lower confidence significantly
      Evidence = $"[CONTRADICTED] {contradictingEvidence}. Previous: {Evidence}";
    }

    /// <summary>
    /// Adjusts sentiment based on an interaction.
    /// </summary>
    /// <param name="delta">How much to adjust (-1 to 1).</param>
    public void AdjustSentiment(float delta)
    {
      Sentiment = Math.Clamp(Sentiment + delta, -1f, 1f);
    }

    /// <summary>
    /// Creates a relationship belief about someone.
    /// </summary>
    /// <param name="subject">Who the relationship is about</param>
    /// <param name="relationship">The relationship description</param>
    /// <param name="sentiment">The sentiment value (-1.0 to 1.0)</param>
    /// <returns>A new BeliefMemoryEntry representing a relationship</returns>
    public static BeliefMemoryEntry CreateRelationship(string subject, string relationship, float sentiment = 0f)
    {
      return new BeliefMemoryEntry(subject, relationship, BeliefType.Relationship)
      {
        Sentiment = sentiment,
        Confidence = 0.7f
      };
    }

    /// <summary>
    /// Creates an opinion about something.
    /// </summary>
    /// <param name="subject">What the opinion is about</param>
    /// <param name="opinion">The opinion content</param>
    /// <param name="sentiment">The sentiment value (-1.0 to 1.0)</param>
    /// <param name="confidence">The confidence level (0.0 to 1.0)</param>
    /// <returns>A new BeliefMemoryEntry representing an opinion</returns>
    public static BeliefMemoryEntry CreateOpinion(string subject, string opinion, float sentiment = 0f, float confidence = 0.5f)
    {
      return new BeliefMemoryEntry(subject, opinion, BeliefType.Opinion)
      {
        Sentiment = sentiment,
        Confidence = confidence
      };
    }

    /// <summary>
    /// Creates a belief about a fact (may be incorrect).
    /// </summary>
    /// <param name="subject">What the belief is about</param>
    /// <param name="belief">The belief content</param>
    /// <param name="confidence">The confidence level (0.0 to 1.0)</param>
    /// <param name="evidence">Optional evidence for the belief</param>
    /// <returns>A new BeliefMemoryEntry representing a belief</returns>
    public static BeliefMemoryEntry CreateBelief(string subject, string belief, float confidence = 0.5f, string? evidence = null)
    {
      return new BeliefMemoryEntry(subject, belief, BeliefType.Belief)
      {
        Confidence = confidence,
        Evidence = evidence
      };
    }
  }
}
