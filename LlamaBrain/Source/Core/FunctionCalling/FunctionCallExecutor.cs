using System;
using System.Collections.Generic;
using System.Linq;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Persona;

namespace LlamaBrain.Core.FunctionCalling
{
    /// <summary>
    /// Executes function calls from parsed output and returns results.
    /// Integrates with the pipeline to handle function calls from LLM responses.
    /// </summary>
    public class FunctionCallExecutor
    {
        private readonly FunctionCallDispatcher _dispatcher;
        private readonly StateSnapshot _snapshot;
        private readonly AuthoritativeMemorySystem? _memorySystem;

        /// <summary>
        /// Creates a new function call executor.
        /// </summary>
        /// <param name="dispatcher">The function call dispatcher with registered functions</param>
        /// <param name="snapshot">The current state snapshot (for context access)</param>
        /// <param name="memorySystem">Optional memory system for memory queries</param>
        public FunctionCallExecutor(
            FunctionCallDispatcher dispatcher,
            StateSnapshot snapshot,
            AuthoritativeMemorySystem? memorySystem = null)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _memorySystem = memorySystem;
        }

        /// <summary>
        /// Executes all function calls from parsed output and returns results.
        /// </summary>
        /// <param name="parsedOutput">The parsed output containing function calls</param>
        /// <returns>A dictionary mapping call IDs (or function names) to results</returns>
        public Dictionary<string, FunctionCallResult> ExecuteAll(Validation.ParsedOutput parsedOutput)
        {
            var results = new Dictionary<string, FunctionCallResult>();

            if (parsedOutput?.FunctionCalls == null || parsedOutput.FunctionCalls.Count == 0)
            {
                return results;
            }

            foreach (var functionCall in parsedOutput.FunctionCalls)
            {
                var result = _dispatcher.DispatchCall(functionCall);
                var key = functionCall.CallId ?? functionCall.FunctionName;
                results[key] = result;
            }

            return results;
        }

        /// <summary>
        /// Executes a single function call.
        /// </summary>
        /// <param name="functionCall">The function call to execute</param>
        /// <returns>The result of executing the function</returns>
        public FunctionCallResult Execute(FunctionCall functionCall)
        {
            return _dispatcher.DispatchCall(functionCall);
        }

        /// <summary>
        /// Creates a function call executor with built-in context functions registered.
        /// </summary>
        /// <param name="snapshot">The current state snapshot</param>
        /// <param name="memorySystem">Optional memory system</param>
        /// <returns>A configured FunctionCallExecutor with built-in functions registered</returns>
        public static FunctionCallExecutor CreateWithBuiltIns(
            StateSnapshot snapshot,
            AuthoritativeMemorySystem? memorySystem = null)
        {
            var dispatcher = new FunctionCallDispatcher();
            BuiltInContextFunctions.RegisterAll(dispatcher, snapshot, memorySystem);
            return new FunctionCallExecutor(dispatcher, snapshot, memorySystem);
        }
    }
}
