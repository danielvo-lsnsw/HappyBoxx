import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient, type PagedResult } from '@/lib/api/httpClient';
import type { DeliverySchedule, DeliveryScheduleItemStatus, DeliveryScheduleSummary } from './types';

export const deliveryScheduleKeys = {
  all: ['delivery-schedules'] as const,
  lists: () => [...deliveryScheduleKeys.all, 'list'] as const,
  list: (page: number, pageSize: number, search: string) =>
    [...deliveryScheduleKeys.lists(), page, pageSize, search] as const,
  detail: (id: string) => [...deliveryScheduleKeys.all, 'detail', id] as const,
};

export function useDeliverySchedulesQuery(page: number, pageSize: number, search = '') {
  return useQuery({
    queryKey: deliveryScheduleKeys.list(page, pageSize, search),
    queryFn: ({ signal }) =>
      httpClient.get<PagedResult<DeliveryScheduleSummary>>(
        '/delivery-schedules',
        { page, pageSize, search: search || undefined },
        signal,
      ),
    placeholderData: keepPreviousData,
  });
}

export function useDeliveryScheduleQuery(id: string | undefined) {
  return useQuery({
    queryKey: deliveryScheduleKeys.detail(id ?? ''),
    queryFn: ({ signal }) =>
      httpClient.get<DeliverySchedule>(`/delivery-schedules/${id}`, undefined, signal),
    enabled: !!id,
  });
}

export function useCreateDeliveryScheduleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: { invoiceIds: string[]; scheduledDate?: string; name?: string }) =>
      httpClient.post<DeliverySchedule>('/delivery-schedules', request),
    onSuccess: (schedule) => {
      queryClient.setQueryData(deliveryScheduleKeys.detail(schedule.id), schedule);
      return queryClient.invalidateQueries({ queryKey: deliveryScheduleKeys.lists() });
    },
  });
}

export function useUpdateDeliveryScheduleItemStatusMutation(scheduleId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: {
      itemId: string;
      status: DeliveryScheduleItemStatus;
      deliveredAtUtc?: string;
      reasonNote?: string;
    }) =>
      httpClient.post<DeliverySchedule>(
        `/delivery-schedules/${scheduleId}/items/${request.itemId}/status`,
        {
          status: request.status,
          deliveredAtUtc: request.deliveredAtUtc,
          reasonNote: request.reasonNote,
        },
      ),
    onSuccess: (schedule) => {
      queryClient.setQueryData(deliveryScheduleKeys.detail(schedule.id), schedule);
      return queryClient.invalidateQueries({ queryKey: deliveryScheduleKeys.lists() });
    },
  });
}

export function useMarkAllDeliveryItemsDeliveredMutation(scheduleId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: { deliveredAtUtc?: string }) =>
      httpClient.post<DeliverySchedule>(`/delivery-schedules/${scheduleId}/deliver-all`, request),
    onSuccess: (schedule) => {
      queryClient.setQueryData(deliveryScheduleKeys.detail(schedule.id), schedule);
      return queryClient.invalidateQueries({ queryKey: deliveryScheduleKeys.lists() });
    },
  });
}
