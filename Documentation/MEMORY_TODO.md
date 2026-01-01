## Feature Upgrade Summary: “I’ve seen this” Recognition

### Goal

Add a deterministic pre-LLM recognition step that detects repetition (locations, topics, conversations) and influences the next generated line. Examples: NPC gets tired of seeing the same tunnel repeatedly, or gets tired of the player obsessively talking about manta rays.

### Proven So Far

* End-to-end generation works in Unity (interaction -> LLM -> subtitle output, ~263 ms success).
* Ephemeral episodic logging works (player prompt + NPC line recorded).
* Introspection overlay is live (agent identity, inference status, memory categories).

### Not Proven Yet

* Retrieval influences generation (later output explicitly conditioned on earlier episodic state).
* Deterministic reconstruction (byte-stable prompt assembly, deterministic validation and mutation).
* Validation/gating behavior (rules executed, failures/retries, blocked outputs).
* Deterministic fallback path (forced failure -> stable fallback selection + logging).
* Memory lifecycle (guaranteed ephemeral across scene/domain resets).

### Related Roadmap Work

* **Phase 11: RAG-Based Memory Retrieval & Memory Proving** - This feature is part of Phase 11. The phase includes:
  - Enhancing memory lookup with semantic search using embeddings and vector similarity (replacing keyword-based matching)
  - Implementing the repetition recognition system described in this document (locations, topics, conversations)
  - Proving that retrieval influences generation through deterministic repetition recognition
  - All components needed for repetition recognition: `RecognitionResult`, repetition event tracking, prompt constraint injection, and validation
* **Phase 12: Dedicated Structured Output** - Switching from regex-based text parsing to LLM-native structured output formats (JSON mode, function calling, schema-based outputs) for more reliable and deterministic parsing.
* **Phase 13: Structured Output Integration** - Full integration of structured output with validation pipeline, mutation extraction, and backward compatibility with existing parsing system.

---

## Minimal Deterministic Implementation

### 1) Record Repetition Events

Write episodic events for repetition detection:

**Location Repetition** (Tier A - ship now):
* `Kind = EnteredLocation`
* `LocationId` (author-tagged stable ID)
* `TimeIndex` (monotonic tick, not wall clock)

**Topic/Conversation Repetition** (Tier B - after Tier A):
* `Kind = RepeatedTopic`
* `TopicId` (extracted from player input via semantic similarity or keyword matching)
* `TopicText` (normalized topic text for matching)
* `TimeIndex` (monotonic tick)

### 2) Deterministic Recognition Query

On interaction trigger (location entry or player input):

**For Location Recognition:**
* If `LocationId` already exists in episodic memory: `Recognized = true`
* Track `VisitCount`, `LastVisitTick`, `EvidenceSummary`

**For Topic Recognition:**
* Query episodic memory for similar topics (using RAG semantic similarity or keyword matching)
* If similar topic found with `RepeatCount >= threshold`: `Recognized = true`
* Track `RepeatCount`, `LastMentionTick`, `TopicText`, `EvidenceSummary`

Return a pure DTO:

```csharp
public readonly record struct RecognitionResult(
  bool Recognized,
  RecognitionType Type, // Location, Topic, Conversation
  string RecognizedId, // LocationId or TopicId
  int RepeatCount,
  long LastOccurrenceTick,
  string EvidenceSummary
);

public enum RecognitionType
{
  Location,
  Topic,
  Conversation
}
```

### 3) Prompt Constraint Injection

If recognized, inject a fixed-format block into prompt assembly:

* Hard constraint: “include a brief familiarity cue in one clause; no fourth-wall break.”
* Evidence is optional flavor, not lore.

This is the mechanism that proves retrieval influence.

### 4) Memory Mutation

After generation:

* Append `EnteredLocation` (dedupe per tick)
* Append `SpokeLine` with `RecognitionUsed=true/false`

---

## Proof Plan

### Tests

**Location Recognition:**
1. Domain: two entries into same `LocationId` -> `Recognized=true`, `RepeatCount=2`.
2. Integration: second visit prompt bytes include `RECOGNITION` block; validator confirms cue present.
3. Determinism: identical memory state + identical entry event -> identical `RecognitionResult` + identical prompt text bytes.

### On-Screen Demo Acceptance Criteria

**Location Recognition:**
* Overlay shows `RecognitionResult(Recognized=true, Type=Location, RepeatCount>=2, ...)`.
* Overlay shows prompt section containing the `RECOGNITION` block.
* Overlay shows validation result “recognition cue present”.
* Subtitle on second visit contains a short familiarity clause (“been here before”, “feels familiar”, etc.).
