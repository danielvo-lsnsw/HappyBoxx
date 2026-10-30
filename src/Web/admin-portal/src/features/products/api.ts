import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient, type PagedResult } from '@/lib/api/httpClient';
import type {
  BulkActionResponse,
  BulkAdjustProductPricesRequest,
  BulkUpdateProductsRequest,
  CreateProductRequest,
  ListProductsParams,
  Product,
  ProductChangeHistoryItem,
  ProductProfitabilityDashboard,
  UpsertProductsFromCsvResponse,
  UpdateProductRequest,
} from './types';

export const productKeys = {
  all: ['products'] as const,
  lists: () => [...productKeys.all, 'list'] as const,
  list: (params: ListProductsParams) => [...productKeys.lists(), params] as const,
  detail: (id: string) => [...productKeys.all, 'detail', id] as const,
  historyRoot: (id: string) => [...productKeys.all, 'history', id] as const,
  history: (id: string, take: number) => [...productKeys.all, 'history', id, take] as const,
  profitability: (trendDays: number, lowMarginThresholdPct: number) =>
    [...productKeys.all, 'profitability', trendDays, lowMarginThresholdPct] as const,
};

export function useProductsQuery(params: ListProductsParams = {}, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: productKeys.list(params),
    queryFn: ({ signal }) =>
      httpClient.get<PagedResult<Product>>('/products', { ...params }, signal),
    placeholderData: keepPreviousData,
    enabled: options?.enabled ?? true,
  });
}

export function useProductQuery(id: string | undefined) {
  return useQuery({
    queryKey: productKeys.detail(id ?? ''),
    queryFn: ({ signal }) => httpClient.get<Product>(`/products/${id}`, undefined, signal),
    enabled: !!id,
  });
}

export function useProductChangeHistoryQuery(id: string | undefined, take = 100) {
  return useQuery({
    queryKey: productKeys.history(id ?? '', take),
    queryFn: ({ signal }) =>
      httpClient.get<ProductChangeHistoryItem[]>(
        `/products/${id}/change-history`,
        { take },
        signal,
      ),
    enabled: !!id,
  });
}

export function useProductProfitabilityQuery(trendDays = 30, lowMarginThresholdPct = 10) {
  return useQuery({
    queryKey: productKeys.profitability(trendDays, lowMarginThresholdPct),
    queryFn: ({ signal }) =>
      httpClient.get<ProductProfitabilityDashboard>(
        '/products/profitability',
        { trendDays, lowMarginThresholdPct },
        signal,
      ),
  });
}

export function useCreateProductMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateProductRequest) => httpClient.post<Product>('/products', request),
    onSuccess: (product) => {
      queryClient.setQueryData(productKeys.detail(product.id), product);
      return queryClient.invalidateQueries({ queryKey: productKeys.lists() });
    },
  });
}

export function useUpdateProductMutation(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateProductRequest) =>
      httpClient.put<Product>(`/products/${id}`, request),
    onSuccess: (product) => {
      queryClient.setQueryData(productKeys.detail(id), product);
      return Promise.all([
        queryClient.invalidateQueries({ queryKey: productKeys.lists() }),
        queryClient.invalidateQueries({ queryKey: productKeys.historyRoot(id) }),
      ]);
    },
  });
}

export function useBulkUpdateProductsMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: BulkUpdateProductsRequest) =>
      httpClient.post<BulkActionResponse>('/products/bulk/update', request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: productKeys.all }),
  });
}

export function useBulkAdjustPricesMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: BulkAdjustProductPricesRequest) =>
      httpClient.post<BulkActionResponse>('/products/bulk/price-adjust', request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: productKeys.all }),
  });
}

export function useCsvUpsertProductsMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => {
      const formData = new FormData();
      formData.append('file', file);
      return httpClient.postForm<UpsertProductsFromCsvResponse>('/products/import/csv', formData);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: productKeys.all }),
  });
}
