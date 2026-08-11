using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workspaces")]
public sealed class WorkspacesController(
    CreateWorkspaceHandler createWorkspaceHandler,
    ListUserWorkspacesHandler listUserWorkspacesHandler,
    SwitchActiveWorkspaceHandler switchActiveWorkspaceHandler,
    RenameWorkspaceHandler renameWorkspaceHandler,
    GetWorkspaceClosureOptionsHandler closureOptionsHandler,
    CloseWorkspaceHandler closeWorkspaceHandler,
    TransferWorkspaceOwnershipHandler transferOwnershipHandler,
    LeaveWorkspaceHandler leaveWorkspaceHandler,
    WorkspaceOwnershipGuard ownershipGuard,
    IActiveWorkspaceResolver activeWorkspaceResolver,
    IWorkspaceContext workspaceContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkspaceRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        try
        {
            var result = await createWorkspaceHandler.HandleAsync(
                new CreateWorkspaceCommand(userId.Value, User.GetDisplayName(), request.Name),
                ct);

            return CreatedAtAction(
                nameof(GetActive),
                new
                {
                    workspace = new { id = result.Workspace.Id, name = result.Workspace.Name },
                    access_token = result.AccessToken,
                    expires_in = result.ExpiresIn
                });
        }
        catch (WorkspaceValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    /// <summary>MVP-104 — Workspaces disponibles del usuario y cuál está activo (HU-1, CA-1).</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        var result = await listUserWorkspacesHandler.HandleAsync(
            new ListUserWorkspacesQuery(userId.Value, User.GetWorkspaceId()),
            ct);

        return Ok(new
        {
            data = result.Workspaces.Select(w => new
            {
                id = w.Id,
                name = w.Name,
                role = w.Role,
                status = w.Status,
                is_active = w.Id == result.ActiveWorkspaceId,
                joined_at = w.JoinedAt
            }),
            meta = new
            {
                total = result.Workspaces.Count,
                active_workspace_id = result.ActiveWorkspaceId
            }
        });
    }

    /// <summary>MVP-104 — Cambia el Workspace activo y reemite la sesión situada en él (HU-2, CA-2, CA-3).</summary>
    [HttpPut("active")]
    public async Task<IActionResult> SetActive([FromBody] SetActiveWorkspaceRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        // Activar un Workspace sin membresía activa lanza WorkspaceAccessDeniedException, que el
        // WorkspaceAccessExceptionFilter traduce a 403 AUTH_WORKSPACE_FORBIDDEN de forma uniforme.
        var result = await switchActiveWorkspaceHandler.HandleAsync(
            new SwitchActiveWorkspaceCommand(userId.Value, User.GetDisplayName(), request.WorkspaceId!.Value),
            ct);

        return Ok(new
        {
            workspace = new { id = result.Workspace.Id, name = result.Workspace.Name },
            access_token = result.AccessToken,
            expires_in = result.ExpiresIn
        });
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        var workspace = await activeWorkspaceResolver.ResolveAsync(userId.Value, User.GetWorkspaceId(), ct);

        if (workspace is null)
            return NotFound(new ApiErrorResponse(ApiError.WorkspaceNotFound()));

        return Ok(new { id = workspace.Id, name = workspace.Name });
    }

    /// <summary>
    /// MVP-206 (HU-1, CA-1) — Renombra el Workspace activo. Permisos planos (RN-034): cualquier
    /// miembro activo. El Workspace no viaja en la petición: se resuelve en servidor como en el resto
    /// de operaciones con ámbito (MVP-105). No reemite la sesión: el nombre no está en el token.
    /// </summary>
    [HttpPatch("active")]
    [RequireWorkspaceScope]
    public async Task<IActionResult> Rename([FromBody] RenameWorkspaceRequest request, CancellationToken ct)
    {
        try
        {
            var workspace = await renameWorkspaceHandler.HandleAsync(
                workspaceContext.WorkspaceId, request.Name, ct);

            return Ok(new { id = workspace.Id, name = workspace.Name });
        }
        catch (WorkspaceValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (WorkspaceMemberException ex)
        {
            return WorkspaceMemberErrorMapper.ToActionResult(ex);
        }
    }

    /// <summary>
    /// MVP-206 — Qué implica dar de baja el Workspace activo para quien lo pregunta (árbol de
    /// decisión del spec). La UI lo usa para plantear la pregunta correcta y **exigir la decisión**
    /// al propietario único (CA-3) en vez de reimplementar la regla de propiedad en cliente.
    /// </summary>
    [HttpGet("active/closure")]
    [RequireWorkspaceScope]
    public async Task<IActionResult> GetClosureOptions(CancellationToken ct)
    {
        try
        {
            var options = await closureOptionsHandler.HandleAsync(
                workspaceContext.WorkspaceId, User.GetUserId()!.Value, ct);

            return Ok(new
            {
                workspace = new { id = options.WorkspaceId, name = options.WorkspaceName },
                is_owner = options.IsOwner,
                mode = options.Mode,
                active_owners = options.ActiveOwners,
                successor_name = options.SuccessorDisplayName,
                candidates = options.Candidates.Select(c => new
                {
                    user_id = c.UserId,
                    name = c.DisplayName,
                    email = c.Email,
                    role = c.Role
                })
            });
        }
        catch (WorkspaceMemberException ex)
        {
            return WorkspaceMemberErrorMapper.ToActionResult(ex);
        }
    }

    /// <summary>
    /// MVP-206 (HU-2/HU-4, CA-2/CA-5/CA-6) — Da de baja el Workspace activo. Con copropietarios el
    /// Workspace sigue vivo y cambia de manos (quien lo pide sale); siendo propietario único es una
    /// **baja lógica** y el resto de miembros recibe el enlace de reactivación.
    /// </summary>
    [HttpPost("active/closure")]
    [RequireWorkspaceScope]
    public async Task<IActionResult> Close(CancellationToken ct)
    {
        var workspace = workspaceContext.Workspace;

        try
        {
            var result = await closeWorkspaceHandler.HandleAsync(
                new CloseWorkspaceCommand(
                    workspace.Id,
                    workspace.Name,
                    User.GetUserId()!.Value,
                    User.GetDisplayName()),
                ct);

            return Ok(ClosurePayload(result));
        }
        catch (WorkspaceMemberException ex)
        {
            return WorkspaceMemberErrorMapper.ToActionResult(ex);
        }
    }

    /// <summary>
    /// MVP-807 (HU-1, <c>P-048</c>) — <b>Abandonar</b> el Workspace activo. Es el hueco simétrico de
    /// «retirar el acceso a otra persona» (<c>MVP-204</c>) y de «salir siendo propietario»
    /// (<c>MVP-206</c>): un miembro corriente no tenía ninguna vía de salir.
    ///
    /// No reemite la sesión: el cliente resincroniza el contexto y el servidor le resuelve el
    /// Workspace activo que corresponda —otro, o ninguno—, igual que tras dar de baja un Workspace.
    /// Responde <c>204</c> porque no hay nada que devolver: lo que hay que saber después es cuál es el
    /// contexto nuevo, y eso lo dice <c>GET /workspaces/active</c>.
    /// </summary>
    [HttpPost("active/leave")]
    [RequireWorkspaceScope]
    public async Task<IActionResult> Leave(CancellationToken ct)
    {
        try
        {
            await leaveWorkspaceHandler.HandleAsync(
                workspaceContext.WorkspaceId, User.GetUserId()!.Value, ct);

            return NoContent();
        }
        catch (WorkspaceMemberException ex)
        {
            return WorkspaceMemberErrorMapper.ToActionResult(ex);
        }
    }

    /// <summary>
    /// MVP-206 (HU-3, CA-4) — Traspasa la propiedad del Workspace activo a un miembro activo. Es la
    /// alternativa a dar de baja para el propietario único; quien traspasa se queda como miembro.
    /// </summary>
    [HttpPost("active/transfer-ownership")]
    [RequireWorkspaceScope]
    public async Task<IActionResult> TransferOwnership(
        [FromBody] TransferOwnershipRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await transferOwnershipHandler.HandleAsync(
                new TransferOwnershipCommand(
                    workspaceContext.WorkspaceId,
                    User.GetUserId()!.Value,
                    request.NewOwnerUserId!.Value),
                ct);

            return Ok(ClosurePayload(result));
        }
        catch (WorkspaceValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (WorkspaceMemberException ex)
        {
            return WorkspaceMemberErrorMapper.ToActionResult(ex);
        }
    }

    /// <summary>
    /// MVP-206 (HU-3, CA-9) — Workspaces de los que la cuenta es propietaria única y que hay que
    /// resolver (traspasar o dar de baja) antes de poder cerrarla. No exige Workspace activo: es una
    /// pregunta sobre la cuenta, no sobre un contexto. El flujo completo de baja de cuenta (RGPD) es
    /// alcance de otra historia (`MVP-999`, P-024); aquí vive la regla que deberá respetar.
    /// </summary>
    [HttpGet("ownership-obligations")]
    public async Task<IActionResult> GetOwnershipObligations(CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        var obligations = await ownershipGuard.ListObligationsAsync(userId.Value, ct);

        return Ok(new
        {
            data = obligations.Workspaces.Select(w => new
            {
                workspace_id = w.WorkspaceId,
                name = w.Name,
                other_active_members = w.OtherActiveMembers,
                can_transfer = w.OtherActiveMembers > 0
            }),
            meta = new { total = obligations.Workspaces.Count, is_clear = obligations.IsClear }
        });
    }

    private static object ClosurePayload(WorkspaceClosureResult result) => new
    {
        outcome = result.Outcome,
        workspace = new { id = result.WorkspaceId, name = result.WorkspaceName },
        new_owner_name = result.NewOwnerDisplayName,
        notified_members = result.NotifiedMembers,
        emails_sent = result.EmailsSent
    };
}

