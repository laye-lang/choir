namespace Choir.Driver.Options;

public static class HelpTextRenderer
{
    public static string RenderHelpText<TOptions, TArgParseState>()
        where TOptions : BaseLayeDriverOptions<TOptions, TArgParseState>, new()
        where TArgParseState : BaseLayeCompilerDriverArgParseState, new()
    {
        throw new NotImplementedException();
    }
}
