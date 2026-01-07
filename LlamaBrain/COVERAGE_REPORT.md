# Test Coverage Report

**Generated:** 2026-01-06
**Overall Coverage:** 91.37% line coverage (7,655/8,378 lines), 84.94% branch coverage (2,307/2,716 branches)
**Total Files Analyzed:** 78 source files
**Total Tests:** 2,254 tests (2,249 passed, 5 skipped)

## Summary

| Metric | Value |
|--------|-------|
| Files with 0% coverage | 0 ✅ |
| Files with < 50% coverage | 0 ✅ |
| Files with 50-80% coverage | 2 |
| Files with >= 80% coverage | 76 |

## Coverage Changes (2026-01-06)

### Files Still Below 80% (2 remaining)

| File | Coverage | Branch | Priority |
|------|----------|--------|----------|
| `Core\ServerManager.cs` | 69.21% | 89.55% ✅ | Medium - complex process management |
| `Utilities\ProcessUtils.cs` | 79.55% | 100% ✅ | Low - perfect branch coverage. Line coverage would require DI refactoring |

## Files with Good Coverage (>= 80%)

**76 files** have good test coverage (>= 80%):

**Perfect Coverage (100%):**
- `Core\PromptComposer.cs` - 379/379 lines, 100% branch
- `Core\Metrics\DialogueInteraction.cs` - 132/132 lines, 100% branch
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
- `Persistence\SaveSlotInfo.cs` - 5/6 lines (83.33%, 0% branch)
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
- `Utilities\FileSystem.cs` - 55/55 lines, 100% branch
- `Persistence\SaveData.cs` - 12/12 lines
- `Persistence\Dtos\DialogueEntryDto.cs` - 3/3 lines
- `Persistence\ConversationHistorySnapshot.cs` - 3/3 lines

**Excellent Coverage (95-99%):**
- `Core\Inference\EphemeralWorkingMemory.cs` - 99.61% (255/256 lines, 100% branch)
- `Core\FallbackSystem.cs` - 98.59% (210/213 lines, 100% branch)
- `Core\FunctionCalling\FunctionCallDispatcher.cs` - 98.77% (80/81 lines, 100% branch)
- `Core\Inference\RetryPolicy.cs` - 98.78% (81/82 lines, 100% branch)
- `Core\Inference\ContextRetrievalLayer.cs` - 97.91% (187/191 lines, 100% branch)
- `Core\Expectancy\Constraint.cs` - 97.44% (38/39 lines)
- `Core\Inference\ResponseValidator.cs` - 96.73% (148/153 lines, 100% branch)
- `Core\StructuredOutput\LlamaCppStructuredOutputProvider.cs` - 96.72% (59/61 lines, 100% branch)

**Very Good Coverage (90-95%):**
- `Core\LlmConfig.cs` - 91.07% (51/56 lines, 100% branch)
- `Core\ClientManager.cs` - 91.49% (43/47 lines, 100% branch)
- `Core\StructuredOutput\StructuredPipelineMetrics.cs` - 91.55% (65/71 lines, 66.67% branch)
- `Core\Validation\ParsedOutput.cs` - 93.96% (140/149 lines, 100% branch)
- `Persona\AuthoritativeMemorySystem.cs` - 93.79% (287/306 lines, 98.25% branch)
- `Core\Validation\ValidationGate.cs` - 93.12% (298/320 lines, 97.3% branch)
- `Persona\PersonaProfileManager.cs` - 93.47% (186/199 lines, 100% branch)
- `Persona\PersonaMemoryFileStore.cs` - 93.04% (147/158 lines, 100% branch)
- `Core\StructuredOutput\JsonSchemaBuilder.cs` - 92.61% (326/352 lines, 100% branch)
- `Core\Validation\OutputParser.cs` - 92.44% (330/357 lines, 97.14% branch)
- `Core\Inference\PromptAssembler.cs` - 90.44% (227/251 lines, 89.66% branch)
- `Core\StructuredOutput\StructuredDialoguePipeline.cs` - 90.61% (164/181 lines, 91.67% branch)
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
- `Core\BrainAgent.cs` - 85.42% (246/288 lines, 90.38% branch)
- `Utilities\JsonUtils.cs` - 83.12% (192/231 lines, 100% branch)
- `Utilities\PathUtils.cs` - 81.3% (100/123 lines, 96% branch)
- `Persistence\FileSystemSaveSystem.cs` - 81.2% (108/133 lines, 100% branch)

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

### Step 2: Prepare the Analysis Script

1. **Find the latest coverage file**: After running tests, locate the most recent coverage file:
   ```powershell
   # List all coverage files sorted by creation time (newest first)
   Get-ChildItem -Path "TestResults" -Recurse -Filter "coverage.cobertura.xml" | 
       Sort-Object CreationTime -Descending | 
       Select-Object -First 1 FullName
   ```

2. **Extract the GUID**: The coverage file will be in a path like:
   ```
   TestResults\<guid>\coverage.cobertura.xml
   ```
   Copy the `<guid>` portion for use in Step 3.

   **Quick helper**: To automatically get the latest coverage file path:
   ```powershell
   $latestCoverage = (Get-ChildItem -Path "TestResults" -Recurse -Filter "coverage.cobertura.xml" | 
       Sort-Object CreationTime -Descending | 
       Select-Object -First 1).FullName
   Write-Host "Latest coverage file: $latestCoverage"
   ```

3. **Verify the coverage file has data**: Check that the file is not empty (should contain coverage data, not just an empty XML structure):
   ```powershell
   # Quick check - should show line-rate > 0 if data exists
   [xml]$coverage = Get-Content "TestResults\<guid>\coverage.cobertura.xml"
   $coverage.coverage.'line-rate'
   ```

### Step 3: Analyze Coverage Data

Run the analysis script with the coverage file path:

```powershell
.\analyze-coverage.ps1 -CoverageFile "TestResults\<guid>\coverage.cobertura.xml"
```

**Alternative**: If you want to use the default path (which may be outdated), you can update the default parameter in `analyze-coverage.ps1` line 3, or always pass the `-CoverageFile` parameter explicitly.

**Expected Output**:
- Console output showing files by coverage category
- Summary statistics
- `coverage-analysis.csv` file generated in the current directory

### Step 4: Update This Report

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
  - **Note**: The default `$CoverageFile` parameter (line 3) contains a hardcoded GUID that will be outdated after each test run. Always pass `-CoverageFile` explicitly or update the default.
- **`coverage-analysis.csv`** - Detailed coverage metrics for all files (generated by the script)
