import type { UseQueryResult } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { ApiError } from '@/lib/api/httpClient';

interface QueryStateProps<T> {
  query: UseQueryResult<T>;
  isEmpty?: (data: T) => boolean;
  emptyMessage?: string;
  children: (data: T) => ReactNode;
}

/** Renders the loading / error / empty / success states of a query consistently. */
export function QueryState<T>({
  query,
  isEmpty,
  emptyMessage = 'Nothing here yet.',
  children,
}: QueryStateProps<T>) {
  if (query.isPending) {
    return <p role="status">Loading…</p>;
  }

  if (query.isError) {
    const error = query.error;
    const message = error instanceof ApiError ? error.message : 'Something went wrong.';
    return <p role="alert">{message}</p>;
  }

  if (isEmpty?.(query.data)) {
    return <p>{emptyMessage}</p>;
  }

  return <>{children(query.data)}</>;
}
