namespace LlamaBrain.Persona
{
  /// <summary>
  /// Provides deterministic ID generation for memory entries.
  /// Allows injection of deterministic ID generators in tests.
  /// </summary>
  public interface IIdGenerator
  {
    /// <summary>
    /// Generates a new unique identifier.
    /// </summary>
    /// <returns>A unique identifier string</returns>
    string GenerateId();
  }

  /// <summary>
  /// Default ID generator using GUIDs.
  /// </summary>
  public class GuidIdGenerator : IIdGenerator
  {
    /// <inheritdoc/>
    public string GenerateId() => System.Guid.NewGuid().ToString("N")[..8];
  }

  /// <summary>
  /// Sequential ID generator for deterministic testing.
  /// Generates IDs in a predictable sequence: "id_0001", "id_0002", etc.
  /// </summary>
  public class SequentialIdGenerator : IIdGenerator
  {
    private long _counter = 0;

    /// <summary>
    /// Creates a sequential ID generator starting from 0.
    /// </summary>
    public SequentialIdGenerator()
    {
    }

    /// <summary>
    /// Creates a sequential ID generator starting from a specific value.
    /// </summary>
    /// <param name="startValue">The starting counter value</param>
    public SequentialIdGenerator(long startValue)
    {
      _counter = startValue;
    }

    /// <inheritdoc/>
    public string GenerateId()
    {
      var id = $"id_{_counter:D4}";
      _counter++;
      return id;
    }

    /// <summary>
    /// Resets the counter to 0.
    /// </summary>
    public void Reset()
    {
      _counter = 0;
    }
  }
}
