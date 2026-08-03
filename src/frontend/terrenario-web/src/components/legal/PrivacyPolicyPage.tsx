import React from 'react';
import { LegalPage, Placeholder } from './LegalPage';

/**
 * MVP-505 (HU-1, CA-1) — Política de Privacidad.
 *
 * Sustituye al enlace roto del login (`P-008`). El contenido refleja lo que el sistema **hace de
 * verdad** —los tratamientos, las bases jurídicas, los encargados y los plazos salen de
 * `docs/07-seguridad/privacidad-datos.md`— y no una plantilla genérica: una política que describe un
 * producto distinto del real no cumple nada.
 *
 * Los datos del responsable van como marcadores: solo el negocio puede aportarlos (decisión del PO).
 */
export const PrivacyPolicyPage: React.FC = () => (
  <LegalPage title="Política de Privacidad" updatedAt="3 de agosto de 2026">
    <p>
      Esta política explica qué datos personales trata Terrenario, con qué finalidad, durante cuánto
      tiempo y qué derechos tienes sobre ellos.
    </p>

    <h2>1. Responsable del tratamiento</h2>
    <ul>
      <li>Titular: <Placeholder>RAZÓN SOCIAL</Placeholder></li>
      <li>NIF/CIF: <Placeholder>NIF</Placeholder></li>
      <li>Domicilio: <Placeholder>DOMICILIO SOCIAL</Placeholder></li>
      <li>Contacto de privacidad: <Placeholder>EMAIL DE CONTACTO</Placeholder></li>
      <li>Delegado de Protección de Datos: <Placeholder>DPO O «NO DESIGNADO»</Placeholder></li>
    </ul>

    <h2>2. Qué datos tratamos</h2>
    <p>
      Terrenario es una herramienta de gestión agrícola. Los datos personales que trata son los
      mínimos para que funcione:
    </p>
    <table>
      <thead>
        <tr><th>Dato</th><th>De dónde viene</th><th>Para qué</th></tr>
      </thead>
      <tbody>
        <tr>
          <td>Nombre y dirección de correo</td>
          <td>De tu cuenta de Google al iniciar sesión</td>
          <td>Identificarte, mostrar quién hace cada cosa en tu explotación y enviarte invitaciones</td>
        </tr>
        <tr>
          <td>Identificador de tu cuenta de Google</td>
          <td>De Google</td>
          <td>Reconocerte al volver a entrar</td>
        </tr>
        <tr>
          <td>Nombres de las personas de tu cuadrilla</td>
          <td>Los introduces tú</td>
          <td>Poder asignar labores a cada persona</td>
        </tr>
        <tr>
          <td>Nombre del propietario de un terreno cedido</td>
          <td>Lo introduces tú (campo opcional)</td>
          <td>Saber de quién es cada parcela que trabajas</td>
        </tr>
        <tr>
          <td>Datos de tu explotación (terrenos, labores, cosechas, compras)</td>
          <td>Los introduces tú</td>
          <td>Prestarte el servicio</td>
        </tr>
      </tbody>
    </table>
    <p>
      <strong>No tratamos datos de categorías especiales</strong> (salud, ideología, biometría) ni
      datos de menores. No elaboramos perfiles ni tomamos decisiones automatizadas sobre ti.
    </p>
    <p>
      Medimos el <strong>embudo de acceso</strong> (si se vio la pantalla de login, si se pulsó el
      botón y si se abandonó) con un identificador aleatorio que no está vinculado a ti y que
      desaparece al cerrar la pestaña. Es medición propia y agregada: no hay analítica de terceros ni
      seguimiento entre sitios.
    </p>

    <h2>2 bis. Datos de otras personas que introduces tú</h2>
    <p>
      Si registras a tu cuadrilla, al propietario de un terreno cedido o mencionas a alguien en el
      texto de una labor, <strong>esos datos los aportas tú</strong>. En ese caso eres quien decide
      tratarlos y quien debe informar a esas personas y tener base legítima para hacerlo; nosotros los
      tratamos por tu cuenta.
    </p>
    <p>
      Esas personas no tienen cuenta, así que <strong>no pueden ejercer sus derechos desde la
      aplicación</strong>: deben dirigirse a ti o al contacto de privacidad. Te pedimos que introduzcas
      solo los datos que necesites.
    </p>

    <h2>3. Base jurídica</h2>
    <ul>
      <li>
        <strong>Ejecución del contrato</strong>: crear y mantener tu cuenta, y prestarte el servicio
        que has solicitado. Es la base de casi todo lo anterior.
      </li>
      <li>
        <strong>Interés legítimo</strong>: seguridad del servicio y prevención de abusos (registros
        técnicos de acceso).
      </li>
      <li>
        <strong>Consentimiento</strong>: hoy no lo necesitamos, porque todo lo que usamos está exento.
        Si incorporáramos cualquier tecnología no esencial, te lo pediríamos antes de activarla.
      </li>
    </ul>

    <h2>4. Quién más puede acceder a tus datos</h2>
    <p>
      Solo los proveedores estrictamente necesarios para prestar el servicio, cada uno con contrato
      de encargo del tratamiento:
    </p>
    <table>
      <thead>
        <tr><th>Proveedor</th><th>Qué trata</th><th>Para qué</th></tr>
      </thead>
      <tbody>
        <tr>
          <td>Google (inicio de sesión)</td>
          <td>Identificador de cuenta, nombre y correo</td>
          <td>Autenticar tu acceso</td>
        </tr>
        <tr>
          <td><Placeholder>PROVEEDOR DE CORREO</Placeholder></td>
          <td>Dirección de la persona invitada y nombre de quien invita</td>
          <td>Enviar invitaciones a un Workspace</td>
        </tr>
        <tr>
          <td><Placeholder>PROVEEDOR DE ALOJAMIENTO</Placeholder></td>
          <td>Todo lo almacenado</td>
          <td>Alojar la aplicación y la base de datos</td>
        </tr>
      </tbody>
    </table>
    <p>
      Las personas con las que compartes un Workspace ven los registros de esa explotación y el
      nombre de quien los creó. <strong>No vendemos ni cedemos datos a terceros</strong> para ninguna
      finalidad ajena al servicio.
    </p>
    <p>
      Transferencias internacionales: <Placeholder>INDICAR SI LAS HAY Y CON QUÉ GARANTÍAS</Placeholder>.
    </p>

    <h2>5. Cuánto tiempo los conservamos</h2>
    <ul>
      <li>Mientras tu cuenta esté activa, para poder prestarte el servicio.</li>
      <li>
        Si eliminas tu cuenta, <strong>tus datos personales se borran o anonimizan en el acto</strong>:
        tu nombre, tu correo y tu identificador de Google desaparecen de la cuenta, de los Workspaces
        en los que participabas y de las invitaciones que te nombraban.
      </li>
      <li>
        Lo que queda es un registro <strong>anonimizado</strong>, que ya no te identifica y solo
        sirve para que el histórico de trabajo de las explotaciones en las que colaborabas no pierda
        su trazabilidad. Ese registro se elimina definitivamente a los <strong>24 meses</strong>.
      </li>
      <li>Registros técnicos de acceso: 12 meses.</li>
    </ul>

    <h2>6. Cookies y almacenamiento en tu navegador</h2>
    <p>
      Terrenario usa <strong>solo</strong> tecnologías exentas de consentimiento: las estrictamente
      necesarias para mantener tu sesión y que la aplicación funcione, más la medición del embudo de
      acceso descrita arriba, que es propia, agregada y sin datos que te identifiquen. No hay
      analítica de terceros, publicidad ni perfilado, y las tipografías se sirven desde nuestro propio
      servidor para no comunicar tu dirección IP a nadie. Por eso no verás un banner de cookies: no
      hay nada que consentir.
    </p>
    <p>
      Puedes consultar el inventario completo desde <strong>Ajustes → Privacidad</strong> dentro de la
      aplicación.
    </p>

    <h2>7. Tus derechos</h2>
    <p>
      Puedes ejercer los derechos de acceso, rectificación, supresión, oposición, limitación y
      portabilidad escribiendo a <Placeholder>EMAIL DE CONTACTO</Placeholder>. Responderemos en el
      plazo de un mes.
    </p>
    <p>
      El <strong>derecho de supresión lo puedes ejercer tú directamente</strong>, sin escribir a
      nadie: entra en <strong>Ajustes → Eliminar mi cuenta</strong>.
    </p>
    <p>
      Si consideras que no hemos atendido bien tu solicitud, puedes reclamar ante la Agencia Española
      de Protección de Datos (<span className="whitespace-nowrap">www.aepd.es</span>).
    </p>

    <h2>8. Cambios en esta política</h2>
    <p>
      Si cambiamos algo relevante, lo indicaremos en la fecha de actualización de esta página y te lo
      comunicaremos por los medios de contacto que tengamos.
    </p>
  </LegalPage>
);
