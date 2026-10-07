import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient, type PagedResult } from '@/lib/api/httpClient';
import type {
  CreateProductRequest,
  ListProductsParams,
  Product,
  UpdateProductRequest,
} from './types';

export const productKeys = {
  all: ['products'] as const,
  lists: () => [...productKeys.all, 'list'] as const,
  list: (params: ListProductsParams) => [...productKeys.lists(), params] as const,
  detail: (id: string) => [...productKeys.all, 'detail', id] as const,
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
      return queryClient.invalidateQueries({ queryKey: productKeys.lists() });
    },
  });
}
