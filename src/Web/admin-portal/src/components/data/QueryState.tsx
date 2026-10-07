import { Alert, Button, Skeleton, Stack } from '@mantine/core';
import { IconAlertTriangle, IconInbox } from '@tabler/icons-react';
import type { UseQueryResult } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { EmptyState } from './EmptyState';

interface QueryStateProps<T> {
  query: UseQueryResult<T>;
  isEmpty?: (data: T) => boolean;
  empty?: ReactNode;
  skeletonRows?: number;
  children: (data: T) => ReactNode;
}

/** Consistent loading / error / empty / success rendering for every data view. */
export function QueryState<T>({
  query,
  isEmpty,
  empty,
  skeletonRows = 6,
  children,
}: QueryStateProps<T>) {
  if (query.isPending) {
    return (
      <Stack gap="xs" p="md" aria-busy="true" aria-label="Loading">
        {Array.from({ length: skeletonRows }, (_, i) => (
          <Skeleton key={i} height={36} radius="sm" />
        ))}
      </Stack>
    );
  }

  if (query.isError) {
    return (
      <Alert
        m="md"
        color="red"
        variant="light"
        title="Couldn't load data"
        icon={<IconAlertTriangle />}
      >
        <Stack gap="sm" align="flex-start">
          {query.error.message}
          <Button size="sm" variant="white" color="red" onClick={() => void query.refetch()}>
            Retry
          </Button>
        </Stack>
      </Alert>
    );
  }

  if (isEmpty?.(query.data)) {
    return empty ?? <EmptyState icon={IconInbox} title="Nothing here yet" />;
  }

  return <>{children(query.data)}</>;
}
