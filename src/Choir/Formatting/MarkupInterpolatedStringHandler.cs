using System.Runtime.CompilerServices;

namespace Choir.Formatting;

[InterpolatedStringHandler]
public readonly struct MarkupInterpolatedStringHandler
{
    private readonly MarkupBuilder _builder;

    public Markup Markup => _builder.Markup;

    public MarkupInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        _builder = new();
    }

    public void AppendLiteral(string s)
    {
        _builder.Append(s);
    }

    public void AppendFormatted<T>(T t)
    {
        switch (t)
        {
            default: AppendLiteral(t?.ToString() ?? ""); break;
            case IMarkupFormattable formattable: formattable.BuildMarkup(_builder); break;
        }
    }
}
