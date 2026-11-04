namespace EMS.Application.Dtos.OrganizationHierarchy;

public static class OrganizationHierarchyRules
{
    public static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    public static int Rank(string? level)
    {
        return Normalize(level) switch
        {
            "zone" => 0,
            "organizationmanagement" => 1,
            "field" => 2,
            "organization" => 3,
            _ => -1
        };
    }

    public static bool IsValidParent(
        string? searchLevel,
        string? parentLevel)
    {
        if (string.IsNullOrWhiteSpace(parentLevel))
        {
            return true;
        }

        var searchRank = Rank(searchLevel);
        var parentRank = Rank(parentLevel);

        return searchRank >= 0
            && parentRank >= 0
            && parentRank < searchRank;
    }
}
