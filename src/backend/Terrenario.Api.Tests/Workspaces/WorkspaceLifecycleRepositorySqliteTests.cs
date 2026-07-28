using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests contra SQLite real de las consultas que sostienen la baja lógica (MVP-206, CA-2/CA-8). Son
/// filtros que los repositorios mockeados no ven: si el <c>WHERE deleted_at IS NULL</c> se cayera de
/// alguna consulta, un Workspace dado de baja seguiría resolviendo contexto o apareciendo en el
/// selector y los tests de handler no se enterarían (lección de P-014).
/// </summary>
public sealed class WorkspaceLifecycleRepositorySqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TerrenarioDbContext _db;
    private readonly WorkspaceRepository _repository;

    public WorkspaceLifecycleRepositorySqliteTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TerrenarioDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TerrenarioDbContext(options);
        _db.Database.EnsureCreated();
        _repository = new WorkspaceRepository(_db);
    }

    private async Task<User> SeedUserAsync(string suffix, string displayName)
    {
        var user = User.Create($"google-sub{suffix}", displayName, $"user{suffix}@ejemplo.com");
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task<Workspace> SeedWorkspaceAsync(User owner, string name)
    {
        var workspace = Workspace.Create(owner.Id, name);
        _db.Workspaces.Add(workspace);
        _db.WorkspaceMembers.Add(workspace.CreateOwnerMembership());
        await _db.SaveChangesAsync();
        return workspace;
    }

    [Fact]
    public async Task UnWorkspaceDadoDeBaja_Deberia_DesaparecerDeTodasLasLecturas()
    {
        // Arrange — dos Workspaces del mismo usuario; uno se da de baja.
        var owner = await SeedUserAsync("-owner", "Antonio");
        var vivo = await SeedWorkspaceAsync(owner, "Finca Viva");
        var baja = await SeedWorkspaceAsync(owner, "Finca Cerrada");
        baja.SoftDelete(owner.Id, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync();

        // Act + Assert — CA-8: ni resuelve contexto ni aparece en el selector...
        (await _repository.FindForMemberAsync(baja.Id, owner.Id)).Should().BeNull();
        (await _repository.FindByIdAsync(baja.Id)).Should().BeNull();
        (await _repository.HasActiveMembershipAsync(baja.Id, owner.Id)).Should().BeFalse();
        (await _repository.ListActiveMembershipsAsync(owner.Id))
            .Should().ContainSingle().Which.WorkspaceId.Should().Be(vivo.Id);
        (await _repository.FindDefaultForUserAsync(owner.Id))!.Id.Should().Be(vivo.Id);

        // ...pero CA-2: la fila sigue ahí con todos sus datos, y la reactivación puede verla.
        var enBaseDeDatos = await _repository.FindIncludingDeletedAsync(baja.Id);
        enBaseDeDatos.Should().NotBeNull();
        enBaseDeDatos!.Name.Should().Be("Finca Cerrada");
        enBaseDeDatos.DeletedByUserId.Should().Be(owner.Id);
    }

    [Fact]
    public async Task FindDefaultForUserAsync_Deberia_CaerAOtroWorkspace_CuandoElActivoSeDaDeBaja()
    {
        // CA-8 — si el Workspace dado de baja era el activo, la sesión cae al por defecto (MVP-104).
        var owner = await SeedUserAsync("-owner", "Antonio");
        var otro = await SeedWorkspaceAsync(owner, "Finca Alternativa");
        var activo = await SeedWorkspaceAsync(owner, "Finca Cerrada");
        owner.SetActiveWorkspace(activo.Id);
        activo.SoftDelete(owner.Id, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync();

        (await _repository.FindForMemberAsync(activo.Id, owner.Id)).Should().BeNull();
        (await _repository.FindDefaultForUserAsync(owner.Id))!.Id.Should().Be(otro.Id);
    }

    [Fact]
    public async Task FindOtherActiveOwnerAsync_Deberia_DevolverAlCopropietarioMasAntiguo()
    {
        // CA-5 — el sucesor del traspaso automático es determinista.
        var owner = await SeedUserAsync("-owner", "Antonio");
        var antiguo = await SeedUserAsync("-antiguo", "Bruno");
        var reciente = await SeedUserAsync("-reciente", "Zoe");
        var miembro = await SeedUserAsync("-miembro", "Lucia");
        var workspace = await SeedWorkspaceAsync(owner, "Finca El Olivar");

        var copropietarioAntiguo = WorkspaceMember.CreateOwner(workspace.Id, antiguo.Id);
        var copropietarioReciente = WorkspaceMember.CreateOwner(workspace.Id, reciente.Id);
        _db.WorkspaceMembers.AddRange(
            copropietarioReciente,
            copropietarioAntiguo,
            WorkspaceMember.CreateMember(workspace.Id, miembro.Id));
        await _db.SaveChangesAsync();

        // El orden de inserción no manda: manda joined_at.
        var successor = await _repository.FindOtherActiveOwnerAsync(workspace.Id, owner.Id);

        successor.Should().NotBeNull();
        successor!.UserId.Should().Be(
            copropietarioAntiguo.JoinedAt <= copropietarioReciente.JoinedAt ? antiguo.Id : reciente.Id);
    }

    [Fact]
    public async Task FindOtherActiveOwnerAsync_Deberia_IgnorarPropietariosSinAcceso()
    {
        var owner = await SeedUserAsync("-owner", "Antonio");
        var revocado = await SeedUserAsync("-revocado", "Bruno");
        var workspace = await SeedWorkspaceAsync(owner, "Finca El Olivar");

        var copropietarioRevocado = WorkspaceMember.CreateOwner(workspace.Id, revocado.Id);
        copropietarioRevocado.Revoke();
        _db.WorkspaceMembers.Add(copropietarioRevocado);
        await _db.SaveChangesAsync();

        (await _repository.FindOtherActiveOwnerAsync(workspace.Id, owner.Id)).Should().BeNull();
    }

    [Fact]
    public async Task ListSoleOwnedAsync_Deberia_DevolverSoloLosQueQuedarianHuerfanos()
    {
        // CA-9 — regla de no-orfandad de la baja de cuenta.
        var owner = await SeedUserAsync("-owner", "Antonio");
        var otro = await SeedUserAsync("-otro", "Marta");

        var soloSuyo = await SeedWorkspaceAsync(owner, "A Finca Solitaria");
        var compartido = await SeedWorkspaceAsync(owner, "B Finca Compartida");
        var dadoDeBaja = await SeedWorkspaceAsync(owner, "C Finca Cerrada");
        var ajeno = await SeedWorkspaceAsync(otro, "D Finca Ajena");

        // En el compartido hay otro propietario: no quedaría huérfano.
        _db.WorkspaceMembers.Add(WorkspaceMember.CreateOwner(compartido.Id, otro.Id));
        // En el solitario hay un miembro sin propiedad: sigue siendo propietario único, pero con
        // alguien a quien traspasar.
        _db.WorkspaceMembers.Add(WorkspaceMember.CreateMember(soloSuyo.Id, otro.Id));
        // Los dados de baja ya están resueltos y no bloquean nada.
        dadoDeBaja.SoftDelete(owner.Id, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync();

        var obligations = await _repository.ListSoleOwnedAsync(owner.Id);

        obligations.Should().ContainSingle();
        obligations[0].WorkspaceId.Should().Be(soloSuyo.Id);
        obligations[0].OtherActiveMembers.Should().Be(1);
        obligations.Should().NotContain(o => o.WorkspaceId == ajeno.Id);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
