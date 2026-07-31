using FluentAssertions;
using System.Text;
using System.Text.Json;
using Terrenario.Api.Common.Http;

namespace Terrenario.Api.Tests.Http;

/// <summary>
/// MVP-502 (<c>P-027</c>) — El lector común de cuerpos de edición parcial. Los tests de integración
/// comprueban el <c>400</c> por la API; estos fijan el contrato del lector, que es lo que usan los
/// ocho controladores.
/// </summary>
public sealed class PartialUpdateBodyTests
{
    private static Dictionary<string, JsonElement> Parse(string json)
        => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    /// <summary>Cuerpo cuyo valor de texto contiene bytes que no son UTF-8 válido (<c>0xFF</c>).</summary>
    private static Dictionary<string, JsonElement> WithInvalidUtf8Text()
    {
        var prefix = Encoding.UTF8.GetBytes("{\"name\":\"");
        var suffix = Encoding.UTF8.GetBytes("\"}");
        var payload = new byte[prefix.Length + 1 + suffix.Length];
        prefix.CopyTo(payload, 0);
        payload[prefix.Length] = 0xFF;
        suffix.CopyTo(payload, prefix.Length + 1);

        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload)!;
    }

    [Fact]
    public void Deberia_DistinguirAusenteDeVacio_Cuando_SeLeeUnTexto()
    {
        var body = PartialUpdateBody.From(Parse("""{"name": "La Hoya", "alias": null}"""));

        // Es la razón de ser de `FieldUpdate`: omitir un campo conserva su valor, enviarlo nulo lo limpia.
        body.ReadString("name").Should().BeEquivalentTo(new { Present = true, Value = "La Hoya" });
        body.ReadNullableString("alias").Should().BeEquivalentTo(new { Present = true, Value = (string?)null });
        body.ReadNullableString("location").Present.Should().BeFalse();
    }

    [Fact]
    public void Deberia_TratarElCuerpoAusenteComoSinCambios_Cuando_NoLlegaNada()
    {
        var body = PartialUpdateBody.From(null);

        body.Has("name").Should().BeFalse();
        body.ReadString("name").Present.Should().BeFalse();
    }

    [Fact]
    public void Deberia_LanzarErrorDePeticion_Cuando_ElTextoNoEsUtf8Valido()
    {
        var body = PartialUpdateBody.From(WithInvalidUtf8Text());

        // `P-027`: antes esto era una `InvalidOperationException` sin capturar ⇒ HTTP 500.
        var act = () => body.ReadString("name");

        act.Should().Throw<InvalidRequestBodyException>().WithMessage("*UTF-8*");
    }

    [Theory]
    [InlineData("""{"tree_count": 250}""", true, 250)]
    [InlineData("""{"tree_count": null}""", true, null)]
    [InlineData("""{"tree_count": "muchos"}""", false, null)]
    [InlineData("""{"tree_count": 12.5}""", false, null)]
    public void Deberia_ReportarSiElEnteroEsLegible_Cuando_SeLeeUnNumero(string json, bool valido, int? esperado)
    {
        var ok = PartialUpdateBody.From(Parse(json)).TryReadInt("tree_count", out var field);

        ok.Should().Be(valido);
        if (valido) field.Value.Should().Be(esperado);
    }

    [Theory]
    [InlineData("""{"is_active": true}""", true, true)]
    [InlineData("""{"is_active": "sí"}""", false, null)]
    [InlineData("""{"is_active": 1}""", false, null)]
    public void Deberia_ReportarSiElBooleanoEsLegible_Cuando_SeLeeUnaBandera(string json, bool valido, bool? esperado)
    {
        var ok = PartialUpdateBody.From(Parse(json)).TryReadBool("is_active", out var field);

        ok.Should().Be(valido);
        if (valido) field.Value.Should().Be(esperado!.Value);
    }

    [Fact]
    public void Deberia_AdmitirDecimalNulo_Cuando_SeLimpiaElPrecioPorHora()
    {
        var ok = PartialUpdateBody.From(Parse("""{"hourly_rate": null}"""))
            .TryReadDecimal("hourly_rate", out var field);

        ok.Should().BeTrue();
        field.Present.Should().BeTrue();
        field.Value.Should().BeNull();
    }

    [Fact]
    public void Deberia_LanzarErrorDePeticion_Cuando_SeLeeTextoSueltoNoUtf8()
    {
        // `JsonText` es la primitiva que usan también los lectores de fecha e identificador de los
        // controladores operativos: se cubre por su cuenta.
        var element = WithInvalidUtf8Text()["name"];

        var act = () => JsonText.Read(element, "name");

        act.Should().Throw<InvalidRequestBodyException>();
    }
}
