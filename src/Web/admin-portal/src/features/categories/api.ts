import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient, type PagedResult } from '@/lib/api/httpClient';
import type {
  Category,
  CreateCategoryRequest,
  ListCategoriesParams,
  UpdateCategoryRequest,
} from './types';

export const categoryKeys = {
  all: ['categories'] as const,
  lists: () => [...categoryKeys.all, 'list'] as const,
  list: (params: ListCategoriesParams) => [...categoryKeys.lists(), params] as const,
  detail: (id: string) => [...categoryKeys.all, 'detail', id] as const,
};

export function useCategoriesQuery(params: ListCategoriesParams = {}) {
  return useQuery({
    queryKey: categoryKeys.list(params),
    queryFn: ({ signal }) =>
      httpClient.get<PagedResult<Category>>('/categories', { ...params }, signal),
    placeholderData: keepPreviousData,
  });
}

export function useCreateCategoryMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateCategoryRequest) =>
      httpClient.post<Category>('/categories', request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: categoryKeys.lists() }),
  });
}

export function useUpdateCategoryMutation(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateCategoryRequest) =>
      httpClient.put<Category>(`/categories/${id}`, request),
    onSuccess: (category) => {
      queryClient.setQueryData(categoryKeys.detail(id), category);
      return queryClient.invalidateQueries({ queryKey: categoryKeys.lists() });
    },
  });
}
