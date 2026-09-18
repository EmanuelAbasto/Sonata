import { useJobHubGlobalEvents } from '@/hooks/useJobUpdates';
import { useToast } from '@/context/ToastContext';
import { useAudioLibrary } from '@/context/AudioLibraryContext';

export function GlobalJobNotifications() {
  const { push } = useToast();
  const { recentFiles } = useAudioLibrary();

  useJobHubGlobalEvents((event, payload) => {
    if (event !== 'JobCompleted' && event !== 'JobFailed') return;

    const file = recentFiles.find((f) => f.jobId === payload.jobId);
    const fileName = file?.fileName ?? 'Un audio';

    push({
      variant: event === 'JobCompleted' ? 'success' : 'error',
      title:
        event === 'JobCompleted'
          ? `${fileName} terminó de procesarse`
          : `${fileName} falló al procesarse`,
      description: payload.message,
    });
  });

  return null;
}
