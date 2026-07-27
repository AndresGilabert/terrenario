using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Plots;
using Terrenario.Api.Application.Plots.Commands;
using Terrenario.Api.Domain.Plots;

namespace Terrenario.Api.Tests.Plots;

public class CreatePlotHandlerTests
{
    private readonly IPlotRepository _plotRepository = Substitute.For<IPlotRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private CreatePlotHandler CreateSut() => new(_plotRepository);

    [Fact]
    public async Task Deberia_DarDeAltaConDatosMinimos_Y_Persistir()
    {
        // Arrange
        Plot? persisted = null;
        await _plotRepository.AddAsync(Arg.Do<Plot>(p => persisted = p), Arg.Any<CancellationToken>());
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(new CreatePlotCommand(
            WorkspaceId, "La Hoya Norte", PlotOwnershipTypes.Propia, null, null, null, null, null));

        // Assert
        result.Name.Should().Be("La Hoya Norte");
        result.OwnershipType.Should().Be("propia");
        result.IsActive.Should().BeTrue();
        result.HasTreeCount.Should().BeFalse();
        persisted.Should().NotBeNull();
        persisted!.WorkspaceId.Should().Be(WorkspaceId);
        await _plotRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_NoPersistir_Cuando_TipoPropiedadNoEsValido()
    {
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(new CreatePlotCommand(
            WorkspaceId, "La Hoya", "arrendada", null, null, null, null, null));

        await act.Should().ThrowAsync<PlotValidationException>();
        await _plotRepository.DidNotReceive().AddAsync(Arg.Any<Plot>(), Arg.Any<CancellationToken>());
        await _plotRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
