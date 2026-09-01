import { describe, expect, it } from 'vitest';
import { HOME_META, LANDING_CONTENTS, getLandingBySlug, getRelatedLandings } from './landings';

/**
 * MKT-102 (CA-1, CA-2) — El plan P0 fija 10 landings y exige que cada una enlace a landings
 * relacionadas. Estas comprobaciones viven en datos y no en cada componente porque es donde se
 * puede romper en silencio: un slug relacionado mal escrito no falla al renderizar, falla al
 * navegar.
 */
describe('contenido de landings públicas', () => {
  it('publica exactamente las 10 URLs del plan P0', () => {
    const paths = LANDING_CONTENTS.map((content) => content.path).sort();

    expect(paths).toEqual(
      [
        '/funcionalidades/gestion-terrenos',
        '/funcionalidades/diario-de-campo',
        '/funcionalidades/control-cosechas',
        '/funcionalidades/compras-y-consumos',
        '/funcionalidades/dashboard-campana',
        '/funcionalidades/workspaces-colaboracion',
        '/funcionalidades/trabajadores-y-tareas',
        '/para/agricultor-particular',
        '/para/explotacion-familiar',
        '/para/gestion-multiterreno',
      ].sort()
    );
  });

  it('no repite slugs ni rutas', () => {
    const slugs = LANDING_CONTENTS.map((content) => content.slug);
    const paths = LANDING_CONTENTS.map((content) => content.path);

    expect(new Set(slugs).size).toBe(slugs.length);
    expect(new Set(paths).size).toBe(paths.length);
  });

  it('cada landing relacionada existe y no se enlaza a sí misma', () => {
    for (const content of LANDING_CONTENTS) {
      expect(content.relatedSlugs).not.toContain(content.slug);

      for (const relatedSlug of content.relatedSlugs) {
        expect(getLandingBySlug(relatedSlug), `${content.slug} enlaza a "${relatedSlug}"`).toBeDefined();
      }
    }
  });

  it('toda landing enlaza al menos a otra relacionada (CA-2)', () => {
    for (const content of LANDING_CONTENTS) {
      expect(content.relatedSlugs.length).toBeGreaterThan(0);
    }
  });

  it('getRelatedLandings resuelve los slugs a su contenido completo', () => {
    const gestionTerrenos = getLandingBySlug('gestion-terrenos')!;
    const relacionadas = getRelatedLandings(gestionTerrenos);

    expect(relacionadas.map((c) => c.slug)).toEqual(gestionTerrenos.relatedSlugs);
  });

  it('título y descripción de meta no están vacíos ni se repiten entre landings', () => {
    const publicPages = [HOME_META, ...LANDING_CONTENTS];
    const titles = publicPages.map((content) => content.title);
    const descriptions = publicPages.map((content) => content.metaDescription);

    for (const content of publicPages) {
      expect(content.title.length).toBeGreaterThan(0);
      expect(content.metaDescription.length).toBeGreaterThan(0);
    }
    expect(new Set(titles).size).toBe(titles.length);
    expect(new Set(descriptions).size).toBe(descriptions.length);
  });

  it('cada landing publica FAQ visibles no vacías', () => {
    for (const content of LANDING_CONTENTS) {
      expect(content.faqs.length).toBeGreaterThan(0);
      for (const faq of content.faqs) {
        expect(faq.question.trim()).not.toBe('');
        expect(faq.answer.trim()).not.toBe('');
      }
    }
  });
});
