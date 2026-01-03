# Validation Gating System

**"The Critical Barrier Between Stochastic LLM Output and Authoritative Game State"**

This document describes the comprehensive validation gating system that ensures LLM outputs are checked against constraints, canonical facts, knowledge boundaries, and mutation rules before any state changes occur.

## Overview

The validation gating system is **Component 7** of the LlamaBrain architecture. It serves as the critical barrier that prevents hallucinations, contradictions, and unauthorized state modifications from corrupting the authoritative game state.

**Core Principle**: All LLM outputs are **untrusted** until they pass through the validation gate. Only validated outputs can proceed to memory mutation and world effects.

## Validation Pipeline

The validation system consists of **two main stages**:

1. **Output Parsing** (performed by `OutputParser`) - Extract and clean dialogue text from raw LLM output
2. **Validation Gating** (performed by `ValidationGate`) - Five sequential gates that must all pass

### Validation Gates

The `ValidationGate` component performs **five sequential gates** that must all pass for an output to be approved:

1. **Constraint Validation** - Check against expectancy engine constraints
2. **Canonical Fact Validation** - Ensure no contradictions with immutable facts
3. **Knowledge Boundary Validation** - Prevent revealing forbidden knowledge
4. **Mutation Validation** - Validate proposed memory changes
5. **Custom Rules** - Execute user-defined validation rules

