using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workers;
using Terrenario.Api.Application.Workers.Commands;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Tests.Workers;

public class CreateWorkerHandlerTests
{
    private readonly IWorkerRepository _workerRepository = Substitute.For<IWorkerRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private CreateWorkerHandler CreateSut() => new(_workerRepository);

    [Fact]
    public async Task Deberia_DarDeAltaTrabajador_Y_Persistir()
    {
        // Arrange
        Worker? added = null;
        await _workerRepository.AddAsync(Arg.Do<Worker>(w => added = w), Arg.Any<CancellationToken>());
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(new CreateWorkerCommand(WorkspaceId, "Antonio", 12m));

        // Assert
        result.Name.Should().Be("Antonio");
        result.HourlyRate.Should().Be(12m);
        result.IsActive.Should().BeTrue();
        added.Should().NotBeNull();
        added!.WorkspaceId.Should().Be(WorkspaceId);
        await _workerRepository.Received(1).AddAsync(Arg.Any<Worker>(), Arg.Any<CancellationToken>());
        await _workerRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
