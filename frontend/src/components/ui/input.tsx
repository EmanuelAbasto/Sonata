import { forwardRef, type InputHTMLAttributes } from 'react';
import { cn } from '@/lib/utils';

export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement>>(
  ({ className, ...props }, ref) => (
    <input
      ref={ref}
      className={cn(
        'h-10 w-full rounded-lg border border-border bg-surface-raised/60 px-3 text-sm text-ink placeholder:text-ink-faint focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-accent',
        className,
      )}
      {...props}
    />
  ),
);
Input.displayName = 'Input';
