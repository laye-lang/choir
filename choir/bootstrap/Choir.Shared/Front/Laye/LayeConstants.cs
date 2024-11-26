namespace Choir.Front.Laye;

public static class LayeConstants
{
    public const string ModuleSectionNamePrefix = ".__laye_module_description";

    public static string GetModuleDescriptionSectionName(string? moduleName)
    {
        if (moduleName is null)
            return ModuleSectionNamePrefix;
        else return $"{ModuleSectionNamePrefix}.{moduleName}";
    }
}
