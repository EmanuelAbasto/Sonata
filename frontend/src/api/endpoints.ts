import { apiClient } from './client';
import type { AudioFile, UploadResponse, JobStatusResponse, PagedResult } from '../types';

export const audioApi = {
  upload: (files: File[], target: string) => {
    const formData = new FormData();
    files.forEach((file) => {
      formData.append('files', file);
    });
    formData.append('target', target);

    return apiClient.post<UploadResponse[]>('/api/Audio', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  },

  getAudioFiles: (params: {
    page?: number;
    pageSize?: number;
    searchTerm?: string;
    status?: string;
    fromDate?: string;
    toDate?: string;
    format?: string;
  }) => {
    return apiClient.get<PagedResult<AudioFile>>('/api/Audio', { params });
  },

  getAudioFile: (audioId: number) => {
    return apiClient.get<AudioFile>(`/api/Audio/${audioId}`);
  },

  getJobStatus: (jobId: string) => {
    return apiClient.get<JobStatusResponse>(`/api/Jobs/${jobId}/status`);
  },

  getJobResult: (jobId: string) => {
    return apiClient.get<{ text: string }>(`/api/Jobs/${jobId}/result`);
  },
};