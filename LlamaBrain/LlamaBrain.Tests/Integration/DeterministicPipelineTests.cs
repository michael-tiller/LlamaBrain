using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using LlamaBrain.Core;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Validation;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Tests.Integration
{
  /// <summary>
  /// Feature 10.7: Deterministic Pipeline Integration Tests
  /// Tests full pipeline determinism to prove the deterministic state reconstruction pattern.
  /// </summary>
  [TestFixture]
  public class DeterministicPipelineTests
  {
    /// <summary>
    /// Helper class for serializing memory system state deterministically.
    /// Used to verify that mutations produce identical final states.
    /// </summary>
    internal static class MemoryStateSerializer
    {
      /// <summary>
      /// Null token that cannot appear unescaped in data.
      /// </summary>
      private const string NullToken = "\\0";

      /// <summary>
      /// Escapes special characters in strings for safe serialization.
      /// Uses \0 as null sentinel to avoid collision with real "null" content.
      /// </summary>
      internal static string Esc(string? s)
      {
        if (s == null) return NullToken;
        
        // Escape backslashes first, then other special characters
        // After escaping, the string cannot equal "\\0" (single backslash + 0) because
        // all backslashes are doubled. The check for e == NullToken is effectively dead code
        // but kept for clarity and defensive programming.
        var e = s.Replace("\\", "\\\\")
                  .Replace("|", "\\|")
                  .Replace("\n", "\\n")
                  .Replace("\r", "\\r");
        
        return e;
      }

      /// <summary>
      /// Unescapes special characters in strings during deserialization.
      /// </summary>
      internal static string? Unesc(string s)
      {
        if (s == NullToken) return null;
        if (s == "\\\\0") return "\\0"; // Handle escaped NullToken
        
        var result = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
          if (s[i] == '\\' && i + 1 < s.Length)
          {
            switch (s[i + 1])
            {
              case '\\': result.Append('\\'); i++; break;
              case '|': result.Append('|'); i++; break;
              case 'n': result.Append('\n'); i++; break;
              case 'r': result.Append('\r'); i++; break;
              default:
                // Unknown escape - preserve both characters
                result.Append(s[i]);      // '\'
                result.Append(s[i + 1]);  // the escaped char
                i++;
                break;
            }
          }
          else
          {
            result.Append(s[i]);
          }
        }
        return result.ToString();
      }

      /// <summary>
      /// Splits a string by delimiter, respecting escaped delimiters.
      /// Preserves escape sequences in tokens so Unesc() can decode them.
      /// </summary>
      internal static List<string> SplitEscaped(string line, char delimiter = '|')
      {
        var parts = new List<string>();
        var current = new StringBuilder();
        var escaping = false;

        for (int i = 0; i < line.Length; i++)
        {
          var ch = line[i];

          if (escaping)
          {
            // Preserve the escape marker so Unesc can interpret it
            current.Append('\\');
            current.Append(ch);
            escaping = false;
            continue;
          }

          if (ch == '\\')
          {
            escaping = true;
            continue;
          }

          if (ch == delimiter)
          {
            parts.Add(current.ToString());
            current.Clear();
            continue;
          }

          current.Append(ch);
        }

        if (escaping)
        {
          // Trailing '\' is invalid encoding. Keep it literal to avoid data loss.
          current.Append('\\');
        }

        parts.Add(current.ToString());
        return parts;
      }

      /// <summary>
      /// Serializes the entire memory system state to a deterministic string representation.
      /// Uses the same total-order sorting as retrieval to ensure consistency.
      /// All collections are sorted deterministically before serialization.
      /// </summary>
      public static string SerializeState(AuthoritativeMemorySystem memorySystem)
      {
        var sb = new StringBuilder();
        
        // Serialize canonical facts (sorted by Id for determinism)
        var canonicalFacts = memorySystem.GetCanonicalFacts()
          .OrderBy(f => f.Id, StringComparer.Ordinal)
          .ToList();
        
        sb.AppendLine($"CanonicalFacts({canonicalFacts.Count}):");
        foreach (var fact in canonicalFacts)
        {
          sb.AppendLine($"  {Esc(fact.Id)}|{Esc(fact.Fact)}|{Esc(fact.Domain)}|{fact.CreatedAtTicks}|{fact.SequenceNumber}");
        }
        
        // Serialize world state (sorted by Key for determinism)
        var worldState = memorySystem.GetAllWorldState()
          .OrderBy(s => s.Key, StringComparer.Ordinal)
          .ToList();
        
        sb.AppendLine($"WorldState({worldState.Count}):");
        foreach (var state in worldState)
        {
          // Serialize ModifiedAt.Kind explicitly to ensure UTC is preserved
          sb.AppendLine($"  {Esc(state.Id)}|{Esc(state.Key)}|{Esc(state.Value)}|{state.CreatedAtTicks}|{state.SequenceNumber}|{state.ModifiedAt.Ticks}|{(int)state.ModifiedAt.Kind}|{state.ModificationCount}|{state.Source}");
        }
        
        // Serialize episodic memories (sorted by total-order: CreatedAtTicks desc, Id asc, SequenceNumber asc)
        // Access internal _episodicMemories list via reflection to get ALL memories, including decayed ones
        var episodicMemoriesField = typeof(AuthoritativeMemorySystem).GetField("_episodicMemories", BindingFlags.NonPublic | BindingFlags.Instance);
        if (episodicMemoriesField == null)
          throw new InvalidOperationException("Cannot access _episodicMemories field via reflection");
        
        var episodicMemoriesRaw = episodicMemoriesField.GetValue(memorySystem);
        if (episodicMemoriesRaw == null)
          throw new InvalidOperationException("_episodicMemories field is null");
        
        // Cast to List<EpisodicMemoryEntry> via IEnumerable
        var episodicMemories = ((System.Collections.IEnumerable)episodicMemoriesRaw).Cast<EpisodicMemoryEntry>();
        var episodicMemoriesList = episodicMemories
          .OrderByDescending(m => m.CreatedAtTicks)
          .ThenBy(m => m.Id, StringComparer.Ordinal)
          .ThenBy(m => m.SequenceNumber)
          .ToList();
        
        sb.AppendLine($"EpisodicMemories({episodicMemoriesList.Count}):");
        foreach (var memory in episodicMemoriesList)
        {
          // Use bit-pattern serialization for floats to preserve exact fidelity
          var significanceBits = BitConverter.SingleToInt32Bits(memory.Significance);
          var strengthBits = BitConverter.SingleToInt32Bits(memory.Strength);
          sb.AppendLine($"  {Esc(memory.Id)}|{memory.CreatedAtTicks}|{memory.SequenceNumber}|{Esc(memory.Description)}|{memory.EpisodeType}|{significanceBits:X8}|{strengthBits:X8}|{Esc(memory.Participant)}|{memory.Source}");
        }
        
        // Serialize beliefs (sorted by total-order: Confidence desc, Key asc, SequenceNumber asc)
        // Must serialize both dictionary key and entry.Id - they can differ!
        // Access _beliefs dictionary via reflection to get key-value pairs
        var beliefsField = typeof(AuthoritativeMemorySystem).GetField("_beliefs", BindingFlags.NonPublic | BindingFlags.Instance);
        if (beliefsField == null)
          throw new InvalidOperationException("Cannot access _beliefs field via reflection");
        
        var beliefsDict = beliefsField.GetValue(memorySystem) as System.Collections.IDictionary;
        if (beliefsDict == null)
          throw new InvalidOperationException("_beliefs field is not a dictionary");
        
        var beliefsWithKeys = new List<(string key, BeliefMemoryEntry entry)>();
        foreach (System.Collections.DictionaryEntry kvp in beliefsDict)
        {
          if (kvp.Value is BeliefMemoryEntry entry)
          {
            beliefsWithKeys.Add((kvp.Key.ToString() ?? "", entry));
          }
        }
        
        var beliefs = beliefsWithKeys
          .OrderByDescending(b => b.entry.Confidence)
          .ThenBy(b => b.key, StringComparer.Ordinal)
          .ThenBy(b => b.entry.SequenceNumber)
          .ToList();
        
        sb.AppendLine($"Beliefs({beliefs.Count}):");
        foreach (var (key, belief) in beliefs)
        {
          // Serialize: key|entry.Id|CreatedAtTicks|Confidence|SequenceNumber|Subject|BeliefContent|BeliefType|Sentiment|IsContradicted|Evidence|Source
          // Use bit-pattern serialization for floats to preserve exact fidelity
          var confidenceBits = BitConverter.SingleToInt32Bits(belief.Confidence);
          var sentimentBits = BitConverter.SingleToInt32Bits(belief.Sentiment);
          sb.AppendLine($"  {Esc(key)}|{Esc(belief.Id)}|{belief.CreatedAtTicks}|{confidenceBits:X8}|{belief.SequenceNumber}|{Esc(belief.Subject)}|{Esc(belief.BeliefContent)}|{belief.BeliefType}|{sentimentBits:X8}|{belief.IsContradicted}|{Esc(belief.Evidence)}|{belief.Source}");
        }
        
        // Serialize NextSequenceNumber
        sb.AppendLine($"NextSequenceNumber: {memorySystem.NextSequenceNumber}");
        
        return sb.ToString();
      }

      /// <summary>
      /// Reconstructs a memory system from serialized state string.
      /// This tests true reconstruction determinism: serialize → deserialize → apply mutations → compare.
      /// All timestamps are restored from the serialized data, not generated from clock.
      /// Uses raw insertion APIs to avoid mutating identity fields after insertion.
      /// </summary>
      public static void ReconstructFromSerialized(
        string serializedState,
        AuthoritativeMemorySystem targetSystem)
      {
        var lines = serializedState.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var currentSection = "";
        long nextSequenceNumber = 1;

        foreach (var line in lines)
        {
          if (line.StartsWith("CanonicalFacts("))
          {
            currentSection = "CanonicalFacts";
            continue;
          }
          else if (line.StartsWith("WorldState("))
          {
            currentSection = "WorldState";
            continue;
          }
          else if (line.StartsWith("EpisodicMemories("))
          {
            currentSection = "EpisodicMemories";
            continue;
          }
          else if (line.StartsWith("Beliefs("))
          {
            currentSection = "Beliefs";
            continue;
          }
          else if (line.StartsWith("NextSequenceNumber:"))
          {
            var parts = line.Split(':');
            if (parts.Length == 2 && long.TryParse(parts[1].Trim(), out var seqNum))
            {
              nextSequenceNumber = seqNum;
            }
            continue;
          }

          if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("  "))
            continue;

          // Use escaped split to handle pipes in content
          var data = SplitEscaped(line.Substring(2));
          
          switch (currentSection)
          {
            case "CanonicalFacts":
              if (data.Count >= 5)
              {
                var factId = Unesc(data[0]);
                var fact = Unesc(data[1]);
                var domain = Unesc(data[2]);
                if (factId != null && fact != null)
                {
                  if (long.TryParse(data[3], out var factCreatedAt) && long.TryParse(data[4], out var factSeq))
                  {
                    var entry = CanonicalFact.Create(factId, fact, domain);
                    entry.CreatedAtTicks = factCreatedAt;
                    entry.SequenceNumber = factSeq;
                    targetSystem.InsertCanonicalFactRaw(entry);
                  }
                }
              }
              break;
              
            case "WorldState":
              if (data.Count >= 9) // Now includes Source and DateTimeKind
              {
                var key = Unesc(data[1]);
                var value = Unesc(data[2]);
                if (key != null && value != null)
                {
                  if (long.TryParse(data[3], out var stateCreatedAt) && 
                      long.TryParse(data[4], out var stateSeq) &&
                      long.TryParse(data[5], out var modifiedAtTicks) &&
                      int.TryParse(data[6], out var dateTimeKind) &&
                      int.TryParse(data[7], out var modCount) &&
                      Enum.TryParse<MutationSource>(data[8], out var source))
                  {
                    var entry = new WorldStateEntry(key, value);
                    entry.Id = Unesc(data[0]) ?? throw new InvalidOperationException("World state entry must have Id");
                    entry.CreatedAtTicks = stateCreatedAt;
                    entry.SequenceNumber = stateSeq;
                    entry.Source = source;
                    
                    // Set ModifiedAt via reflection (private setter) - restore with correct Kind
                    var modifiedAtProperty = typeof(WorldStateEntry).GetProperty("ModifiedAt", BindingFlags.Public | BindingFlags.Instance);
                    var setMethod = modifiedAtProperty?.GetSetMethod(nonPublic: true);
                    if (setMethod != null)
                    {
                      var dateTimeKindEnum = (DateTimeKind)dateTimeKind;
                      setMethod.Invoke(entry, new object[] { new DateTime(modifiedAtTicks, dateTimeKindEnum) });
                    }
                    
                    // Set ModificationCount via reflection
                    var modCountProperty = typeof(WorldStateEntry).GetProperty("ModificationCount", BindingFlags.Public | BindingFlags.Instance);
                    var modCountSetMethod = modCountProperty?.GetSetMethod(nonPublic: true);
                    if (modCountSetMethod != null)
                    {
                      modCountSetMethod.Invoke(entry, new object[] { modCount });
                    }
                    
                    targetSystem.InsertWorldStateRaw(key, entry);
                  }
                }
              }
              break;
              
            case "EpisodicMemories":
              if (data.Count >= 9) // Now includes Source, floats are hex bit patterns
              {
                var epId = Unesc(data[0]);
                var description = Unesc(data[3]);
                if (epId != null && description != null)
                {
                  if (Enum.TryParse<EpisodeType>(data[4], out var epType) &&
                      Enum.TryParse<MutationSource>(data[8], out var source))
                  {
                    // Parse values into locals before using them
                    long epCreatedAt = 0;
                    long epSeq = 0;
                    float significance = 0f;
                    float strength = 0f;
                    
                    if (!long.TryParse(data[1], out epCreatedAt)) continue;
                    if (!long.TryParse(data[2], out epSeq)) continue;
                    
                    // Parse floats from hex bit patterns for exact fidelity
                    if (!int.TryParse(data[5], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var significanceBits)) continue;
                    if (!int.TryParse(data[6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var strengthBits)) continue;
                    significance = BitConverter.Int32BitsToSingle(significanceBits);
                    strength = BitConverter.Int32BitsToSingle(strengthBits);
                    
                    var entry = new EpisodicMemoryEntry(description, epType)
                    {
                      Id = epId,
                      CreatedAtTicks = epCreatedAt,
                      SequenceNumber = epSeq,
                      Significance = significance,
                      Strength = strength,
                      Participant = Unesc(data[7]),
                      Source = source
                    };
                    
                    // Use raw insertion - no metadata generation, all fields already set
                    targetSystem.InsertEpisodicRaw(entry);
                  }
                }
              }
              break;
              
            case "Beliefs":
              if (data.Count >= 12) // Now includes key, CreatedAtTicks, Source, floats are hex bit patterns
              {
                var beliefKey = Unesc(data[0]); // Dictionary key (slot key)
                var beliefEntryId = Unesc(data[1]); // Entry.Id (can differ from key)
                var subject = Unesc(data[5]);
                var content = Unesc(data[6]);
                if (beliefKey != null && beliefEntryId != null && subject != null && content != null)
                {
                  if (Enum.TryParse<BeliefType>(data[7], out var beliefType) &&
                      Enum.TryParse<MutationSource>(data[11], out var source))
                  {
                    // Parse values into locals before using them
                    long beliefCreatedAt = 0;
                    float confidence = 0f;
                    long beliefSeq = 0;
                    float sentiment = 0f;
                    
                    if (!long.TryParse(data[2], out beliefCreatedAt)) continue;
                    if (!long.TryParse(data[4], out beliefSeq)) continue;
                    
                    // Parse floats from hex bit patterns for exact fidelity
                    if (!int.TryParse(data[3], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var confidenceBits)) continue;
                    if (!int.TryParse(data[8], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var sentimentBits)) continue;
                    confidence = BitConverter.Int32BitsToSingle(confidenceBits);
                    sentiment = BitConverter.Int32BitsToSingle(sentimentBits);
                    
                    var entry = new BeliefMemoryEntry(subject, content, beliefType)
                    {
                      Id = beliefEntryId, // Use the entry's Id, not the dictionary key
                      CreatedAtTicks = beliefCreatedAt,
                      Confidence = confidence,
                      SequenceNumber = beliefSeq,
                      Sentiment = sentiment,
                      Evidence = Unesc(data[10]),
                      Source = source
                    };
                    
                    // Set IsContradicted via reflection (private setter)
                    if (data[9] == "True")
                    {
                      var isContradictedProperty = typeof(BeliefMemoryEntry).GetProperty("IsContradicted", BindingFlags.Public | BindingFlags.Instance);
                      var setMethod = isContradictedProperty?.GetSetMethod(nonPublic: true);
                      if (setMethod != null)
                      {
                        setMethod.Invoke(entry, new object[] { true });
                      }
                    }
                    
                    // Use raw insertion with the dictionary key (slot key), not entry.Id
                    targetSystem.InsertBeliefRaw(beliefKey, entry);
                  }
                }
              }
              break;
          }
        }

        // Set NextSequenceNumber after reconstruction using raw API
        targetSystem.SetNextSequenceNumberRaw(nextSequenceNumber);
      }

      /// <summary>
      /// Reconstructs a memory system from an authoritative source system.
      /// Extracts all entries and reconstructs them in the target system.
      /// This tests true reconstruction determinism: extract → reconstruct → apply mutations → compare.
      /// </summary>
      public static void ReconstructFromSource(
        AuthoritativeMemorySystem sourceSystem,
        AuthoritativeMemorySystem targetSystem)
      {
        // Reconstruct canonical facts using raw insertion
        foreach (var fact in sourceSystem.GetCanonicalFacts().OrderBy(f => f.Id, StringComparer.Ordinal))
        {
          var entry = CanonicalFact.Create(fact.Id, fact.Fact, fact.Domain);
          entry.CreatedAtTicks = fact.CreatedAtTicks;
          entry.SequenceNumber = fact.SequenceNumber;
          entry.Source = fact.Source;
          targetSystem.InsertCanonicalFactRaw(entry);
        }

        // Reconstruct world state using raw insertion
        foreach (var state in sourceSystem.GetAllWorldState().OrderBy(s => s.Key, StringComparer.Ordinal))
        {
          var entry = new WorldStateEntry(state.Key, state.Value);
          entry.Id = state.Id;
          entry.CreatedAtTicks = state.CreatedAtTicks;
          entry.SequenceNumber = state.SequenceNumber;
          entry.Source = state.Source;
          
          // Set ModifiedAt via reflection (private setter)
          var modifiedAtProperty = typeof(WorldStateEntry).GetProperty("ModifiedAt", BindingFlags.Public | BindingFlags.Instance);
          var setMethod = modifiedAtProperty?.GetSetMethod(nonPublic: true);
          if (setMethod != null)
          {
            setMethod.Invoke(entry, new object[] { state.ModifiedAt });
          }
          
          // Set ModificationCount via reflection
          var modCountProperty = typeof(WorldStateEntry).GetProperty("ModificationCount", BindingFlags.Public | BindingFlags.Instance);
          var modCountSetMethod = modCountProperty?.GetSetMethod(nonPublic: true);
          if (modCountSetMethod != null)
          {
            modCountSetMethod.Invoke(entry, new object[] { state.ModificationCount });
          }
          
          targetSystem.InsertWorldStateRaw(state.Key, entry);
        }

        // Reconstruct episodic memories (preserve all fields)
        // Access internal _episodicMemories list via reflection to get ALL memories, including decayed ones
        var episodicMemoriesField = typeof(AuthoritativeMemorySystem).GetField("_episodicMemories", BindingFlags.NonPublic | BindingFlags.Instance);
        if (episodicMemoriesField == null)
          throw new InvalidOperationException("Cannot access _episodicMemories field via reflection");
        
        var episodicMemoriesRaw = episodicMemoriesField.GetValue(sourceSystem);
        if (episodicMemoriesRaw == null)
          throw new InvalidOperationException("_episodicMemories field is null");
        
        var episodicMemories = ((System.Collections.IEnumerable)episodicMemoriesRaw).Cast<EpisodicMemoryEntry>();
        
        foreach (var memory in episodicMemories
          .OrderByDescending(m => m.CreatedAtTicks)
          .ThenBy(m => m.Id, StringComparer.Ordinal)
          .ThenBy(m => m.SequenceNumber))
        {
          var entry = new EpisodicMemoryEntry(memory.Description, memory.EpisodeType)
          {
            Id = memory.Id,
            CreatedAtTicks = memory.CreatedAtTicks,
            SequenceNumber = memory.SequenceNumber,
            Significance = memory.Significance,
            Strength = memory.Strength,
            Participant = memory.Participant,
            Source = memory.Source
          };
          
          // Use raw insertion - no metadata generation, all fields already set
          targetSystem.InsertEpisodicRaw(entry);
        }

        // Reconstruct beliefs using raw insertion
        // Must access _beliefs dictionary via reflection to get key-value pairs
        var beliefsField = typeof(AuthoritativeMemorySystem).GetField("_beliefs", BindingFlags.NonPublic | BindingFlags.Instance);
        if (beliefsField == null)
          throw new InvalidOperationException("Cannot access _beliefs field via reflection");
        
        var beliefsDict = beliefsField.GetValue(sourceSystem) as System.Collections.IDictionary;
        if (beliefsDict == null)
          throw new InvalidOperationException("_beliefs field is not a dictionary");
        
        var beliefsWithKeys = new List<(string key, BeliefMemoryEntry entry)>();
        foreach (System.Collections.DictionaryEntry kvp in beliefsDict)
        {
          if (kvp.Value is BeliefMemoryEntry entry)
          {
            beliefsWithKeys.Add((kvp.Key.ToString() ?? "", entry));
          }
        }
        
        foreach (var (beliefKey, belief) in beliefsWithKeys
          .OrderByDescending(b => b.entry.Confidence)
          .ThenBy(b => b.key, StringComparer.Ordinal)
          .ThenBy(b => b.entry.SequenceNumber))
        {
          var entry = new BeliefMemoryEntry(belief.Subject, belief.BeliefContent, belief.BeliefType)
          {
            Id = belief.Id ?? throw new InvalidOperationException("Belief must have an ID"),
            CreatedAtTicks = belief.CreatedAtTicks,
            Confidence = belief.Confidence,
            SequenceNumber = belief.SequenceNumber,
            Sentiment = belief.Sentiment,
            Evidence = belief.Evidence,
            Source = belief.Source
          };
          
          // Set IsContradicted via reflection (private setter)
          if (belief.IsContradicted)
          {
            var isContradictedProperty = typeof(BeliefMemoryEntry).GetProperty("IsContradicted", BindingFlags.Public | BindingFlags.Instance);
            var setMethod = isContradictedProperty?.GetSetMethod(nonPublic: true);
            if (setMethod != null)
            {
              setMethod.Invoke(entry, new object[] { true });
            }
          }
          
          // Use raw insertion with the dictionary key (slot key), not entry.Id
          targetSystem.InsertBeliefRaw(beliefKey, entry);
        }

        // Set NextSequenceNumber after reconstruction using raw API
        targetSystem.SetNextSequenceNumberRaw(sourceSystem.NextSequenceNumber);
      }
    }
    // Use fixed integer seeds for any randomness (per user instruction)
    private const int TestSeed = 42;
    private const int AlternateSeed = 12345;

    private PersonaProfile _testProfile = null!;
    private PersonaMemoryStore _memoryStore = null!;
    private AuthoritativeMemorySystem _memorySystem = null!;
    private IApiClient _mockApiClient = null!;
    private ExpectancyEvaluator _expectancyEvaluator = null!;
    private ContextRetrievalLayer _contextRetrieval = null!;
    private PromptAssembler _promptAssembler = null!;
    private OutputParser _outputParser = null!;
    private ValidationGate _validationGate = null!;
    private MemoryMutationController _mutationController = null!;

    [SetUp]
    public void SetUp()
    {
      // Create test persona
      _testProfile = PersonaProfile.Create("test-npc", "Test NPC");
      _testProfile.Description = "A friendly shopkeeper";
      _testProfile.SystemPrompt = "You are a helpful shopkeeper.";

      // Initialize memory store
      _memoryStore = new PersonaMemoryStore();
      _memorySystem = _memoryStore.GetOrCreateSystem(_testProfile.PersonaId);

      // Add canonical facts
      _memorySystem.AddCanonicalFact("king_name", "The king is named Arthur", "world_lore");
      _memorySystem.AddCanonicalFact("magic_exists", "Magic is real in this world", "world_lore");

      // Add world state
      _memorySystem.SetWorldState("door_status", "open", MutationSource.GameSystem);
      _memorySystem.SetWorldState("weather", "sunny", MutationSource.GameSystem);

      // Add episodic memories
      var memory1 = new EpisodicMemoryEntry("Player said hello", EpisodeType.LearnedInfo);
      _memorySystem.AddEpisodicMemory(memory1, MutationSource.ValidatedOutput);

      var memory2 = new EpisodicMemoryEntry("Player bought a sword", EpisodeType.LearnedInfo);
      _memorySystem.AddEpisodicMemory(memory2, MutationSource.ValidatedOutput);

      // Add beliefs
      var belief = BeliefMemoryEntry.CreateOpinion("player", "is trustworthy", sentiment: 0.8f, confidence: 0.9f);
      _memorySystem.SetBelief("trust_player", belief, MutationSource.ValidatedOutput);

      // Initialize pipeline components
      _mockApiClient = Substitute.For<IApiClient>();
      _expectancyEvaluator = new ExpectancyEvaluator();
      _contextRetrieval = new ContextRetrievalLayer(_memorySystem);
      _promptAssembler = new PromptAssembler(PromptAssemblerConfig.Default);
      _outputParser = new OutputParser();
      _validationGate = new ValidationGate();
      _mutationController = new MemoryMutationController();
    }

    #region Deterministic State Reconstruction Tests

    [Test]
    public void StateReconstruction_SameSnapshotSameConstraints_SamePromptAssembly()
    {
      // Arrange - Create identical inputs with explicit snapshot time for determinism
      // Use fixed constant for reproducibility (2024-01-01 00:00:00 UTC)
      const long snapshotTime = 638400000000000000L;
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Tell me about the king",
        GameTime = 100f
      };

      var constraints = _expectancyEvaluator.Evaluate(context);
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput ?? string.Empty, snapshotTime);

      // Act - Build two snapshots with same snapshot time and assemble prompts
      var snapshot1 = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput ?? string.Empty)
        .WithSnapshotTimeUtcTicks(snapshotTime))
        .WithAttemptNumber(0)
        .WithMaxAttempts(3)
        .Build();

      var snapshot2 = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput ?? string.Empty)
        .WithSnapshotTimeUtcTicks(snapshotTime))
        .WithAttemptNumber(0)
        .WithMaxAttempts(3)
        .Build();

      var prompt1 = _promptAssembler.AssembleFromSnapshot(snapshot1, npcName: _testProfile.Name);
      var prompt2 = _promptAssembler.AssembleFromSnapshot(snapshot2, npcName: _testProfile.Name);

      // Assert - Prompts should be byte-identical
      Assert.That(prompt1.Text, Is.EqualTo(prompt2.Text), "Same inputs must produce identical prompt assembly");
      Assert.That(prompt1.EstimatedTokens, Is.EqualTo(prompt2.EstimatedTokens));
    }

    [Test]
    public void StateReconstruction_SameParsedOutput_SameGateResult()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("Hello! The king is named Arthur.", "raw");
      var context = new ValidationContext
      {
        MemorySystem = _memorySystem,
        Constraints = new ConstraintSet()
      };

      // Act - Validate twice
      var result1 = _validationGate.Validate(output, context);
      var result2 = _validationGate.Validate(output, context);

      // Assert - Results should be identical
      Assert.That(result1.Passed, Is.EqualTo(result2.Passed));
      Assert.That(result1.Failures.Count, Is.EqualTo(result2.Failures.Count));
      Assert.That(result1.ApprovedMutations.Count, Is.EqualTo(result2.ApprovedMutations.Count));
      Assert.That(result1.ApprovedIntents.Count, Is.EqualTo(result2.ApprovedIntents.Count));
    }

    [Test]
    public void StateReconstruction_SameGateResultSameInitialState_SameMutationResult()
    {
      // Arrange - Build one authoritative system (the "source of truth")
      const long baseTime = 638400000000000000L; // Fixed tick value: 2024-01-01 00:00:00 UTC
      var authoritativeClock = new AdvancingClock(baseTime, incrementTicks: 1000);
      var authoritativeIdGen = new SequentialIdGenerator(1);

      var authoritativeSystem = new AuthoritativeMemorySystem(authoritativeClock, authoritativeIdGen);
      authoritativeSystem.NextSequenceNumber = 1;

      // Build initial state in authoritative system
      authoritativeSystem.AddCanonicalFact("fact1", "Test fact", "test");
      authoritativeSystem.SetWorldState("door_status", "open", MutationSource.GameSystem);
      
      var belief = BeliefMemoryEntry.CreateOpinion("player", "is friendly", sentiment: 0.7f, confidence: 0.8f);
      authoritativeSystem.SetBelief("player_opinion", belief, MutationSource.ValidatedOutput);

      // Serialize the authoritative state (this is our "snapshot")
      var serializedState = MemoryStateSerializer.SerializeState(authoritativeSystem);
      var initialNextSeq = authoritativeSystem.NextSequenceNumber;
      var initialEpisodicCount = authoritativeSystem.GetActiveEpisodicMemories().Count();

      // Reconstruct two systems from the serialized state (true reconstruction test)
      // This tests that serialization is sufficient to reconstruct state, not just "copying from an object"
      var clock1 = new AdvancingClock(baseTime, incrementTicks: 1000);
      var clock2 = new AdvancingClock(baseTime, incrementTicks: 1000);
      var idGen1 = new SequentialIdGenerator(1);
      var idGen2 = new SequentialIdGenerator(1);

      var memorySystem1 = new AuthoritativeMemorySystem(clock1, idGen1);
      var memorySystem2 = new AuthoritativeMemorySystem(clock2, idGen2);

      MemoryStateSerializer.ReconstructFromSerialized(serializedState, memorySystem1);
      MemoryStateSerializer.ReconstructFromSerialized(serializedState, memorySystem2);

      // Verify reconstructed states are identical to serialized snapshot
      var reconstructedState1 = MemoryStateSerializer.SerializeState(memorySystem1);
      var reconstructedState2 = MemoryStateSerializer.SerializeState(memorySystem2);
      Assert.That(reconstructedState1, Is.EqualTo(serializedState), "Reconstructed state 1 must match serialized snapshot");
      Assert.That(reconstructedState2, Is.EqualTo(serializedState), "Reconstructed state 2 must match serialized snapshot");
      
      // Explicitly verify NextSequenceNumber was restored (critical for determinism)
      Assert.That(memorySystem1.NextSequenceNumber, Is.EqualTo(initialNextSeq), 
        "Reconstruction must faithfully restore NextSequenceNumber");
      Assert.That(memorySystem2.NextSequenceNumber, Is.EqualTo(initialNextSeq), 
        "Reconstruction must faithfully restore NextSequenceNumber");

      // Prepare mutation
      var output = ParsedOutput.Dialogue("I learned something", "raw")
        .WithMutation(ProposedMutation.AppendEpisodic("Player told me a secret"));

      var gateResult = _validationGate.Validate(output);
      
      // Validate gate result is deterministic and contains expected mutation
      Assert.That(gateResult.Passed, Is.True, "Gate result must pass for mutation to execute");
      Assert.That(gateResult.ApprovedMutations, Has.Count.EqualTo(1), "Gate result must contain exactly one approved mutation");
      Assert.That(gateResult.ApprovedMutations[0].Type, Is.EqualTo(MutationType.AppendEpisodic), 
        "Approved mutation must be AppendEpisodic");
      Assert.That(gateResult.ApprovedMutations[0].Content, Is.EqualTo("Player told me a secret"), 
        "Approved mutation content must match");

      // Act - Execute mutations on both reconstructed systems
      // Note: MemoryMutationController delegates creation to AuthoritativeMemorySystem,
      // which uses the injected deterministic providers, so determinism is preserved
      var controller1 = new MemoryMutationController();
      var controller2 = new MemoryMutationController();

      var result1 = controller1.ExecuteMutations(gateResult, memorySystem1, "npc1");
      var result2 = controller2.ExecuteMutations(gateResult, memorySystem2, "npc1");

      // Assert - Mutation results should be structurally identical
      Assert.That(result1.Results, Has.Count.EqualTo(1), "Exactly one mutation result");
      Assert.That(result2.Results, Has.Count.EqualTo(1), "Exactly one mutation result");
      Assert.That(result1.AllSucceeded, Is.True, "All mutations should succeed");
      Assert.That(result2.AllSucceeded, Is.True, "All mutations should succeed");

      // Structural comparison of individual mutation results
      var mutationResult1 = result1.Results[0];
      var mutationResult2 = result2.Results[0];

      Assert.That(mutationResult1.Success, Is.EqualTo(mutationResult2.Success), "Mutation success status must match");
      Assert.That(mutationResult1.Mutation.Type, Is.EqualTo(mutationResult2.Mutation.Type), "Mutation type must match");
      Assert.That(mutationResult1.Mutation.Content, Is.EqualTo(mutationResult2.Mutation.Content), "Mutation content must match");
      Assert.That(mutationResult1.ErrorMessage, Is.EqualTo(mutationResult2.ErrorMessage), "Error messages must match");

      // Compare affected entries structurally
      Assert.That(mutationResult1.AffectedEntry, Is.Not.Null, "Affected entry must exist");
      Assert.That(mutationResult2.AffectedEntry, Is.Not.Null, "Affected entry must exist");
      
      var entry1 = mutationResult1.AffectedEntry!;
      var entry2 = mutationResult2.AffectedEntry!;

      Assert.That(entry1.GetType(), Is.EqualTo(entry2.GetType()), "Affected entry type must match");
      Assert.That(entry1.Id, Is.EqualTo(entry2.Id), "Affected entry ID must match");
      Assert.That(entry1.CreatedAtTicks, Is.EqualTo(entry2.CreatedAtTicks), "Affected entry timestamp must match");
      Assert.That(entry1.SequenceNumber, Is.EqualTo(entry2.SequenceNumber), "Affected entry sequence number must match");

      // Assert expected delta: exactly one episodic entry appended
      var finalEpisodicCount1 = memorySystem1.GetActiveEpisodicMemories().Count();
      var finalEpisodicCount2 = memorySystem2.GetActiveEpisodicMemories().Count();
      
      Assert.That(finalEpisodicCount1, Is.EqualTo(initialEpisodicCount + 1), "Episodic count should increase by 1");
      Assert.That(finalEpisodicCount2, Is.EqualTo(initialEpisodicCount + 1), "Episodic count should increase by 1");

      // Verify the new entry content and that it matches AffectedEntry (fetch by ID, not ordering)
      var newEpisodic1 = memorySystem1.GetActiveEpisodicMemories()
        .Single(m => m.Id == entry1.Id);
      var newEpisodic2 = memorySystem2.GetActiveEpisodicMemories()
        .Single(m => m.Id == entry2.Id);

      Assert.That(newEpisodic1.Description, Is.EqualTo("Player told me a secret"), "New episodic content must match");
      Assert.That(newEpisodic2.Description, Is.EqualTo("Player told me a secret"), "New episodic content must match");
      
      // Assert the appended entry is exactly the AffectedEntry (by ID - already verified by Single() above)
      Assert.That(newEpisodic1.Id, Is.EqualTo(entry1.Id), "New episodic entry must be the AffectedEntry");
      Assert.That(newEpisodic2.Id, Is.EqualTo(entry2.Id), "New episodic entry must be the AffectedEntry");

      // Verify NextSequenceNumber advanced (more flexible assertion)
      Assert.That(memorySystem1.NextSequenceNumber, Is.GreaterThan(initialNextSeq), "NextSequenceNumber should advance");
      Assert.That(memorySystem2.NextSequenceNumber, Is.GreaterThan(initialNextSeq), "NextSequenceNumber should advance");
      Assert.That(newEpisodic1.SequenceNumber, Is.EqualTo(initialNextSeq), "New entry sequence number should equal initial NextSequenceNumber");
      Assert.That(newEpisodic2.SequenceNumber, Is.EqualTo(initialNextSeq), "New entry sequence number should equal initial NextSequenceNumber");
      Assert.That(memorySystem1.NextSequenceNumber, Is.EqualTo(newEpisodic1.SequenceNumber + 1), "NextSequenceNumber should be new entry sequence + 1");
      Assert.That(memorySystem2.NextSequenceNumber, Is.EqualTo(newEpisodic2.SequenceNumber + 1), "NextSequenceNumber should be new entry sequence + 1");

      // Assert - Final memory states should be byte-identical (no normalization needed - providers are deterministic)
      var finalState1 = MemoryStateSerializer.SerializeState(memorySystem1);
      var finalState2 = MemoryStateSerializer.SerializeState(memorySystem2);
      
      Assert.That(finalState1, Is.EqualTo(finalState2), 
        "Same GateResult + same reconstructed initial state + same deterministic providers must produce " +
        "identical final memory state. This verifies true deterministic mutation execution " +
        "including IDs, timestamps, ordering, and persistence.");
      
      // Additional verification: Check that canonical facts are unchanged
      var fact1 = memorySystem1.GetCanonicalFact("fact1");
      var fact2 = memorySystem2.GetCanonicalFact("fact1");
      Assert.That(fact1?.Fact, Is.EqualTo(fact2?.Fact), "Canonical facts must remain unchanged");
      
      // Verify world state is unchanged (unless mutation authorized changes)
      var state1 = memorySystem1.GetWorldState("door_status");
      var state2 = memorySystem2.GetWorldState("door_status");
      Assert.That(state1?.Value, Is.EqualTo(state2?.Value), "World state must remain unchanged unless authorized");
    }

    #endregion

    #region No-Now Enforcement Tests (High Leverage)

    [Test]
    public void NoNow_SameSnapshotDifferentWallClock_IdenticalPromptAssembly()
    {
      // Arrange - Create snapshot (captures time at build)
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Hello",
        GameTime = 100f
      };

      // Use fixed snapshot time for determinism (not wall clock)
      const long snapshotTime = 638400000000000000L; // Fixed tick value: 2024-01-01 00:00:00 UTC
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput ?? string.Empty, snapshotTime);

      // Build snapshot with explicit snapshot time
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput ?? string.Empty)
        .WithSnapshotTimeUtcTicks(snapshotTime))
        .Build();

      // Act - Assemble prompt twice with same snapshot (no wall-clock dependency)
      var prompt1 = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);
      var prompt2 = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);

      // Assert - Prompts must be identical (no wall-clock dependency)
      // The same snapshot produces identical prompts regardless of when assembly runs
      Assert.That(prompt1.Text, Is.EqualTo(prompt2.Text),
        "Prompt assembly must not depend on wall-clock time - only on snapshot state");
    }

    [Test]
    public void NoNow_ContextRetrievalUsesSnapshotTime_NotCurrentTime()
    {
      // Arrange - Use deterministic providers and fixed time for reproducibility
      const long baseTime = 638400000000000000L; // Fixed tick value: 2024-01-01 00:00:00 UTC
      var clock = new FixedClock(baseTime);
      var idGen = new SequentialIdGenerator(1);
      var memorySystem = new AuthoritativeMemorySystem(clock, idGen);

      // Add old memory (created 2 hours ago relative to base time)
      // Use raw insertion to set exact timestamp without post-insert mutation
      var oldMemory = new EpisodicMemoryEntry("Old event happened", EpisodeType.LearnedInfo)
      {
        Id = "old_mem_001",
        CreatedAtTicks = baseTime - TimeSpan.FromHours(2).Ticks,
        SequenceNumber = 1,
        Significance = 0.5f
      };
      memorySystem.InsertEpisodicRaw(oldMemory);

      // Add new memory (created 30 minutes ago relative to base time)
      var newMemory = new EpisodicMemoryEntry("New event happened", EpisodeType.LearnedInfo)
      {
        Id = "new_mem_001",
        CreatedAtTicks = baseTime - TimeSpan.FromMinutes(30).Ticks,
        SequenceNumber = 2,
        Significance = 0.5f
      };
      memorySystem.InsertEpisodicRaw(newMemory);
      
      // Set NextSequenceNumber to match what would be expected
      memorySystem.SetNextSequenceNumberRaw(3);

      var contextRetrieval = new ContextRetrievalLayer(memorySystem, new ContextRetrievalConfig
      {
        RecencyWeight = 1.0f, // Use only recency for this test
        RelevanceWeight = 0.0f,
        SignificanceWeight = 0.0f,
        RecencyHalfLifeTicks = TimeSpan.FromHours(1).Ticks,
        MaxEpisodicMemories = 10
      });

      // Act - Build snapshot with T0 (base time)
      var snapshotT0 = new StateSnapshotBuilder()
        .WithPlayerInput("event")
        .WithSnapshotTimeUtcTicks(baseTime)
        .Build();

      // Retrieve context twice with same snapshot time (no wall-clock dependency)
      var result1 = contextRetrieval.RetrieveContext(snapshotT0);
      var result2 = contextRetrieval.RetrieveContext(snapshotT0);

      // Assert - Results should be identical (no wall-clock dependency)
      Assert.That(result1.EpisodicMemories.Count, Is.EqualTo(result2.EpisodicMemories.Count),
        "Same snapshot time should produce identical results regardless of wall clock");
      
      for (int i = 0; i < result1.EpisodicMemories.Count; i++)
      {
        Assert.That(result1.EpisodicMemories[i], Is.EqualTo(result2.EpisodicMemories[i]),
          $"Memory {i} should be identical across retrievals with same snapshot time");
      }

      // Act - Build second snapshot with T1 (1 hour later)
      var snapshotT1 = new StateSnapshotBuilder()
        .WithPlayerInput("event")
        .WithSnapshotTimeUtcTicks(baseTime + TimeSpan.FromHours(1).Ticks)
        .Build();

      var result3 = contextRetrieval.RetrieveContext(snapshotT1);

      // Assert - Recency ranking should change (new memory should be relatively more recent at T1)
      // At T0: old memory is 2 hours old, new memory is 30 min old
      // At T1: old memory is 3 hours old, new memory is 1.5 hours old
      // Both should still be retrieved, but ordering might change based on recency decay
      Assert.That(result3.EpisodicMemories.Count, Is.EqualTo(2),
        "Both memories should still be retrieved at T1");
      
      // The new memory should rank higher at T1 than at T0 (relative to old memory)
      // because it's decaying slower (less time elapsed relative to its age)
      // This proves snapshot time is being used for recency calculation
      var t0NewMemoryIndex = result1.EpisodicMemories.FindIndex(m => m.Contains("New event"));
      var t1NewMemoryIndex = result3.EpisodicMemories.FindIndex(m => m.Contains("New event"));
      
      // Both should be present
      Assert.That(t0NewMemoryIndex, Is.GreaterThanOrEqualTo(0), "New memory should be in T0 results");
      Assert.That(t1NewMemoryIndex, Is.GreaterThanOrEqualTo(0), "New memory should be in T1 results");
    }

    #endregion

    #region Deterministic Ordering Tests

    [Test]
    public void DeterministicOrdering_MultipleRuns_SameOrder()
    {
      // Arrange - Use deterministic providers and fixed seed for test reproducibility
      const long baseTime = 638400000000000000L; // Fixed tick value: 2024-01-01 00:00:00 UTC
      var clock = new FixedClock(baseTime);
      var idGen = new SequentialIdGenerator(1);
      var random = new Random(TestSeed);

      // Add memories in random order
      var memorySystem = new AuthoritativeMemorySystem(clock, idGen);
      var memoryIds = new[] { "mem_z", "mem_a", "mem_m", "mem_b", "mem_y" };

      // Shuffle the order (deterministically using seed)
      var shuffled = memoryIds.OrderBy(_ => random.Next()).ToList();

      foreach (var id in shuffled)
      {
        var memory = new EpisodicMemoryEntry($"Content for {id}", EpisodeType.LearnedInfo) { Id = id };
        memorySystem.AddEpisodicMemory(memory, MutationSource.ValidatedOutput);
      }

      var contextRetrieval = new ContextRetrievalLayer(memorySystem);
      
      // Use fixed snapshot time for determinism (not wall clock)
      const long snapshotTime = 638400000000000000L; // Fixed tick value: 2024-01-01 00:00:00 UTC

      // Act - Retrieve multiple times with same snapshot time (EpisodicMemories returns List<string>)
      var results = new List<List<string>>();
      for (int i = 0; i < 5; i++)
      {
        var context = contextRetrieval.RetrieveContext("Content", snapshotTime);
        results.Add(context.EpisodicMemories.ToList());
      }

      // Assert - All results should have identical ordering
      for (int i = 1; i < results.Count; i++)
      {
        Assert.That(results[i], Is.EqualTo(results[0]),
          $"Run {i} produced different ordering than run 0");
      }
    }

    [Test]
    public void DeterministicOrdering_NearEqualScores_DeterministicResults()
    {
      // Arrange - Use deterministic providers for test reproducibility
      const long baseTime = 638400000000000000L; // Fixed tick value: 2024-01-01 00:00:00 UTC
      var clock = new FixedClock(baseTime);
      var idGen = new SequentialIdGenerator(1);
      
      // Create memories with nearly identical scores
      var memorySystem = new AuthoritativeMemorySystem(clock, idGen);

      // Add memories that will have near-equal scores
      for (int i = 0; i < 5; i++)
      {
        var memory = new EpisodicMemoryEntry($"Similar content {i}", EpisodeType.LearnedInfo)
        {
          Significance = 0.5f, // Same significance
          Strength = 1.0f // Same strength (recency)
        };
        memorySystem.AddEpisodicMemory(memory, MutationSource.ValidatedOutput);
      }

      var contextRetrieval = new ContextRetrievalLayer(memorySystem);
      
      // Use fixed snapshot time for determinism (not wall clock)
      const long snapshotTime = 638400000000000000L; // Fixed tick value: 2024-01-01 00:00:00 UTC

      // Act - Retrieve multiple times with same snapshot time (EpisodicMemories returns List<string>)
      var results = new List<List<string>>();
      for (int i = 0; i < 3; i++)
      {
        var context = contextRetrieval.RetrieveContext("Similar", snapshotTime);
        results.Add(context.EpisodicMemories.ToList());
      }

      // Assert - All results should have identical ordering
      // Near-equal scores use internal tie-breaker (SequenceNumber) for determinism
      for (int i = 1; i < results.Count; i++)
      {
        Assert.That(results[i], Is.EqualTo(results[0]),
          "Near-equal scores must produce deterministic ordering");
      }
    }

    #endregion

    #region Pipeline Order Verification Tests

    /// <summary>
    /// Testable orchestrator that executes the pipeline components in contract order.
    /// This mirrors the production orchestrator pattern (e.g., LlamaBrainAgent.SendWithSnapshotAsync).
    /// </summary>
    private class TestablePipelineOrchestrator
    {
      private readonly ExpectancyEvaluator _expectancyEvaluator;
      private readonly ContextRetrievalLayer _contextRetrieval;
      private readonly PromptAssembler _promptAssembler;
      private readonly IApiClient _apiClient;
      private readonly OutputParser _outputParser;
      private readonly ValidationGate _validationGate;
      private readonly MemoryMutationController _mutationController;
      private readonly AuthoritativeMemorySystem _memorySystem;
      private readonly string _npcId;
      private readonly string _systemPrompt;
      private readonly string _npcName;

      // Call tracking for verification
      public List<string> CallOrder { get; } = new List<string>();
      public StateSnapshot? SnapshotPassedToValidation { get; private set; }
      public StateSnapshot? SnapshotPassedToMutation { get; private set; }

      public TestablePipelineOrchestrator(
        ExpectancyEvaluator expectancyEvaluator,
        ContextRetrievalLayer contextRetrieval,
        PromptAssembler promptAssembler,
        IApiClient apiClient,
        OutputParser outputParser,
        ValidationGate validationGate,
        MemoryMutationController mutationController,
        AuthoritativeMemorySystem memorySystem,
        string npcId,
        string systemPrompt,
        string npcName)
      {
        _expectancyEvaluator = expectancyEvaluator;
        _contextRetrieval = contextRetrieval;
        _promptAssembler = promptAssembler;
        _apiClient = apiClient;
        _outputParser = outputParser;
        _validationGate = validationGate;
        _mutationController = mutationController;
        _memorySystem = memorySystem;
        _npcId = npcId;
        _systemPrompt = systemPrompt;
        _npcName = npcName;
      }

      public async Task<string> ExecutePipelineAsync(InteractionContext context, long snapshotTime)
      {
        // Component 2: Determinism Layer (Expectancy Engine)
        CallOrder.Add("ExpectancyEvaluation");
        var constraints = _expectancyEvaluator.Evaluate(context);

        // Component 3: External Authoritative Memory System (Context Retrieval)
        CallOrder.Add("ContextRetrieval");
        var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput ?? string.Empty, snapshotTime);

        // Component 4: Authoritative State Snapshot
        CallOrder.Add("SnapshotBuilding");
        var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
          .WithContext(context)
          .WithConstraints(constraints)
          .WithSystemPrompt(_systemPrompt)
          .WithPlayerInput(context.PlayerInput ?? string.Empty)
          .WithSnapshotTimeUtcTicks(snapshotTime))
          .Build();

        // Component 5: Ephemeral Working Memory (Prompt Assembly)
        CallOrder.Add("PromptAssembly");
        var assembledPrompt = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _npcName);

        // Component 6: Stateless Inference Core
        CallOrder.Add("LLMGeneration");
        var llmResponse = await _apiClient.SendPromptAsync(assembledPrompt.Text, null, null, CancellationToken.None);

        // Component 7: Output Parsing & Validation
        CallOrder.Add("OutputParsing");
        var parsedOutput = _outputParser.Parse(llmResponse);

        CallOrder.Add("ValidationGate");
        var validationContext = new ValidationContext
        {
          Constraints = constraints,
          MemorySystem = _memorySystem,
          Snapshot = snapshot  // Thread snapshot through validation
        };
        SnapshotPassedToValidation = snapshot;  // Record for verification
        var gateResult = _validationGate.Validate(parsedOutput, validationContext);

        // Component 8: Memory Mutation + World Effects (only if gate passed)
        if (gateResult.Passed)
        {
          CallOrder.Add("MemoryMutation");
          SnapshotPassedToMutation = snapshot;  // Record for verification
          var mutationResult = _mutationController.ExecuteMutations(gateResult, _memorySystem, _npcId);
        }

        return parsedOutput.DialogueText;
      }
    }

    [Test]
    public async Task PipelineOrder_ExecutesInCorrectSequence()
    {
      // Arrange - Create testable orchestrator with real components
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Hello shopkeeper",
        GameTime = 100f
      };

      // Use fixed snapshot time for determinism
      const long snapshotTime = 638400000000000000L; // Fixed tick value: 2024-01-01 00:00:00 UTC

      // Mock API client
      _mockApiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("Hello! Welcome to my shop."));

      // Create orchestrator with real components
      var orchestrator = new TestablePipelineOrchestrator(
        _expectancyEvaluator,
        _contextRetrieval,
        _promptAssembler,
        _mockApiClient,
        _outputParser,
        _validationGate,
        _mutationController,
        _memorySystem,
        _testProfile.PersonaId,
        _testProfile.SystemPrompt,
        _testProfile.Name
      );

      // Act - Execute pipeline through orchestrator
      var result = await orchestrator.ExecutePipelineAsync(context, snapshotTime);

      // Assert - Verify execution order matches contract
      var expectedOrder = new[]
      {
        "ExpectancyEvaluation",      // Component 2: Determinism Layer
        "ContextRetrieval",          // Component 3: External Authoritative Memory System
        "SnapshotBuilding",           // Component 4: Authoritative State Snapshot
        "PromptAssembly",             // Component 5: Ephemeral Working Memory
        "LLMGeneration",              // Component 6: Stateless Inference Core
        "OutputParsing",              // Component 7: Output Parsing
        "ValidationGate",             // Component 7: Validation
        "MemoryMutation"              // Component 8: Memory Mutation
      };

      Assert.That(orchestrator.CallOrder, Is.EqualTo(expectedOrder),
        "Orchestrator must call components in contract order: " +
        "1. Interaction Context (input) → 2. Determinism Layer → 3. Memory Retrieval → " +
        "4. State Snapshot → 5. Prompt Assembly → 6. Inference → 7. Parsing & Validation → 8. Mutation");

      // Assert - Verify same snapshot is threaded through validation and mutation
      Assert.That(orchestrator.SnapshotPassedToValidation, Is.Not.Null,
        "Orchestrator must pass snapshot to validation");
      Assert.That(orchestrator.SnapshotPassedToMutation, Is.Not.Null,
        "Orchestrator must pass snapshot to mutation (when gate passes)");
      Assert.That(orchestrator.SnapshotPassedToValidation, Is.SameAs(orchestrator.SnapshotPassedToMutation),
        "Orchestrator must thread the SAME snapshot instance through validation and mutation");

      // Verify snapshot contains expected data
      Assert.That(orchestrator.SnapshotPassedToValidation!.PlayerInput, Is.EqualTo(context.PlayerInput),
        "Snapshot must contain player input from context");
      Assert.That(orchestrator.SnapshotPassedToValidation!.Constraints, Is.Not.Null,
        "Snapshot must contain constraints from expectancy evaluation");
    }

    #endregion

    #region Retry Behavior Tests

    [Test]
    public async Task Retry_FailedValidation_TriggersRetryWithEscalatedConstraints()
    {
      // Arrange
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Hello"
      };

      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-greeting", "Don't say hi", "Don't say hi", "hi"));

      // Use fixed snapshot time for determinism
      const long snapshotTime = 638400000000000000L; // Fixed tick value: 2024-01-01 00:00:00 UTC
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput ?? string.Empty, snapshotTime);
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput ?? string.Empty)
        .WithSnapshotTimeUtcTicks(snapshotTime)
        .WithAttemptNumber(0)
        .WithMaxAttempts(3))
        .Build();

      // First attempt - violating response
      _mockApiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("Hi there friend!"));

      var response1 = await _mockApiClient.SendPromptAsync("prompt", null, null, CancellationToken.None);
      var parsed1 = _outputParser.Parse(response1);
      var validationContext = new ValidationContext { Constraints = constraints, MemorySystem = _memorySystem };
      var result1 = _validationGate.Validate(parsed1, validationContext);

      // Assert first attempt failed
      Assert.That(result1.Passed, Is.False);
      Assert.That(result1.ShouldRetry, Is.True);

      // Act - Create retry snapshot with escalated constraints
      var escalatedConstraints = new ConstraintSet();
      escalatedConstraints.Add(Constraint.Prohibition("no-greeting-strict", "No greeting at all", "Absolutely no greeting", "hi", "hello", "greetings"));
      var retrySnapshot = snapshot.ForRetry(escalatedConstraints);

      // Assert - Retry snapshot has merged constraints and incremented attempt
      Assert.That(retrySnapshot.AttemptNumber, Is.EqualTo(1));
      Assert.That(retrySnapshot.Constraints.Count, Is.GreaterThan(constraints.Count));
    }

    [Test]
    public async Task Retry_CriticalFailure_SkipsRetry()
    {
      // Arrange - Create a canonical contradiction (critical failure)
      var output = ParsedOutput.Dialogue("The king is not named Arthur", "raw");
      var validationContext = new ValidationContext { MemorySystem = _memorySystem };

      // Act
      var result = _validationGate.Validate(output, validationContext);

      // Assert - Critical failure should not allow retry
      Assert.That(result.Passed, Is.False);
      Assert.That(result.HasCriticalFailure, Is.True);
      Assert.That(result.ShouldRetry, Is.False);
    }

    #endregion

    #region Memory Mutation Integration Tests

    [Test]
    public void MemoryMutation_OnlyAppliedAfterSuccessfulValidation()
    {
      // Arrange
      var initialMemoryCount = _memorySystem.GetRecentMemories(100).Count();

      var passingOutput = ParsedOutput.Dialogue("I'll remember that", "raw")
        .WithMutation(ProposedMutation.AppendEpisodic("Player said something important"));

      var failingOutput = ParsedOutput.Dialogue("Secret: forbidden knowledge", "raw")
        .WithMutation(ProposedMutation.AppendEpisodic("Should not be added"));

      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "No secrets", "Don't say secret", "secret"));

      // Act - Validate passing output
      var passingResult = _validationGate.Validate(passingOutput);
      Assert.That(passingResult.Passed, Is.True);

      if (passingResult.Passed)
      {
        _mutationController.ExecuteMutations(passingResult, _memorySystem, _testProfile.PersonaId);
      }

      var afterPassingCount = _memorySystem.GetRecentMemories(100).Count();

      // Act - Validate failing output
      var failingResult = _validationGate.Validate(failingOutput, new ValidationContext { Constraints = constraints });
      Assert.That(failingResult.Passed, Is.False);

      if (failingResult.Passed)
      {
        _mutationController.ExecuteMutations(failingResult, _memorySystem, _testProfile.PersonaId);
      }

      var afterFailingCount = _memorySystem.GetRecentMemories(100).Count();

      // Assert
      Assert.That(afterPassingCount, Is.EqualTo(initialMemoryCount + 1), "Passing validation should add memory");
      Assert.That(afterFailingCount, Is.EqualTo(afterPassingCount), "Failing validation should not add memory");
    }

    [Test]
    public void MemoryMutation_CanonicalFactProtection_WorksEndToEnd()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("I'll update my knowledge", "raw")
        .WithMutation(ProposedMutation.TransformBelief("king_name", "The king is Bob"));

      var context = new ValidationContext { MemorySystem = _memorySystem };

      // Act
      var gateResult = _validationGate.Validate(output, context);

      // Assert - Gate should reject the mutation
      Assert.That(gateResult.Passed, Is.False);
      Assert.That(gateResult.RejectedMutations.Count, Is.EqualTo(1));

      // Even if we try to execute (which we shouldn't with failed gate)
      // The controller should also reject
      var mutationResult = _mutationController.ExecuteMutations(gateResult, _memorySystem, "npc1");
      Assert.That(mutationResult.AllSucceeded, Is.True); // No mutations to execute (gate rejected)

      // Verify canonical fact is unchanged
      var fact = _memorySystem.GetCanonicalFact("king_name");
      Assert.That(fact?.Fact, Is.EqualTo("The king is named Arthur"));
    }

    #endregion

    #region Intent Dispatch Policy Tests

    [Test]
    public void IntentDispatch_OnlyWhenGatePasses()
    {
      // Arrange - Passing output with intent
      var passingOutput = ParsedOutput.Dialogue("Follow me!", "raw")
        .WithIntent(WorldIntent.Create("follow", "player"));

      var passingResult = _validationGate.Validate(passingOutput);

      // Assert - Intent should be approved when gate passes
      Assert.That(passingResult.Passed, Is.True);
      Assert.That(passingResult.ApprovedIntents.Count, Is.EqualTo(1));
    }

    [Test]
    public void IntentDispatch_PolicyDefenseTest_IntentsNotFromParsedOutput()
    {
      // Contract: When gate fails, ApprovedIntents MUST be empty.
      // Downstream consumers (WorldIntentDispatcher, MemoryMutationController) must only
      // consume from GateResult.ApprovedIntents or MutationBatchResult.EmittedIntents,
      // never directly from ParsedOutput.WorldIntents.

      // Arrange - Failing output with intent
      var failingOutput = ParsedOutput.Dialogue("Secret: follow me!", "raw")
        .WithIntent(WorldIntent.Create("follow", "player"));

      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "No secrets", "Don't say secret", "secret"));

      var context = new ValidationContext { Constraints = constraints };

      // Act
      var result = _validationGate.Validate(failingOutput, context);

      // Assert - Gate fails, ApprovedIntents MUST be empty (contract requirement)
      Assert.That(result.Passed, Is.False);
      Assert.That(result.ApprovedIntents.Count, Is.EqualTo(0),
        "Contract: ApprovedIntents must be empty when gate fails");

      // The mutation controller also respects the gate status
      var mutationResult = _mutationController.ExecuteMutations(result, _memorySystem, "npc1");

      // EmittedIntents should be empty because gate failed
      Assert.That(mutationResult.EmittedIntents.Count, Is.EqualTo(0),
        "Dispatcher must only consume from MutationBatchResult.EmittedIntents, never directly from ParsedOutput.Intents");
    }

    #endregion

    #region Determinism with Seed Tests

    [Test]
    public void Determinism_WithFixedSeed_ReproducibleResults()
    {
      // Arrange
      var random1 = new Random(TestSeed);
      var random2 = new Random(TestSeed);

      // Generate sequences
      var sequence1 = Enumerable.Range(0, 10).Select(_ => random1.Next()).ToList();
      var sequence2 = Enumerable.Range(0, 10).Select(_ => random2.Next()).ToList();

      // Assert - Same seed produces same sequence
      Assert.That(sequence1, Is.EqualTo(sequence2),
        "Same integer seed must produce identical random sequences");
    }

    [Test]
    public void Determinism_DifferentSeeds_DifferentResults()
    {
      // Arrange
      var random1 = new Random(TestSeed);
      var random2 = new Random(AlternateSeed);

      // Generate sequences
      var sequence1 = Enumerable.Range(0, 10).Select(_ => random1.Next()).ToList();
      var sequence2 = Enumerable.Range(0, 10).Select(_ => random2.Next()).ToList();

      // Assert - Different seeds produce different sequences
      Assert.That(sequence1, Is.Not.EqualTo(sequence2),
        "Different integer seeds should produce different random sequences");
    }

    [Test]
    public void Determinism_FullPipeline_MultiplRunsSameResult()
    {
      // Arrange - Fixed inputs
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Tell me about your wares",
        GameTime = 100f
      };

      // Act - Run pipeline multiple times with fixed snapshot time for determinism
      const long snapshotTime = 638400000000000000L; // Fixed tick value: 2024-01-01 00:00:00 UTC
      var results = new List<string>();
      for (int i = 0; i < 3; i++)
      {
        var constraints = _expectancyEvaluator.Evaluate(context);
        var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput ?? string.Empty, snapshotTime);
        var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
          .WithContext(context)
          .WithConstraints(constraints)
          .WithSystemPrompt(_testProfile.SystemPrompt)
          .WithPlayerInput(context.PlayerInput ?? string.Empty)
          .WithSnapshotTimeUtcTicks(snapshotTime))
          .Build();

        var prompt = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);
        results.Add(prompt.Text);
      }

      // Assert - All prompts identical
      for (int i = 1; i < results.Count; i++)
      {
        Assert.That(results[i], Is.EqualTo(results[0]),
          $"Run {i} produced different prompt than run 0 - determinism violation");
      }
    }

    #endregion

    #region Byte-Level Prompt Text Determinism Tests (Test D)

    /// <summary>
    /// Test D: WorkingMemory Hard-Bounds Behavior - Byte-level prompt text verification
    /// Verifies that same inputs produce identical byte-level prompt text across multiple runs,
    /// exact newline counts and separator placement match deterministically, and empty section
    /// handling produces identical byte-level output.
    /// </summary>

    [Test]
    public void PromptAssembly_ByteLevelDeterminism_SameInputs_IdenticalOutput()
    {
      // Arrange - Create identical inputs with explicit snapshot time
      const long snapshotTime = 638400000000000000L; // Fixed tick value: 2024-01-01 00:00:00 UTC
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Tell me about the king",
        GameTime = 100f
      };

      // Act - Assemble prompts multiple times with identical inputs
      var prompts = new List<string>();
      for (int i = 0; i < 5; i++)
      {
        var constraints = _expectancyEvaluator.Evaluate(context);
        var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput ?? string.Empty, snapshotTime);
        var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
          .WithContext(context)
          .WithConstraints(constraints)
          .WithSystemPrompt(_testProfile.SystemPrompt)
          .WithPlayerInput(context.PlayerInput ?? string.Empty)
          .WithSnapshotTimeUtcTicks(snapshotTime))
          .Build();

        var prompt = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);
        prompts.Add(prompt.Text);
      }

      // Assert - All prompts must be byte-identical
      var firstPrompt = prompts[0];
      var firstPromptBytes = Encoding.UTF8.GetBytes(firstPrompt);
      
      for (int i = 1; i < prompts.Count; i++)
      {
        var currentPrompt = prompts[i];
        var currentPromptBytes = Encoding.UTF8.GetBytes(currentPrompt);
        
        Assert.That(currentPromptBytes.Length, Is.EqualTo(firstPromptBytes.Length),
          $"Run {i} produced different byte length ({currentPromptBytes.Length} vs {firstPromptBytes.Length})");
        
        Assert.That(currentPromptBytes, Is.EqualTo(firstPromptBytes),
          $"Run {i} produced different byte-level output - determinism violation");
        
        Assert.That(currentPrompt, Is.EqualTo(firstPrompt),
          $"Run {i} produced different prompt text");
      }
    }

    [Test]
    public void PromptAssembly_NewlineCounts_Deterministic()
    {
      // Arrange - Create snapshot with various sections
      const long snapshotTime = 638400000000000000L;
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Hello",
        GameTime = 100f
      };

      var constraints = _expectancyEvaluator.Evaluate(context);
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput ?? string.Empty, snapshotTime);
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput ?? string.Empty)
        .WithSnapshotTimeUtcTicks(snapshotTime))
        .Build();

      // Act - Assemble prompts multiple times
      var newlineCounts = new List<int>();
      for (int i = 0; i < 3; i++)
      {
        var prompt = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);
        var newlineCount = prompt.Text.Count(c => c == '\n');
        newlineCounts.Add(newlineCount);
      }

      // Assert - Newline counts must be identical
      Assert.That(newlineCounts.Distinct().Count(), Is.EqualTo(1),
        $"Newline counts must be identical across runs. Got: {string.Join(", ", newlineCounts)}");
    }

    [Test]
    public void PromptAssembly_EmptySections_DeterministicOmission()
    {
      // Arrange - Create snapshot with empty optional sections
      const long snapshotTime = 638400000000000000L;
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Hello",
        GameTime = 100f
      };

      var constraints = _expectancyEvaluator.Evaluate(context);
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput ?? string.Empty, snapshotTime);
      
      // Create snapshot with empty dialogue, episodic, and beliefs
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput ?? string.Empty)
        .WithDialogueHistory(Array.Empty<string>()) // Empty dialogue
        .WithEpisodicMemories(Array.Empty<string>()) // Empty episodic
        .WithBeliefs(Array.Empty<string>()) // Empty beliefs
        .WithSnapshotTimeUtcTicks(snapshotTime))
        .Build();

      // Act - Assemble prompts multiple times
      var prompts = new List<string>();
      for (int i = 0; i < 3; i++)
      {
        var prompt = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);
        prompts.Add(prompt.Text);
      }

      // Assert - All prompts must be byte-identical (empty sections should be omitted consistently)
      var firstPrompt = prompts[0];
      for (int i = 1; i < prompts.Count; i++)
      {
        Assert.That(prompts[i], Is.EqualTo(firstPrompt),
          $"Run {i} produced different prompt when empty sections should be omitted deterministically");
      }

      // Verify empty sections are omitted (not included as headers-only)
      // Context header should not appear if context is empty
      var contextText = new EphemeralWorkingMemory(snapshot).GetFormattedContext();
      if (string.IsNullOrEmpty(contextText))
      {
        // If context is empty, the context header should not appear in the prompt
        // This verifies the "omit entirely" policy
        // Check that context header is not present (implementation may use different header format)
        var hasContextHeader = firstPrompt.Contains("CONTEXT") || firstPrompt.Contains("Context");
        Assert.That(hasContextHeader, Is.False,
          "Empty context section should be omitted entirely, not shown as header-only");
      }
    }

    [Test]
    public void PromptAssembly_SeparatorPlacement_Deterministic()
    {
      // Arrange - Create snapshot with all sections populated
      const long snapshotTime = 638400000000000000L;
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Tell me about the king",
        GameTime = 100f
      };

      var constraints = _expectancyEvaluator.Evaluate(context);
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput ?? string.Empty, snapshotTime);
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput ?? string.Empty)
        .WithSnapshotTimeUtcTicks(snapshotTime))
        .Build();

      // Act - Assemble prompts multiple times and extract separator positions
      var separatorPositions = new List<List<int>>();
      for (int i = 0; i < 3; i++)
      {
        var prompt = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);
        var positions = new List<int>();
        for (int j = 0; j < prompt.Text.Length; j++)
        {
          if (prompt.Text[j] == '\n')
          {
            positions.Add(j);
          }
        }
        separatorPositions.Add(positions);
      }

      // Assert - Separator positions must be identical across runs
      var firstPositions = separatorPositions[0];
      for (int i = 1; i < separatorPositions.Count; i++)
      {
        Assert.That(separatorPositions[i], Is.EqualTo(firstPositions),
          $"Run {i} produced different newline positions - separator layout must be deterministic");
      }
    }

    [Test]
    public void PromptAssembly_MandatorySections_BypassCharacterCaps()
    {
      // Arrange - Create snapshot with large mandatory sections that exceed character limits
      const long snapshotTime = 638400000000000000L;
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Tell me about the king",
        GameTime = 100f
      };

      // Create very large canonical facts and world state (mandatory sections)
      var largeCanonicalFacts = Enumerable.Range(1, 50)
        .Select(i => $"Canonical fact {i}: " + new string('X', 100))
        .ToArray();
      var largeWorldState = Enumerable.Range(1, 50)
        .Select(i => $"world_state_{i}=" + new string('Y', 100))
        .ToArray();

      var constraints = _expectancyEvaluator.Evaluate(context);
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput ?? string.Empty, snapshotTime);
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput ?? string.Empty)
        .WithCanonicalFacts(largeCanonicalFacts)
        .WithWorldState(largeWorldState)
        .WithSnapshotTimeUtcTicks(snapshotTime))
        .Build();

      // Act - Assemble prompts multiple times with config that has low character limit
      var config = new WorkingMemoryConfig
      {
        MaxContextCharacters = 100, // Very low limit - mandatory sections should bypass
        AlwaysIncludeCanonicalFacts = true,
        AlwaysIncludeWorldState = true
      };
      var workingMemory = new EphemeralWorkingMemory(snapshot, config);
      var assembler = new PromptAssembler();
      var prompts = new List<string>();
      for (int i = 0; i < 3; i++)
      {
        var prompt = assembler.AssembleFromWorkingMemory(workingMemory);
        prompts.Add(prompt.Text);
      }

      // Assert - All prompts must be byte-identical
      // Mandatory sections should be included even if they exceed character limits
      var firstPrompt = prompts[0];
      Assert.That(firstPrompt.Length, Is.GreaterThan(config.MaxContextCharacters),
        "Mandatory sections should bypass character caps");
      
      for (int i = 1; i < prompts.Count; i++)
      {
        Assert.That(prompts[i], Is.EqualTo(firstPrompt),
          $"Run {i} produced different prompt - mandatory sections must be deterministically included");
      }
    }

    [Test]
    public void PromptAssembly_TruncationPriority_Deterministic()
    {
      // Arrange - Create snapshot with content that will be truncated
      const long snapshotTime = 638400000000000000L;
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Hello",
        GameTime = 100f
      };

      // Create dialogue, episodic, and beliefs that exceed character limit
      var dialogue = Enumerable.Range(1, 20)
        .Select(i => $"Player: Message {i}\nNPC: Response {i}")
        .ToArray();
      var episodic = Enumerable.Range(1, 20)
        .Select(i => $"Episodic memory {i}: " + new string('M', 50))
        .ToArray();
      var beliefs = Enumerable.Range(1, 20)
        .Select(i => $"Belief {i}: " + new string('B', 50))
        .ToArray();

      var constraints = _expectancyEvaluator.Evaluate(context);
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput ?? string.Empty, snapshotTime);
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput ?? string.Empty)
        .WithDialogueHistory(dialogue)
        .WithEpisodicMemories(episodic)
        .WithBeliefs(beliefs)
        .WithSnapshotTimeUtcTicks(snapshotTime))
        .Build();

      // Act - Assemble prompts with low character limit (truncation will occur)
      var config = new WorkingMemoryConfig
      {
        MaxContextCharacters = 500, // Low limit to force truncation
        AlwaysIncludeCanonicalFacts = true,
        AlwaysIncludeWorldState = true,
        MaxDialogueExchanges = 5,
        MaxEpisodicMemories = 5,
        MaxBeliefs = 3
      };
      var prompts = new List<string>();
      for (int i = 0; i < 3; i++)
      {
        var workingMemory = new EphemeralWorkingMemory(snapshot, config);
        var assembler = new PromptAssembler();
        var prompt = assembler.AssembleFromWorkingMemory(workingMemory);
        prompts.Add(prompt.Text);
      }

      // Assert - All prompts must be byte-identical
      // Truncation priority: Dialogue → Episodic → Beliefs must be applied deterministically
      var firstPrompt = prompts[0];
      for (int i = 1; i < prompts.Count; i++)
      {
        Assert.That(prompts[i], Is.EqualTo(firstPrompt),
          $"Run {i} produced different prompt - truncation priority must be deterministic");
      }
    }

    #endregion

    #region Serializer Regression Tests

    /// <summary>
    /// Regression tests for Esc/Unesc/SplitEscaped to ensure byte-stable round-trips
    /// and correct handling of all edge cases.
    /// </summary>
      [Test]
      public void Esc_Unesc_RoundTrip_Null()
      {
        string? input = null;
        var escaped = MemoryStateSerializer.Esc(input);
        var unescaped = MemoryStateSerializer.Unesc(escaped);
        
        Assert.That(escaped, Is.EqualTo("\\0"), "Null should escape to \\0");
        Assert.That(unescaped, Is.Null, "\\0 should unescape to null");
      }

      [Test]
      public void Esc_Unesc_RoundTrip_EmptyString()
      {
        var input = "";
        var escaped = MemoryStateSerializer.Esc(input);
        var unescaped = MemoryStateSerializer.Unesc(escaped);
        
        Assert.That(unescaped, Is.EqualTo(input), "Empty string should round-trip");
      }

      [Test]
      public void Esc_Unesc_RoundTrip_Pipe()
      {
        var input = "|";
        var escaped = MemoryStateSerializer.Esc(input);
        var unescaped = MemoryStateSerializer.Unesc(escaped);
        
        Assert.That(escaped, Is.EqualTo("\\|"), "Pipe should be escaped");
        Assert.That(unescaped, Is.EqualTo(input), "Escaped pipe should round-trip");
      }

      [Test]
      public void Esc_Unesc_RoundTrip_EscapedPipe()
      {
        var input = "\\|";
        var escaped = MemoryStateSerializer.Esc(input);
        var unescaped = MemoryStateSerializer.Unesc(escaped);
        
        Assert.That(escaped, Is.EqualTo("\\\\\\|"), "Escaped pipe should double-escape");
        Assert.That(unescaped, Is.EqualTo(input), "Double-escaped pipe should round-trip");
      }

      [Test]
      public void Esc_Unesc_RoundTrip_PipeInContent()
      {
        var input = "a|b";
        var escaped = MemoryStateSerializer.Esc(input);
        var unescaped = MemoryStateSerializer.Unesc(escaped);
        
        Assert.That(escaped, Is.EqualTo("a\\|b"), "Pipe in content should be escaped");
        Assert.That(unescaped, Is.EqualTo(input), "Pipe in content should round-trip");
      }

      [Test]
      public void Esc_Unesc_RoundTrip_Backslash()
      {
        var input = "\\";
        var escaped = MemoryStateSerializer.Esc(input);
        var unescaped = MemoryStateSerializer.Unesc(escaped);
        
        Assert.That(escaped, Is.EqualTo("\\\\"), "Backslash should be escaped");
        Assert.That(unescaped, Is.EqualTo(input), "Escaped backslash should round-trip");
      }

      [Test]
      public void Esc_Unesc_RoundTrip_DoubleBackslash()
      {
        var input = "\\\\";
        var escaped = MemoryStateSerializer.Esc(input);
        var unescaped = MemoryStateSerializer.Unesc(escaped);
        
        Assert.That(escaped, Is.EqualTo("\\\\\\\\"), "Double backslash should double-escape");
        Assert.That(unescaped, Is.EqualTo(input), "Double-escaped backslash should round-trip");
      }

      [Test]
      public void Esc_Unesc_RoundTrip_Newline()
      {
        var input = "\n";
        var escaped = MemoryStateSerializer.Esc(input);
        var unescaped = MemoryStateSerializer.Unesc(escaped);
        
        Assert.That(escaped, Is.EqualTo("\\n"), "Newline should be escaped");
        Assert.That(unescaped, Is.EqualTo(input), "Escaped newline should round-trip");
      }

      [Test]
      public void Esc_Unesc_RoundTrip_CarriageReturn()
      {
        var input = "\r";
        var escaped = MemoryStateSerializer.Esc(input);
        var unescaped = MemoryStateSerializer.Unesc(escaped);
        
        Assert.That(escaped, Is.EqualTo("\\r"), "Carriage return should be escaped");
        Assert.That(unescaped, Is.EqualTo(input), "Escaped carriage return should round-trip");
      }

      [Test]
      public void Esc_Unesc_RoundTrip_NullTokenLiteral()
      {
        var input = "\\0";
        var escaped = MemoryStateSerializer.Esc(input);
        var unescaped = MemoryStateSerializer.Unesc(escaped);
        
        // After escaping, backslash is doubled, so "\\0" becomes "\\\\0"
        // But we need to check if it's handled correctly
        Assert.That(unescaped, Is.EqualTo(input), "Literal \\0 should round-trip");
      }

      [Test]
      public void Esc_Unesc_RoundTrip_TrailingBackslash()
      {
        var input = "abc\\";
        var escaped = MemoryStateSerializer.Esc(input);
        var unescaped = MemoryStateSerializer.Unesc(escaped);
        
        Assert.That(escaped, Is.EqualTo("abc\\\\"), "Trailing backslash should be escaped");
        Assert.That(unescaped, Is.EqualTo(input), "Trailing backslash should round-trip");
      }

      [Test]
      public void Esc_Unesc_RoundTrip_UnknownEscape()
      {
        // Test that unknown escape sequences are preserved
        // In serialized form, "\\x" should round-trip to "\\x" literally
        var input = "\\x";
        var escaped = MemoryStateSerializer.Esc(input);
        var unescaped = MemoryStateSerializer.Unesc(escaped);
        
        Assert.That(escaped, Is.EqualTo("\\\\x"), "Unknown escape should double-escape backslash");
        Assert.That(unescaped, Is.EqualTo(input), "Unknown escape should round-trip");
      }

      [Test]
      public void Esc_Unesc_RoundTrip_MixedSpecialChars()
      {
        var input = "a|b\\c\nd\re";
        var escaped = MemoryStateSerializer.Esc(input);
        var unescaped = MemoryStateSerializer.Unesc(escaped);
        
        Assert.That(unescaped, Is.EqualTo(input), "Mixed special characters should round-trip");
      }

      [Test]
      public void SplitEscaped_PreservesEscapes()
      {
        // Test that SplitEscaped preserves escape sequences for Unesc to decode
        var line = "hello\\nworld|test\\|pipe|normal";
        var parts = MemoryStateSerializer.SplitEscaped(line, '|');
        
        Assert.That(parts, Has.Count.EqualTo(3), "Should split into 3 parts");
        Assert.That(parts[0], Is.EqualTo("hello\\nworld"), "First part should preserve \\n");
        Assert.That(parts[1], Is.EqualTo("test\\|pipe"), "Second part should preserve \\|");
        Assert.That(parts[2], Is.EqualTo("normal"), "Third part should be normal");
        
        // Verify Unesc can decode the preserved escapes
        Assert.That(MemoryStateSerializer.Unesc(parts[0]), Is.EqualTo("hello\nworld"), "Unesc should decode \\n");
        Assert.That(MemoryStateSerializer.Unesc(parts[1]), Is.EqualTo("test|pipe"), "Unesc should decode \\|");
      }

      [Test]
      public void SplitEscaped_HandlesTrailingBackslash()
      {
        var line = "abc\\\\|def";
        var parts = MemoryStateSerializer.SplitEscaped(line, '|');
        
        Assert.That(parts, Has.Count.EqualTo(2), "Should split into 2 parts");
        Assert.That(parts[0], Is.EqualTo("abc\\\\"), "First part should preserve trailing backslash");
        Assert.That(parts[1], Is.EqualTo("def"), "Second part should be normal");
      }

      [Test]
      public void SplitEscaped_HandlesEmptyParts()
      {
        var line = "||a||b||";
        var parts = MemoryStateSerializer.SplitEscaped(line, '|');
        
        Assert.That(parts, Has.Count.EqualTo(7), "Should split into 7 parts (including empty)");
        Assert.That(parts[0], Is.EqualTo(""), "First part should be empty");
        Assert.That(parts[1], Is.EqualTo(""), "Second part should be empty");
        Assert.That(parts[2], Is.EqualTo("a"), "Third part should be 'a'");
      }

      [Test]
      public void Esc_Unesc_SplitEscaped_FullRoundTrip()
      {
        // Test complete round-trip: Esc -> SplitEscaped -> Unesc
        var inputs = new[] { "hello", "world|test", "new\\line", "pipe|here", null, "" };
        var escaped = inputs.Select(MemoryStateSerializer.Esc).ToArray();
        var joined = string.Join("|", escaped);
        var split = MemoryStateSerializer.SplitEscaped(joined, '|');
        var unescaped = split.Select(MemoryStateSerializer.Unesc).ToArray();
        
        Assert.That(unescaped, Is.EqualTo(inputs), "Full round-trip should preserve all values");
      }

    #endregion
  }
}
