# Test Coverage Report

**Generated:** 2026-01-03
**Overall Coverage:** 90.0% line coverage (7,541/8,378 lines), 83.46% branch coverage (2,267/2,716 branches)
**Total Files Analyzed:** 78 source files
**Total Tests:** 2,126 tests (all passing)

## Summary

| Metric | Value |
|--------|-------|
| Files with 0% coverage | 2 ⚠️ |
| Files with < 50% coverage | 0 ✅ |
| Files with 50-80% coverage | 6 |
| Files with >= 80% coverage | 70 |

## Coverage Changes (2026-01-03)

### New Files with 0% Coverage (2 files)

| File | Coverage | Branch | Priority |
|------|----------|--------|----------|
| `Persistence\Dtos\DialogueEntryDto.cs` | 0% | 0% | Low - DTO class |
| `Persistence\ConversationHistorySnapshot.cs` | 0% | 0% | Low - snapshot class |

### Files Still Below 80% (6 remaining)

| File | Coverage | Branch | Priority |
|------|----------|--------|----------|
| `Persistence\SaveData.cs` | 66.67% | 0% | Medium - persistence logic |
| `Core\ServerManager.cs` | 69.21% | 89.55% ✅ | Medium - complex process management |
| `Utilities\FileSystem.cs` | 70.91% | 0% | Medium - file operations |
| `Core\StructuredOutput\StructuredDialoguePipeline.cs` | 72.38% | 79.17% | Medium - needs integration tests |
| `Core\StructuredOutput\JsonSchemaBuilder.cs` | 79.26% | 89.29% | Low - very close to 80% |
| `Utilities\ProcessUtils.cs` | 79.55% | 100% ✅ | Low - perfect branch coverage |

## Files with Good Coverage (>= 80%)

**70 files** have good test coverage (>= 80%):

**Perfect Coverage (100%):**
- `Core\PromptComposer.cs` - 379/379 lines, 100% branch
- `Core\Metrics\DialogueInteraction.cs` - 132/132 lines, 100% branch
- `Persona\PersonaProfile.cs` - 45/45 lines, 100% branch
- `Persona\MemoryTypes\EpisodicMemory.cs` - 44/44 lines, 100% branch
- `Core\Expectancy\ConstraintSet.cs` - 69/69 lines, 100% branch
- `Core\DialogueSession.cs` - 81/81 lines, 100% branch
- `Core\Inference\InferenceResult.cs` - 120/120 lines, 100% branch
- `Persona\MemoryTypes\CanonicalFact.cs` - 15/15 lines, 100% branch
- `Core\Inference\EphemeralWorkingMemory.cs` - 255/256 lines, 100% branch
- `Core\Inference\StateSnapshot.cs` - 168/168 lines, 100% branch
- `Core\Expectancy\ExpectancyEvaluator.cs` - 42/42 lines, 100% branch
- `Persona\MemoryTypes\BeliefMemory.cs` - 63/63 lines, 100% branch
- `Core\ProcessConfig.cs` - 13/13 lines
- `Core\ServerModels.cs` - 11/11 lines
- `Utilities\Logger.cs` - 3/3 lines
- `Core\StructuredInput\ContextSerializer.cs` - 39/39 lines, 100% branch
- `Core\ApiContracts.cs` - 46/46 lines, 100% branch
- `Persona\IIdGenerator.cs` - 17/17 lines
- `Core\FunctionCalling\FunctionCall.cs` - 52/52 lines, 100% branch
- `Core\FunctionCalling\FunctionCallResult.cs` - 26/26 lines, 100% branch
- `Core\FunctionCalling\FunctionCallExecutor.cs` - 32/32 lines, 100% branch
- `Core\StructuredOutput\StructuredPipelineResult.cs` - 53/53 lines, 100% branch
- `Persistence\MemorySnapshotBuilder.cs` - 88/88 lines, 100% branch
- `Persistence\Dtos\WorldStateDto.cs` - 9/9 lines
- `Persistence\Dtos\EpisodicMemoryDto.cs` - 11/11 lines
- `Persistence\Dtos\BeliefDto.cs` - 13/13 lines
- `Persistence\Dtos\CanonicalFactDto.cs` - 8/8 lines
- `Persistence\PersonaMemorySnapshot.cs` - 10/10 lines
- `Persistence\SaveResult.cs` - 15/15 lines
- `Persistence\SaveSlotInfo.cs` - 5/6 lines (83.33%)
- `Core\StructuredInput\Schemas\ContextSection.cs` - 14/14 lines
- `Core\StructuredInput\Schemas\ConstraintSection.cs` - 3/3 lines
- `Core\StructuredInput\Schemas\DialogueSection.cs` - 4/4 lines
- `Core\StructuredInput\Schemas\ContextJsonSchema.cs` - 4/4 lines
- `Core\StructuredInput\StructuredContextConfig.cs` - 22/22 lines
- `Core\StructuredOutput\StructuredOutputConfig.cs` - 21/21 lines
- `Core\StructuredOutput\StructuredOutputParameters.cs` - 31/31 lines
- `Core\StructuredOutput\StructuredPipelineConfig.cs` - 24/24 lines
- `Core\Expectancy\InteractionContext.cs` - 32/32 lines
- `Persona\IClock.cs` - 16/16 lines

