import { NavLink } from 'react-router-dom';
import { AudioLines, FileAudio, Loader2, SearchCheck, UploadCloud } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { useAudioLibrary } from '@/context/AudioLibraryContext';

const JOB_BADGE: Record<string, 'neutral' | 'pending' | 'good' | 'bad'> = {
  Completed: 'good',
  Failed: 'bad',
  Pending: 'neutral',
  Processing: 'pending',
  Compressing: 'pending',
  Transcribing: 'pending',
  Summarizing: 'pending',
};

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  cn(
    'flex items-center gap-2 rounded-lg px-3 py-2 text-sm transition-colors',
    isActive ? 'bg-accent-soft text-accent' : 'text-ink-muted hover:bg-surface-hover hover:text-ink',
  );

export function Sidebar() {
  const { recentFiles, isLoading, error } = useAudioLibrary();

  return (
    <aside className="flex h-screen w-72 shrink-0 flex-col border-r border-border bg-surface/40">
      <div className="flex items-center gap-2 px-4 pb-4 pt-5">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-accent-soft text-accent">
          <AudioLines className="h-4 w-4" />
        </div>
        <span className="font-display text-lg text-ink">Audio Uploader</span>
      </div>

      <nav className="flex flex-col gap-1 px-3">
        <NavLink to="/" end className={navLinkClass}>
          <UploadCloud className="h-4 w-4" />
          Subir audios
        </NavLink>
        <NavLink to="/search" className={navLinkClass}>
          <SearchCheck className="h-4 w-4" />
          Búsqueda avanzada
        </NavLink>
      </nav>

      <div className="mt-5 flex min-h-0 flex-1 flex-col border-t border-border-soft">
        <p className="px-4 pb-2 pt-4 text-xs font-medium uppercase tracking-wide text-ink-faint">
          Historial
        </p>

        <div className="min-h-0 flex-1 overflow-y-auto px-2 pb-4">
          {isLoading && recentFiles.length === 0 && (
            <div className="flex items-center gap-2 px-3 py-4 text-sm text-ink-faint">
              <Loader2 className="h-4 w-4 animate-spin" />
              Cargando…
            </div>
          )}

          {error && <p className="px-3 py-2 text-xs text-bad">{error}</p>}

          {!isLoading && recentFiles.length === 0 && !error && (
            <p className="px-3 py-2 text-xs text-ink-faint">Todavía no hay audios subidos.</p>
          )}

          <ul className="flex flex-col gap-1">
            {[...recentFiles].reverse().map((file) => (
              <li key={file.id}>
                <NavLink
                  to={`/audio/${file.id}`}
                  className={({ isActive }) =>
                    cn(
                      'flex items-start gap-2 rounded-lg px-3 py-2 text-left transition-colors',
                      isActive ? 'bg-surface-hover text-ink' : 'text-ink-muted hover:bg-surface-hover hover:text-ink',
                    )
                  }
                >
                  <FileAudio className="mt-0.5 h-3.5 w-3.5 shrink-0" />
                  <span className="min-w-0 flex-1">
                    <span className="block truncate text-xs">{file.fileName ?? `Audio #${file.id}`}</span>
                    {file.jobStatusDisplay && (
                      <Badge
                        variant={JOB_BADGE[file.jobStatusDisplay] ?? 'neutral'}
                        className="mt-1"
                      >
                        {file.jobStatusDisplay}
                      </Badge>
                    )}
                  </span>
                </NavLink>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </aside>
  );
}
