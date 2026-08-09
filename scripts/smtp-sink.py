"""Receptor SMTP local para MVP-715 (CA-5).

No es un doble del emisor: habla SMTP de verdad por el socket, así que lo que captura es
**exactamente lo que MailKit pone en el cable** —cabeceras, `multipart/alternative`, juego de
caracteres y codificación de los acentos—, que es justo lo que el HTML renderizado no cubre.

Cada mensaje se guarda como `.eml`, que es el formato que abren Outlook, Thunderbird y Apple Mail
como si acabara de llegar.
"""

import asyncio
import email
import os
import re
import sys

DESTINO = sys.argv[1] if len(sys.argv) > 1 else "."
PUERTO = int(sys.argv[2]) if len(sys.argv) > 2 else 1025
os.makedirs(DESTINO, exist_ok=True)

recibidos = []


def nombre_fichero(datos: bytes) -> str:
    msg = email.message_from_bytes(datos)
    asunto = str(email.header.make_header(email.header.decode_header(msg.get("Subject", "sin-asunto"))))
    limpio = re.sub(r"[^a-zA-Z0-9]+", "-", asunto).strip("-").lower()[:60]
    return f"{len(recibidos):02d}-{limpio or 'sin-asunto'}.eml"


async def sesion(reader, writer):
    async def responde(linea: str):
        writer.write((linea + "\r\n").encode())
        await writer.drain()

    await responde("220 sink.local ESMTP listo")
    while True:
        linea = await reader.readline()
        if not linea:
            break
        orden = linea.decode("utf-8", "replace").strip()
        arriba = orden.upper()

        if arriba.startswith("EHLO") or arriba.startswith("HELO"):
            await responde("250-sink.local")
            await responde("250 SMTPUTF8")
        elif arriba.startswith(("MAIL", "RCPT")):
            await responde("250 OK")
        elif arriba.startswith("DATA"):
            await responde("354 Adelante")
            cuerpo = bytearray()
            while True:
                trozo = await reader.readline()
                if not trozo or trozo in (b".\r\n", b".\n"):
                    break
                # Des-escapado del punto inicial que exige el protocolo.
                cuerpo += trozo[1:] if trozo.startswith(b"..") else trozo
            datos = bytes(cuerpo)
            ruta = os.path.join(DESTINO, nombre_fichero(datos))
            with open(ruta, "wb") as f:
                f.write(datos)
            recibidos.append(ruta)
            print(f"RECIBIDO {ruta} ({len(datos)} bytes)", flush=True)
            await responde("250 Aceptado")
        elif arriba.startswith("QUIT"):
            await responde("221 Adiós")
            break
        elif arriba.startswith("RSET"):
            await responde("250 OK")
        else:
            await responde("250 OK")

    writer.close()


async def main():
    servidor = await asyncio.start_server(sesion, "127.0.0.1", PUERTO)
    print(f"ESCUCHANDO en 127.0.0.1:{PUERTO} -> {DESTINO}", flush=True)
    async with servidor:
        await servidor.serve_forever()


asyncio.run(main())
