import { describe, it, expect } from 'vitest';
import {
  legalEntity,
  missingLegalFields,
  resolveLegalEntity,
  type LegalEntity
} from './legal-entity';

/**
 * MVP-504 (B-1) — La identidad legal es contenido de un documento con efectos jurídicos, así que lo
 * que se prueba no es «el módulo devuelve algo» sino que **nunca puede publicarse un hueco**.
 */
describe('identidad del responsable del tratamiento', () => {
  it('no deja ningún campo sin valor', () => {
    // Este es el test que importa: si alguien añade un campo a `LegalEntity` y olvida darle valor,
    // falla el build en vez de publicarse una Política de Privacidad con un dato en blanco.
    expect(missingLegalFields(legalEntity)).toEqual([]);
  });

  it('publica los datos que exigen la LSSI y el RGPD', () => {
    expect(legalEntity.legalName).toBe('Andrés Gilabert Sánchez');
    expect(legalEntity.taxId).toBe('21.679.361-K');
    expect(legalEntity.address).toContain('Muro de Alcoi');
    expect(legalEntity.privacyEmail).toBe('hola@andresgilabert.dev');
    // No designarlo es una respuesta válida (art. 37); dejarlo en blanco no lo es.
    expect(legalEntity.dpo).toBe('No designado');
  });

  it('declara los encargados y dónde se alojan los datos', () => {
    expect(legalEntity.emailProvider).toBe('Arsys');
    expect(legalEntity.hostingProvider).toBe('Microsoft Azure');
    expect(legalEntity.hostingRegion).toBe('España');
  });

  it('permite sobreescribir un campo por variable de entorno', () => {
    const resolved = resolveLegalEntity({ VITE_LEGAL_PRIVACY_EMAIL: 'privacidad@ejemplo.test' });

    expect(resolved.privacyEmail).toBe('privacidad@ejemplo.test');
    // Sobreescribir uno no debe arrastrar a los demás.
    expect(resolved.legalName).toBe(legalEntity.legalName);
  });

  it('ignora una variable vacía o con solo espacios en vez de dejar el hueco', () => {
    // Un despliegue mal configurado no puede vaciar un dato obligatorio: cae al valor versionado.
    const resolved = resolveLegalEntity({ VITE_LEGAL_TAX_ID: '   ', VITE_LEGAL_NAME: '' });

    expect(resolved.taxId).toBe(legalEntity.taxId);
    expect(resolved.legalName).toBe(legalEntity.legalName);
    expect(missingLegalFields(resolved)).toEqual([]);
  });

  it('detecta los campos vacíos', () => {
    const incompleta = { ...legalEntity, address: '', dpo: '  ' } as LegalEntity;

    expect(missingLegalFields(incompleta)).toEqual(['address', 'dpo']);
  });
});
