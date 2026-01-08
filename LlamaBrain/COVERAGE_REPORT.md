# Test Coverage Report

**Generated:** 2026-01-07
**Overall Coverage:** 91.3% line coverage (9,614/10,530 lines), 84.63% branch coverage (2,902/3,429 branches)
**Total Files Analyzed:** 102 source files
**Total Tests:** 2,254 tests (2,246 passed, 8 failed, 5 skipped)

## Summary

| Metric | Value |
|--------|-------|
| Files with 0% coverage | 1 |
| Files with < 50% coverage | 0 ✅ |
| Files with 50-80% coverage | 4 |
| Files with >= 80% coverage | 97 |

## Coverage Changes (2026-01-07)

### Files Below 80% (5 remaining)

| File | Coverage | Branch | Priority |
|------|----------|--------|----------|
| `Core\Audit\AuditCaptureContext.cs` | 0% | 0% | High - new file with no coverage |
| `Core\ServerManager.cs` | 68.8% | 89.55% ✅ | Medium - complex process management |
| `Core\StructuredOutput\IntentParameters.cs` | 70.68% | 84% | Medium - structured output parameters |
| `Core\StructuredInput\PartialContextBuilder.cs` | 73.02% | 100% ✅ | Low - perfect branch coverage |
| `Utilities\ProcessUtils.cs` | 79.55% | 100% ✅ | Low - perfect branch coverage. Line coverage would require DI refactoring |

## Files with Good Coverage (>= 80%)

**97 files** have good test coverage (>= 80%):

**Perfect Coverage (100%):**
- `Core\PromptComposer.cs` - 379/379 lines, 100% branch
- `Core\Metrics\DialogueInteraction.cs` - 139/139 lines, 100% branch
- `Persona\PersonaProfile.cs` - 45/45 lines, 100% branch
- `Persona\MemoryTypes\EpisodicMemory.cs` - 44/44 lines, 100% branch
- `Core\Expectancy\ConstraintSet.cs` - 69/69 lines, 100% branch
- `Core\DialogueSession.cs` - 81/81 lines, 100% branch
- `Core\Inference\InferenceResult.cs` - 120/120 lines, 100% branch
- `Persona\MemoryTypes\CanonicalFact.cs` - 15/15 lines, 100% branch
- `Core\Inference\StateSnapshot.cs` - 168/168 lines, 100% branch
- `Core\Expectancy\ExpectancyEvaluator.cs` - 42/42 lines, 100% branch
- `Persona\MemoryTypes\BeliefMemory.cs` - 63/63 lines, 100% branch
- `Core\ProcessConfig.cs` - 13/13 lines
- `Core\ServerModels.cs` - 11/11 lines
- `Utilities\Logger.cs` - 3/3 lines
- `Core\StructuredInput\ContextSerializer.cs` - 39/39 lines, 100% branch
- `Core\ApiContracts.cs` - 47/47 lines, 100% branch
- `Persona\IIdGenerator.cs` - 17/17 lines
- `Core\FunctionCalling\FunctionCall.cs` - 52/52 lines, 100% branch
- `Core\FunctionCalling\FunctionCallResult.cs` - 26/26 lines, 100% branch
- `Core\FunctionCalling\FunctionCallExecutor.cs` - 32/32 lines, 100% branch
- `Core\StructuredOutput\StructuredPipelineResult.cs` - 53/53 lines, 100% branch
- `Persistence\MemorySnapshotBuilder.cs` - 89/89 lines, 100% branch
- `Persistence\Dtos\WorldStateDto.cs` - 9/9 lines
- `Persistence\Dtos\EpisodicMemoryDto.cs` - 11/11 lines
- `Persistence\Dtos\BeliefDto.cs` - 13/13 lines
- `Persistence\Dtos\CanonicalFactDto.cs` - 8/8 lines
- `Persistence\PersonaMemorySnapshot.cs` - 11/11 lines
- `Persistence\SaveResult.cs` - 15/15 lines
- `Core\StructuredInput\Schemas\ContextSection.cs` - 15/15 lines
- `Core\StructuredInput\Schemas\ConstraintSection.cs` - 18/18 lines
- `Core\StructuredInput\Schemas\DialogueSection.cs` - 10/10 lines
- `Core\StructuredInput\Schemas\ContextJsonSchema.cs` - 4/4 lines
- `Core\StructuredInput\StructuredContextConfig.cs` - 22/22 lines
- `Core\StructuredOutput\StructuredOutputConfig.cs` - 21/21 lines
- `Core\StructuredOutput\StructuredOutputParameters.cs` - 31/31 lines
- `Core\StructuredOutput\StructuredPipelineConfig.cs` - 24/24 lines
- `Core\Expectancy\InteractionContext.cs` - 32/32 lines
- `Persona\IClock.cs` - 16/16 lines
- `Utilities\FileSystem.cs` - 55/55 lines, 100% branch
- `Persistence\SaveData.cs` - 12/12 lines
- `Persistence\Dtos\DialogueEntryDto.cs` - 3/3 lines
- `Persistence\ConversationHistorySnapshot.cs` - 3/3 lines
- `Core\Audit\DebugPackage.cs` - 44/44 lines, 100% branch
- `Core\Audit\AuditRecordBuilder.cs` - 131/131 lines, 100% branch
- `Core\Audit\ModelFingerprint.cs` - 29/29 lines, 100% branch
- `Core\Audit\ImportResult.cs` - 31/31 lines, 100% branch
- `Core\Metrics\CacheEfficiencyMetrics.cs` - 61/61 lines, 100% branch
- `Core\StructuredInput\Schemas\RelationshipEntry.cs` - 59/59 lines, 100% branch
- `Core\Inference\KvCacheConfig.cs` - 26/26 lines
- `Core\Audit\ExportOptions.cs` - 6/6 lines
- `Core\Audit\AuditRecord.cs` - 27/27 lines