**Excellent Coverage (95-99%):**
- `Core\Inference\EphemeralWorkingMemory.cs` - 99.61% (255/256 lines, 100% branch)
- `Core\FallbackSystem.cs` - 98.59% (210/213 lines, 100% branch)
- `Core\FunctionCalling\FunctionCallDispatcher.cs` - 98.77% (80/81 lines, 100% branch)
- `Core\Inference\RetryPolicy.cs` - 98.78% (81/82 lines, 100% branch)
- `Core\Inference\ContextRetrievalLayer.cs` - 97.91% (187/191 lines, 100% branch)
- `Core\Expectancy\Constraint.cs` - 97.44% (38/39 lines)
- `Core\Validation\OutputParser.cs` - 92.44% (330/357 lines, 97.14% branch)
- `Core\Inference\ResponseValidator.cs` - 96.73% (148/153 lines, 100% branch)
- `Core\StructuredOutput\LlamaCppStructuredOutputProvider.cs` - 96.72% (59/61 lines, 100% branch)

**Very Good Coverage (90-95%):**
- `Core\LlmConfig.cs` - 91.07% (51/56 lines, 100% branch)
- `Core\ClientManager.cs` - 91.49% (43/47 lines, 100% branch)
- `Core\Validation\ParsedOutput.cs` - 93.96% (140/149 lines, 100% branch)
- `Persona\AuthoritativeMemorySystem.cs` - 93.79% (287/306 lines, 98.25% branch)
- `Core\Validation\ValidationGate.cs` - 93.12% (298/320 lines, 97.3% branch)
- `Persona\PersonaProfileManager.cs` - 93.47% (186/199 lines, 100% branch)
- `Persona\PersonaMemoryFileStore.cs` - 93.04% (147/158 lines, 100% branch)
- `Core\Inference\PromptAssembler.cs` - 90.44% (227/251 lines, 89.66% branch)
- `Core\StructuredOutput\StructuredPipelineMetrics.cs` - 90.14% (64/71 lines, 66.67% branch)
- `Core\ApiClient.cs` - 89.61% (388/433 lines, 98.44% branch)
- `Persistence\MemorySnapshotRestorer.cs` - 89.11% (90/101 lines, 85.71% branch)

**Good Coverage (80-90%):**
- `Persona\MemoryTypes\MemoryEntry.cs` - 92% (23/25 lines)
- `Persona\MemoryMutationController.cs` - 83.9% (245/292 lines, 76.36% branch)
- `Persona\MemoryTypes\WorldState.cs` - 88.46% (23/26 lines, 100% branch)
- `Core\StructuredOutput\StructuredSchemaValidator.cs` - 85.79% (163/190 lines, 97.44% branch)
- `Core\StructuredInput\LlamaCppStructuredContextProvider.cs` - 84.94% (141/166 lines, 100% branch)
- `Core\FunctionCalling\BuiltInContextFunctions.cs` - 87.56% (190/217 lines, 100% branch)
- `Persona\PersonaMemoryStore.cs` - 82.76% (96/116 lines, 88.24% branch)
- `Core\BrainAgent.cs` - 82.99% (239/288 lines, 90.38% branch)
- `Utilities\JsonUtils.cs` - 83.12% (192/231 lines, 100% branch)
- `Utilities\PathUtils.cs` - 81.3% (100/123 lines, 96% branch)
- `Persistence\FileSystemSaveSystem.cs` - 81.2% (108/133 lines, 100% branch)

See `coverage-analysis.csv` for complete details.

## Regenerating the Coverage Report

### Step 1: Run Tests with Coverage Collection

```powershell
dotnet test --collect:"XPlat code coverage" --settings coverlet.runsettings --results-directory TestResults
```

### Step 2: Analyze Coverage Data

```powershell
.\analyze-coverage.ps1 -CoverageFile "TestResults\<guid>\coverage.cobertura.xml"
```

### Step 3: Update This Report

1. Review the console output and `coverage-analysis.csv`
2. Update the coverage percentages in this markdown file
3. Update the "Generated" date at the top

### Scripts and Files

- **`coverlet.runsettings`** - Coverlet configuration (excludes test files)
- **`analyze-coverage.ps1`** - PowerShell script for coverage analysis
- **`coverage-analysis.csv`** - Detailed coverage metrics for all files
