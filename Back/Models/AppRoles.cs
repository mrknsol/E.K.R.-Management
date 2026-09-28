namespace EKR.API.Models;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Factory = "Factory";

    public static readonly string[] All = [SuperAdmin, Admin, Factory];
}
