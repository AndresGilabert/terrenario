using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Tests.Http;

/// <summary>
/// MVP-502 (<c>P-043</c>) — La traducción del <c>ModelState</c> al contrato de error de la API.
///
/// Los tests de integración comprueban el resultado por la API real; estos cubren las ramas que
/// desde fuera cuestan de provocar, sobre todo la que impide que un mensaje del framework —siempre
/// en inglés— llegue al usuario.
/// </summary>
public sealed class ModelStateErrorTranslatorTests
{
    private static ModelStateDictionary WithError(string key, string message)
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(key, message);
        return modelState;
    }

    [Fact]
    public void Deberia_UsarElCodigoDeLaAnotacion_Cuando_ElMensajeLoLleva()
    {
        var modelState = WithError(
            "Name",
            ApiValidationMessage.Encode(ErrorCodes.ValidationPlotNameLength, "El nombre del terreno es demasiado largo."));

        var error = ModelStateErrorTranslator.Translate(modelState);

        error.Code.Should().Be(ErrorCodes.ValidationPlotNameLength);
        // El separador es interno: no puede asomar en el mensaje que ve el usuario.
        error.Message.Should().Be("El nombre del terreno es demasiado largo.");
    }

    [Theory]
    [InlineData("The Date field is required.")]
    [InlineData("The value 'x' is not valid for StartDate.")]
    [InlineData("The JSON value could not be converted to System.DateOnly.")]
    public void Deberia_SustituirElMensaje_Cuando_LoGeneroElFramework(string frameworkMessage)
    {
        var error = ModelStateErrorTranslator.Translate(WithError("$.start_date", frameworkMessage));

        error.Code.Should().Be(ErrorCodes.ValidationFormatInvalid);
        // El texto de ASP.NET viene en inglés y la UI lo mostraba tal cual (`P-043`).
        error.Message.Should().NotContain("field is required");
        error.Message.Should().NotContain("JSON value");
        error.Message.Should().Contain("start_date");
    }

    [Fact]
    public void Deberia_LimpiarElPrefijoDelBinder_Cuando_NombraElCampoDelCuerpo()
    {
        var error = ModelStateErrorTranslator.Translate(WithError("$.purchase_date", "The field is required."));

        // La clave llega como `$.purchase_date`; el mensaje debe hablar el idioma del contrato.
        error.Message.Should().Contain("'purchase_date'");
        error.Message.Should().NotContain("$.");
    }

    [Fact]
    public void Deberia_ConservarElComportamientoAnterior_Cuando_ElMensajeEsPropioYSinCodigo()
    {
        var error = ModelStateErrorTranslator.Translate(WithError("Name", "Un mensaje nuestro de toda la vida."));

        error.Code.Should().Be(ErrorCodes.ValidationRequired);
        error.Message.Should().Be("Un mensaje nuestro de toda la vida.");
    }

    [Fact]
    public void Deberia_DarUnMensajeGenerico_Cuando_NoHayNingunMensaje()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("", string.Empty);

        var error = ModelStateErrorTranslator.Translate(modelState);

        error.Code.Should().Be(ErrorCodes.ValidationRequired);
        error.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Deberia_PreferirLaAnotacionPropia_Cuando_ConcurreConUnErrorDelBinder()
    {
        // Con dos fallos a la vez gana el que sabe decir **qué** arreglar.
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(
            "Name",
            ApiValidationMessage.Encode(ErrorCodes.ValidationRequiredName, "El nombre es obligatorio."));
        modelState.AddModelError("$.tree_count", "The JSON value could not be converted.");

        var error = ModelStateErrorTranslator.Translate(modelState);

        error.Code.Should().Be(ErrorCodes.ValidationRequiredName);
    }

    [Fact]
    public void Deberia_DevolverFalse_Cuando_SeDescodificaUnMensajeSinCodigo()
    {
        ApiValidationMessage.TryDecode("solo texto", out _, out _).Should().BeFalse();
        ApiValidationMessage.TryDecode(null, out _, out _).Should().BeFalse();
    }
}
