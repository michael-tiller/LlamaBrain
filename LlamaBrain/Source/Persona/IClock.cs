namespace LlamaBrain.Persona
{
  /// <summary>
  /// Provides deterministic time generation for memory entries.
  /// Allows injection of fixed time in tests for deterministic behavior.
  /// </summary>
  public interface IClock
  {
    /// <summary>
    /// Gets the current UTC time in ticks.
    /// </summary>
    long UtcNowTicks { get; }
  }

  /// <summary>
  /// Default clock implementation using system time.
  /// </summary>
  public class SystemClock : IClock
  {
    /// <inheritdoc/>
    public long UtcNowTicks => System.DateTimeOffset.UtcNow.UtcTicks;
  }

  /// <summary>
  /// Fixed clock implementation for deterministic testing.
  /// Always returns the same time value.
  /// </summary>
  public class FixedClock : IClock
  {
    private readonly long _fixedTicks;

    /// <summary>
    /// Creates a fixed clock with the specified time.
    /// </summary>
    /// <param name="fixedTicks">The fixed UTC ticks value to always return</param>
    public FixedClock(long fixedTicks)
    {
      _fixedTicks = fixedTicks;
    }

    /// <inheritdoc/>
    public long UtcNowTicks => _fixedTicks;
  }

  /// <summary>
  /// Advancing clock implementation for deterministic testing.
  /// Increments by a fixed amount each time UtcNowTicks is accessed.
  /// Ensures that sequential operations get sequential timestamps.
  /// </summary>
  public class AdvancingClock : IClock
  {
    private long _currentTicks;
    private readonly long _incrementTicks;

    /// <summary>
    /// Creates an advancing clock starting at the specified time.
    /// </summary>
    /// <param name="startTicks">The starting UTC ticks value</param>
    /// <param name="incrementTicks">The amount to increment on each access (default: 1 tick)</param>
    public AdvancingClock(long startTicks, long incrementTicks = 1)
    {
      _currentTicks = startTicks;
      _incrementTicks = incrementTicks;
    }

    /// <inheritdoc/>
    public long UtcNowTicks
    {
      get
      {
        var ticks = _currentTicks;
        _currentTicks += _incrementTicks;
        return ticks;
      }
    }
  }
}
