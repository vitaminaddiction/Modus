using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Modus.Domain.Catalog;
using Modus.Domain.Security;
using Modus.Infrastructure.Multitenancy;
using Modus.Infrastructure.Persistence;
using Modus.Infrastructure.Security;

// ── Modus.Tools ── DB 마이그레이션 / 테넌트 프로비저닝 CLI ──────────────────
//   migrate-catalog          카탈로그 DB 마이그레이션 적용
//   migrate-tenants          등록된 전 테넌트 DB에 마이그레이션 적용
//   provision <code> [name]  테넌트 DB 생성 + 카탈로그 등록 + 마이그 + admin 시드
// ────────────────────────────────────────────────────────────────────────────

string Env(string k, string d) =>
    Environment.GetEnvironmentVariable(k) is { Length: > 0 } v ? v : d;

var host = Env("MODUS_PG_HOST", "localhost");
var port = int.Parse(Env("MODUS_PG_PORT", "5433"));
var user = Env("MODUS_PG_USER", "modus");
var pw = Env("MODUS_PG_PASSWORD", "modus_dev_pw");
const string catalogDb = "modus_catalog";

string Cs(string db) => $"Host={host};Port={port};Database={db};Username={user};Password={pw}";
var catalogCs = Cs(catalogDb);

CatalogDbContext NewCatalog() => new(new DbContextOptionsBuilder<CatalogDbContext>()
    .UseNpgsql(catalogCs).UseSnakeCaseNamingConvention().Options);
ModusDbContext NewTenant(string cs) => new(new DbContextOptionsBuilder<ModusDbContext>()
    .UseNpgsql(cs).UseSnakeCaseNamingConvention().Options);

var cmd = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

switch (cmd)
{
    case "migrate-catalog":
    {
        using var cat = NewCatalog();
        cat.Database.Migrate();
        Console.WriteLine("✔ catalog migrated");
        break;
    }

    case "migrate-tenants":
    {
        using var cat = NewCatalog();
        cat.Database.Migrate();
        var tenants = cat.Tenants.Where(t => t.Enabled).ToList();
        if (tenants.Count == 0) Console.WriteLine("(등록된 테넌트 없음)");
        foreach (var t in tenants)
        {
            using var tctx = NewTenant(TenantConnectionString.For(t));
            tctx.Database.Migrate();
            Console.WriteLine($"✔ tenant '{t.Code}' ({t.DbName}) migrated");
        }
        break;
    }

    case "provision":
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("사용법: provision <code> [name]");
            return 1;
        }
        var code = args[1].ToLowerInvariant();
        if (!Regex.IsMatch(code, "^[a-z0-9_]+$"))
        {
            Console.Error.WriteLine("테넌트 코드는 소문자/숫자/_ 만 허용됩니다.");
            return 1;
        }
        var name = args.Length > 2 ? args[2] : code;
        var dbName = $"modus_t_{code}";

        // 1) 테넌트 DB 생성 (없으면)
        await using (var maint = new NpgsqlConnection(Cs("postgres")))
        {
            await maint.OpenAsync();
            await using (var check = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @n", maint))
            {
                check.Parameters.AddWithValue("n", dbName);
                var exists = await check.ExecuteScalarAsync() is not null;
                if (exists)
                {
                    Console.WriteLine($"· DB '{dbName}' 이미 존재");
                }
                else
                {
                    await using var create = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", maint);
                    await create.ExecuteNonQueryAsync();
                    Console.WriteLine($"✔ DB '{dbName}' 생성");
                }
            }
        }

        // 2) 카탈로그 등록
        using (var cat = NewCatalog())
        {
            cat.Database.Migrate();
            var tenant = cat.Tenants.FirstOrDefault(t => t.Code == code);
            if (tenant is null)
            {
                cat.Tenants.Add(new Tenant
                {
                    Code = code, Name = name,
                    Host = host, Port = port, DbName = dbName,
                    DbUser = user, DbPassword = pw, Enabled = true,
                    CreatedAt = DateTime.UtcNow, CreatedBy = "provision",
                });
                cat.SaveChanges();
                Console.WriteLine($"✔ 카탈로그에 테넌트 '{code}' 등록");
            }
            else
            {
                Console.WriteLine($"· 카탈로그에 테넌트 '{code}' 이미 등록됨");
            }
        }

        // 3) 테넌트 DB 마이그레이션 + admin 시드
        using (var tctx = NewTenant(Cs(dbName)))
        {
            tctx.Database.Migrate();
            Console.WriteLine($"✔ 테넌트 DB '{dbName}' 마이그레이션");

            if (!tctx.Users.Any(u => u.LoginId == "admin"))
            {
                var hasher = new Pbkdf2PasswordHasher();
                tctx.Users.Add(new User
                {
                    LoginId = "admin", Name = "관리자", Role = "Admin",
                    PasswordHash = hasher.Hash("admin1234"), Enabled = true,
                    CreatedAt = DateTime.UtcNow, CreatedBy = "provision",
                });
                tctx.SaveChanges();
                Console.WriteLine("✔ admin 계정 시드 (admin / admin1234)");
            }
            else
            {
                Console.WriteLine("· admin 계정 이미 존재");
            }
        }

        Console.WriteLine($"\n완료: 테넌트 '{code}' 준비됨. (X-Tenant-Code: {code})");
        break;
    }

    default:
        Console.WriteLine(
            "Modus.Tools\n" +
            "  migrate-catalog          카탈로그 DB 마이그레이션\n" +
            "  migrate-tenants          전 테넌트 DB 마이그레이션\n" +
            "  provision <code> [name]  테넌트 생성+등록+마이그+admin 시드");
        break;
}

return 0;
