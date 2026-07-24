export const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000';

export interface ApiErrorBody {
  error?: { code?: string; message?: string };
}

export async function readErrorBody(response: Response): Promise<ApiErrorBody | null> {
  return response.json().catch(() => null);
}
