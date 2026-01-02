using System;

namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// Metrics for tracking structured output pipeline performance.
    /// Used to monitor success rates and fallback usage.
    /// </summary>
    [Serializable]
    public sealed class StructuredPipelineMetrics
    {
        /// <summary>
        /// Total number of pipeline requests processed.
        /// </summary>
        public int TotalRequests { get; private set; }

        /// <summary>
        /// Number of requests that used structured output successfully.
        /// </summary>
        public int StructuredSuccessCount { get; private set; }

        /// <summary>
        /// Number of requests where structured output failed.
        /// </summary>
        public int StructuredFailureCount { get; private set; }

        /// <summary>
        /// Number of requests that fell back to regex parsing.
        /// </summary>
        public int RegexFallbackCount { get; private set; }

        /// <summary>
        /// Number of requests that used regex parsing from the start.
        /// </summary>
        public int RegexDirectCount { get; private set; }

        /// <summary>
        /// Number of validation failures.
        /// </summary>
        public int ValidationFailureCount { get; private set; }

        /// <summary>
        /// Number of retries across all requests.
        /// </summary>
        public int TotalRetries { get; private set; }

        /// <summary>
        /// Number of mutations successfully executed.
        /// </summary>
        public int MutationsExecuted { get; private set; }

        /// <summary>
        /// Number of intents successfully emitted.
        /// </summary>
        public int IntentsEmitted { get; private set; }

        /// <summary>
        /// Structured output success rate as a percentage (0-100).
        /// </summary>
        public float StructuredSuccessRate
        {
            get
            {
                var structuredAttempts = StructuredSuccessCount + StructuredFailureCount;
                return structuredAttempts > 0
                    ? (float)StructuredSuccessCount / structuredAttempts * 100f
                    : 0f;
            }
        }

        /// <summary>
        /// Overall pipeline success rate as a percentage (0-100).
        /// </summary>
        public float OverallSuccessRate
        {
            get
            {
                var successCount = StructuredSuccessCount + RegexFallbackCount + RegexDirectCount;
                return TotalRequests > 0
                    ? (float)successCount / TotalRequests * 100f
                    : 0f;
            }
        }

        /// <summary>
        /// Fallback rate as a percentage of structured attempts (0-100).
        /// </summary>
        public float FallbackRate
        {
            get
            {
                var structuredAttempts = StructuredSuccessCount + StructuredFailureCount;
                return structuredAttempts > 0
                    ? (float)RegexFallbackCount / structuredAttempts * 100f
                    : 0f;
            }
        }

        /// <summary>
        /// Records a successful structured output request.
        /// </summary>
        public void RecordStructuredSuccess()
        {
            TotalRequests++;
            StructuredSuccessCount++;
        }

        /// <summary>
        /// Records a failed structured output request.
        /// </summary>
        public void RecordStructuredFailure()
        {
            StructuredFailureCount++;
        }

        /// <summary>
        /// Records a fallback to regex parsing.
        /// </summary>
        public void RecordFallbackToRegex()
        {
            TotalRequests++;
            RegexFallbackCount++;
        }

        /// <summary>
        /// Records a direct regex parsing request (no structured output attempted).
        /// </summary>
        public void RecordRegexDirect()
        {
            TotalRequests++;
            RegexDirectCount++;
        }

        /// <summary>
        /// Records a validation failure.
        /// </summary>
        public void RecordValidationFailure()
        {
            ValidationFailureCount++;
        }

        /// <summary>
        /// Records a retry attempt.
        /// </summary>
        public void RecordRetry()
        {
            TotalRetries++;
        }

        /// <summary>
        /// Records successful mutation execution.
        /// </summary>
        /// <param name="count">Number of mutations executed.</param>
        public void RecordMutationsExecuted(int count)
        {
            MutationsExecuted += count;
        }

        /// <summary>
        /// Records successful intent emission.
        /// </summary>
        /// <param name="count">Number of intents emitted.</param>
        public void RecordIntentsEmitted(int count)
        {
            IntentsEmitted += count;
        }

        /// <summary>
        /// Resets all metrics to zero.
        /// </summary>
        public void Reset()
        {
            TotalRequests = 0;
            StructuredSuccessCount = 0;
            StructuredFailureCount = 0;
            RegexFallbackCount = 0;
            RegexDirectCount = 0;
            ValidationFailureCount = 0;
            TotalRetries = 0;
            MutationsExecuted = 0;
            IntentsEmitted = 0;
        }

        /// <inheritdoc/>
        /// <returns>A string representation of the pipeline metrics including request counts and success rates.</returns>
        public override string ToString()
        {
            return $"PipelineMetrics: {TotalRequests} requests, " +
                   $"Structured: {StructuredSuccessRate:F1}% success, " +
                   $"Fallback: {FallbackRate:F1}%, " +
                   $"Mutations: {MutationsExecuted}, Intents: {IntentsEmitted}";
        }
    }
}
