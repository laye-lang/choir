using System.Text;

namespace Choir.Formatting;

public sealed class MarkupStringRenderer
{
    public string Render(Markup markup)
    {
        var builder = new StringBuilder();
        RenderImpl(builder, markup);
        return builder.ToString();
    }

    private void RenderImpl(StringBuilder builder, Markup markup)
    {
        switch (markup)
        {
            default: throw new InvalidOperationException($"Unhandled {nameof(Markup)} node in {nameof(MarkupStringRenderer)}: {markup.GetType().FullName}.");

            case MarkupLineBreak: builder.AppendLine(); break;
            case MarkupLiteral literal: builder.Append(literal.Literal); break;
            case MarkupScopedColor colored: RenderImpl(builder, colored.Contents); break;
            case MarkupScopedStyle styled: RenderImpl(builder, styled.Contents); break;
            case MarkupScopedSemantic semantic: RenderImpl(builder, semantic.Contents); break;

            case MarkupSequence sequence:
            {
                foreach (var child in sequence.Children)
                    RenderImpl(builder, child);
            } break;
        }
    }
}
