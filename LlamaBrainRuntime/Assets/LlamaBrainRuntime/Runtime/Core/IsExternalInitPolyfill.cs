// Polyfill for System.Runtime.CompilerServices.IsExternalInit
// Required for C# 9.0+ init accessors in Unity projects targeting older .NET Standard versions
// This type is built into .NET 5+ and .NET Standard 2.1+, but Unity may target older versions
namespace System.Runtime.CompilerServices
{
  /// <summary>
  /// Allows the use of init-only setters in C# 9.0+ when targeting older .NET versions.
  /// This is a polyfill for the IsExternalInit attribute that is built into .NET 5+ and .NET Standard 2.1+.
  /// If the type already exists in the framework, this definition will be ignored by the compiler.
  /// </summary>
  internal static class IsExternalInit
  {
  }
}
