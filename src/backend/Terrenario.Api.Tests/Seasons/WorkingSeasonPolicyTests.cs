using FluentAssertions;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Tests.Seasons;

/// <summary>
/// Tests de la regla de defecto de la temporada de trabajo (MVP-209), aislada del reloj: se le pasa
/// «hoy» para que sea determinista.
/// </summary>
public class WorkingSeasonPolicyTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 6, 15);

    private static Season Season_(string name, DateOnly start, DateOnly? end, bool closed = false)
    {
        var s = Season.Create(WorkspaceId, name, start, end);
        if (closed) s.Close();
        return s;
    }

    [Fact]
    public void Deberia_DevolverNull_SinTemporadas()
    {
        WorkingSeasonPolicy.ResolveDefault([], Today).Should().BeNull();
    }

    [Fact]
    public void Deberia_PreferirLaCampanaQueContieneHoy()
    {
        var pasada = Season_("2024/2025", new DateOnly(2024, 9, 1), new DateOnly(2025, 2, 28));
        var actual = Season_("2025/2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)); // contiene 15-jun-2026
        var futura = Season_("2026/2027", new DateOnly(2026, 9, 1), null);

        WorkingSeasonPolicy.ResolveDefault([pasada, actual, futura], Today).Should().Be(actual);
    }

    [Fact]
    public void Deberia_CaerEnLaAbiertaMasReciente_SiNingunaContieneHoy()
    {
        // Ninguna contiene el 15-jun-2026; las dos abiertas ya terminaron. Gana la de inicio más reciente.
        var vieja = Season_("2023", new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));
        var menosVieja = Season_("2025", new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));

        WorkingSeasonPolicy.ResolveDefault([vieja, menosVieja], Today).Should().Be(menosVieja);
    }

    [Fact]
    public void Deberia_CaerEnLaMasReciente_SiTodoEsPlanificadoOCerrado()
    {
        // Nada abierto: una futura (planificada) y una cerrada. Se ofrece la de inicio más reciente.
        var futura = Season_("2027", new DateOnly(2027, 9, 1), null);
        var cerrada = Season_("2025", new DateOnly(2025, 1, 1), null, closed: true);

        WorkingSeasonPolicy.ResolveDefault([cerrada, futura], Today).Should().Be(futura);
    }
}
