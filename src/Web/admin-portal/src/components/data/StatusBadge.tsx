import { Badge, type BadgeProps } from '@mantine/core';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';

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
  const { t } = useUiLanguage();
  return (
    <Badge
      color={toneColors[tone]}
      variant="light"
      radius="sm"
      leftSection={<span className="status-dot" aria-hidden />}
      {...props}
    >
      {t(label)}
    </Badge>
  );
}

export function ActiveBadge({ isActive }: { isActive: boolean }) {
  const { t } = useUiLanguage();
  return isActive ? (
    <StatusBadge tone="success" label={t('Active')} />
  ) : (
    <StatusBadge tone="neutral" label={t('Inactive')} />
  );
}
