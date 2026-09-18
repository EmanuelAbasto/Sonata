import { useCallback, useEffect, useState } from 'react';
import { getAudioList } from '@/lib/api';
import type { AudioFileDtoPagedResult, GetAudioListParams } from '@/lib/types';

export function useAudioList(params: GetAudioListParams) {
  const [data, setData] = useState<AudioFileDtoPagedResult | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { page, pageSize, searchTerm, status, fromDate, toDate, format } = params;

  const fetchList = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await getAudioList({
        page,
        pageSize,
        searchTerm,
        status,
        fromDate,
        toDate,
        format,
      });
      setData(result.response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo cargar la lista de audios.');
    } finally {
      setIsLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, searchTerm, status, fromDate, toDate, format]);

  useEffect(() => {
    fetchList();
  }, [fetchList]);

  return { data, isLoading, error, refetch: fetchList };
}
