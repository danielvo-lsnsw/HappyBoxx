import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ApiError, httpClient, type PagedResult } from '@/lib/api/httpClient';
import type {
  AdjustStockRequest,
  CreateStockItemRequest,
  ListStockParams,
  StockItem,
} from './types';

export const stockKeys = {
  all: ['stock'] as const,
  lists: () => [...stockKeys.all, 'list'] as const,
  list: (params: ListStockParams) => [...stockKeys.lists(), params] as const,
  byProduct: (productId: string) => [...stockKeys.all, 'product', productId] as const,
};

export function useStockQuery(params: ListStockParams = {}) {
  return useQuery({
    queryKey: stockKeys.list(params),
    queryFn: ({ signal }) =>
      httpClient.get<PagedResult<StockItem>>('/stock', { ...params }, signal),
    placeholderData: keepPreviousData,
  });
}

/** Resolves to `null` when the product isn't tracked in Inventory yet. */
export function useStockByProductQuery(productId: string | undefined) {
  return useQuery({
    queryKey: stockKeys.byProduct(productId ?? ''),
    queryFn: async ({ signal }) => {
      try {
        return await httpClient.get<StockItem>(`/stock/${productId}`, undefined, signal);
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) {
          return null;
        }
        throw error;
      }
    },
    enabled: !!productId,
  });
}

function useInvalidateStock() {
  const queryClient = useQueryClient();
  return (item: StockItem) => {
    queryClient.setQueryData(stockKeys.byProduct(item.productId), item);
    return queryClient.invalidateQueries({ queryKey: stockKeys.lists() });
  };
}

export function useCreateStockItemMutation() {
  const onSuccess = useInvalidateStock();
  return useMutation({
    mutationFn: (request: CreateStockItemRequest) => httpClient.post<StockItem>('/stock', request),
    onSuccess,
  });
}

export function useAdjustStockMutation(productId: string) {
  const onSuccess = useInvalidateStock();
  return useMutation({
    mutationFn: (request: AdjustStockRequest) =>
      httpClient.post<StockItem>(`/stock/${productId}/adjustments`, request),
    onSuccess,
  });
}
