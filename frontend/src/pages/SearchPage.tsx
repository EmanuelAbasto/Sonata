import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { ChevronLeft, ChevronRight, FileAudio, Loader2, Search, X } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { useAudioList } from '@/hooks/useAudioList';
import { formatBytes, formatDateTime, formatDuration } from '@/lib/utils';
import { JOB_STATUS_OPTIONS } from '@/lib/types';

const JOB_BADGE: Record<string, 'neutral' | 'pending' | 'good' | 'bad'> = {
  Completed: 'good',
  Failed: 'bad',
  Pending: 'neutral',
  Processing: 'pending',
  Compressing: 'pending',
  Transcribing: 'pending',
  Summarizing: 'pending',
};

const PAGE_SIZE = 10;

interface Filters {
  searchTerm: string;
  status: string;
  format: string;
  fromDate: string;
  toDate: string;
}

const EMPTY_FILTERS: Filters = {
  searchTerm: '',
  status: '',
  format: '',
  fromDate: '',
  toDate: '',
};

export default function SearchPage() {
  const [draft, setDraft] = useState<Filters>(EMPTY_FILTERS);
  const [applied, setApplied] = useState<Filters>(EMPTY_FILTERS);
  const [page, setPage] = useState(1);

  const { data, isLoading, error } = useAudioList({
    page,
    pageSize: PAGE_SIZE,
    searchTerm: applied.searchTerm || undefined,
    status: applied.status || undefined,
    format: applied.format || undefined,
    fromDate: applied.fromDate ? new Date(applied.fromDate).toISOString() : undefined,
    toDate: applied.toDate ? new Date(applied.toDate).toISOString() : undefined,
  });

  const handleSearch = (event: FormEvent) => {
    event.preventDefault();
    setPage(1);
    setApplied(draft);
  };

  const handleClear = () => {
    setDraft(EMPTY_FILTERS);
    setApplied(EMPTY_FILTERS);
    setPage(1);
  };

  const totalPages = data?.totalPages ?? 1;
  const items = data?.items ?? [];

  return (
    <>
      <header>
        <h1 className="font-display text-2xl text-ink">Búsqueda avanzada</h1>
        <p className="text-sm text-ink-muted">
          Filtrá el historial completo por estado, formato, fechas o nombre.
        </p>
      </header>

      <Card>
        <CardHeader>
          <CardTitle>Filtros</CardTitle>
          <CardDescription>Combiná los que necesites y presioná buscar.</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSearch} className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="flex flex-col gap-1.5 sm:col-span-2">
              <label className="text-xs text-ink-muted">Nombre de archivo</label>
              <Input
                placeholder="ej: aladino"
                value={draft.searchTerm}
                onChange={(e) => setDraft((prev) => ({ ...prev, searchTerm: e.target.value }))}
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <label className="text-xs text-ink-muted">Estado</label>
              <Select
                value={draft.status}
                onChange={(e) => setDraft((prev) => ({ ...prev, status: e.target.value }))}
              >
                <option value="">Todos</option>
                {JOB_STATUS_OPTIONS.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </Select>
            </div>

            <div className="flex flex-col gap-1.5">
              <label className="text-xs text-ink-muted">Formato</label>
              <Input
                placeholder="mp3, m4a…"
                value={draft.format}
                onChange={(e) => setDraft((prev) => ({ ...prev, format: e.target.value }))}
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <label className="text-xs text-ink-muted">Desde</label>
              <Input
                type="date"
                value={draft.fromDate}
                onChange={(e) => setDraft((prev) => ({ ...prev, fromDate: e.target.value }))}
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <label className="text-xs text-ink-muted">Hasta</label>
              <Input
                type="date"
                value={draft.toDate}
                onChange={(e) => setDraft((prev) => ({ ...prev, toDate: e.target.value }))}
              />
            </div>

            <div className="flex items-center gap-2 sm:col-span-2">
              <Button type="submit">
                <Search className="h-4 w-4" />
                Buscar
              </Button>
              <Button type="button" variant="ghost" onClick={handleClear}>
                <X className="h-4 w-4" />
                Limpiar filtros
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="flex flex-col gap-4 pt-6">
          {error && <p className="rounded-lg bg-bad-soft px-3 py-2 text-sm text-bad">{error}</p>}

          {isLoading ? (
            <div className="flex items-center justify-center gap-2 py-10 text-sm text-ink-muted">
              <Loader2 className="h-4 w-4 animate-spin" />
              Buscando…
            </div>
          ) : items.length === 0 ? (
            <p className="rounded-xl border border-dashed border-border-soft px-4 py-10 text-center text-sm text-ink-faint">
              No se encontraron audios con esos filtros.
            </p>
          ) : (
            <ul className="flex flex-col gap-2">
              {items.map((file) => (
                <li key={file.id}>
                  <Link
                    to={`/audio/${file.id}`}
                    className="flex items-center gap-3 rounded-xl border border-border-soft bg-surface/40 px-4 py-3 transition-colors hover:bg-surface-hover"
                  >
                    <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-surface-raised text-ink-muted">
                      <FileAudio className="h-4 w-4" />
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <p className="truncate text-sm text-ink">
                          {file.fileName ?? `Audio #${file.id}`}
                        </p>
                        {file.jobStatusDisplay && (
                          <Badge variant={JOB_BADGE[file.jobStatusDisplay] ?? 'neutral'}>
                            {file.jobStatusDisplay}
                          </Badge>
                        )}
                      </div>
                      <p className="mt-1 text-xs text-ink-faint">
                        {formatBytes(file.fileSize)} · {formatDuration(file.durationSeconds)} ·{' '}
                        {formatDateTime(file.createdAt)}
                      </p>
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
          )}

          {data && data.totalCount > 0 && (
            <div className="flex items-center justify-between border-t border-border-soft pt-4">
              <p className="text-xs text-ink-faint">
                Página {data.page} de {totalPages} · {data.totalCount} resultados
              </p>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page <= 1}
                >
                  <ChevronLeft className="h-4 w-4" />
                  Anterior
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={page >= totalPages}
                >
                  Siguiente
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </>
  );
}
