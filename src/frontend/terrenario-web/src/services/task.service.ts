import type { HttpClient } from './http-client';
import type {
  CreateTaskPayload,
  TaskListResponse,
  UpdateTaskPayload,
  WorkTask,
} from '../types/task.types';

/**
 * Servicio del catálogo de tareas (MVP-205) sobre el cliente HTTP común. Como el resto de recursos
 * con ámbito de Workspace, el manejo de 401/403 de scope vive en el cliente (P-007); aquí solo queda
 * la forma del recurso.
 */
export function createTaskService(http: HttpClient) {
  return {
    /** Lista el catálogo del Workspace. Filtro opcional por estado de actividad. */
    async listTasks(params?: { isActive?: boolean }): Promise<WorkTask[]> {
      const body = await http.request<TaskListResponse>('/api/v1/tasks', {
        query: { is_active: params?.isActive },
      });
      return body.data;
    },

    /** Alta de tarea con el dato mínimo obligatorio (nombre). */
    async createTask(payload: CreateTaskPayload): Promise<WorkTask> {
      return http.request<WorkTask>('/api/v1/tasks', { method: 'POST', body: payload });
    },

    /** Renombra una tarea o cambia su estado de actividad (inactivación CA-3). */
    async updateTask(taskId: string, payload: UpdateTaskPayload): Promise<WorkTask> {
      return http.request<WorkTask>(`/api/v1/tasks/${taskId}`, { method: 'PATCH', body: payload });
    },
  };
}

export type TaskService = ReturnType<typeof createTaskService>;
