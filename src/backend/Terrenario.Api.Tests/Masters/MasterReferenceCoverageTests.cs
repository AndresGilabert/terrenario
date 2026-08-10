using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Terrenario.Api.Domain.Masters;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;

namespace Terrenario.Api.Tests.Masters;

/// <summary>
/// MVP-806 — La guarda del hallazgo que el spec anticipa: «comprobar el sin uso contra una sola tabla
/// es exactamente el fallo que dejaría un registro huérfano».
///
/// El resto de los tests de esta historia comprueban que las referencias <b>declaradas</b> se cuentan
/// y se reapuntan bien. Este comprueba lo otro, que es lo que ninguno de ellos puede ver: que no falte
/// ninguna por declarar. Se le pregunta al <b>modelo de EF</b> qué claves ajenas apuntan a cada
/// maestro y se contrasta con <see cref="MasterReferenceMap"/>.
///
/// Su valor está en el futuro: el día que aparezca una entidad operativa nueva con un
/// <c>plot_id</c> —o que una existente gane una referencia— este test se pone rojo antes de que nadie
/// pueda borrar un terreno que sí se usaba. Sin él, el fallo aparecería en producción como un 500 de
/// clave ajena, o peor, como un recuento que dice «sin uso» sobre una ficha que sí lo tiene.
/// </summary>
public sealed class MasterReferenceCoverageTests
{
    /// <summary>
    /// El modelo se construye sin tocar la base de datos: <c>UseNpgsql</c> con una cadena de conexión
    /// que nunca se abre basta para que EF resuelva el mapeo completo.
    /// </summary>
    private static readonly IModel Model = new TerrenarioDbContext(
        new DbContextOptionsBuilder<TerrenarioDbContext>()
            .UseNpgsql("Host=modelo-en-memoria;Database=terrenario")
            .Options).Model;

    public static TheoryData<MasterKind> Kinds =>
        [MasterKind.Plot, MasterKind.Season, MasterKind.Worker, MasterKind.Task];

    [Theory]
    [MemberData(nameof(Kinds))]
    public void ElMapa_Deberia_DeclararTodasLasClavesAjenasQueApuntanAlMaestro(MasterKind kind)
    {
        var master = MasterReferenceMap.EntityTypeOf(kind);

        var inTheModel = Model.GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys())
            .Where(fk => fk.PrincipalEntityType.ClrType == master)
            .Select(fk => $"{fk.DeclaringEntityType.ClrType.Name}.{fk.Properties[0].Name}")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var declared = MasterReferenceMap.For(kind)
            .Select(reference => $"{reference.EntityType.Name}.{reference.ForeignKey}")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        declared.Should().BeEquivalentTo(inTheModel, because:
            $"MasterReferenceMap tiene que recoger todas las referencias a {master.Name}: si falta una, " +
            "el «sin uso» dirá que no hay histórico cuando sí lo hay");
    }

    [Fact]
    public void LaUnicaReferenciaNoOperativa_Deberia_SerLaPreferenciaDeTemporadaDeTrabajo()
    {
        // Marcar una referencia como no operativa la saca del recuento que bloquea el borrado, así que
        // no puede ser una decisión que se tome de pasada. Hoy hay exactamente una: la temporada de
        // trabajo de un miembro (MVP-209), cuya FK es `ON DELETE SET NULL` porque es una preferencia
        // que se resuelve sola cayendo al defecto, no histórico que se perdería.
        var nonOperational = Enum.GetValues<MasterKind>()
            .SelectMany(MasterReferenceMap.For)
            .Where(reference => !reference.IsOperational)
            .Select(reference => $"{reference.EntityType.Name}.{reference.ForeignKey}")
            .Distinct()
            .ToList();

        nonOperational.Should().Equal("WorkspaceMember.ActiveSeasonId");
    }
}
