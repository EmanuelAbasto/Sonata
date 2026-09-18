import { useState } from 'react';
import { audioApi } from '../api/endpoints';
import type { UploadResponse } from '../types';

export const useAudioUpload = () => {
  const [loading, setLoading] = useState(false);
  const [results, setResults] = useState<UploadResponse[]>([]);
  const [error, setError] = useState<string | null>(null);

  const uploadFiles = async (files: File[], target: string) => {
    setLoading(true);
    setError(null);
    try {
      const response = await audioApi.upload(files, target);
      setResults(response.data);
      return response.data;
    } catch (err: any) {
      const message = err.response?.data?.message || err.message || 'Error al subir los archivos';
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  return { uploadFiles, loading, results, error };
};