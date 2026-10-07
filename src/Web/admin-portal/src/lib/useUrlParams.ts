import { useCallback } from 'react';
import { useSearchParams } from 'react-router';

type ParamValue = string | number | boolean | null | undefined;

/** Reads/writes page state (filters, paging, open drawer) in the URL so views are shareable. */
export function useUrlParams() {
  const [params, setParams] = useSearchParams();

  const get = useCallback((key: string) => params.get(key) ?? undefined, [params]);

  const getNumber = useCallback(
    (key: string, fallback: number) => {
      const value = Number(params.get(key));
      return Number.isInteger(value) && value > 0 ? value : fallback;
    },
    [params],
  );

  const set = useCallback(
    (updates: Record<string, ParamValue>, options?: { push?: boolean }) =>
      setParams(
        (previous) => {
          const next = new URLSearchParams(previous);
          for (const [key, value] of Object.entries(updates)) {
            if (value === undefined || value === null || value === '') {
              next.delete(key);
            } else {
              next.set(key, String(value));
            }
          }
          return next;
        },
        { replace: !options?.push },
      ),
    [setParams],
  );

  return { params, get, getNumber, set };
}
