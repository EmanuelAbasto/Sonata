import { useCallback, useMemo, useRef, useState } from 'react';
import { uploadAudioFiles } from '@/lib/api';
import { getAudioValidationPool } from '@/lib/workerPool';
import type { UploadResponse } from '@/lib/types';

export type UploadItemStatus =
  | 'validating'
  | 'valid'
  | 'invalid'
  | 'uploading'
  | 'uploaded'
  | 'error';

export interface UploadItem {
  id: string;
  file: File;
  status: UploadItemStatus;
  reason?: string;
  progress?: number;
  serverResult?: UploadResponse;
}

export function useAudioUpload(onUploaded?: () => void) {
  const [items, setItems] = useState<UploadItem[]>([]);
  const [isUploading, setIsUploading] = useState(false);
  const pool = useRef(getAudioValidationPool());

  const updateItem = useCallback((id: string, patch: Partial<UploadItem>) => {
    setItems((prev) => prev.map((item) => (item.id === id ? { ...item, ...patch } : item)));
  }, []);

  const addFiles = useCallback(
    (fileList: File[]) => {
      const newItems: UploadItem[] = fileList.map((file) => ({
        id: crypto.randomUUID(),
        file,
        status: 'validating',
      }));

      setItems((prev) => [...newItems, ...prev]);

      newItems.forEach((item) => {
        pool.current.validate(item.file).then((reply) => {
          updateItem(item.id, {
            status: reply.valid ? 'valid' : 'invalid',
            reason: reply.reason,
          });
        });
      });
    },
    [updateItem],
  );

  const removeItem = useCallback((id: string) => {
    setItems((prev) => prev.filter((item) => item.id !== id));
  }, []);

  const validItems = useMemo(() => items.filter((item) => item.status === 'valid'), [items]);

  const uploadValid = useCallback(async () => {
    if (validItems.length === 0) return;

    const ids = validItems.map((item) => item.id);
    setIsUploading(true);
    ids.forEach((id) => updateItem(id, { status: 'uploading', progress: 0 }));

    try {
      const data = await uploadAudioFiles(
        validItems.map((item) => item.file),
        (percent) => ids.forEach((id) => updateItem(id, { progress: percent })),
      );

      const results = data.response ?? [];
      ids.forEach((id, index) => {
        updateItem(id, {
          status: 'uploaded',
          progress: 100,
          serverResult: results[index],
        });
      });

      onUploaded?.();
    } catch (error) {
      const message =
        error instanceof Error ? error.message : 'Ocurrió un error subiendo los archivos.';
      ids.forEach((id) => updateItem(id, { status: 'error', reason: message }));
    } finally {
      setIsUploading(false);
    }
  }, [validItems, updateItem, onUploaded]);

  const clearFinished = useCallback(() => {
    setItems((prev) => prev.filter((item) => item.status !== 'uploaded'));
  }, []);

  return {
    items,
    isUploading,
    validCount: validItems.length,
    addFiles,
    removeItem,
    uploadValid,
    clearFinished,
  };
}
