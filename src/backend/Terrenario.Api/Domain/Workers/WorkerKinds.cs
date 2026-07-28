namespace Terrenario.Api.Domain.Workers;

/// <summary>
/// Catálogo cerrado <c>worker_kind</c> (MVP-208, CA-2): la señal que distingue las dos clases de
/// persona del maestro de responsables. Es vocabulario de contrato de API, estable para el cliente, y
/// se deriva de <c>user_account_id</c>: no es una columna propia.
/// </summary>
public static class WorkerKinds
{
    /// <summary>Miembro del Workspace: tiene cuenta y su nombre llega de ella (RN-027/RN-036).</summary>
    public const string Member = "member";

    /// <summary>Cuadrilla sin cuenta: se da de alta, edita e inactiva en el maestro (MVP-204).</summary>
    public const string Crew = "crew";

    public static string Of(Worker worker) => worker.HasAccount ? Member : Crew;
}
