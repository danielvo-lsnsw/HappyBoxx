import { Button, Group, Paper } from '@mantine/core';
import { IconFilterOff } from '@tabler/icons-react';
import type { ReactNode } from 'react';

interface FilterBarProps {
  children: ReactNode;
  /** Right-aligned controls such as a unit toggle. */
  aside?: ReactNode;
  hasActiveFilters?: boolean;
  onReset?: () => void;
}

export function FilterBar({ children, aside, hasActiveFilters, onReset }: FilterBarProps) {
  return (
    <Paper withBorder p="sm" radius="md">
      <Group justify="space-between" gap="sm" wrap="wrap">
        <Group gap="sm" wrap="wrap" align="flex-end">
          {children}
          {hasActiveFilters && onReset && (
            <Button variant="subtle" leftSection={<IconFilterOff size={18} />} onClick={onReset}>
              Reset
            </Button>
          )}
        </Group>
        {aside}
      </Group>
    </Paper>
  );
}
