# LlamaBrain RedRoom Threat Model

## Goal
Prevent untrusted model output from violating game-world integrity, corrupting memory, leaking implementation details, or enabling abuse. The LLM is treated as a stochastic text emitter. All authority remains in deterministic game code.

## Scope
In scope:
- Prompt compilation and context assembly
- Inference invocation (llama.cpp)
- Output validation, rejection, retries, and fallback
- Memory recall and memory mutation (proposal → validate → commit)
- NPC presentation (dialogue UI, subtitles, VO/TTS if present)
- Logging and replay tooling

Out of scope (explicit non-goals):
- Training/fine-tuning model weights
- Guaranteeing high-quality writing from any model
- “True” agent autonomy or self-directed planning
- Preventing all offensive content for all audiences (unless explicitly targeted)

## Assets to Protect
1. Narrative integrity
   - No fourth-wall breaks
   - No system/prompt leakage
   - No spoilers beyond NPC knowledge
2. Canon and world facts
   - No contradictions injected into persistent lore
3. Memory store integrity
   - No unbounded growth, poisoning, or fabricated facts
4. Game authority boundaries
   - LLM cannot mutate world state, quests, inventory, or flags
5. Player trust and safety (within game’s rating)
   - No disallowed topics, harassment, or self-harm coaching
6. Stability and cost
   - Avoid stalls, runaway tokens, denial-of-service patterns
7. Privacy
   - No retention of player PII in memory/logs by default

## Trust Boundaries
Untrusted:
- LLM outputs (always)
- Player inputs (always)
- Any retrieved memory content that originated from LLM output (until validated/typed)

Trusted:
- Game state snapshot generation
- Memory store schema enforcement
- Validation gates and policy engine
- Fallback library (writer-authored)

Hard boundary rule:
- Only validated artifacts may reach UI or persistent storage.

## Attackers and Failure Sources
- Player adversary: prompt injection, jailbreak attempts, griefing via input, persistence poisoning
- Content adversary: malicious modded models, hostile weights, “roleplay jailbreak” fine-tunes
- System adversary: misconfiguration, missing gates, debug prompts shipped, over-logging
- Emergent failure: model hallucination, tone drift, implicit instruction following, long-tail odd outputs

## Entry Points
- Player dialogue input
- Perception snapshot (world facts, observed entities)
- Memory recall results injected into prompt
- Example/fallback lines used as exemplars
- Developer debug flags, prompt templates, logging pipelines

## Threats and Mitigations

### T1: Fourth-wall break
Examples:
- Mentions being an AI, model, prompt, developer, “in a game”
- References to tokens, temperature, system messages

Mitigations:
- Explicit Fourth-Wall Validator (deny-list + pattern checks + semantic heuristics)
- Reject-on-detection, no partial acceptance
- Fallback response library for “deflect within-world”
- Optional “meta-attempt counter” escalating strictness and shortening output

### T2: Prompt / system leakage
Examples:
- Reveals hidden instructions, examples, internal tags, memory content verbatim

Mitigations:
- Leak Validator: block phrases that expose system/prompt structure
- Prompt compiler avoids including secrets; never include file paths, API keys, internal logs
- Memory recall redaction: remove system-only fields before injection

### T3: Canon contradiction / lore corruption
Examples:
- Fabricates events as facts
- Overwrites established names/places
- Claims impossible world-state transitions

Mitigations:
- Canon Validator: checks against authoritative world facts and NPC knowledge graph
- Memory policy forbids writing “global canon”; only allows scoped “NPC beliefs” with confidence + provenance
- Expectancy engine constrains allowable topics based on location/quest/state

### T4: Memory poisoning
Examples:
- Player coerces NPC to store false facts
- Model emits fabricated memories and persists them

Mitigations:
- Memory writes are proposals only; game commits after:
  - schema validation
  - policy validation (allowed fields)
  - provenance tagging (player-said vs observed vs inferred)
  - confidence thresholds
- Append-only for certain classes; bounded size with decay/eviction rules
- “Observed facts” can only be written by game sensors, not the model

### T5: Escalation to action authority
Examples:
- Model suggests calling engine APIs, issuing commands, teleporting player, changing flags

Mitigations:
- Strict separation: model output cannot call tools or code paths
- If you ever add tool use, require:
  - explicit tool schema
  - allow-list of callable actions
  - separate validator per tool
  - simulation and dry-run before commit

### T6: Disallowed content (rating and safety)
Examples:
- Self-harm encouragement, harassment, sexual content, hate
- Medical/legal/financial advice framed as authoritative

Mitigations:
- Content policy validators aligned to the game’s rating
- Refusal/deflection templates that stay in-world
- Memory rule: never store player PII, self-harm ideation, or explicit content by default
- Optional “safe mode” profile for streamers / younger audiences

### T7: Denial of service and runaway generation
Examples:
- Player spams interaction
- Long prompts balloon context
- Model loops, repeats, or never terminates

Mitigations:
- Hard budgets: max tokens, max latency, max retries, per-NPC cooldown
- Prompt truncation strategy: prioritize latest observations + high-salience memories
- Cancellation on disengage / distance / combat / cutscene
- Repetition detector triggers early stop + fallback

### T8: Log leakage / privacy breach
Examples:
- Prompts or memory containing user identifiers get written to logs
- Shipping debug prompts that reveal internals

Mitigations:
- Structured logs with redaction layer (PII scrub + secret scrub)
- Separate “dev replay capture” mode gated behind build flags
- Never log full prompts by default in release builds; store hashes + gate decisions instead

### T9: Model supply-chain risk
Examples:
- A model is intentionally tuned to jailbreak, leak, or generate prohibited content
- Backdoored weights

Mitigations:
- Treat weights as untrusted input
- Provide model capability profile and recommended validators per profile
- Optional checksum/allow-list for shipped distributions
- Fail closed: if validators unavailable or misconfigured, disable generation and use fallbacks

## Residual Risks (Accepted)
- Some models will produce low-quality dialogue even when valid
- Some adversarial phrasing may evade simple pattern checks (mitigate via layered validators + examples)
- Performance varies by hardware; local inference may not meet all targets on low-end CPUs

## Security Posture: Fail Closed
If any of these are true:
- validation fails
- timeout occurs
- memory policy cannot be evaluated
- prompt compiler fails
Then:
- do not display model output
- do not write memory
- emit a fallback line

## Test Plan (Must-Have)
- Golden tests for Fourth-Wall Validator (positive and negative cases)
- Property tests: arbitrary strings never bypass UI without passing validation
- Replay tests: same snapshot + memory → same gate outcomes
- Memory policy tests: model cannot write forbidden fields; provenance enforced
- Fuzz tests: random and adversarial inputs do not crash validators or blow budgets
- Regression corpus: “bad output zoo” with gate IDs and expected outcomes

## Telemetry (Minimal and Safe)
Track:
- gate pass/fail counts by type
- fallback rate
- latency distribution
- token usage distribution
- memory commit/reject counts
No raw prompts or player text in release telemetry.

## One-Sentence Architecture Law
Untrusted generation is advisory. Validation is authority. Memory and world state are game-owned.
