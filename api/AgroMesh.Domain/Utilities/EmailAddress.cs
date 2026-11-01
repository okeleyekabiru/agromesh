namespace AgroMesh.Domain.Utilities;

public static class EmailAddress
{
    /// <summary>A cheap shape check (one @, something either side, a dot in the domain, no spaces), not a deliverability check.</summary>
    public static bool IsPlausible(string email)
    {
        if (email.Any(char.IsWhiteSpace)) return false;
        var at = email.IndexOf('@');
        if (at <= 0 || at != email.LastIndexOf('@')) return false;
        var domain = email[(at + 1)..];
        var dot = domain.LastIndexOf('.');
        return dot > 0 && dot < domain.Length - 1;
    }
}
