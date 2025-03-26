using System.Text;

namespace Choir.FrontEnd.Score.Syntax;

public sealed class ScoreSyntaxFormatter
{
    public static ScoreSyntaxUnit Format(ScoreSyntaxUnit unit)
    {
        var printer = new ScoreSyntaxFormatter(unit);
        return printer.Format();
    }

    private readonly StringBuilder _builder;
    private readonly ScoreSyntaxUnit _unit;

    private int _indentLevel = 0;

    private ScoreSyntaxFormatter(ScoreSyntaxUnit unit)
    {
        _builder = new(unit.Source.Text.Length);
        _unit = unit;
    }

    public ScoreSyntaxUnit Format()
    {
        var topLevelNodes = new List<ScoreSyntaxNode>();

        _builder.Append(_unit.Source.Text);

        for (int i = 0; i < _unit.TopLevelNodes.Count; i++)
        {
            var syntax = _unit.TopLevelNodes[i];
            var previous = i > 0 ? _unit.TopLevelNodes[i - 1] : null;
            var next = i < _unit.TopLevelNodes.Count - 1 ? _unit.TopLevelNodes[i + 1] : null;

            var node = FormatSyntax(syntax, _unit, previous, next);
            topLevelNodes.Add(node);
        }

        return new(new(_unit.Source.Name, _builder.ToString()), _unit.Tokens, topLevelNodes);
    }

    private ScoreSyntaxNode FormatSyntax(ScoreSyntaxNode syntax, ScoreSyntaxNode parent, ScoreSyntaxNode? previous, ScoreSyntaxNode? next)
    {
        return syntax;
    }
}
