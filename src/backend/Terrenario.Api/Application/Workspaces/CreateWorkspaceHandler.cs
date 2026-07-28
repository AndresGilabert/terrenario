using Terrenario.Api.Application.Workers;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Auth;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-102 — Crea el Workspace, vincula al creador como miembro activo y devuelve una sesión
/// ya situada en ese contexto. Deja el Workspace nuevo como activo persistido para que la sesión
/// renovada no lo pierda (MVP-104).
///
/// MVP-201 — La creación NO siembra ninguna temporada por defecto (decisión de producto): la
/// temporada es un acto explícito y cancelable que el frontend ofrece justo después de crear el
/// Workspace (y también cuando el Workspace activo no tiene temporada), vía <c>POST /seasons</c>.
///
/// MVP-208 (CA-1/CA-4) — Sí siembra al creador en el maestro de responsables: RN-027 obliga a que
/// todo miembro sea seleccionable, así que su fila de <c>workers</c> nace con la membresía y no hace
/// falta darse de alta a uno mismo como trabajador.
/// </summary>
public sealed class CreateWorkspaceHandler(
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    MemberRosterService memberRoster,
    IJwtService jwtService)
{
    public async Task<CreateWorkspaceResult> HandleAsync(CreateWorkspaceCommand command, CancellationToken ct = default)
    {
        var workspace = Workspace.Create(command.UserId, command.Name);
        var ownerMembership = workspace.CreateOwnerMembership();

        await workspaceRepository.AddAsync(workspace, ownerMembership, ct);

        var user = await userRepository.FindByIdAsync(command.UserId, ct);
        user?.SetActiveWorkspace(workspace.Id);

        // El Workspace nace vacío, así que el nombre nunca colisiona: la fila se materializa tal cual
        // llega de la identidad de Google (RN-036).
        var displayName = user?.DisplayName ?? command.DisplayName;
        if (!string.IsNullOrWhiteSpace(displayName))
            await memberRoster.EnsureMemberAsync(workspace.Id, command.UserId, displayName, ct);

        // Todos los repositorios comparten el DbContext de la petición: Workspace, membresía, fila de
        // responsable y preferencia del usuario se escriben en la misma transacción implícita de EF
        // Core, así que no puede quedar un miembro sin su responsable ni al revés.
        await workspaceRepository.SaveChangesAsync(ct);

        var accessToken = jwtService.IssueAccessToken(command.UserId, command.DisplayName, workspace.Id);

        return new CreateWorkspaceResult(
            new WorkspaceSummary(workspace.Id, workspace.Name),
            accessToken.Token,
            accessToken.ExpiresIn);
    }
}
