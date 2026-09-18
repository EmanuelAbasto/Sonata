import { Loader2, UploadCloud } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { UploadDropzone } from '@/components/UploadDropzone';
import { FileList } from '@/components/FileList';
import { useAudioUpload } from '@/hooks/useAudioUpload';
import { useAudioLibrary } from '@/context/AudioLibraryContext';

export default function UploadPage() {
  const { refresh } = useAudioLibrary();
  const { items, isUploading, validCount, addFiles, removeItem, uploadValid } =
    useAudioUpload(refresh);

  return (
    <>
      <header>
        <h1 className="font-display text-2xl text-ink">Subir audios</h1>
        <p className="text-sm text-ink-muted">
          Subí tus audios en mp3 o m4a. La validación corre en paralelo, en segundo plano.
        </p>
      </header>

      <Card>
        <CardHeader>
          <CardTitle>Nueva subida</CardTitle>
          <CardDescription>
            Arrastrá varios archivos a la vez: cada uno se valida en su propio Web Worker antes de
            enviarse al servidor.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-5">
          <UploadDropzone onFilesAccepted={addFiles} />

          <FileList items={items} onRemove={removeItem} />

          {items.length > 0 && (
            <div className="flex items-center justify-between border-t border-border-soft pt-4">
              <p className="text-sm text-ink-muted">
                {validCount > 0
                  ? `${validCount} archivo${validCount === 1 ? '' : 's'} listo${
                      validCount === 1 ? '' : 's'
                    } para subir`
                  : 'Ningún archivo válido todavía'}
              </p>
              <Button onClick={uploadValid} disabled={validCount === 0 || isUploading}>
                {isUploading ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <UploadCloud className="h-4 w-4" />
                )}
                Subir {validCount > 0 ? `(${validCount})` : ''}
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </>
  );
}
