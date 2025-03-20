namespace Choir.Driver;

public sealed class CliArgumentIterator(string[] args)
{
    private int _index = 0;

    public int RemainingCount => args.Length - _index;

    public bool Shift(out string arg)
    {
        arg = "";

        if (_index >= args.Length) return false;

        arg = args[_index++];
        return true;
    }
}
