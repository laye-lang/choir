namespace Choir.CommandLine;

public sealed class CliArgumentIterator
{
    private readonly string[] _args;
    private int _index = 0;

    public int RemainingCount => _args.Length - _index;

    public CliArgumentIterator(string[] args)
    {
        _args = args;
    }

    public bool Shift(out string arg)
    {
        arg = "";

        if (_index >= _args.Length) return false;
        
        arg = _args[_index++];
        return true;
    }
}
