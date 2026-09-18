import axios, { type AxiosProgressEvent } from 'axios';
import type {
  ApiResponse,
  AudioDetailResponse,
  AudioFileDtoPagedResult,
  GetAudioListParams,
  JobStatusResponse,
  UploadResponse,
} from './types';

// Configura la URL base de tu API en un archivo .env (ver .env.example)
const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5248';

export const apiClient = axios.create({ baseURL });

/**
 * Sube uno o más archivos de audio ya validados en el cliente.
 * El backend espera multipart/form-data con el campo repetido "files".
 */
export async function uploadAudioFiles(
  files: File[],
  onProgress?: (percent: number) => void,
): Promise<ApiResponse<UploadResponse[]>> {
  const formData = new FormData();
  files.forEach((file) => formData.append('files', file, file.name));

  const { data } = await apiClient.post<ApiResponse<UploadResponse[]>>(
    '/api/audio',
    formData,
    {
      headers: { 'Content-Type': 'multipart/form-data' },
      onUploadProgress: (event: AxiosProgressEvent) => {
        if (!onProgress || !event.total) return;
        onProgress(Math.round((event.loaded / event.total) * 100));
      },
    },
  );

  return data;
}

/** Lista paginada de audios ya subidos al servidor. */
export async function getAudioList(
  params: GetAudioListParams = {},
): Promise<ApiResponse<AudioFileDtoPagedResult>> {
  const { data } = await apiClient.get<ApiResponse<AudioFileDtoPagedResult>>(
    '/api/audio',
    { params },
  );
  return data;
}

/** Detalle de un audio puntual, incluye lightFile y filteredFile. */
export async function getAudioDetail(
  audioId: number,
): Promise<ApiResponse<AudioDetailResponse>> {
  const { data } = await apiClient.get<ApiResponse<AudioDetailResponse>>(
    `/api/audio/${audioId}`,
  );
  return data;
}

/** Estado actual del job (incluye lightFile/filteredFile con su audioUrl). */
export async function getJobStatus(jobId: string): Promise<ApiResponse<JobStatusResponse>> {
  const { data } = await apiClient.get<ApiResponse<JobStatusResponse>>(
    `/api/jobs/${jobId}/status`,
  );
  return data;
}

/**
 * Texto de la transcripción del job. Devuelve 404 mientras el job no llegó a
 * esa etapa (Pending/Processing/Compressing) — es esperable, no un error real.
 */
export async function getJobTranscription(jobId: string): Promise<ApiResponse<unknown>> {
  const { data } = await apiClient.get<ApiResponse<unknown>>(`/api/jobs/${jobId}/transcription`, {
    validateStatus: (status) => status === 200 || status === 404,
  });
  return data;
}

/** Texto del resumen del job. Mismo criterio que arriba con el 404 esperado. */
export async function getJobSummary(jobId: string): Promise<ApiResponse<unknown>> {
  const { data } = await apiClient.get<ApiResponse<unknown>>(`/api/jobs/${jobId}/summary`, {
    validateStatus: (status) => status === 200 || status === 404,
  });
  return data;
}
