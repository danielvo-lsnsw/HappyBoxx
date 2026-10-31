import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient, type PagedResult } from '@/lib/api/httpClient';
import type {
  CreateCustomerRequest,
  Customer,
  ListCustomersParams,
  UpdateCustomerRequest,
} from './types';

export const customerKeys = {
  all: ['customers'] as const,
  lists: () => [...customerKeys.all, 'list'] as const,
  list: (params: ListCustomersParams) => [...customerKeys.lists(), params] as const,
  detail: (id: string) => [...customerKeys.all, 'detail', id] as const,
};

export function useCustomersQuery(params: ListCustomersParams = {}) {
  return useQuery({
    queryKey: customerKeys.list(params),
    queryFn: ({ signal }) =>
      httpClient.get<PagedResult<Customer>>('/customers', { ...params }, signal),
    placeholderData: keepPreviousData,
  });
}

export function useCustomerQuery(id: string | undefined) {
  return useQuery({
    queryKey: customerKeys.detail(id ?? ''),
    queryFn: ({ signal }) => httpClient.get<Customer>(`/customers/${id}`, undefined, signal),
    enabled: !!id,
  });
}

export function useCreateCustomerMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateCustomerRequest) => httpClient.post<Customer>('/customers', request),
    onSuccess: (customer) => {
      queryClient.setQueryData(customerKeys.detail(customer.id), customer);
      return queryClient.invalidateQueries({ queryKey: customerKeys.lists() });
    },
  });
}

export function useUpdateCustomerMutation(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateCustomerRequest) =>
      httpClient.put<Customer>(`/customers/${id}`, request),
    onSuccess: (customer) => {
      queryClient.setQueryData(customerKeys.detail(id), customer);
      return queryClient.invalidateQueries({ queryKey: customerKeys.lists() });
    },
  });
}

export function useDeleteCustomerMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => httpClient.delete(`/customers/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: customerKeys.lists() }),
  });
}
