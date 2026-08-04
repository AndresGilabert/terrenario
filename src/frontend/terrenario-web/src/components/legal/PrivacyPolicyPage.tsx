import React from 'react';
import { LegalPage } from './LegalPage';
import { legalEntity } from '../../config/legal-entity';

/**
 * MVP-505 (HU-1, CA-1) — Política de Privacidad.
 *
 * Sustituye al enlace roto del login (`P-008`). El contenido refleja lo que el sistema **hace de
 * verdad** —los tratamientos, las bases jurídicas, los encargados y los plazos salen de
 * `docs/07-seguridad/privacidad-datos.md`— y no una plantilla genérica: una política que describe un
 * producto distinto del real no cumple nada.
 *
 * MVP-504 (B-1) — Los datos del responsable ya no son marcadores: salen de `legal-entity`.
 */
export const PrivacyPolicyPage: React.FC = () => (
  <LegalPage title="Política de Privacidad" updatedAt="4 de agosto de 2026">
    <p>
      Esta política explica qué datos personales trata Terrenario, con qué finalidad, durante cuánto
      tiempo y qué derechos tienes sobre ellos.
    </p>

    <h2>1. Responsable del tratamiento</h2>
    <ul>
      <li>Titular: {legalEntity.legalName}</li>
      <li>NIF: {legalEntity.taxId}</li>
      <li>Domicilio: {legalEntity.address}</li>
      <li>
        Contacto de privacidad:{' '}
        <a
          href={`mailto:${legalEntity.privacyEmail}`}
          className="text-[#33450d] font-semibold hover:underline"
        >
          {legalEntity.privacyEmail}
        </a>
      </li>
      <li>Delegado de Protección de Datos: {legalEntity.dpo}</li>
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
      Tus datos podrán ser tratados por proveedores tecnológicos que actúan como{' '}
      <strong>encargados del tratamiento</strong>: los tratan por nuestra cuenta y siguiendo nuestras
      instrucciones, solo para prestarte el servicio.
    </p>
    <table>
      <thead>
        <tr><th>Proveedor</th><th>Qué trata</th><th>Para qué</th></tr>
      </thead>
      <tbody>
        <tr>
          <td>{legalEntity.hostingProvider} (alojamiento)</td>
          <td>Todo lo almacenado</td>
          <td>Alojar la aplicación y la base de datos</td>
        </tr>
        <tr>
          <td>{legalEntity.emailProvider} (correo electrónico)</td>
          <td>Dirección de la persona invitada y nombre de quien invita</td>
          <td>Enviar invitaciones a un Workspace</td>
        </tr>
      </tbody>
    </table>
    <p>
      <strong>Google es distinto</strong>: puedes autenticarte mediante Google, que actúa como{' '}
      <strong>responsable independiente</strong> conforme a sus propias condiciones. Cuando inicias
      sesión, Google trata tus datos bajo su propia política de privacidad y no por cuenta nuestra;
      nosotros solo recibimos de él tu identificador de cuenta, tu nombre y tu correo.
    </p>
    <p>
      Las personas con las que compartes un Workspace ven los registros de esa explotación y el
      nombre de quien los creó. <strong>No vendemos ni cedemos datos a terceros</strong> para ninguna
      finalidad ajena al servicio.
    </p>
    <h2>4 bis. Dónde se guardan tus datos y transferencias internacionales</h2>
    <p>
      La aplicación y la base de datos están alojadas en {legalEntity.hostingProvider}, en su región
      de <strong>{legalEntity.hostingRegion}</strong>: tus datos se almacenan{' '}
      <strong>dentro de la Unión Europea</strong>. El correo de invitación se envía a través de{' '}
      {legalEntity.emailProvider}, proveedor español.
    </p>
    <p>
      Así que <strong>ninguno de nuestros encargados trata tus datos fuera de la Unión Europea</strong>.
    </p>
    <p>
      Lo que sí sale del Espacio Económico Europeo es el <strong>inicio de sesión con Google</strong>,
      que es el único modo de acceder al servicio. Como Google actúa ahí por su cuenta y no por la
      nuestra, esa comunicación se rige por sus propias condiciones y por las garantías que él aplica
      —cláusulas contractuales tipo de la Comisión Europea y decisión de adecuación del Marco de
      Privacidad de Datos UE–EE. UU.—. Si no quieres que ocurra, la vía es no crear la cuenta: sin
      identificarte no podemos prestarte el servicio.
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
      portabilidad escribiendo a{' '}
      <a
        href={`mailto:${legalEntity.privacyEmail}`}
        className="text-[#33450d] font-semibold hover:underline"
      >
        {legalEntity.privacyEmail}
      </a>. Responderemos en el plazo de un mes.
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
