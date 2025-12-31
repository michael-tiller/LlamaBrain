// Re-export core types for Unity layer compatibility
using CoreExpectancy = LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Runtime.Core.Expectancy
{
  /// <summary>
  /// Interface for expectancy rules that generate constraints.
  /// This is a re-export of LlamaBrain.Core.Expectancy.IExpectancyRule for Unity namespace compatibility.
  /// </summary>
  public interface IExpectancyRule : CoreExpectancy.IExpectancyRule
  {
    // Inherits all members from core interface
    // Unity-specific rules can implement this interface directly
  }
}
