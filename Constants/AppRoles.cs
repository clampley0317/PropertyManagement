namespace PropertyManagement.Constants;

public static class AppRoles
{
    public const string Applicant = "Applicant";
    public const string PropertyManager = "Property Manager";

    public static readonly IReadOnlyList<string> All = [Applicant, PropertyManager];
}