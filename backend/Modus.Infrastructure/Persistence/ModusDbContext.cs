using Microsoft.EntityFrameworkCore;
using Modus.Domain.BaseCode;
using Modus.Domain.Security;

namespace Modus.Infrastructure.Persistence;

/// <summary>테넌트 데이터 DB(modus_t_*). 커넥션은 요청 시점에 테넌트별로 해석된다.</summary>
public class ModusDbContext : DbContext
{
    public ModusDbContext(DbContextOptions<ModusDbContext> options) : base(options) { }

    public DbSet<CodeGroup> CodeGroups => Set<CodeGroup>();
    public DbSet<Code> Codes => Set<Code>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        var cg = b.Entity<CodeGroup>();
        cg.ToTable("code_group");
        cg.HasKey(x => x.Id);
        cg.HasIndex(x => x.Code).IsUnique();
        cg.Property(x => x.Code).HasMaxLength(50).IsRequired();
        cg.Property(x => x.Name).HasMaxLength(200).IsRequired();

        var c = b.Entity<Code>();
        c.ToTable("code");
        c.HasKey(x => x.Id);
        c.HasIndex(x => new { x.CodeGroupId, x.Code }).IsUnique();
        c.Property(x => x.Code).HasMaxLength(50).IsRequired();
        c.Property(x => x.Name).HasMaxLength(200).IsRequired();
        c.HasOne(x => x.CodeGroup)
            .WithMany(g => g.Codes)
            .HasForeignKey(x => x.CodeGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        var u = b.Entity<User>();
        u.ToTable("app_user");        // "user"는 PG 예약어라 회피
        u.HasKey(x => x.Id);
        u.HasIndex(x => x.LoginId).IsUnique();
        u.Property(x => x.LoginId).HasMaxLength(100).IsRequired();
        u.Property(x => x.Name).HasMaxLength(200).IsRequired();
        u.Property(x => x.PasswordHash).HasMaxLength(400).IsRequired();
        u.Property(x => x.Role).HasMaxLength(50).IsRequired();
    }
}
