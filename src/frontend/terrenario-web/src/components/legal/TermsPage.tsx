import React from 'react';
import { LegalPage } from './LegalPage';
import { legalEntity } from '../../config/legal-entity';

/**
 * MVP-505 (HU-1, CA-1) — Términos del Servicio. El otro enlace roto del login (`P-008`).
 *
 * Describe el MVP tal y como es, incluidos sus límites: sin ellos, los términos prometerían un
 * producto que no existe.
 *
 * MVP-504 (B-1) — Identidad del prestador desde `legal-entity`, la misma fuente que la política.
 */
export const TermsPage: React.FC = () => (
  <LegalPage title="Términos del Servicio" updatedAt="4 de agosto de 2026">
    <p>
      Estos términos regulan el uso de Terrenario. Al acceder al servicio aceptas lo que sigue.
    </p>

    <h2>1. Quién presta el servicio</h2>
    <ul>
      <li>Titular: {legalEntity.legalName}</li>
      <li>NIF: {legalEntity.taxId}</li>
      <li>Domicilio: {legalEntity.address}</li>
      <li>
        Contacto:{' '}
        <a
          href={`mailto:${legalEntity.privacyEmail}`}
          className="text-[#33450d] font-semibold hover:underline"
        >
          {legalEntity.privacyEmail}
        </a>
      </li>
    </ul>

    <h2>2. Qué es Terrenario</h2>
    <p>
      Una herramienta para registrar y consultar la actividad de una explotación agrícola: terrenos,
      campañas, labores, cosechas, compras y consumos. Se organiza en <strong>Workspaces</strong>, que
      representan una explotación y pueden compartirse con otras personas por invitación.
    </p>

    <h2>3. Acceso y cuenta</h2>
    <ul>
      <li>Se accede con una cuenta de Google. No creamos ni gestionamos contraseñas.</li>
      <li>Debes ser mayor de edad y tener capacidad para contratar.</li>
      <li>Eres responsable de la actividad realizada desde tu cuenta.</li>
      <li>
        Puedes <strong>eliminar tu cuenta en cualquier momento</strong> desde Ajustes. Es irreversible.
      </li>
    </ul>

    <h2>4. Uso aceptable</h2>
    <p>Al usar Terrenario te comprometes a no:</p>
    <ul>
      <li>Introducir datos personales de terceros sin base legítima para ello.</li>
      <li>Intentar acceder a Workspaces o datos que no te correspondan.</li>
      <li>Interferir en el funcionamiento del servicio o en su seguridad.</li>
      <li>Usarlo para fines ilícitos.</li>
    </ul>
    <p>
      Si registras a personas de tu cuadrilla, <strong>tú eres responsable</strong> de informarles de
      que sus datos figuran en la herramienta y de tener base legítima para tratarlos.
    </p>

    <h2>5. Tus datos y tus contenidos</h2>
    <p>
      Los datos de tu explotación son tuyos. No los usamos para ninguna finalidad ajena a prestarte el
      servicio. El tratamiento de datos personales se rige por la{' '}
      <a href="/legal/privacidad" className="text-[#33450d] font-semibold hover:underline">
        Política de Privacidad
      </a>.
    </p>
    <p>
      Al invitar a alguien a tu Workspace le das acceso a los registros de esa explotación y al nombre
      de quien los creó.
    </p>

    <h2>6. Disponibilidad y límites</h2>
    <ul>
      <li>
        El servicio se presta <strong>tal cual</strong>, sin garantía de disponibilidad ininterrumpida.
      </li>
      <li>
        Terrenario es una herramienta de registro: <strong>no sustituye asesoramiento agronómico,
        fiscal ni contable</strong>, y las cifras que muestra dependen de lo que introduzcas.
      </li>
      <li>
        Podemos modificar o interrumpir funcionalidades. Si el cambio es relevante, lo avisaremos con
        antelación razonable.
      </li>
    </ul>

    <h2>7. Responsabilidad</h2>
    <p>
      En la medida que permita la ley, no respondemos de daños indirectos ni de pérdidas derivadas de
      decisiones tomadas a partir de los datos que registres. Nada en estos términos excluye la
      responsabilidad que no pueda excluirse legalmente, incluidos los derechos que te correspondan
      como consumidor.
    </p>

    <h2>8. Baja y terminación</h2>
    <p>
      Puedes dejar de usar el servicio cuando quieras. Podemos suspender una cuenta que incumpla estos
      términos, informando del motivo salvo que la ley lo impida.
    </p>

    <h2>9. Ley aplicable</h2>
    <p>
      Se aplica la <strong>legislación española</strong>. Para cualquier controversia serán
      competentes los juzgados y tribunales que determine la normativa aplicable; si actúas como
      consumidor, los de tu domicilio. No te imponemos ningún fuero distinto del que la ley te
      reconoce.
    </p>

    <h2>10. Cambios en estos términos</h2>
    <p>
      Si los modificamos, actualizaremos la fecha de esta página y te lo comunicaremos por los medios
      de contacto que tengamos.
    </p>
  </LegalPage>
);
