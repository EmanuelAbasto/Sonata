import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { useAudioList } from '@/hooks/useAudioList';
import { getAudioDetail } from '@/lib/api';
import { jobHubManager } from '@/lib/jobHubManager';
import type { AudioFileDto } from '@/lib/types';

interface AudioLibraryContextValue {
  recentFiles: AudioFileDto[];
  isLoading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
}

const AudioLibraryContext = createContext<AudioLibraryContextValue | null>(null);

export function AudioLibraryProvider({ children }: { children: ReactNode }) {
  const { data, isLoading, error, refetch } = useAudioList({ page: 1, pageSize: 40 });
  const [overrides, setOverrides] = useState<Record<number, Partial<AudioFileDto>>>({});

  const baseFiles = data?.items ?? [];

  const recentFiles = useMemo(
    () => baseFiles.map((file) => (overrides[file.id] ? { ...file, ...overrides[file.id] } : file)),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [baseFiles, overrides],
  );

  const jobIdsKey = baseFiles.map((file) => file.jobId).filter(Boolean).join(',');
  const filesRef = useRef(baseFiles);
  filesRef.current = baseFiles;

  useEffect(() => {
    const jobIds = jobIdsKey ? jobIdsKey.split(',') : [];
    if (jobIds.length === 0) return;

    let cancelled = false;
    const unsubscribers: Array<() => void> = [];

    jobIds.forEach((jobId) => {
      jobHubManager
        .subscribeJob(jobId, async () => {
          const file = filesRef.current.find((f) => f.jobId === jobId);
          if (!file) return;
          try {
            const detail = await getAudioDetail(file.id);
            setOverrides((prev) => ({
              ...prev,
              [file.id]: {
                jobStatusDisplay: detail.response.jobStatusDisplay,
                durationSeconds: detail.response.durationSeconds,
                format: detail.response.format,
                fileSize: detail.response.fileSize,
              },
            }));
          } catch {
          }
        })
        .then((unsub) => {
          if (cancelled) unsub();
          else unsubscribers.push(unsub);
        });
    });

    return () => {
      cancelled = true;
      unsubscribers.forEach((unsub) => unsub());
    };
  }, [jobIdsKey]);

  useEffect(() => {
    setOverrides({});
  }, [data]);

  return (
    <AudioLibraryContext.Provider
      value={{
        recentFiles,
        isLoading,
        error,
        refresh: refetch,
      }}
    >
      {children}
    </AudioLibraryContext.Provider>
  );
}

export function useAudioLibrary() {
  const ctx = useContext(AudioLibraryContext);
  if (!ctx) {
    throw new Error('useAudioLibrary debe usarse dentro de <AudioLibraryProvider>');
  }
  return ctx;
}