**Excellent Coverage (95-99%):**
- `Core\Audit\ReplayEngine.cs` - 99.25% (133/134 lines, 100% branch)
- `Core\FunctionCalling\FunctionCallDispatcher.cs` - 98.77% (80/81 lines, 100% branch)
- `Core\Inference\RetryPolicy.cs` - 98.78% (81/82 lines, 100% branch)
- `Core\FallbackSystem.cs` - 98.59% (210/213 lines, 100% branch)
- `Core\Audit\AuditRecorder.cs` - 98% (98/100 lines, 100% branch)
- `Core\Inference\ContextRetrievalLayer.cs` - 97.91% (187/191 lines, 100% branch)
- `Core\StructuredOutput\RelationshipAuthorityValidator.cs` - 97.5% (117/120 lines, 100% branch)
- `Core\Expectancy\Constraint.cs` - 97.44% (38/39 lines)
- `Core\Audit\AuditHasher.cs` - 97.22% (35/36 lines, 100% branch)
- `Core\Audit\DriftDetector.cs` - 95.96% (95/99 lines, 100% branch)
- `Core\Audit\ReplayResult.cs` - 96.63% (86/89 lines, 100% branch)
- `Core\Audit\RingBuffer.cs` - 96.84% (92/95 lines, 100% branch)
- `Core\Inference\ResponseValidator.cs` - 96.73% (148/153 lines, 100% branch)
- `Core\StructuredOutput\LlamaCppStructuredOutputProvider.cs` - 96.72% (59/61 lines, 100% branch)

**Very Good Coverage (90-95%):**
- `Core\StructuredOutput\SchemaVersion.cs` - 94.66% (124/131 lines, 94.12% branch)
- `Core\Audit\DebugPackageExporter.cs` - 94.17% (97/103 lines, 100% branch)
- `Persona\AuthoritativeMemorySystem.cs` - 94.19% (308/327 lines, 98.39% branch)
- `Core\Validation\ParsedOutput.cs` - 93.96% (140/149 lines, 100% branch)
- `Core\StructuredOutput\StructuredPipelineMetrics.cs` - 93.81% (91/97 lines, 80% branch)
- `Persona\PersonaProfileManager.cs` - 93.47% (186/199 lines, 100% branch)
- `Persona\PersonaMemoryFileStore.cs` - 93.04% (147/158 lines, 100% branch)
- `Core\Validation\ValidationGate.cs` - 93.12% (298/320 lines, 97.3% branch)
- `Core\StructuredOutput\JsonSchemaBuilder.cs` - 92.41% (341/369 lines, 100% branch)
- `Core\Validation\OutputParser.cs` - 92.44% (330/357 lines, 97.14% branch)
- `Persona\MemoryTypes\MemoryEntry.cs` - 92% (23/25 lines)
- `Core\BrainAgent.cs` - 91.56% (293/320 lines, 100% branch)
- `Core\LlmConfig.cs` - 91.67% (55/60 lines, 100% branch)
- `Core\Inference\EphemeralWorkingMemory.cs` - 91.8% (291/317 lines, 88% branch)
- `Core\Inference\PrefixStabilityValidator.cs` - 89.91% (98/109 lines, 100% branch)
- `Core\ClientManager.cs` - 89.36% (42/47 lines, 100% branch)
- `Persistence\MemorySnapshotRestorer.cs` - 89.11% (90/101 lines, 85.71% branch)
- `Core\Inference\PromptAssembler.cs` - 89.04% (333/374 lines, 90% branch)
- `Core\ApiClient.cs` - 90.34% (393/435 lines, 98.51% branch)
- `Core\StructuredOutput\StructuredDialoguePipeline.cs` - 90.66% (165/182 lines, 91.67% branch)