public sealed record CreateWorkspaceRequest(
    [RequiredField(ErrorCodes.ValidationRequiredWorkspaceName, "El nombre del Workspace es obligatorio.")]
    [MaxTextLength(Workspace.NameMaxLength, ErrorCodes.ValidationWorkspaceNameLength, "El nombre del Workspace es demasiado largo.")]
    string Name);

public sealed record SetActiveWorkspaceRequest(
    [RequiredField(ErrorCodes.ValidationRequired, "Indica el Workspace que quieres activar.")]
    [property: JsonPropertyName("workspace_id")]
    Guid? WorkspaceId);

/// <summary>MVP-206 (HU-1) — Nuevo nombre del Workspace activo.</summary>
public sealed record RenameWorkspaceRequest(
    [RequiredField(ErrorCodes.ValidationRequiredWorkspaceName, "El nombre del Workspace es obligatorio.")]
    [MaxTextLength(Workspace.NameMaxLength, ErrorCodes.ValidationWorkspaceNameLength, "El nombre del Workspace es demasiado largo.")]
    string Name);

/// <summary>MVP-206 (HU-3/CA-4) — Persona que recibe la propiedad del Workspace.</summary>
public sealed record TransferOwnershipRequest(
    [RequiredField(ErrorCodes.ValidationRequiredNewOwner, "Indica a qué persona traspasas la propiedad.")]
    [property: JsonPropertyName("new_owner_user_id")]
    Guid? NewOwnerUserId);
