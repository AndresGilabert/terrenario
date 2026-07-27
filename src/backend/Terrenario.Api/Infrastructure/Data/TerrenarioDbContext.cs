using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Seasons;
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
    public DbSet<WorkspaceInvitation> WorkspaceInvitations => Set<WorkspaceInvitation>();
    public DbSet<Season> Seasons => Set<Season>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasColumnName("id");
            entity.Property(u => u.GoogleSub).HasColumnName("google_sub").IsRequired();
            entity.Property(u => u.DisplayName).HasColumnName("display_name").IsRequired();
            entity.Property(u => u.Email).HasColumnName("email").IsRequired();
            entity.Property(u => u.IsActive).HasColumnName("is_active");
            entity.Property(u => u.ActiveWorkspaceId).HasColumnName("active_workspace_id");
            entity.Property(u => u.CreatedAt).HasColumnName("created_at");
            entity.Property(u => u.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(u => u.GoogleSub).IsUnique();

            // Si el Workspace preferido desaparece, la sesión vuelve a resolver por defecto
            // en lugar de quedar apuntando a un contexto inexistente.
            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(u => u.ActiveWorkspaceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.Id).HasColumnName("id");
            entity.Property(rt => rt.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(rt => rt.TokenHash).HasColumnName("token_hash").IsRequired();
            entity.Property(rt => rt.ExpiresAt).HasColumnName("expires_at").IsRequired();
            entity.Property(rt => rt.RevokedAt).HasColumnName("revoked_at");
            entity.Property(rt => rt.CreatedAt).HasColumnName("created_at");

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
            entity.Property(w => w.Name).HasColumnName("name").HasMaxLength(Workspace.NameMaxLength).IsRequired();
            entity.Property(w => w.CreatedAt).HasColumnName("created_at");
            entity.Property(w => w.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(w => w.OwnerId);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(w => w.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.ToTable("workspace_members");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).HasColumnName("id");
            entity.Property(m => m.WorkspaceId).HasColumnName("workspace_id").IsRequired();
            entity.Property(m => m.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(m => m.Role).HasColumnName("role").HasMaxLength(50).IsRequired();
            entity.Property(m => m.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(m => m.JoinedAt).HasColumnName("joined_at");
            entity.Ignore(m => m.IsActive);

            entity.HasIndex(m => new { m.WorkspaceId, m.UserId }).IsUnique();

            // El selector consulta siempre por usuario y estado (MVP-104).
            entity.HasIndex(m => new { m.UserId, m.Status });

            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(m => m.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkspaceInvitation>(entity =>
        {
            entity.ToTable("workspace_invitations");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Id).HasColumnName("id");
            entity.Property(i => i.WorkspaceId).HasColumnName("workspace_id").IsRequired();
            entity.Property(i => i.InvitedByUserId).HasColumnName("invited_by_user_id").IsRequired();
            entity.Property(i => i.Channel).HasColumnName("channel").HasMaxLength(20).IsRequired();
            entity.Property(i => i.Email).HasColumnName("email").HasMaxLength(WorkspaceInvitation.EmailMaxLength);
            entity.Property(i => i.TokenHash).HasColumnName("token_hash").IsRequired();
            entity.Property(i => i.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(i => i.ExpiresAt).HasColumnName("expires_at");
            entity.Property(i => i.CreatedAt).HasColumnName("created_at");
            entity.Property(i => i.AcceptedAt).HasColumnName("accepted_at");
            entity.Property(i => i.AcceptedByUserId).HasColumnName("accepted_by_user_id");
            entity.Property(i => i.RejectedAt).HasColumnName("rejected_at");
            entity.Property(i => i.RejectedByUserId).HasColumnName("rejected_by_user_id");

            entity.HasIndex(i => i.TokenHash).IsUnique();
            entity.HasIndex(i => new { i.WorkspaceId, i.Status });
            // La bandeja de invitaciones recibidas (MVP-107) filtra por email + estado.
            entity.HasIndex(i => new { i.Email, i.Status });

            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(i => i.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(i => i.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(i => i.AcceptedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(i => i.RejectedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Season>(entity =>
        {
            entity.ToTable("seasons");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).HasColumnName("id");
            entity.Property(s => s.WorkspaceId).HasColumnName("workspace_id").IsRequired();
            entity.Property(s => s.Name).HasColumnName("name").HasMaxLength(Season.NameMaxLength).IsRequired();
            entity.Property(s => s.StartDate).HasColumnName("start_date").IsRequired();
            entity.Property(s => s.EndDate).HasColumnName("end_date");
            entity.Property(s => s.IsActive).HasColumnName("is_active");
            entity.Property(s => s.IsClosed).HasColumnName("is_closed");
            entity.Property(s => s.CreatedAt).HasColumnName("created_at");
            entity.Property(s => s.UpdatedAt).HasColumnName("updated_at");

            // RN-022 — una sola temporada activa por Workspace: índice único parcial sobre las filas
            // activas. La invariante deja de depender solo de la lógica de aplicación (CA-3). También
            // es el índice de acceso de la consulta de temporada activa (workspace_id + is_active).
            entity.HasIndex(s => s.WorkspaceId)
                .IsUnique()
                .HasFilter("is_active")
                .HasDatabaseName("ux_seasons_workspace_active");

            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(s => s.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