**Good Coverage (80-90%):**
- `Persona\MemoryTypes\WorldState.cs` - 88.46% (23/26 lines, 100% branch)
- `Core\FunctionCalling\BuiltInContextFunctions.cs` - 87.56% (190/217 lines, 100% branch)
- `Core\StructuredOutput\StructuredSchemaValidator.cs` - 86.84% (165/190 lines, 97.44% branch)
- `Core\Inference\PromptWithCacheInfo.cs` - 85.71% (36/42 lines, 100% branch)
- `Core\StructuredInput\LlamaCppStructuredContextProvider.cs` - 84.94% (141/166 lines, 100% branch)
- `Persona\MemoryMutationController.cs` - 83.9% (245/292 lines, 76.36% branch)
- `Persistence\SaveSlotInfo.cs` - 83.33% (5/6 lines, 0% branch)
- `Utilities\JsonUtils.cs` - 83.12% (192/231 lines, 100% branch)
- `Core\Audit\DebugPackageImporter.cs` - 82.79% (101/122 lines, 88.24% branch)
- `Persona\PersonaMemoryStore.cs` - 82.76% (96/116 lines, 88.24% branch)
- `Persistence\FileSystemSaveSystem.cs` - 81.34% (109/134 lines, 100% branch)
- `Utilities\PathUtils.cs` - 81.3% (100/123 lines, 96% branch)

See `coverage-analysis.csv` for complete details.

## Regenerating the Coverage Report

### Prerequisites

1. **Ensure you're in the correct directory**: The script must be run from the `LlamaBrain` directory (where `analyze-coverage.ps1` is located)
   ```powershell
   cd E:\Personal\LlamaBrain\LlamaBrain
   ```

2. **Verify PowerShell execution policy**: Ensure scripts can run (if needed, run as Administrator):
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

### Step 1: Run Tests with Coverage Collection

```powershell
dotnet test --collect:"XPlat code coverage" --settings coverlet.runsettings --results-directory TestResults
```

**Note**: This will create a new GUID directory in `TestResults\` containing the coverage file.

### Step 2: Analyze Coverage Data

Run the analysis script. It will automatically find the latest coverage file:

```powershell
.\analyze-coverage.ps1
```

**Optional**: If you want to analyze a specific coverage file (not the latest), you can specify it:

```powershell
.\analyze-coverage.ps1 -CoverageFile "TestResults\<guid>\coverage.cobertura.xml"
```

**Note**: The script automatically searches for the most recent `coverage.cobertura.xml` file in the `TestResults` directory, so you don't need to manually find the GUID directory.

**Expected Output**:
- Console output showing files by coverage category
- Summary statistics
- `coverage-analysis.csv` file generated in the current directory

### Step 3: Update This Report

1. Review the console output and `coverage-analysis.csv`
2. Update the coverage percentages in this markdown file:
   - Update "Overall Coverage" line with exact percentages
   - Update "Total Files Analyzed" count
   - Update "Total Tests" count
   - Update summary table metrics
   - Update file listings with current coverage percentages
3. Update the "Generated" date at the top
4. Update the "Coverage Changes" section if files improved or regressed

### Troubleshooting

**Issue: Script reports "0 files analyzed"**
- **Cause**: Coverage file may be empty or in wrong format
- **Solution**: Verify the coverage file contains actual data (check `line-rate` attribute in XML)
- **Check**: Ensure tests actually ran and coverage was collected (look for "Attachments:" in test output)

**Issue: "Cannot find path" error**
- **Cause**: Running script from wrong directory or coverage file path is incorrect
- **Solution**: Ensure you're in the `LlamaBrain` directory and the coverage file path is relative to that directory

**Issue: PowerShell execution policy error**
- **Cause**: Script execution is blocked by PowerShell policy
- **Solution**: Run `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser` (may require Administrator)

**Issue: Coverage file shows 0% coverage**
- **Cause**: Coverage collection may have failed or tests didn't execute properly
- **Solution**: Re-run tests and verify they pass, then check the coverage file was generated correctly

### Scripts and Files

- **`coverlet.runsettings`** - Coverlet configuration (excludes test files)
- **`analyze-coverage.ps1`** - PowerShell script for coverage analysis
  - **Note**: The script automatically finds the latest coverage file if no path is specified. You can optionally pass `-CoverageFile` to analyze a specific file.
- **`coverage-analysis.csv`** - Detailed coverage metrics for all files (generated by the script)
