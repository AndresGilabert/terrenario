using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Activities;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Tasks;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Application.Activities;

/// <summary>
/// Comprueba que los vínculos de una actividad (terreno, temporada, responsable y tarea del catálogo)
/// existen <b>en el Workspace activo</b> antes de persistirla. Es la guarda de
/// <c>FOREIGN_KEY_WORKSPACE_MISMATCH</c> del contrato: sin ella, un id de otra explotación llegaría a
/// la base de datos y la violación de clave ajena se traduciría en un 500 en vez de en un 400 con
/// mensaje útil.
///
/// <b>Los maestros inactivos siguen siendo válidos.</b> La UI ofrece solo los activos para registros
/// nuevos (MVP-202/204/205, CA-3), pero una actividad antigua que referencia un terreno ya inactivado
/// debe poder corregirse sin obligar a reactivarlo: inactivar deja de ofrecer, no invalida el
/// histórico.
/// </summary>
public sealed class ActivityLinkResolver(
    IPlotRepository plotRepository,
    ISeasonRepository seasonRepository,
    IWorkerRepository workerRepository,
    ITaskRepository taskRepository)
{
    public async Task EnsureLinksAsync(
        Guid workspaceId,
        Guid plotId,
        Guid seasonId,
        Guid workerId,
        Guid? taskId,
        CancellationToken ct)
    {
        // Un vínculo vacío es un campo obligatorio que falta, no una referencia rota: se distingue
        // antes de gastar consultas para que el cliente reciba el código correcto (RN-001/RN-002/RN-021).
        if (plotId == Guid.Empty || seasonId == Guid.Empty || workerId == Guid.Empty)
            throw new ActivityValidationException(
                ErrorCodes.ValidationActivityRequiredFields,
                "La actividad necesita terreno, temporada y responsable.");

        if (await plotRepository.FindByIdAsync(workspaceId, plotId, ct) is null)
            throw Mismatch("El terreno indicado no existe en tu Workspace activo.");

        if (await seasonRepository.FindByIdAsync(workspaceId, seasonId, ct) is null)
            throw Mismatch("La temporada indicada no existe en tu Workspace activo.");

        if (await workerRepository.FindByIdAsync(workspaceId, workerId, ct) is null)
            throw Mismatch("El responsable indicado no existe en tu Workspace activo.");

        if (taskId is { } id && await taskRepository.FindByIdAsync(workspaceId, id, ct) is null)
            throw Mismatch("La tarea indicada no existe en el catálogo de tu Workspace activo.");
    }

    private static ActivityValidationException Mismatch(string message) =>
        new(ErrorCodes.ForeignKeyWorkspaceMismatch, message);
}
