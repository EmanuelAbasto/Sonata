import { useCallback, useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Loader2, Wifi } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Collapsible } from '@/components/ui/collapsible';
import { JobStages } from '@/components/JobStages';
import { getAudioDetail, getJobStatus, getJobSummary, getJobTranscription } from '@/lib/api';
import { extractTextResponse, formatBytes, formatDateTime, formatDuration } from '@/lib/utils';
import { useJobUpdates } from '@/hooks/useJobUpdates';
import type { JobHubEvent } from '@/lib/signalr';
import type { AudioDetailResponse, AudioFileDto, JobStatusResponse } from '@/lib/types';

const JOB_BADGE: Record<string, 'neutral' | 'pending' | 'good' | 'bad'> = {
  Completed: 'good',
  Failed: 'bad',
  Pending: 'neutral',
  Processing: 'pending',
  Compressing: 'pending',
  Transcribing: 'pending',
  Summarizing: 'pending',
};

export default function AudioDetailPage() {
  const { audioId } = useParams<{ audioId: string }>();

  const [audio, setAudio] = useState<AudioDetailResponse | null>(null);
  const [jobStatus, setJobStatus] = useState<JobStatusResponse | null>(null);
  const [transcription, setTranscription] = useState<string | null>(null);
  const [summary, setSummary] = useState<string | null>(null);
  const [loadingTranscription, setLoadingTranscription] = useState(false);
  const [loadingSummary, setLoadingSummary] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [selectedFile, setSelectedFile] = useState<{ file: AudioFileDto; label: string } | null>(null);

  const loadAudio = useCallback(async () => {
    if (!audioId) return;
    try {
      const res = await getAudioDetail(Number(audioId));
      setAudio(res.response);
    } catch {
      setError('No se pudo cargar el detalle de este audio.');
    }
  }, [audioId]);

  const loadJobStatus = useCallback(async (jobId: string) => {
    try {
      const res = await getJobStatus(jobId);
      setJobStatus(res.response);
    } catch {
    }
  }, []);

  const loadTranscription = useCallback(async (jobId: string) => {
    setLoadingTranscription(true);
    try {
      const res = await getJobTranscription(jobId);
      setTranscription(extractTextResponse(res.response));
    } catch {
      setTranscription(null);
    } finally {
      setLoadingTranscription(false);
    }
  }, []);

  const loadSummary = useCallback(async (jobId: string) => {
    setLoadingSummary(true);
    try {
      const res = await getJobSummary(jobId);
      setSummary(extractTextResponse(res.response));
    } catch {
      setSummary(null);
    } finally {
      setLoadingSummary(false);
    }
  }, []);

  useEffect(() => {
    setAudio(null);
    setJobStatus(null);
    setTranscription(null);
    setSummary(null);
    setError(null);
    loadAudio();
  }, [loadAudio]);

  useEffect(() => {
    const jobId = audio?.jobId;
    if (!jobId) return;
    loadJobStatus(jobId);
    loadTranscription(jobId);
    loadSummary(jobId);
  }, [audio?.jobId, loadJobStatus, loadTranscription, loadSummary]);

  const handleJobEvent = useCallback(
    (eventName: JobHubEvent) => {
      const jobId = audio?.jobId;
      if (!jobId) return;

      loadJobStatus(jobId);
      if (eventName === 'TranscriptionCompleted') loadTranscription(jobId);
      if (eventName === 'SummaryCompleted') loadSummary(jobId);
      if (eventName === 'JobCompleted' || eventName === 'JobFailed') loadAudio();
    },
    [audio?.jobId, loadJobStatus, loadTranscription, loadSummary, loadAudio],
  );

  useJobUpdates(audio?.jobId, handleJobEvent);

  const availableFiles = useMemo(() => {
    const files: { file: AudioFileDto; label: string }[] = [];

    const light = jobStatus?.lightFile ?? audio?.lightFile;
    const filtered = jobStatus?.filteredFile ?? audio?.filteredFile;
    const original = audio?.audioUrl ? { file: audio, label: 'Original' } : null;

    if (light?.audioUrl) files.push({ file: light, label: 'Ligero' });
    if (filtered?.audioUrl) files.push({ file: filtered, label: 'Filtrado' });
    if (original) files.push(original);

    return files;
  }, [audio, jobStatus]);

  useEffect(() => {
    if (availableFiles.length === 0) {
      setSelectedFile(null);
      return;
    }

    const stillAvailable = selectedFile && availableFiles.some(f => f.file.audioUrl === selectedFile.file.audioUrl);
    if (!stillAvailable) {
      setSelectedFile(availableFiles[0]);
    }
  }, [availableFiles, selectedFile]);

  if (error) {
    return <p className="rounded-lg bg-bad-soft px-4 py-3 text-sm text-bad">{error}</p>;
  }

  if (!audio) {
    return (
      <div className="flex items-center justify-center gap-2 py-16 text-sm text-ink-muted">
        <Loader2 className="h-4 w-4 animate-spin" />
        Cargando audio…
      </div>
    );
  }

  const statusDisplay = jobStatus?.statusDisplay ?? audio.jobStatusDisplay;

  return (
    <>
      <header className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="break-all font-display text-2xl text-ink">{audio.fileName}</h1>
          <div className="mt-2 flex flex-wrap gap-2 text-xs text-ink-muted">
            <Badge>Formato: {audio.format ?? 'N/A'}</Badge>
            <Badge>Tamaño: {formatBytes(audio.fileSize)}</Badge>
            <Badge>Duración: {formatDuration(audio.durationSeconds)}</Badge>
            <Badge>Creado: {formatDateTime(audio.createdAt)}</Badge>
            {statusDisplay && (
              <Badge variant={JOB_BADGE[statusDisplay] ?? 'neutral'}>{statusDisplay}</Badge>
            )}
          </div>
        </div>
        <span className="flex items-center gap-1.5 text-xs text-ink-faint">
          <Wifi className="h-3.5 w-3.5" />
          En vivo
        </span>
      </header>

      {jobStatus && (
        <Card>
          <CardContent className="pt-6">
            <p className="mb-3 text-sm text-ink-muted">Progreso del procesamiento</p>
            <JobStages status={jobStatus.status} completedSteps={jobStatus.completedSteps} />
          </CardContent>
        </Card>
      )}

      {/* Reproductor rediseñado */}
      <Card>
        <CardContent className="flex flex-col gap-3 pt-6">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <p className="text-sm text-ink-muted">Reproductor</p>
            {/* Grupo de botones tipo "pills" */}
            <div className="flex flex-wrap gap-1.5">
              {availableFiles.length > 0 ? (
                availableFiles.map(({ file, label }) => {
                  const isActive = selectedFile?.file.audioUrl === file.audioUrl;
                  return (
                    <button
                      key={file.audioUrl}
                      onClick={() => setSelectedFile({ file, label })}
                      className={`
                        rounded-full px-3 py-1 text-xs font-medium transition-all
                        ${isActive
                          ? 'bg-accent text-white shadow-sm'
                          : 'bg-surface-hover text-ink-muted hover:bg-surface-raised hover:text-ink'
                        }
                      `}
                    >
                      {label} ({formatBytes(file.fileSize ?? 0)})
                    </button>
                  );
                })
              ) : (
                <span className="text-xs text-ink-faint">Sin archivos disponibles</span>
              )}
            </div>
          </div>

          {selectedFile ? (
            <>
              <audio controls className="w-full" src={selectedFile.file.audioUrl ?? undefined} />
              <p className="truncate text-xs text-ink-faint">
                {selectedFile.file.fileName} ({formatBytes(selectedFile.file.fileSize ?? 0)})
              </p>
            </>
          ) : (
            <p className="rounded-lg border border-dashed border-border-soft px-3 py-4 text-center text-xs text-ink-faint">
              Todavía no hay un archivo de audio listo para reproducir.
            </p>
          )}
        </CardContent>
      </Card>

      <Collapsible
        title="Transcripción"
        badgeLabel={loadingTranscription ? 'Cargando…' : transcription ? 'Disponible' : 'Pendiente'}
        badgeVariant={transcription ? 'good' : 'neutral'}
      >
        {loadingTranscription ? (
          <div className="flex items-center gap-2 text-sm text-ink-muted">
            <Loader2 className="h-4 w-4 animate-spin" />
            Cargando transcripción…
          </div>
        ) : transcription ? (
          <p className="whitespace-pre-wrap text-sm leading-relaxed text-ink-muted">
            {transcription}
          </p>
        ) : (
          <p className="text-sm text-ink-faint">Todavía no está disponible.</p>
        )}
      </Collapsible>

      <Collapsible
        title="Resumen"
        badgeLabel={loadingSummary ? 'Cargando…' : summary ? 'Disponible' : 'Pendiente'}
        badgeVariant={summary ? 'good' : 'neutral'}
      >
        {loadingSummary ? (
          <div className="flex items-center gap-2 text-sm text-ink-muted">
            <Loader2 className="h-4 w-4 animate-spin" />
            Cargando resumen…
          </div>
        ) : summary ? (
          <p className="whitespace-pre-wrap text-sm leading-relaxed text-ink-muted">{summary}</p>
        ) : (
          <p className="text-sm text-ink-faint">Todavía no está disponible.</p>
        )}
      </Collapsible>
    </>
  );
}