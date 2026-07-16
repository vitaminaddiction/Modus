using Modus.Domain.Catalog;

namespace Modus.Infrastructure.Multitenancy;

/// <summary>테넌트 레코드로부터 Npgsql 커넥션스트링을 조립.</summary>
public static class TenantConnectionString
{
    public static string For(Tenant t) =>
        $"Host={t.Host};Port={t.Port};Database={t.DbName};Username={t.DbUser};Password={t.DbPassword}";
}
