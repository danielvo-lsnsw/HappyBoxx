import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient, type PagedResult } from '@/lib/api/httpClient';
import type { CreateInvoiceRequest, Invoice, InvoiceSummary } from './types';

export const invoiceKeys = {
  all: ['invoices'] as const,
  lists: () => [...invoiceKeys.all, 'list'] as const,
  list: (page: number, pageSize: number, search: string) =>
    [...invoiceKeys.lists(), page, pageSize, search] as const,
  detail: (id: string) => [...invoiceKeys.all, 'detail', id] as const,
};

export function useInvoicesQuery(page: number, pageSize: number, search = '') {
  return useQuery({
    queryKey: invoiceKeys.list(page, pageSize, search),
    queryFn: ({ signal }) =>
      httpClient.get<PagedResult<InvoiceSummary>>(
        '/invoices',
        { page, pageSize, search: search || undefined },
        signal,
      ),
    placeholderData: keepPreviousData,
  });
}

export function useInvoiceQuery(id: string | undefined) {
  return useQuery({
    queryKey: invoiceKeys.detail(id ?? ''),
    queryFn: ({ signal }) => httpClient.get<Invoice>(`/invoices/${id}`, undefined, signal),
    enabled: !!id,
  });
}

export function useCreateInvoiceMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateInvoiceRequest) => httpClient.post<Invoice>('/invoices', request),
    onSuccess: (invoice) => {
      queryClient.setQueryData(invoiceKeys.detail(invoice.id), invoice);
      return queryClient.invalidateQueries({ queryKey: invoiceKeys.lists() });
    },
  });
}

export function useEmailInvoiceMutation(id: string) {
  return useMutation({
    mutationFn: (request?: { toEmails?: string[]; includeInvoice?: boolean; subject?: string; message?: string }) =>
      httpClient.post<undefined>(`/invoices/${id}/email`, request ?? {}),
  });
}

export function useRecordInvoicePaymentMutation(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: {
      paidOn: string;
      paymentReference?: string;
      paymentNote?: string;
      sendReceiptEmail: boolean;
      receiptToEmails?: string[];
      includeInvoice?: boolean;
    }) => httpClient.post<Invoice>(`/invoices/${id}/payment`, request),
    onSuccess: (invoice) => {
      queryClient.setQueryData(invoiceKeys.detail(invoice.id), invoice);
      return queryClient.invalidateQueries({ queryKey: invoiceKeys.lists() });
    },
  });
}
