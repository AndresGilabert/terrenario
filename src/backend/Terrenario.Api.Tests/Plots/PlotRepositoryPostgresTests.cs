using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Tests.Integration;

namespace Terrenario.Api.Tests.Plots;

/// <summary>
/// Tests del repositorio de terrenos contra PostgreSQL real (MVP-202): ejercitan la traducción a SQL de
/// los filtros de listado y del aislamiento por Workspace, que los mocks no ven (lección de P-014).
/// </summary>
public sealed class PlotRepositoryPostgresTests : RepositoryTestBase
{

    private async Task<Workspace> SeedWorkspaceAsync(string suffix = "")
    {
        var user = User.Create($"google-sub{suffix}", "Andrés", $"andres{suffix}@ejemplo.com");
        Db.Users.Add(user);
        var workspace = Workspace.Create(user.Id, $"Finca El Olivar {suffix}");
        Db.Workspaces.Add(workspace);
        await Db.SaveChangesAsync();
        return workspace;
    }

    [Fact]
    public async Task ListByWorkspaceAsync_Deberia_AislarPorWorkspace()
    {
        var mine = await SeedWorkspaceAsync("-a");
        var other = await SeedWorkspaceAsync("-b");
        Db.Plots.Add(Plot.Create(mine.Id, "La Hoya", PlotOwnershipTypes.Propia));
        Db.Plots.Add(Plot.Create(other.Id, "Ajena", PlotOwnershipTypes.Cedida));
        await Db.SaveChangesAsync();

        var repository = new PlotRepository(Db);

        var result = await repository.ListByWorkspaceAsync(mine.Id, null, null, default);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("La Hoya");
    }

    [Fact]
    public async Task ListByWorkspaceAsync_Deberia_FiltrarPorEstadoYBusqueda()
    {
        var workspace = await SeedWorkspaceAsync("-c");
        var activo = Plot.Create(workspace.Id, "Olivar Alto", PlotOwnershipTypes.Propia, location: "Sector Norte");
        var inactivo = Plot.Create(workspace.Id, "Olivar Bajo", PlotOwnershipTypes.Propia);
        inactivo.SetActive(false);
        Db.Plots.AddRange(activo, inactivo);
        await Db.SaveChangesAsync();

        var repository = new PlotRepository(Db);

        // Filtro por estado
        var soloActivos = await repository.ListByWorkspaceAsync(workspace.Id, null, isActive: true, default);
        soloActivos.Should().ContainSingle().Which.Name.Should().Be("Olivar Alto");

        // Búsqueda por texto (nombre/alias/ubicación), insensible a mayúsculas
        var porUbicacion = await repository.ListByWorkspaceAsync(workspace.Id, "norte", null, default);
        porUbicacion.Should().ContainSingle().Which.Name.Should().Be("Olivar Alto");

        // Orden: activos primero
        var todos = await repository.ListByWorkspaceAsync(workspace.Id, null, null, default);
        todos.Select(p => p.Name).Should().ContainInOrder("Olivar Alto", "Olivar Bajo");
    }

    [Fact]
    public async Task FindByIdAsync_Deberia_NoDevolverTerrenoDeOtroWorkspace()
    {
        var mine = await SeedWorkspaceAsync("-d");
        var other = await SeedWorkspaceAsync("-e");
        var ajeno = Plot.Create(other.Id, "Ajena", PlotOwnershipTypes.Cedida);
        Db.Plots.Add(ajeno);
        await Db.SaveChangesAsync();

        var repository = new PlotRepository(Db);

        var found = await repository.FindByIdAsync(mine.Id, ajeno.Id, default);

        found.Should().BeNull();
    }

    [Fact]
    public async Task ExistsWithNameAsync_Deberia_IgnorarMayusculas_Y_AcotarPorWorkspace()
    {
        // MVP-207 (CA-2) — mismo criterio que el índice único ux_plots_workspace_name.
        var mine = await SeedWorkspaceAsync("-dup-a");
        var other = await SeedWorkspaceAsync("-dup-b");
        Db.Plots.Add(Plot.Create(mine.Id, "La Hoya", PlotOwnershipTypes.Propia));
        await Db.SaveChangesAsync();

        var repository = new PlotRepository(Db);

        (await repository.ExistsWithNameAsync(mine.Id, "La Hoya", null, default)).Should().BeTrue();
        (await repository.ExistsWithNameAsync(mine.Id, "la hoya", null, default)).Should().BeTrue();
        (await repository.ExistsWithNameAsync(mine.Id, "LA HOYA", null, default)).Should().BeTrue();
        (await repository.ExistsWithNameAsync(mine.Id, "El Cerro", null, default)).Should().BeFalse();
        // El maestro de otro Workspace no genera conflicto (aislamiento multi-tenant).
        (await repository.ExistsWithNameAsync(other.Id, "La Hoya", null, default)).Should().BeFalse();
    }

    [Fact]
    public async Task ExistsWithNameAsync_Deberia_ExcluirElPropioTerreno_VerLosInactivos_Y_NoMirarElAlias()
    {
        var workspace = await SeedWorkspaceAsync("-dup-c");
        var propio = Plot.Create(workspace.Id, "La Hoya", PlotOwnershipTypes.Propia, alias: "El Cerro");
        var inactivo = Plot.Create(workspace.Id, "El Llano", PlotOwnershipTypes.Cedida);
        inactivo.SetActive(false);
        Db.Plots.AddRange(propio, inactivo);
        await Db.SaveChangesAsync();

        var repository = new PlotRepository(Db);

        // Cambiar solo las mayúsculas del propio nombre no es un conflicto consigo mismo.
        (await repository.ExistsWithNameAsync(workspace.Id, "LA HOYA", propio.Id, default)).Should().BeFalse();
        // Un terreno inactivo sigue ocupando su nombre.
        (await repository.ExistsWithNameAsync(workspace.Id, "el llano", null, default)).Should().BeTrue();
        // El alias es un apodo libre: no entra en la comparación.
        (await repository.ExistsWithNameAsync(workspace.Id, "El Cerro", null, default)).Should().BeFalse();
    }


}
