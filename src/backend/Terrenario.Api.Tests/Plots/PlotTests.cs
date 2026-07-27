using FluentAssertions;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Plots;

namespace Terrenario.Api.Tests.Plots;

/// <summary>
/// Tests del agregado <see cref="Plot"/> (MVP-202). Cubren el alta mínima (RN-028), la normalización
/// de campos opcionales, las validaciones y la inactivación reversible (CA-3).
/// </summary>
public sealed class PlotTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    [Fact]
    public void Create_Deberia_DarDeAltaConSoloNombreYTipoPropiedad()
    {
        var plot = Plot.Create(WorkspaceId, "La Hoya Norte", PlotOwnershipTypes.Propia);

        plot.Id.Should().NotBeEmpty();
        plot.WorkspaceId.Should().Be(WorkspaceId);
        plot.Name.Should().Be("La Hoya Norte");
        plot.OwnershipType.Should().Be("propia");
        plot.IsActive.Should().BeTrue();
        plot.Alias.Should().BeNull();
        plot.OwnerName.Should().BeNull();
        plot.CadastralReference.Should().BeNull();
        plot.Location.Should().BeNull();
        plot.TreeCount.Should().BeNull();
    }

    [Fact]
    public void Create_Deberia_NormalizarNombreYVaciarOpcionalesEnBlanco()
    {
        var plot = Plot.Create(
            WorkspaceId, "  Olivar Alto  ", PlotOwnershipTypes.Cedida,
            alias: "   ", ownerName: "  Antonio  ", cadastralReference: "", location: "  Sector Sur ", treeCount: 850);

        plot.Name.Should().Be("Olivar Alto");
        plot.Alias.Should().BeNull();
        plot.OwnerName.Should().Be("Antonio");
        plot.CadastralReference.Should().BeNull();
        plot.Location.Should().Be("Sector Sur");
        plot.TreeCount.Should().Be(850);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Deberia_RechazarNombreVacio(string name)
    {
        var act = () => Plot.Create(WorkspaceId, name, PlotOwnershipTypes.Propia);

        act.Should().Throw<PlotValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredName);
    }

    [Fact]
    public void Create_Deberia_RechazarNombreLargo()
    {
        var act = () => Plot.Create(WorkspaceId, new string('x', Plot.NameMaxLength + 1), PlotOwnershipTypes.Propia);

        act.Should().Throw<PlotValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationPlotNameLength);
    }

    [Fact]
    public void Create_Deberia_RechazarTipoPropiedadVacio()
    {
        var act = () => Plot.Create(WorkspaceId, "La Hoya", "  ");

        act.Should().Throw<PlotValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredPlotOwnershipType);
    }

    [Fact]
    public void Create_Deberia_RechazarTipoPropiedadFueraDeCatalogo()
    {
        var act = () => Plot.Create(WorkspaceId, "La Hoya", "arrendada");

        act.Should().Throw<PlotValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationPlotOwnershipTypeInvalid);
    }

    [Fact]
    public void Create_Deberia_RechazarNumeroDeArbolesNegativo()
    {
        var act = () => Plot.Create(WorkspaceId, "La Hoya", PlotOwnershipTypes.Propia, treeCount: -1);

        act.Should().Throw<PlotValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRangeTreeCount);
    }

    [Fact]
    public void Create_Deberia_RechazarWorkspaceInvalido()
    {
        var act = () => Plot.Create(Guid.Empty, "La Hoya", PlotOwnershipTypes.Propia);

        act.Should().Throw<PlotValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredPlotWorkspace);
    }

    [Fact]
    public void Update_Deberia_ReemplazarDatosDescriptivos()
    {
        var plot = Plot.Create(WorkspaceId, "La Hoya", PlotOwnershipTypes.Propia);
        var createdAt = plot.CreatedAt;

        plot.Update("La Hoya Sur", PlotOwnershipTypes.Cedida, "LH-04", "Antonio", "1234-A", "Sector Sur", 500);

        plot.Name.Should().Be("La Hoya Sur");
        plot.OwnershipType.Should().Be("cedida");
        plot.Alias.Should().Be("LH-04");
        plot.OwnerName.Should().Be("Antonio");
        plot.CadastralReference.Should().Be("1234-A");
        plot.Location.Should().Be("Sector Sur");
        plot.TreeCount.Should().Be(500);
        plot.CreatedAt.Should().Be(createdAt);
        plot.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SetActive_Deberia_InactivarYReactivarSinBorrar()
    {
        var plot = Plot.Create(WorkspaceId, "La Hoya", PlotOwnershipTypes.Propia);

        plot.SetActive(false);
        plot.IsActive.Should().BeFalse();

        plot.SetActive(true);
        plot.IsActive.Should().BeTrue();
    }
}
