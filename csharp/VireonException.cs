namespace Vireon;

/// <summary>
/// Thrown when a native Vireon call returns a non-zero error code.
/// The message is populated from the thread-local last error.
/// </summary>
public class VireonException : Exception
{
    public VireonException(string message) : base(message) { }
}
