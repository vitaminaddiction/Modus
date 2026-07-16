namespace Modus.Api.Contracts;

public record LoginRequest(string LoginId, string Password);

public record UserInfo(long Id, string LoginId, string Name, string Role, string Tenant);
