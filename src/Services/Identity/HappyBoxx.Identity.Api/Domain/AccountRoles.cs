namespace HappyBoxx.Identity.Api.Domain;

public static class AccountRoles
{
    public const string Admin = "Admin";
    public const string OrderCreator = "Order Creator";
    public const string Customer = "Customer";
}

public enum AccountKind
{
    Staff = 1,
    Customer = 2,
}

public enum AccountStatus
{
    Active = 1,
    Disabled = 2,
}