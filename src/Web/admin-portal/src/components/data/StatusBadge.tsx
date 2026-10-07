import { Badge, type BadgeProps } from '@mantine/core';

export type StatusTone = 'success' | 'warning' | 'danger' | 'neutral' | 'info';

// Status colours are fixed across the app – see docs/UI_GUIDELINES.md §5.1.
const toneColors: Record<StatusTone, string> = {
  success: 'green',
  warning: 'yellow',
  danger: 'red',
  neutral: 'gray',
  info: 'blue',
};

interface StatusBadgeProps extends Omit<BadgeProps, 'color' | 'children'> {
  tone: StatusTone;
  label: string;
}

export function StatusBadge({ tone, label, ...props }: StatusBadgeProps) {
  return (
    <Badge
      color={toneColors[tone]}
      variant="light"
      radius="sm"
      leftSection={<span className="status-dot" aria-hidden />}
      {...props}
    >
      {label}
    </Badge>
  );
}

export function ActiveBadge({ isActive }: { isActive: boolean }) {
  return isActive ? (
    <StatusBadge tone="success" label="Active" />
  ) : (
    <StatusBadge tone="neutral" label="Inactive" />
  );
}
