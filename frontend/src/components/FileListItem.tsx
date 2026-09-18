import { AlertTriangle, CheckCircle2, Loader2, Music2, X } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { formatBytes } from '@/lib/utils';
import type { UploadItem } from '@/hooks/useAudioUpload';

const STATUS_CONFIG: Record<
  UploadItem['status'],
  { label: string; variant: 'neutral' | 'pending' | 'good' | 'bad' }
> = {
  validating: { label: 'Validando…', variant: 'pending' },
  valid: { label: 'Listo para subir', variant: 'good' },
  invalid: { label: 'No válido', variant: 'bad' },
  uploading: { label: 'Subiendo…', variant: 'pending' },
  uploaded: { label: 'Subido', variant: 'good' },
  error: { label: 'Error', variant: 'bad' },
};

export function FileListItem({
  item,
  onRemove,
}: {
  item: UploadItem;
  onRemove: (id: string) => void;
}) {
  const status = STATUS_CONFIG[item.status];
  const isBusy = item.status === 'validating' || item.status === 'uploading';
  const canRemove = item.status !== 'uploading';

  return (
    <li className="flex animate-fade-in items-center gap-3 rounded-xl border border-border-soft bg-surface/60 px-4 py-3">
      <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-surface-raised text-ink-muted">
        {item.status === 'invalid' || item.status === 'error' ? (
          <AlertTriangle className="h-4 w-4 text-bad" />
        ) : item.status === 'uploaded' ? (
          <CheckCircle2 className="h-4 w-4 text-good" />
        ) : isBusy ? (
          <Loader2 className="h-4 w-4 animate-spin text-accent" />
        ) : (
          <Music2 className="h-4 w-4" />
        )}
      </div>

      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <p className="truncate text-sm text-ink">{item.file.name}</p>
          <Badge variant={status.variant} className="shrink-0">
            {status.label}
          </Badge>
        </div>

        {item.status === 'uploading' && typeof item.progress === 'number' ? (
          <Progress value={item.progress} className="mt-2" />
        ) : (
          <p className="mt-1 text-xs text-ink-faint">
            {formatBytes(item.file.size)}
            {item.reason ? <span className="text-bad"> · {item.reason}</span> : null}
          </p>
        )}
      </div>

      {canRemove && (
        <button
          type="button"
          onClick={() => onRemove(item.id)}
          className="shrink-0 rounded-md p-1.5 text-ink-faint transition-colors hover:bg-surface-hover hover:text-ink"
          aria-label={`Quitar ${item.file.name}`}
        >
          <X className="h-4 w-4" />
        </button>
      )}
    </li>
  );
}
