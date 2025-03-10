using System.Diagnostics.CodeAnalysis;

namespace Choir;

public sealed class ChoirContext
{
    public void Assert([DoesNotReturnIf(false)] bool condition, string message)
    {
        throw new NotImplementedException();
    }
}
