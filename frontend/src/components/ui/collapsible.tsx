import { useState, type ReactNode } from 'react';
import { ChevronUp } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Badge } from './badge';

interface CollapsibleProps {
  title: string;
  badgeLabel?: string;
  badgeVariant?: 'neutral' | 'pending' | 'good' | 'bad';
  defaultOpen?: boolean;
  children: ReactNode;
}

export function Collapsible({
  title,
  badgeLabel,
  badgeVariant = 'neutral',
  defaultOpen = true,
  children,
}: CollapsibleProps) {
  const [open, setOpen] = useState(defaultOpen);

  return (
    <div className="overflow-hidden rounded-xl border border-border-soft bg-surface/50">
      <button
        type="button"
        onClick={() => setOpen((prev) => !prev)}
        className="flex w-full items-center justify-between px-4 py-3 text-left"
      >
        <span className="flex items-center gap-2">
          <span className="font-display text-base text-ink">{title}</span>
          {badgeLabel && <Badge variant={badgeVariant}>{badgeLabel}</Badge>}
        </span>
        <ChevronUp
          className={cn('h-4 w-4 text-ink-faint transition-transform', !open && 'rotate-180')}
        />
      </button>
      {open && <div className="border-t border-border-soft px-4 py-4">{children}</div>}
    </div>
  );
}
