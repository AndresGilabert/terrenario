using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Plots;
using Terrenario.Api.Application.Plots.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Domain.Plots;

namespace Terrenario.Api.Tests.Plots;

public class UpdatePlotHandlerTests
{
    private readonly IPlotRepository _plotRepository = Substitute.For<IPlotRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private UpdatePlotHandler CreateSut() => new(_plotRepository);

    /// <summary>Edición completa: todos los campos presentes.</summary>
    private static UpdatePlotCommand FullEdit(Guid plotId) => new(
        WorkspaceId, plotId,
        FieldUpdate<string>.Set("La Hoya Sur"),
        FieldUpdate<string>.Set(PlotOwnershipTypes.Cedida),
        FieldUpdate<string?>.Set("LH-04"),
        FieldUpdate<string?>.Set("Antonio"),
        FieldUpdate<string?>.Set("1234-A"),
        FieldUpdate<string?>.Set("Sector Sur"),
        FieldUpdate<int?>.Set(500),
        FieldUpdate<bool>.Absent);

    /// <summary>PATCH mínimo: solo cambia el estado de actividad (el resto ausente).</summary>
    private static UpdatePlotCommand ActiveOnly(Guid plotId, bool isActive) => new(
        WorkspaceId, plotId,
        FieldUpdate<string>.Absent,
        FieldUpdate<string>.Absent,
        FieldUpdate<string?>.Absent,
        FieldUpdate<string?>.Absent,
        FieldUpdate<string?>.Absent,
        FieldUpdate<string?>.Absent,
        FieldUpdate<int?>.Absent,
        FieldUpdate<bool>.Set(isActive));

    [Fact]
    public async Task Deberia_DevolverNull_Cuando_ElTerrenoNoEstaEnElWorkspace()
    {
        // Arrange — aislamiento multi-tenant: el repositorio no lo encuentra en el Workspace activo
        var plotId = Guid.NewGuid();
        _plotRepository.FindByIdAsync(WorkspaceId, plotId, Arg.Any<CancellationToken>())
            .Returns((Plot?)null);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(FullEdit(plotId));

        // Assert
        result.Should().BeNull();
        await _plotRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_EditarDatos_Y_Persistir()
    {
        // Arrange
        var plot = Plot.Create(WorkspaceId, "La Hoya", PlotOwnershipTypes.Propia);
        _plotRepository.FindByIdAsync(WorkspaceId, plot.Id, Arg.Any<CancellationToken>()).Returns(plot);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(FullEdit(plot.Id));

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("La Hoya Sur");
        result.OwnershipType.Should().Be("cedida");
        result.TreeCount.Should().Be(500);
        await _plotRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Inactivar_SinBorrar_Los_CamposOmitidos()
    {
        // Arrange — CA-3 + regresión: un PATCH que solo inactiva NO debe borrar los datos opcionales
        // (bug detectado en la verificación end-to-end: el PATCH "PUT-style" los vaciaba).
        var plot = Plot.Create(
            WorkspaceId, "La Hoya", PlotOwnershipTypes.Cedida,
            alias: "LH-01", ownerName: "Antonio", cadastralReference: "1234-A", location: "Sector Norte", treeCount: 700);
        _plotRepository.FindByIdAsync(WorkspaceId, plot.Id, Arg.Any<CancellationToken>()).Returns(plot);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(ActiveOnly(plot.Id, isActive: false));

        // Assert
        result!.IsActive.Should().BeFalse();
        result.Alias.Should().Be("LH-01");
        result.OwnerName.Should().Be("Antonio");
        result.CadastralReference.Should().Be("1234-A");
        result.Location.Should().Be("Sector Norte");
        result.TreeCount.Should().Be(700);
        await _plotRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
