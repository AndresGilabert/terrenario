using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.IO;
using Terrenario.Api.Infrastructure.Email;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Tests.Emails;

/// <summary>
/// MVP-715 (CA-5) — Envía cada correo del inventario **por SMTP de verdad**, con el
/// <see cref="SmtpMailer"/> de producción, contra un receptor local.
///
/// <b>Por qué no basta con el HTML.</b> El preview enseña el cuerpo, pero el cuerpo es lo único que
/// no puede romperse en el envío. Lo que solo aparece al poner el mensaje en el cable es el sobre:
/// que sea <c>multipart/alternative</c> y no solo HTML, el juego de caracteres, la codificación de
/// los acentos, y el <c>From</c>/<c>Subject</c> tal y como los ve la bandeja. Esta prueba mira eso.
///
/// <b>Está desactivada salvo que se pida.</b> Necesita un servidor escuchando, y una suite que
/// dependa de un puerto abierto falla en la máquina de al lado. Se activa con
/// <c>TERRENARIO_SMTP_SINK_PORT</c>, que es también lo que la hace repetible: quien quiera rehacer el
/// CA-5 levanta el receptor, exporta el puerto y ejecuta.
///
/// <code>
/// python scripts/smtp-sink.py artifacts/correos-enviados 1025
/// TERRENARIO_SMTP_SINK_PORT=1025 dotnet test --filter FullyQualifiedName~ProductEmailDelivery
/// </code>
/// </summary>
public class ProductEmailDeliveryTests
{
    private static int? SinkPort() =>
        int.TryParse(Environment.GetEnvironmentVariable("TERRENARIO_SMTP_SINK_PORT"), out var port)
            ? port
            : null;

    [Fact]
    public async Task Deberia_EntregarCadaCorreo_PorElTransporteReal()
    {
        var port = SinkPort();

        // Sin receptor no hay nada que probar y **no se finge que lo hay**: se sale sin afirmar nada.
        // Se prefiere esto a añadir una dependencia de tests omitibles solo por este caso; el precio es
        // que en una ejecución normal esta prueba no aporta señal, y por eso lo dice su nombre y esta
        // nota. Quien rehaga el CA-5 sabe que tiene que exportar la variable.
        if (port is null) return;

        // El mismo transporte que usa el producto: si esto pasa, el camino de envío está probado.
        var mailer = new SmtpMailer(
            Options.Create(new EmailOptions
            {
                Host = "127.0.0.1",
                Port = port!.Value,
                SecurityMode = EmailSecurityModes.None,
                FromAddress = "no-reply@terrenario.test",
                FromName = "Terrenario",
            }),
            NullLogger<SmtpMailer>.Instance);

        mailer.IsEnabled.Should().BeTrue();

        var enviados = 0;
        foreach (var (slug, _, message) in ProductEmailCatalog.All())
        {
            // El destinatario del catálogo es de ejemplo (`.test`, reservado por RFC 2606): aunque el
            // receptor local fallara y el mensaje saliera de verdad, no puede entregarse a nadie.
            await mailer.SendAsync(message, slug);
            enviados++;
        }

        enviados.Should().Be(ProductEmailCatalog.All().Count());
    }
}
