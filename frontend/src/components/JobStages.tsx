import { CheckCircle2, Circle, Loader2, XCircle } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { JobStatus, JobStepFlags } from '@/lib/types';

interface StageDef {
  key: string;
  label: string;
  flag: string;
  activeStatuses: JobStatus[];
}

const STAGES: StageDef[] = [
  { key: 'uploaded', label: 'Subida', flag: 'Uploaded', activeStatuses: ['Pending', 'Processing'] },
  { key: 'compressed', label: 'Compresión', flag: 'Compressed', activeStatuses: ['Compressing'] },
  { key: 'filtered', label: 'Filtrado', flag: 'Filtered', activeStatuses: [] },
  {
    key: 'transcribed',
    label: 'Transcripción',
    flag: 'Transcribed',
    activeStatuses: ['Transcribing'],
  },
  { key: 'summarized', label: 'Resumen', flag: 'Summarized', activeStatuses: ['Summarizing'] },
];

type StageState = 'done' | 'active' | 'failed' | 'pending';

/**
 * completedSteps llega como un flags-enum de .NET; puede venir como un solo
 * valor o como varios separados por coma (ej: "Uploaded, Compressed").
 */
function getStageStates(status: JobStatus, completedSteps: JobStepFlags | string): StageState[] {
  const doneTokens = String(completedSteps ?? '')
    .split(',')
    .map((token) => token.trim())
    .filter(Boolean);

  const states: StageState[] = STAGES.map((stage) =>
    doneTokens.includes(stage.flag) ? 'done' : 'pending',
  );

  if (status === 'Completed') {
    return states.map(() => 'done');
  }

  const firstPendingIndex = states.findIndex((state) => state === 'pending');
  if (firstPendingIndex === -1) return states;

  if (status === 'Failed') {
    states[firstPendingIndex] = 'failed';
    return states;
  }

  if (STAGES[firstPendingIndex].activeStatuses.includes(status)) {
    states[firstPendingIndex] = 'active';
  }

  return states;
}

export function JobStages({
  status,
  completedSteps,
}: {
  status: JobStatus;
  completedSteps: JobStepFlags | string;
}) {
  const states = getStageStates(status, completedSteps);

  return (
    <ul className="flex flex-col gap-2.5">
      {STAGES.map((stage, index) => {
        const state = states[index];
        return (
          <li key={stage.key} className="flex items-center gap-2.5 text-sm">
            {state === 'done' && <CheckCircle2 className="h-4 w-4 shrink-0 text-good" />}
            {state === 'active' && (
              <Loader2 className="h-4 w-4 shrink-0 animate-spin text-accent" />
            )}
            {state === 'failed' && <XCircle className="h-4 w-4 shrink-0 text-bad" />}
            {state === 'pending' && <Circle className="h-4 w-4 shrink-0 text-ink-faint" />}
            <span
              className={cn(
                state === 'pending' ? 'text-ink-faint' : 'text-ink',
                state === 'active' && 'text-accent',
              )}
            >
              {stage.label}
            </span>
          </li>
        );
      })}
    </ul>
  );
}