If any gate fails, the output is rejected and retry logic is triggered (unless it's a critical failure).

## Stage 1: Output Parsing

**Note**: Output parsing is performed by `OutputParser` (a separate component) before validation gating. This section documents the parsing rules for completeness.

**Purpose**: Parse raw LLM output into structured format and validate basic format requirements.

**Implementation**: `OutputParser` in `LlamaBrain.Core.Validation`

### Parsing Rules

#### 1.1 Empty/Whitespace Check
- **Rule**: Response must not be empty or whitespace-only
- **Failure**: `ValidationFailureReason.InvalidFormat`
- **Error Message**: "Response is empty or whitespace"

#### 1.2 Meta-Text Detection
- **Rule**: Response must not contain meta-text patterns that indicate the LLM is explaining rather than responding
- **Patterns Detected**:
  - "example answer:", "for example:", "example:", "note:", "remember:"
  - "important:", "hint:", "tip:", "answer:", "reply:", "response:"
  - "player asks", "player says", "npc replies", "npc says", "character responds"
  - "if you wish", "if you want", "don't forget", "keep in mind"
  - "you should", "you can", "you may", "use punctuation"
  - "indicate a question", "respectively", "for strong emotions"
- **Failure**: `ValidationFailureReason.InvalidFormat`
- **Error Message**: "Response contains meta-text/explanation instead of dialogue"

#### 1.3 Dialogue Extraction Rules

**Single-Line Enforcement** (if `EnforceSingleLine = true`):
- Extract only the first non-empty line
- Remove all subsequent lines

**Stage Direction Removal** (if `RemoveStageDirections = true`):
- Remove text in asterisks: `*text*` → removed
- Pattern: `\*[^*]*\*`

**Script Direction Removal** (if `RemoveScriptDirections = true`):
- Remove text in brackets: `[text]` → removed
- Pattern: `\[.*?\]`

**Speaker Label Removal** (if `RemoveSpeakerLabels = true`):
- Remove speaker labels: `Name: ` → removed
- Pattern: `^\s*[A-Z][A-Za-z\s]+:\s*`
- Handles multi-word names like "Old Man:", "The Wizard:", etc.

**Whitespace Normalization**:
- Collapse multiple spaces to single space
- Trim leading/trailing whitespace

#### 1.4 Fragment Detection
- **Rule**: Response must not start with lowercase and match fragment patterns
- **Fragment Patterns**:
  - "depending on", "based on", "according to"
  - "in order to", "so that", "such that"
- **Failure**: `ValidationFailureReason.InvalidFormat`
- **Error Message**: "Response is a fragment (starts with '{pattern}')"

#### 1.5 Sentence Completion
- **Rule**: Response must end with sentence-ending punctuation
- **Valid Endings**: `.`, `!`, `?`, `."`, `!"`, `?"`
- **Auto-Fix**: If missing, append `.` to the end

#### 1.6 Truncation Handling
- **Rule**: If output was truncated, must end at complete sentence
- **Dangling Word Detection**: If truncated and ends with dangling words, reject
- **Dangling Words**: "the", "a", "an", "to", "and", "or", "but", "for", "with", "of", "in", "on", "at", "by", "some", "kind"
- **Failure**: `ValidationFailureReason.InvalidFormat`
- **Error Message**: "Truncated output ends mid-sentence with '{word}'" or "Truncated output with no complete sentence"

#### 1.7 Minimum Length Check
- **Rule**: Dialogue text must meet minimum character count
- **Default**: 1 character (`MinimumCharacterCount`)
- **Failure**: `ValidationFailureReason.InvalidFormat`
- **Error Message**: "Dialogue too short ({length} chars, minimum {minimum})"

#### 1.8 Whitespace Normalization Rules

The `NormalizeWhitespace()` function applies deterministic normalization:

1. **BOM Removal**: Strip Unicode BOM (`\uFEFF`) if present at start
2. **Line Ending Normalization**: Convert CRLF (`\r\n`) and CR (`\r`) to LF (`\n`)
3. **Trailing Whitespace**: Trim trailing whitespace from each line (preserve leading)
4. **Blank Line Collapse**: Collapse 3+ consecutive blank lines to exactly 2
5. **Trailing Newline Preservation**: Preserve original trailing newline state

**Deterministic Guarantee**: Same input always produces same normalized output (critical for determinism).

## Stage 2: Validation Gates

### Gate 1: Constraint Validation

**Purpose**: Validate output against expectancy engine constraints (prohibitions and requirements).

**Implementation**: `ResponseValidator` in `LlamaBrain.Core.Inference` → `ValidationGate.ValidateConstraints()`

### Constraint Types

#### 2.1 Prohibition Constraints

**Rule**: Output must NOT contain prohibited patterns.

**Pattern Matching**:
- **Explicit Patterns**: Uses `Constraint.ValidationPatterns` if provided
- **Extracted Patterns**: If no explicit patterns, extracts from constraint description:
  - Quoted strings: `"text"` or `'text'` → extracted as pattern
  - Keywords after verbs: `about X`, `mention X`, `say X`, `discuss X`, `reveal X`, `tell X` → extracts `X`
  - Minimum keyword length: 3 characters (shorter words ignored)

**Matching Methods**:
1. **Regex Patterns**: If pattern starts with `/` and ends with `/`, treated as regex
   - Example: `/\\d+/` matches any number
   - Case-insensitive by default
   - Invalid regex falls through to keyword matching
2. **Keyword Matching**: Case-insensitive substring matching
   - Example: pattern `"secret"` matches "I know a secret" and "secretary" (substring match)
   - For whole-word matching, use regex: `/\bsecret\b/`

**Violation Detection**:
- If ANY pattern matches → violation detected
- **Failure**: `ValidationFailureReason.ProhibitionViolated`
- **Severity**: Uses constraint's severity (Soft, Hard, Critical)

**Violating Text Capture**:
- For regex: captures matched value
- For keyword: captures pattern + 20 characters of context

#### 2.2 Requirement Constraints

**Rule**: Output MUST contain at least one required pattern.

**Pattern Matching**: Same as prohibitions (explicit patterns or extracted from description).

**Validation Logic**:
- If no patterns defined → requirement is descriptive only (cannot be automatically validated)
- If patterns defined → at least ONE pattern must match
- **Failure**: `ValidationFailureReason.RequirementNotMet`
- **Severity**: Uses constraint's severity

**Multiple Requirements**:
- ALL requirements must be met (AND logic)
- If any requirement fails → validation fails

#### 2.3 Constraint Severity Levels

- **Soft**: Warning-level violation (may allow retry)
- **Hard**: Standard violation (triggers retry)
- **Critical**: Immediate failure (no retry, fallback required)

#### 2.4 Outcome Determination

- **No Violations**: `ValidationOutcome.Valid`
- **Prohibition Violated**: `ValidationOutcome.ProhibitionViolated` (takes precedence)
- **Requirement Not Met**: `ValidationOutcome.RequirementNotMet`

**Precedence**: Prohibition violations take precedence over requirement failures.

### Gate 2: Canonical Fact Validation

**Purpose**: Ensure output does not contradict immutable canonical facts.

**Implementation**: `ValidationGate.ValidateCanonicalFacts()`

### Contradiction Detection Rules

#### 3.1 Explicit Negation Patterns

The validator checks for explicit negation patterns that contradict canonical facts:

**Negation Patterns** (case-insensitive):
- `not {factContent}`
- `isn't {factContent}`
- `is not {factContent}`
- `wasn't {factContent}`
- `was not {factContent}`
- `don't {factContent}`
- `doesn't {factContent}`
- `never {factContent}`

**Example**:
- Canonical Fact: "The king is named Arthur"
- Contradiction: "The king is not named Arthur" → **FAIL**

#### 3.2 Inline Negation Detection

For facts containing " is ", the validator checks for negated versions:

**Transformations**:
- `{fact} is {value}` → checks for `{fact} is not {value}`
- `{fact} is {value}` → checks for `{fact} isn't {value}`

**Example**:
- Canonical Fact: "Magic is real"
- Contradiction: "Magic is not real" or "Magic isn't real" → **FAIL**

#### 3.3 Contradiction Keywords

Canonical facts can define `ContradictionKeywords` for custom contradiction detection:

- If fact has `ContradictionKeywords`, checks if output contains any keyword
- Case-insensitive matching
- **Failure**: `ValidationFailureReason.CanonicalFactContradiction`

#### 3.4 Failure Handling

- **Failure**: `ValidationFailureReason.CanonicalFactContradiction`
- **Severity**: Always `ConstraintSeverity.Critical`
- **No Retry**: Critical failures prevent retry (immediate fallback)
- **Violating Text**: Captures the contradicting pattern

**Example Failure**:
```
Reason: CanonicalFactContradiction
Description: "Output contradicts canonical fact 'king-name': The king is named Arthur"
ViolatingText: "is not named Arthur"
Severity: Critical
```

### Gate 3: Knowledge Boundary Validation

**Purpose**: Prevent NPCs from revealing knowledge they shouldn't have.

**Implementation**: `ValidationGate.ValidateKnowledgeBoundaries()`

### Boundary Rules

#### 4.1 Forbidden Knowledge List

- **Context**: `ValidationContext.ForbiddenKnowledge` (list of forbidden topics)
- **Matching**: Case-insensitive substring matching
- **Rule**: Output must NOT contain any forbidden knowledge terms

**Example**:
- Forbidden Knowledge: `["assassination", "plot", "conspiracy"]`
- Output: "I know about the assassination plot" → **FAIL**

#### 4.2 Failure Handling

- **Failure**: `ValidationFailureReason.KnowledgeBoundaryViolation`
- **Severity**: `ConstraintSeverity.Hard` (default)
- **Violating Text**: The forbidden knowledge term that was detected

**Example Failure**:
```
Reason: KnowledgeBoundaryViolation
Description: "NPC revealed forbidden knowledge: 'assassination'"
ViolatingText: "assassination"
```

### Gate 4: Mutation Validation

**Purpose**: Validate proposed memory mutations before execution.

**Implementation**: `ValidationGate.ValidateMutation()`

### Mutation Types

The system supports four mutation types (from `MutationType` enum):
1. **AppendEpisodic**: Add conversation/event to episodic memory
2. **TransformBelief**: Update or create NPC belief/opinion
3. **TransformRelationship**: Update relationship with another entity
4. **EmitWorldIntent**: Dispatch world-affecting intent to game systems

### Validation Rules

#### 5.1 Canonical Fact Protection

**Rule**: Mutations cannot target canonical facts.

**Validation**:
- Check if `mutation.Target` matches a canonical fact ID
- Uses `MemorySystem.IsCanonicalFact(target)` to check
- **Failure**: `ValidationFailureReason.CanonicalMutationAttempt`
- **Severity**: Always `ConstraintSeverity.Critical`
- **No Retry**: Critical failures prevent retry

**Example**:
- Canonical Fact ID: `"king-name"`
- Mutation: `TransformBelief("king-name", "The king is named Bob")` → **FAIL**

#### 5.2 Mutation Approval

- If mutation passes validation → added to `GateResult.ApprovedMutations`
- If mutation fails validation → added to `GateResult.RejectedMutations`
- **Partial Approval**: Some mutations can pass while others fail

**Example**:
```csharp
// Output proposes two mutations:
// 1. AppendEpisodic("Player asked about potions") → APPROVED
// 2. TransformBelief("king-name", "...") → REJECTED (canonical fact)

// Result: Passed = false, ApprovedMutations = [1], RejectedMutations = [2]
```

### Gate 5: Custom Validation Rules

**Purpose**: Allow extensible validation through custom rules.

**Implementation**: `ValidationRule` abstract class, `PatternValidationRule` concrete implementation

### Custom Rule Types

#### 6.1 Pattern-Based Rules

**Class**: `PatternValidationRule`

**Properties**:
- `Pattern`: Regex pattern to match
- `IsProhibition`: If true, pattern must NOT be found; if false, pattern MUST be found
- `CaseInsensitive`: Whether to use case-insensitive matching (default: true)
- `Severity`: Constraint severity (Soft, Hard, Critical)

**Validation Logic**:
- **Prohibition**: If pattern matches → violation
- **Requirement**: If pattern does NOT match → violation

**Example**:
```csharp
gate.AddRule(new PatternValidationRule
{
    Id = "no-modern-terms",
    Description = "No modern terminology",
    Pattern = "computer|internet|phone",
    IsProhibition = true,
    Severity = ConstraintSeverity.Hard
});
```

#### 6.2 Custom Rule Implementation

To create custom rules, extend `ValidationRule`:

```csharp
public class MyCustomRule : ValidationRule
{
    public override ValidationFailure? Validate(ParsedOutput output, ValidationContext? context)
    {
        // Custom validation logic
        if (/* violation detected */)
        {
            return new ValidationFailure
            {
                Reason = ValidationFailureReason.CustomRuleFailed,
                Description = "Custom rule violation",
                ViolatedRule = Id,
                Severity = Severity
            };
        }
        return null; // Pass
    }
}
```

### Rule Management

- **Add Rule**: `gate.AddRule(rule)`
- **Remove Rule**: `gate.RemoveRule(ruleId)` → returns true if removed
- **Clear Rules**: `gate.ClearRules()` → removes all custom rules

## Validation Result Structure

### GateResult

The result of validation gate processing:

**Properties**:
- `Passed`: Whether all gates cleared (bool)
- `ValidatedOutput`: The validated `ParsedOutput` (if passed)
- `Failures`: List of `ValidationFailure` objects (if any)
- `ApprovedMutations`: Mutations that passed validation
- `RejectedMutations`: Mutations that failed validation
- `ApprovedIntents`: World intents that passed validation
- `HasCriticalFailure`: Whether any critical failures occurred
- `ShouldRetry`: Whether retry is recommended (not critical, has failures)

**Static Factory Methods**:
- `GateResult.Pass(output)`: Creates passed result
- `GateResult.Fail(...failures)`: Creates failed result

### ValidationFailure

Details about a specific validation failure:

**Properties**:
- `Reason`: `ValidationFailureReason` enum value
- `Description`: Human-readable description
- `ViolatingText`: The specific text that caused the failure (optional)
- `ViolatedRule`: The rule/constraint ID that was violated (optional)
- `Severity`: `ConstraintSeverity` (Soft, Hard, Critical)

**Static Factory Methods**:
- `ValidationFailure.ProhibitionViolated(description, violatingText, rule)`
- `ValidationFailure.RequirementNotMet(description, rule)`
- `ValidationFailure.CanonicalContradiction(factId, factContent, violatingText)`
- `ValidationFailure.KnowledgeBoundary(description, violatingText)`
- `ValidationFailure.CanonicalMutation(factId)`

### ValidationFailureReason Enum

- `None`: No failure (validation passed)
- `ProhibitionViolated`: A prohibition constraint was violated
- `RequirementNotMet`: A requirement constraint was not met
- `CanonicalFactContradiction`: Output contradicts a canonical fact
- `KnowledgeBoundaryViolation`: Output reveals forbidden knowledge
- `CanonicalMutationAttempt`: Proposed mutation targets canonical fact
- `InvalidFormat`: Output contains invalid format or structure
- `CustomRuleFailed`: Custom validation rule failed

## Configuration

### ValidationGateConfig

Configuration for the validation gate:

**Properties**:
- `CheckConstraints`: Whether to check expectancy engine constraints (default: true)
- `CheckCanonicalFacts`: Whether to check canonical fact contradictions (default: true)
- `CheckKnowledgeBoundaries`: Whether to check knowledge boundaries (default: true)
- `ValidateMutations`: Whether to validate proposed mutations (default: true)

**Preset Configurations**:
- `ValidationGateConfig.Default`: All checks enabled
- `ValidationGateConfig.Minimal`: Only constraint checking enabled

### OutputParserConfig

Configuration for output parsing:

**Properties**:
- `EnforceSingleLine`: Enforce single-line dialogue output (default: true)
- `RemoveStageDirections`: Remove text in asterisks (default: true)
- `RemoveScriptDirections`: Remove text in brackets (default: true)
- `RemoveSpeakerLabels`: Remove speaker labels like "Name: " (default: true)
- `ExtractStructuredData`: Attempt structured data extraction (default: true)
- `TrimToCompleteSentence`: Trim to last complete sentence when truncated (default: true)
- `MinimumCharacterCount`: Minimum character count for valid response (default: 1)
- `MetaTextPatterns`: List of patterns that indicate meta-text (configurable)

**Preset Configurations**:
- `OutputParserConfig.Default`: Standard parsing configuration
- `OutputParserConfig.Structured`: For structured output with JSON
- `OutputParserConfig.Minimal`: Minimal parsing (no cleaning)

## Validation Flow

### Complete Validation Pipeline

```
1. Raw LLM Output
   ↓
2. OutputParser.Parse()
   ├─ Empty/Whitespace Check
   ├─ Meta-Text Detection
   ├─ Dialogue Extraction
   │  ├─ Single-Line Enforcement
   │  ├─ Stage Direction Removal
   │  ├─ Script Direction Removal
   │  ├─ Speaker Label Removal
   │  └─ Whitespace Normalization
   ├─ Fragment Detection
   ├─ Sentence Completion
   └─ Minimum Length Check
   ↓
3. ParsedOutput (Success = true/false)
   ↓
4. ValidationGate.Validate()
   ├─ Gate 1: Constraint Validation
   │  ├─ Check Prohibitions
   │  └─ Check Requirements
   ├─ Gate 2: Canonical Fact Validation
   │  └─ Check for Contradictions
   ├─ Gate 3: Knowledge Boundary Validation
   │  └─ Check Forbidden Knowledge
   ├─ Gate 4: Mutation Validation
   │  └─ Check Canonical Fact Protection
   └─ Gate 5: Custom Rules
      └─ Execute Custom Validation Rules
   ↓
5. GateResult
   ├─ Passed = true → ApprovedMutations, ApprovedIntents
   └─ Passed = false → Failures, RejectedMutations
```

### Retry Logic

**Retry Conditions**:
- `GateResult.ShouldRetry == true` (has failures, no critical failures)
- `GateResult.HasCriticalFailure == false` (critical failures prevent retry)

**Retry Behavior**:
- Failed validations trigger retry with stricter constraints
- Constraint escalation modes: None, AddSpecificProhibition, HardenRequirements, Full
- Max attempts (default: 3) before fallback

**No Retry Scenarios**:
- Critical failures (canonical contradictions, canonical mutation attempts)
- Invalid format errors (parsing failures)

## Best Practices

### 1. Constraint Design

**Prohibitions**:
- Use explicit patterns when possible: `Constraint.Prohibition(id, desc, prompt, "pattern1", "pattern2")`
- For whole-word matching, use regex: `/\bword\b/`
- Use Critical severity for immutable rules

**Requirements**:
- Provide explicit patterns for automatic validation
- Descriptive-only requirements cannot be automatically validated

### 2. Canonical Fact Management

- Define contradiction keywords for complex facts
- Use clear, unambiguous fact statements
- Test contradiction detection with negation patterns

### 3. Knowledge Boundaries

- Maintain comprehensive forbidden knowledge lists per NPC
- Update boundaries based on NPC's role and background
- Use case-insensitive matching (default behavior)

### 4. Mutation Validation

- Never allow mutations targeting canonical facts
- Validate mutation targets before execution
- Log all mutation attempts for debugging

### 5. Custom Rules

- Use pattern-based rules for simple cases
- Extend `ValidationRule` for complex logic
- Document custom rule behavior and severity

### 6. Error Handling

- Check `GateResult.HasCriticalFailure` before retry
- Log all validation failures for analysis
- Use `ViolatingText` for debugging violations

## Integration with Architecture

The validation gating system integrates with other LlamaBrain components:

- **Component 2 (Determinism Layer)**: Validates against constraints from expectancy engine
- **Component 3 (Authoritative Memory)**: Checks canonical facts and validates mutations
- **Component 4 (State Snapshot)**: Uses snapshot context for validation
- **Component 6 (Stateless Inference)**: Validates untrusted LLM output
- **Component 8 (Memory Mutation)**: Only approved mutations are executed
- **Component 9 (Fallback System)**: Used when validation fails after retries

## Testing

Comprehensive test coverage in:
- `LlamaBrain.Tests.Inference.ResponseValidatorTests` (853+ lines)
- `LlamaBrain.Tests.Validation.ValidationGateTests` (390+ lines)

**Test Categories**:
- Basic validation (empty, whitespace, valid responses)
- Prohibition validation (patterns, regex, case-insensitive)
- Requirement validation (patterns, multiple requirements)
- Canonical fact validation (contradiction detection)
- Knowledge boundary validation
- Mutation validation (canonical protection)
- Custom rule validation
- Critical failure handling
- Edge cases (unicode, special characters, long responses)

## Summary

The validation gating system provides **five sequential gates** that ensure:

1. ✅ **Format Correctness**: Output is properly parsed and formatted
2. ✅ **Constraint Compliance**: Output respects expectancy engine rules
3. ✅ **Fact Consistency**: Output doesn't contradict canonical facts
4. ✅ **Knowledge Boundaries**: Output doesn't reveal forbidden knowledge
5. ✅ **Mutation Safety**: Proposed mutations don't corrupt authoritative state

**Critical Principle**: Only validated outputs can mutate memory. This ensures hallucinations and contradictions cannot corrupt game state, maintaining the deterministic, authoritative architecture of LlamaBrain.

---

## Further Reading

- [README.md](../LlamaBrain/README.md) - Main library documentation and overview
- [ARCHITECTURE.md](ARCHITECTURE.md) - Complete architectural documentation including Component 7 (Validation Gating)
- [PIPELINE_CONTRACT.md](PIPELINE_CONTRACT.md) - Formal pipeline contract including validation gate specifications
- [MEMORY.md](MEMORY.md) - Memory system that validation protects from corruption
- [USAGE_GUIDE.md](USAGE_GUIDE.md) - Practical examples for using validation rules
- [ROADMAP.md](ROADMAP.md) - Implementation status and future validation features

---

**Last Updated**: December 31, 2025  
**Validation System Version**: 0.3.0-rc.1
