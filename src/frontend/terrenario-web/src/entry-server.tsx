import { renderToStaticMarkup } from 'react-dom/server';
import { ContentLandingPage } from './components/marketing/ContentLandingPage';
import type { LandingContent } from './content/landings';

/**
 * MKT-102 — Punto de entrada para el pre-renderizado de las landings públicas
 * (`scripts/prerenderizar-landings.mjs`).
 *
 * `renderToStaticMarkup` y no `renderToString`: estas páginas no se hidratan (no llevan React en
 * el cliente, ver `ContentLandingPage`), así que los atributos `data-reactroot` de
 * `renderToString` no tendrían ningún consumidor y solo pesarían HTML de más.
 */
export function renderLandingBody(content: LandingContent): string {
  return renderToStaticMarkup(<ContentLandingPage content={content} />);
}
