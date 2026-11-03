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

    public static bool IsValidGroup(
        string? searchLevel,
        string? groupBy)
    {
        var group = Normalize(groupBy);

        if (group.Length == 0)
        {
            return true;
        }

        return Normalize(searchLevel) switch
        {
            "zone" =>
                group is "isactive",

            "organizationmanagement" =>
                group is "zone" or "parent" or "isactive",

            "field" =>
                group is "zone"
                    or "organizationmanagement"
                    or "parent"
                    or "isactive",

            "organization" =>
                group is "zone"
                    or "organizationmanagement"
                    or "field"
                    or "parent"
                    or "isactive",

            _ => false
        };
    }

    public static bool IsValidSort(
        string? groupBy,
        string? sortBy)
    {
        var sort = Normalize(sortBy);

        if (sort.Length == 0)
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(groupBy)
            ? sort is "id" or "title" or "code"
            : sort is "key" or "name" or "count";
    }
}
