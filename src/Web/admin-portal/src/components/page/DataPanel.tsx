import { Paper } from '@mantine/core';
import type { ReactNode } from 'react';

/** Bordered surface that hosts a data view and its pagination. */
export function DataPanel({ children }: { children: ReactNode }) {
  return (
    <Paper withBorder radius="md" mt="md" style={{ overflow: 'hidden' }}>
      {children}
    </Paper>
  );
}
