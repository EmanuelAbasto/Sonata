import { useCallback } from 'react';
import { useDropzone, type FileRejection } from 'react-dropzone';
import { UploadCloud } from 'lucide-react';
import { cn } from '@/lib/utils';

interface UploadDropzoneProps {
  onFilesAccepted: (files: File[]) => void;
}

const ACCEPTED_MIME = {
  'audio/mpeg': ['.mp3'],
  'audio/mp4': ['.m4a'],
  'audio/x-m4a': ['.m4a'],
};

export function UploadDropzone({ onFilesAccepted }: UploadDropzoneProps) {
  const onDrop = useCallback(
    (acceptedFiles: File[], rejections: FileRejection[]) => {
      const rejectedFiles = rejections.map((rejection) => rejection.file);
      onFilesAccepted([...acceptedFiles, ...rejectedFiles]);
    },
    [onFilesAccepted],
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: ACCEPTED_MIME,
    multiple: true,
  });

  return (
    <div
      {...getRootProps()}
      className={cn(
        'group flex cursor-pointer flex-col items-center justify-center gap-3 rounded-2xl border border-dashed border-border bg-surface/60 px-6 py-14 text-center transition-colors duration-150',
        isDragActive && 'border-accent bg-accent-soft',
      )}
    >
      <input {...getInputProps()} />
      <div
        className={cn(
          'flex h-12 w-12 items-center justify-center rounded-full bg-surface-raised text-ink-muted transition-colors group-hover:text-accent',
          isDragActive && 'animate-pulse-ring bg-accent-soft text-accent',
        )}
      >
        <UploadCloud className="h-5 w-5" />
      </div>
      <div>
        <p className="font-display text-lg text-ink">
          {isDragActive ? 'Suelta los audios aquí' : 'Arrastra tus audios aquí'}
        </p>
        <p className="mt-1 text-sm text-ink-muted">
          o hacé clic para elegirlos — solo <span className="text-ink">.mp3</span> y{' '}
          <span className="text-ink">.m4a</span>
        </p>
      </div>
    </div>
  );
}
