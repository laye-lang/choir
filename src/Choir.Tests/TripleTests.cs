using FluentAssertions;

namespace Choir.Tests;

public class TripleTests
{
    [Theory]
    [InlineData("x86-unknown-linux-gnu", "x86-unknown-linux-gnu")]
    [InlineData("x86_64-pc-windows", "x86_64-pc-windows-msvc")]
    [InlineData("x86_64-windows", "x86_64-unknown-windows-msvc")]
    [InlineData("foobar", "unknown")]
    public void Triple_Parse_ReturnsExpectedNormalForm(string input, string normal)
    {
        var triple = Triple.Parse(input);
        triple.NormalForm.Should().Be(normal);
    }
}
