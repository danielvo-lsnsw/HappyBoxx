import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient, type PagedResult } from '@/lib/api/httpClient';
import type { AdjustStockRequest, ListStockParams, StockItem } from './types';

export const stockKeys = {
  all: ['stock'] as const,
  lists: () => [...stockKeys.all, 'list'] as const,
  list: (params: ListStockParams) => [...stockKeys.lists(), params] as const,
};

export function useStockQuery(params: ListStockParams = {}) {
  return useQuery({
    queryKey: stockKeys.list(params),
    queryFn: ({ signal }) =>
      httpClient.get<PagedResult<StockItem>>('/stock', { ...params }, signal),
    placeholderData: keepPreviousData,
  });
}

export function useAdjustStockMutation(productId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: AdjustStockRequest) =>
      httpClient.post<StockItem>(`/stock/${productId}/adjustments`, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: stockKeys.lists() }),
  });
}
