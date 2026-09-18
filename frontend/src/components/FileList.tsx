import { FileListItem } from './FileListItem';
import type { UploadItem } from '@/hooks/useAudioUpload';

export function FileList({
  items,
  onRemove,
}: {
  items: UploadItem[];
  onRemove: (id: string) => void;
}) {
  if (items.length === 0) return null;

  return (
    <ul className="flex flex-col gap-2">
      {items.map((item) => (
        <FileListItem key={item.id} item={item} onRemove={onRemove} />
      ))}
    </ul>
  );
}
