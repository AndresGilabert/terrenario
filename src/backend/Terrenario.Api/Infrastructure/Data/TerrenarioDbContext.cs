using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Activities;
using Terrenario.Api.Domain.Consumptions;
using Terrenario.Api.Domain.Harvests;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Purchases;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Tasks;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workers;
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
    public DbSet<WorkspaceReactivationRequest> WorkspaceReactivationRequests => Set<WorkspaceReactivationRequest>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Plot> Plots => Set<Plot>();
    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseConsumption> PurchaseConsumptions => Set<PurchaseConsumption>();
    public DbSet<Harvest> Harvests => Set<Harvest>();

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
            // MVP-505 (CA-3) — Baja de cuenta: la fila sobrevive anonimizada y esta fecha marca el
            // inicio del plazo de retención (RN-041).
            entity.Property(u => u.DeletedAt).HasColumnName("deleted_at");

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
            // Baja lógica (MVP-206, CA-2): nunca se borra la fila.
            entity.Property(w => w.DeletedAt).HasColumnName("deleted_at");
            entity.Property(w => w.DeletedByUserId).HasColumnName("deleted_by_user_id");
            entity.Ignore(w => w.IsDeleted);

            entity.HasIndex(w => w.OwnerId);

            // Todas las lecturas filtran por "vivo": índice parcial sobre las filas no dadas de baja.
            entity.HasIndex(w => w.DeletedAt)
                .HasFilter("deleted_at IS NULL")
                .HasDatabaseName("ix_workspaces_live");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(w => w.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quien dio de baja es la única persona que puede autorizar la reactivación (CA-10):
            // la referencia se protege igual que la de emisor de invitación.
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(w => w.DeletedByUserId)
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
            // MVP-209 — temporada de trabajo de este usuario en este Workspace (por usuario, no por
            // Workspace). `null` ⇒ se resuelve un defecto (WorkingSeasonPolicy).
            entity.Property(m => m.ActiveSeasonId).HasColumnName("active_season_id");
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

            // MVP-209 — si se borra la temporada de trabajo, la membresía vuelve al defecto en vez de
            // quedar con una referencia colgada.
            entity.HasOne<Season>()
                .WithMany()
                .HasForeignKey(m => m.ActiveSeasonId)
                .OnDelete(DeleteBehavior.SetNull);
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
            // Anulación por el Workspace emisor (MVP-207, CA-4): quién la retiró y cuándo.
            entity.Property(i => i.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(i => i.CancelledByUserId).HasColumnName("cancelled_by_user_id");

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

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(i => i.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkspaceReactivationRequest>(entity =>
        {
            entity.ToTable("workspace_reactivation_requests");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).HasColumnName("id");
            entity.Property(r => r.WorkspaceId).HasColumnName("workspace_id").IsRequired();
            entity.Property(r => r.RecipientUserId).HasColumnName("recipient_user_id").IsRequired();
            entity.Property(r => r.AuthorizerUserId).HasColumnName("authorizer_user_id").IsRequired();
            entity.Property(r => r.TokenHash).HasColumnName("token_hash").IsRequired();
            entity.Property(r => r.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(r => r.ExpiresAt).HasColumnName("expires_at");
            entity.Property(r => r.CreatedAt).HasColumnName("created_at");
            entity.Property(r => r.RequestedAt).HasColumnName("requested_at");
            entity.Property(r => r.ResolvedAt).HasColumnName("resolved_at");

            // El enlace es de un solo uso (CA-10) y se busca siempre por hash: el token en claro
            // solo viaja en el email, igual que en las invitaciones (MVP-103).
            entity.HasIndex(r => r.TokenHash).IsUnique();
            // La bandeja de quien tiene que autorizar filtra por autorizador + estado (HU-6).
            entity.HasIndex(r => new { r.AuthorizerUserId, r.Status });
            entity.HasIndex(r => new { r.WorkspaceId, r.Status });

            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(r => r.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.AuthorizerUserId)
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
            entity.Property(s => s.IsClosed).HasColumnName("is_closed");
            entity.Property(s => s.CreatedAt).HasColumnName("created_at");
            entity.Property(s => s.UpdatedAt).HasColumnName("updated_at");

            // MVP-209 — se retiró `is_active` y el índice único parcial `ux_seasons_workspace_active`:
            // ya no hay «una activa por Workspace». La temporada de trabajo es por usuario y vive en
            // `workspace_members.active_season_id`. El estado (planificada/abierta/cerrada) se deriva de
            // `is_closed` + `start_date` en lectura (`Season.StatusOn`), sin columna propia.

            // MVP-207 (CA-3) — nombre único por Workspace ignorando mayúsculas
            // (ux_seasons_workspace_name, UNIQUE sobre (workspace_id, lower(name))). Es un índice sobre
            // una expresión, que EF Core no sabe declarar en el modelo: se crea en la migración con SQL
            // y se documenta aquí. Misma comparación que SeasonRepository.ExistsWithNameAsync.

            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(s => s.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Plot>(entity =>
        {
            entity.ToTable("plots");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasColumnName("id");
            entity.Property(p => p.WorkspaceId).HasColumnName("workspace_id").IsRequired();
            entity.Property(p => p.Name).HasColumnName("name").HasMaxLength(Plot.NameMaxLength).IsRequired();
            entity.Property(p => p.OwnershipType).HasColumnName("ownership_type").HasMaxLength(20).IsRequired();
            entity.Property(p => p.Alias).HasColumnName("alias").HasMaxLength(Plot.AliasMaxLength);
            entity.Property(p => p.OwnerName).HasColumnName("owner_name").HasMaxLength(Plot.OwnerNameMaxLength);
            entity.Property(p => p.CadastralReference).HasColumnName("cadastral_reference").HasMaxLength(Plot.CadastralReferenceMaxLength);
            entity.Property(p => p.Location).HasColumnName("location").HasMaxLength(Plot.LocationMaxLength);
            entity.Property(p => p.TreeCount).HasColumnName("tree_count");
            entity.Property(p => p.IsActive).HasColumnName("is_active");
            entity.Property(p => p.CreatedAt).HasColumnName("created_at");
            entity.Property(p => p.UpdatedAt).HasColumnName("updated_at");

            // El maestro siempre consulta por Workspace (aislamiento multi-tenant) y suele filtrar por
            // estado de actividad: índice de apoyo (workspace_id, is_active).
            entity.HasIndex(p => new { p.WorkspaceId, p.IsActive });

            // MVP-207 (CA-3) — nombre único por Workspace ignorando mayúsculas
            // (ux_plots_workspace_name, UNIQUE sobre (workspace_id, lower(name))), creado en la
            // migración con SQL por ser un índice sobre una expresión. El alias no entra: es un apodo
            // libre. Misma comparación que PlotRepository.ExistsWithNameAsync.

            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(p => p.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Worker>(entity =>
        {
            entity.ToTable("workers");
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Id).HasColumnName("id");
            entity.Property(w => w.WorkspaceId).HasColumnName("workspace_id").IsRequired();
            entity.Property(w => w.UserAccountId).HasColumnName("user_account_id");
            entity.Property(w => w.Name).HasColumnName("name").HasMaxLength(Worker.NameMaxLength).IsRequired();
            entity.Property(w => w.HourlyRate).HasColumnName("hourly_rate").HasPrecision(10, 2);
            entity.Property(w => w.IsActive).HasColumnName("is_active");
            entity.Property(w => w.CreatedAt).HasColumnName("created_at");
            entity.Property(w => w.UpdatedAt).HasColumnName("updated_at");

            // El maestro consulta por Workspace (aislamiento multi-tenant) y filtra por estado de
            // actividad: índice de apoyo (workspace_id, is_active), coherente con plots (MVP-202).
            entity.HasIndex(w => new { w.WorkspaceId, w.IsActive });

            // MVP-207 (CA-3) — nombre único por Workspace ignorando mayúsculas
            // (ux_workers_workspace_name, UNIQUE sobre (workspace_id, lower(name))), creado en la
            // migración con SQL por ser un índice sobre una expresión. Misma comparación que
            // WorkerRepository.ExistsWithNameAsync. Desde MVP-208 cubre la unión miembro/cuadrilla,
            // que es la que RN-027 define como maestro de responsables (hallazgo R-16).

            // MVP-208 (CA-1) — una cuenta tiene como mucho una fila de responsable por Workspace: es
            // lo que hace de `user_account_id` una identidad y no una etiqueta. Índice parcial: la
            // cuadrilla sin cuenta (NULL) no entra.
            entity.HasIndex(w => new { w.WorkspaceId, w.UserAccountId })
                .IsUnique()
                .HasFilter("user_account_id IS NOT NULL")
                .HasDatabaseName("ux_workers_workspace_user_account");

            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(w => w.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // MVP-208 — el vínculo con la cuenta ya se materializa (cerraba P-034): cada miembro
            // activo tiene su fila. `SET NULL` al borrar la cuenta deja la fila como cuadrilla en vez
            // de perder al responsable de los registros históricos que lo referencian.
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(w => w.UserAccountId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("tasks");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasColumnName("id");
            entity.Property(t => t.WorkspaceId).HasColumnName("workspace_id").IsRequired();
            entity.Property(t => t.Name).HasColumnName("name").HasMaxLength(TaskItem.NameMaxLength).IsRequired();
            entity.Property(t => t.IsActive).HasColumnName("is_active");
            entity.Property(t => t.CreatedAt).HasColumnName("created_at");
            entity.Property(t => t.UpdatedAt).HasColumnName("updated_at");

            // El catálogo consulta por Workspace (aislamiento multi-tenant, CA-1) y filtra por estado
            // de actividad: índice de apoyo (workspace_id, is_active), coherente con plots y workers.
            entity.HasIndex(t => new { t.WorkspaceId, t.IsActive });

            // La unicidad de nombre por Workspace ignorando mayúsculas
            // (ux_tasks_workspace_name, UNIQUE sobre (workspace_id, lower(name))) se crea en la
            // migración con SQL: EF Core no sabe declarar índices sobre expresiones. Es la misma
            // comparación que hace TaskRepository.ExistsWithNameAsync, para que la guarda de
            // aplicación y la invariante de base de datos no puedan discrepar.

            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(t => t.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.ToTable("activities");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).HasColumnName("id");
            entity.Property(a => a.WorkspaceId).HasColumnName("workspace_id").IsRequired();
            entity.Property(a => a.PlotId).HasColumnName("plot_id").IsRequired();
            entity.Property(a => a.SeasonId).HasColumnName("season_id").IsRequired();
            entity.Property(a => a.WorkerId).HasColumnName("worker_id").IsRequired();
            entity.Property(a => a.Date).HasColumnName("date").IsRequired();
            entity.Property(a => a.Hours).HasColumnName("hours").HasPrecision(5, 2).IsRequired();
            // RN-025 — la tarea llega del catálogo (FK opcional) o en texto libre, nunca las dos:
            // la exclusividad la garantiza el agregado, no una restricción de datos, porque la
            // condición es «exactamente una» y depende de la longitud del texto ya normalizado.
            // Cierra P-028: el ER declaraba la tarea como un `string task` suelto.
            entity.Property(a => a.TaskId).HasColumnName("task_id");
            entity.Property(a => a.TaskText).HasColumnName("task_text").HasMaxLength(Activity.TaskTextMaxLength);
            entity.Property(a => a.ManualCost).HasColumnName("manual_cost").HasPrecision(10, 2).IsRequired();
            entity.Property(a => a.Description).HasColumnName("description").HasMaxLength(Activity.DescriptionMaxLength);
            entity.Property(a => a.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(a => a.CreatedAt).HasColumnName("created_at");
            entity.Property(a => a.UpdatedBy).HasColumnName("updated_by").IsRequired();
            entity.Property(a => a.UpdatedAt).HasColumnName("updated_at");
            // Bloqueo optimista (ADR-0005): el dominio incrementa `version` en cada mutación y EF la
            // usa además como token de concurrencia, de modo que dos escrituras simultáneas partiendo
            // de la misma versión no puedan pisarse aunque las dos pasen la guarda de aplicación.
            entity.Property(a => a.Version).HasColumnName("version").IsConcurrencyToken();
            // Eliminación lógica (RN-037): nunca se borra la fila.
            entity.Property(a => a.DeletedAt).HasColumnName("deleted_at");
            entity.Ignore(a => a.IsDeleted);

            // El diario consulta siempre por Workspace y ordena por fecha de negocio (RN-033), y
            // siempre sobre registros vivos: índice parcial, como `ix_workspaces_live` en MVP-206.
            entity.HasIndex(a => new { a.WorkspaceId, a.Date })
                .HasFilter("deleted_at IS NULL")
                .HasDatabaseName("ix_activities_live_by_date");
            // Filtros del listado y del futuro dashboard.
            entity.HasIndex(a => new { a.WorkspaceId, a.PlotId });
            entity.HasIndex(a => new { a.WorkspaceId, a.SeasonId });

            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(a => a.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Los maestros no se borran (se inactivan), así que `Restrict` es la semántica correcta:
            // si algún día se borrara un terreno con histórico, la operativa no debe quedar huérfana.
            entity.HasOne<Plot>()
                .WithMany()
                .HasForeignKey(a => a.PlotId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Season>()
                .WithMany()
                .HasForeignKey(a => a.SeasonId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Worker>()
                .WithMany()
                .HasForeignKey(a => a.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // La tarea del catálogo es opcional (RN-025). `SET NULL` degradaría la actividad a «sin
            // tarea», que RN-025 prohíbe, así que también se restringe.
            entity.HasOne<TaskItem>()
                .WithMany()
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.ToTable("purchases");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasColumnName("id");
            entity.Property(p => p.WorkspaceId).HasColumnName("workspace_id").IsRequired();
            // RN-021 / P-050 — toda compra pertenece a una temporada. El contrato ya lo exigía y el
            // ER no lo declaraba hasta la 3ª pasada de MVP-299; aquí se materializa.
            entity.Property(p => p.SeasonId).HasColumnName("season_id").IsRequired();
            entity.Property(p => p.PurchaseDate).HasColumnName("purchase_date").IsRequired();
            // RN-031 — texto libre: no hay catálogo cerrado de materiales.
            entity.Property(p => p.Product).HasColumnName("product").HasMaxLength(Purchase.ProductMaxLength).IsRequired();
            entity.Property(p => p.TotalQuantity).HasColumnName("total_quantity").HasPrecision(10, 2).IsRequired();
            entity.Property(p => p.TotalCost).HasColumnName("total_cost").HasPrecision(10, 2).IsRequired();
            // Derivado de coste/cantidad pero **persistido**: es lo que MVP-304 usará para el coste
            // proporcional, y guardarlo permite explicar una imputación vieja aunque la compra cambie.
            entity.Property(p => p.UnitPrice).HasColumnName("unit_price").HasPrecision(10, 4).IsRequired();
            entity.Property(p => p.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(p => p.CreatedAt).HasColumnName("created_at");
            entity.Property(p => p.UpdatedBy).HasColumnName("updated_by").IsRequired();
            entity.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            entity.Property(p => p.Version).HasColumnName("version").IsConcurrencyToken();
            entity.Property(p => p.DeletedAt).HasColumnName("deleted_at");
            entity.Ignore(p => p.IsDeleted);

            // El libro de compras y el diario consultan por Workspace y ordenan por fecha de compra,
            // siempre sobre registros vivos: índice parcial, igual que en actividades.
            entity.HasIndex(p => new { p.WorkspaceId, p.PurchaseDate })
                .HasFilter("deleted_at IS NULL")
                .HasDatabaseName("ix_purchases_live_by_date");
            entity.HasIndex(p => new { p.WorkspaceId, p.SeasonId });
            // Las sugerencias de producto (RN-031) agrupan por Workspace y producto.
            entity.HasIndex(p => new { p.WorkspaceId, p.Product });

            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(p => p.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Season>()
                .WithMany()
                .HasForeignKey(p => p.SeasonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseConsumption>(entity =>
        {
            entity.ToTable("purchase_consumptions");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasColumnName("id");
            entity.Property(c => c.WorkspaceId).HasColumnName("workspace_id").IsRequired();
            // RN-032 — **anulable**: se puede registrar consumo sin compra previa, y entonces el
            // coste imputado es 0. Es la excepción operativa más importante de la épica (G-2).
            entity.Property(c => c.PurchaseId).HasColumnName("purchase_id");
            entity.Property(c => c.PlotId).HasColumnName("plot_id").IsRequired();
            entity.Property(c => c.SeasonId).HasColumnName("season_id").IsRequired();
            // G-3 — fecha de negocio propia: el diario ordena por ella, no por `created_at` (RN-033).
            entity.Property(c => c.Date).HasColumnName("date").IsRequired();
            // Se guarda siempre, heredado de la compra al imputar: la fila se explica sola.
            entity.Property(c => c.Product).HasColumnName("product")
                .HasMaxLength(PurchaseConsumption.ProductMaxLength).IsRequired();
            entity.Property(c => c.ConsumedQuantity).HasColumnName("consumed_quantity").HasPrecision(10, 2).IsRequired();
            // Precio unitario **congelado** al imputar. Es lo que hace verdadero el CA-3 por
            // estructura: editar la compra después no reescribe el coste de lo ya consumido (RN-032).
            entity.Property(c => c.UnitPrice).HasColumnName("unit_price").HasPrecision(10, 4).IsRequired();
            entity.Property(c => c.ProportionalCost).HasColumnName("proportional_cost").HasPrecision(10, 2).IsRequired();
            entity.Property(c => c.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(c => c.CreatedAt).HasColumnName("created_at");
            entity.Property(c => c.UpdatedBy).HasColumnName("updated_by").IsRequired();
            entity.Property(c => c.UpdatedAt).HasColumnName("updated_at");
            entity.Property(c => c.Version).HasColumnName("version").IsConcurrencyToken();
            entity.Property(c => c.DeletedAt).HasColumnName("deleted_at");
            entity.Ignore(c => c.IsDeleted);
            entity.Ignore(c => c.HasPurchase);

            entity.HasIndex(c => new { c.WorkspaceId, c.Date })
                .HasFilter("deleted_at IS NULL")
                .HasDatabaseName("ix_purchase_consumptions_live_by_date");
            // La guarda de sobre-imputación suma por compra; el libro muestra «imputado / total».
            entity.HasIndex(c => new { c.WorkspaceId, c.PurchaseId });
            entity.HasIndex(c => new { c.WorkspaceId, c.PlotId });

            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(c => c.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // `RESTRICT` y no `CASCADE`: dar de baja una compra no puede llevarse por delante
            // registros operativos que están en el diario. La guarda de negocio lo impide antes
            // (BUSINESS_RULE_PURCHASE_HAS_CONSUMPTIONS); esto es la red por debajo.
            entity.HasOne<Purchase>()
                .WithMany()
                .HasForeignKey(c => c.PurchaseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Plot>()
                .WithMany()
                .HasForeignKey(c => c.PlotId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Season>()
                .WithMany()
                .HasForeignKey(c => c.SeasonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Harvest>(entity =>
        {
            entity.ToTable("harvests");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Id).HasColumnName("id");
            entity.Property(h => h.WorkspaceId).HasColumnName("workspace_id").IsRequired();
            // RN-001 — toda cosecha es de un terreno; RN-021 — y de una temporada.
            entity.Property(h => h.PlotId).HasColumnName("plot_id").IsRequired();
            entity.Property(h => h.SeasonId).HasColumnName("season_id").IsRequired();
            entity.Property(h => h.Date).HasColumnName("date").IsRequired();
            // RN-030 — producto de catálogo global fijo (no texto libre como el material de compra):
            // la columna guarda el **código**, y el cierre del catálogo lo aplica MVP-402.
            entity.Property(h => h.Product).HasColumnName("product")
                .HasMaxLength(Harvest.ProductMaxLength).IsRequired();
            entity.Property(h => h.Kgs).HasColumnName("kgs").HasPrecision(10, 2).IsRequired();
            // RN-004 — opcionales y **excluyentes**: la exclusividad la garantiza el agregado, no una
            // restricción de datos, igual que el par tarea de ACTIVITY en MVP-301.
            entity.Property(h => h.Yield).HasColumnName("yield").HasPrecision(10, 4);
            entity.Property(h => h.Liters).HasColumnName("liters").HasPrecision(10, 2);
            // RN-012 — catálogo cerrado con `desconocido` como valor válido.
            entity.Property(h => h.Destination).HasColumnName("destination")
                .HasMaxLength(Harvest.DestinationMaxLength).IsRequired();
            entity.Property(h => h.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(h => h.CreatedAt).HasColumnName("created_at");
            entity.Property(h => h.UpdatedBy).HasColumnName("updated_by").IsRequired();
            entity.Property(h => h.UpdatedAt).HasColumnName("updated_at");
            entity.Property(h => h.Version).HasColumnName("version").IsConcurrencyToken();
            entity.Property(h => h.DeletedAt).HasColumnName("deleted_at");
            entity.Ignore(h => h.IsDeleted);

            // El listado y el diario consultan por Workspace y ordenan por fecha de negocio (RN-033),
            // siempre sobre registros vivos: índice parcial, igual que en actividades y compras.
            entity.HasIndex(h => new { h.WorkspaceId, h.Date })
                .HasFilter("deleted_at IS NULL")
                .HasDatabaseName("ix_harvests_live_by_date");
            // Los cuatro widgets del dashboard (MVP-403/404) agrupan por terreno, por temporada y por
            // destino: los tres filtros tienen índice de apoyo desde el principio.
            entity.HasIndex(h => new { h.WorkspaceId, h.PlotId });
            entity.HasIndex(h => new { h.WorkspaceId, h.SeasonId });
            entity.HasIndex(h => new { h.WorkspaceId, h.Destination });

            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(h => h.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Los maestros se inactivan, no se borran: `Restrict` impide que un borrado deje operativa
            // huérfana, igual que en ACTIVITY.
            entity.HasOne<Plot>()
                .WithMany()
                .HasForeignKey(h => h.PlotId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Season>()
                .WithMany()
                .HasForeignKey(h => h.SeasonId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
