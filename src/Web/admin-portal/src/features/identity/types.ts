export type StaffRole = 'Admin' | 'Order Creator';

export interface StaffAccount {
  id: string;
  email: string;
  displayName: string;
  role: StaffRole;
  status: 'Active' | 'Disabled';
}

export interface StaffInvitation {
  id: string;
  email: string;
  role: StaffRole;
  createdAtUtc: string;
  expiresAtUtc: string;
  claimedAtUtc: string | null;
  revokedAtUtc: string | null;
}

export interface CreatedStaffInvitation extends StaffInvitation {
  invitationUrl: string | null;
  emailSent: boolean;
}
