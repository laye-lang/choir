using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;

using Choir.Formatting;
using Choir.Source;

namespace Choir.Diagnostics;

public sealed class FormattedDiagnosticWriter(TextWriter writer, bool useColor)
    : IDiagnosticConsumer
{
    public const int MinRenderWidth = 8;

    public TextWriter Writer { get; } = writer;
    public bool UseColor { get; } = useColor;

    private readonly List<Diagnostic> _diagnosticGroup = new(10);

    private bool _hasPrinted = false;

    public void Consume(Diagnostic diag)
    {
        if (diag.Level != DiagnosticLevel.Note)
            Flush();

        _diagnosticGroup.Add(diag);
    }

    public void Dispose()
    {
        Flush();
    }

    public void Flush()
    {
        if (_diagnosticGroup.Count == 0)
            return;

        if (_hasPrinted)
            Writer.WriteLine();
        else _hasPrinted = true;

        bool isConsole = (Writer == Console.Out && !Console.IsOutputRedirected) || Writer == Console.Error;
        string groupText = RenderDiagnosticGroup([.. _diagnosticGroup], Math.Max(MinRenderWidth, isConsole ? Console.WindowWidth : 80));
        _diagnosticGroup.Clear();

        Writer.Write(groupText);
    }

    private void ResetColor(StringBuilder builder)
    {
        if (!UseColor) return;
    }

    private void WriteColor(StringBuilder builder, MarkupColor color)
    {
        if (!UseColor) return;
        switch (color)
        {
        }
    }

    private string RenderDiagnosticGroup(Diagnostic[] group, int displayWidth)
    {
        Debug.Assert(group.Length > 0, "Attempt to render an empty diagnostic group.");
        Debug.Assert(displayWidth >= MinRenderWidth, "Attempt to render a diagnostic group with a specified width less than the configured minimum.");

        var groupBuilder = new StringBuilder();

        // render the contents of the diagnostics to a subset of the width, since there will be formatting characters at the left.
        int clientWidth = displayWidth - 3;

        for (int i = 0; i < group.Length; i++)
        {
            if (i > 0)
            {
                if (i == 1)
                    groupBuilder.AppendLine("│");
                else groupBuilder.AppendLine("┆");
            }

            // SourceLocation? previousLocation = i > 0 && group[i - 1].Source is not null ? group[i - 1].Location : null;
            string renderedText = FormatDiagnostic(group[i], clientWidth).TrimEnd('\r', '\n');
            
            string[] renderedLines = renderedText.Split(Environment.NewLine);
            for (int j = 0; j < renderedLines.Length; j++)
            {
                ResetColor(groupBuilder);

                if (j == renderedLines.Length - 1 && i == group.Length - 1)
                {
                    if (group.Length == 1 && renderedLines.Length == 1)
                        groupBuilder.Append("── ");
                    else
                    {
                        if (i == 0)
                            groupBuilder.Append("│  ");
                        else groupBuilder.Append("┆  ");
                    }
                }
                else if (j == 0)
                {
                    if (i == 0)
                        groupBuilder.Append("╭─ ");
                    else groupBuilder.Append("├─ ");
                }
                else
                {
                    if (i == 0)
                        groupBuilder.Append("│  ");
                    else groupBuilder.Append("┆  ");
                }

                groupBuilder.AppendLine(renderedLines[j]);
            }

            if (i == group.Length - 1 && (group.Length > 1 || renderedLines.Length > 1))
            {
                ResetColor(groupBuilder);
                //groupBuilder.AppendLine("╯");
                groupBuilder.AppendLine("╰─ ");
            }
        }

        ResetColor(groupBuilder);
        return groupBuilder.ToString();
    }

    private string FormatDiagnostic(Diagnostic diag, int clientWidth)
    {
        var markupRenderer = new FormattedDiagnosticMessageMarkupRenderer(clientWidth);
        var builder = new MarkupBuilder();

        if (diag.Source is not null)
        {
            builder.Append($"{diag.Source.Name}[{diag.Location.Offset}]:");
            builder.Append(MarkupLineBreak.Instance);
        }

        builder.Append(new MarkupScopedColor(MarkupColor.Green, diag.Level.ToString()));
        builder.Append(": ");
        builder.Append(diag.Message);

        if (diag.Source is null)
            return markupRenderer.Render(builder.Markup);

        return markupRenderer.Render(builder.Markup);
    }

    private sealed class FormattedDiagnosticMessageMarkupRenderer(int clientWidth)
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
}
