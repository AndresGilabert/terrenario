using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Auth;

namespace Terrenario.Api.Infrastructure.Data;

public sealed class TerrenarioDbContext(DbContextOptions<TerrenarioDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("usuarios");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasColumnName("id");
            entity.Property(u => u.GoogleSub).HasColumnName("google_sub").IsRequired();
            entity.Property(u => u.DisplayName).HasColumnName("nombre").IsRequired();
            entity.Property(u => u.Email).HasColumnName("email").IsRequired();
            entity.Property(u => u.Active).HasColumnName("activo");
            entity.Property(u => u.CreatedAt).HasColumnName("creado_en");
            entity.Property(u => u.UpdatedAt).HasColumnName("actualizado_en");

            entity.HasIndex(u => u.GoogleSub).IsUnique();
        });

        modelBuilder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.Id).HasColumnName("id");
            entity.Property(rt => rt.UserId).HasColumnName("usuario_id").IsRequired();
            entity.Property(rt => rt.TokenHash).HasColumnName("token_hash").IsRequired();
            entity.Property(rt => rt.ExpiresAt).HasColumnName("expires_at").IsRequired();
            entity.Property(rt => rt.RevokedAt).HasColumnName("revocado_en");
            entity.Property(rt => rt.CreatedAt).HasColumnName("creado_en");

            entity.HasIndex(rt => rt.TokenHash).IsUnique();
            entity.HasIndex(rt => rt.UserId);
            entity.HasIndex(rt => rt.ExpiresAt);
        });

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.ToTable("workspaces");
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Id).HasColumnName("id");
            entity.Property(w => w.OwnerId).HasColumnName("owner_id").IsRequired();
            entity.Property(w => w.Name).HasColumnName("nombre").HasMaxLength(Workspace.NameMaxLength).IsRequired();
            entity.Property(w => w.CreatedAt).HasColumnName("creado_en");
            entity.Property(w => w.UpdatedAt).HasColumnName("actualizado_en");

            entity.HasIndex(w => w.OwnerId);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(w => w.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.ToTable("usuarios_workspace");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).HasColumnName("id");
            entity.Property(m => m.WorkspaceId).HasColumnName("workspace_id").IsRequired();
            entity.Property(m => m.UserId).HasColumnName("usuario_id").IsRequired();
            entity.Property(m => m.Role).HasColumnName("rol").HasMaxLength(50).IsRequired();
            entity.Property(m => m.Active).HasColumnName("activo");
            entity.Property(m => m.JoinedAt).HasColumnName("unido_en");

            entity.HasIndex(m => new { m.WorkspaceId, m.UserId }).IsUnique();
            entity.HasIndex(m => m.UserId);

            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(m => m.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
