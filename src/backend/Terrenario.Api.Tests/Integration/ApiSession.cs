using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-501 — Sesión autenticada contra la API del arnés de integración. Guarda el token vigente y lo
/// pone en cada petición, igual que hace el cliente HTTP común del frontend, para que los tests
/// hablen de flujos («da de alta un terreno») y no de cabeceras.
/// </summary>
public sealed class ApiSession(HttpClient client)
{
    public HttpClient Client { get; } = client;

    /// <summary>Token vigente. Cambia al crear un Workspace o al cambiar de activo: la sesión se reemite.</summary>
    public string? AccessToken { get; private set; }

    public Guid UserId { get; private set; }
    public Guid? WorkspaceId { get; private set; }

    /// <summary>
    /// Entra por el flujo real de login (<c>POST /auth/google/callback</c>): lo único simulado es el
    /// intercambio con Google. Crea el usuario si no existía, emite el JWT y deja la cookie de
    /// refresco, exactamente como en producción.
    /// </summary>
    public static async Task<ApiSession> LoginAsync(TerrenarioApiFactory factory, string code)
    {
        var session = new ApiSession(factory.CreateApiClient());

        var response = await session.Client.PostAsJsonAsync("/api/v1/auth/google/callback", new
        {
            code,
            redirect_uri = "https://terrenario.test/auth/callback",
            code_verifier = "verificador-de-prueba",
            flow_id = "0123456789abcdef0123456789abcdef"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        session.Apply(body);
        session.UserId = body.GetProperty("user").GetProperty("id").GetGuid();
        return session;
    }

    /// <summary>Crea el primer Workspace y deja la sesión situada en él (MVP-102).</summary>
    public async Task<Guid> CreateWorkspaceAsync(string name)
    {
        var response = await PostAsync("/api/v1/workspaces", new { name });
        response.EnsureSuccessStatusCode();

        Apply(await response.Content.ReadFromJsonAsync<JsonElement>());
        return WorkspaceId!.Value;
    }

    public Task<HttpResponseMessage> GetAsync(string path) => SendAsync(HttpMethod.Get, path, null);

    public Task<HttpResponseMessage> PostAsync(string path, object? body)
        => SendAsync(HttpMethod.Post, path, body);

    public Task<HttpResponseMessage> PatchAsync(string path, object? body, int? ifMatch = null)
        => SendAsync(HttpMethod.Patch, path, body, ifMatch);

    public Task<HttpResponseMessage> DeleteAsync(string path, int? ifMatch = null)
        => SendAsync(HttpMethod.Delete, path, null, ifMatch);

    /// <summary>Atajo para el caso normal: la petición va bien y solo interesa el cuerpo.</summary>
    public async Task<JsonElement> GetJsonAsync(string path)
    {
        var response = await GetAsync(path);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<JsonElement> PostJsonAsync(string path, object body)
    {
        var response = await PostAsync(path, body);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"POST {path} devolvió {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? body,
        int? ifMatch = null)
    {
        using var request = new HttpRequestMessage(method, path);

        if (AccessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

        // ADR-0005 — los registros operativos exigen la versión vigente para corregir o eliminar.
        if (ifMatch is not null)
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch.Value.ToString());

        if (body is not null)
            request.Content = JsonContent.Create(body);

        return await Client.SendAsync(request);
    }

    /// <summary>Recoge el token y el Workspace de cualquier respuesta que reemita la sesión.</summary>
    private void Apply(JsonElement body)
    {
        if (body.TryGetProperty("access_token", out var token) && token.ValueKind == JsonValueKind.String)
            AccessToken = token.GetString();

        if (body.TryGetProperty("workspace", out var workspace) && workspace.ValueKind == JsonValueKind.Object)
            WorkspaceId = workspace.GetProperty("id").GetGuid();
    }
}
