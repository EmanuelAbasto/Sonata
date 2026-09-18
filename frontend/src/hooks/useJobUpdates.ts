import { useEffect, useRef } from 'react';
import { jobHubManager } from '@/lib/jobHubManager';
import type { JobHubEvent, JobHubPayload } from '@/lib/signalr';

export function useJobUpdates(
  jobId: string | null | undefined,
  onEvent: (event: JobHubEvent, payload: JobHubPayload) => void,
) {
  const onEventRef = useRef(onEvent);
  onEventRef.current = onEvent;

  useEffect(() => {
    if (!jobId) return;
    let cancelled = false;
    let unsubscribe: (() => void) | undefined;

    jobHubManager
      .subscribeJob(jobId, (event, payload) => onEventRef.current(event, payload))
      .then((unsub) => {
        if (cancelled) unsub();
        else unsubscribe = unsub;
      });

    return () => {
      cancelled = true;
      unsubscribe?.();
    };
  }, [jobId]);
}

/** Escucha eventos de todos los jobs a los que la app ya esté unida (para notificaciones globales). */
export function useJobHubGlobalEvents(onEvent: (event: JobHubEvent, payload: JobHubPayload) => void) {
  const onEventRef = useRef(onEvent);
  onEventRef.current = onEvent;

  useEffect(() => {
    return jobHubManager.subscribeAll((event, payload) => onEventRef.current(event, payload));
  }, []);
}
