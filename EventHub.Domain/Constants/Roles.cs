namespace EventHub.Domain.Constants;

public static class Roles
{
    public const string User = "User";
    public const string OrganizationAdmin = "OrganizationAdmin";
    public const string SuperAdmin = "SuperAdmin";


    public static readonly string[] All =
    [
        User,
        OrganizationAdmin,
        SuperAdmin
    ];
}
