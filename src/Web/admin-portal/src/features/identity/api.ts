import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient } from '@/lib/api/httpClient';
import type { CreatedStaffInvitation, StaffAccount, StaffInvitation, StaffRole } from './types';

export const identityKeys = {
  all: ['identity'] as const,
  staffAccounts: () => [...identityKeys.all, 'staff-accounts'] as const,
  staffInvitations: () => [...identityKeys.all, 'staff-invitations'] as const,
};

export function useStaffAccountsQuery() {
  return useQuery({
    queryKey: identityKeys.staffAccounts(),
    queryFn: ({ signal }) =>
      httpClient.get<StaffAccount[]>('/identity/staff/accounts', undefined, signal),
  });
}

export function useStaffInvitationsQuery() {
  return useQuery({
    queryKey: identityKeys.staffInvitations(),
    queryFn: ({ signal }) =>
      httpClient.get<StaffInvitation[]>('/identity/staff/invitations', undefined, signal),
  });
}

export function useCreateStaffInvitationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: { email: string; role: StaffRole }) =>
      httpClient.post<CreatedStaffInvitation>('/identity/staff/invitations', request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: identityKeys.staffInvitations() }),
  });
}

export function useRevokeStaffInvitationMutation(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => httpClient.post<undefined>(`/identity/staff/invitations/${id}/revoke`, {}),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: identityKeys.staffInvitations() }),
  });
}

export function useChangeStaffRoleMutation(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: { role: StaffRole }) =>
      httpClient.put<StaffAccount>(`/identity/staff/accounts/${id}/role`, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: identityKeys.staffAccounts() }),
  });
}

export function useDisableStaffAccountMutation(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => httpClient.post<undefined>(`/identity/staff/accounts/${id}/disable`, {}),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: identityKeys.staffAccounts() }),
  });
}
