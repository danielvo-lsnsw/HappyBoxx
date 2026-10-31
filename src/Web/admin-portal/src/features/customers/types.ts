export type CustomerType =
  | 'Restaurant'
  | 'Grocery shop'
  | 'Convenience store'
  | 'Cafe'
  | 'Bakery'
  | 'Butcher'
  | 'Other';

export const customerTypeOptions: CustomerType[] = [
  'Restaurant',
  'Grocery shop',
  'Convenience store',
  'Cafe',
  'Bakery',
  'Butcher',
  'Other',
];

export interface Customer {
  id: string;
  name: string;
  address: string;
  phone: string | null;
  email: string | null;
  website: string | null;
  abn: string | null;
  customerType: string | null;
  isActive: boolean;
  updatedAtUtc: string;
}

export interface ListCustomersParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
}

export interface CreateCustomerRequest {
  name: string;
  address: string;
  phone: string;
  email?: string | null;
  website?: string | null;
  abn?: string | null;
  customerType?: CustomerType | null;
}

export interface UpdateCustomerRequest extends CreateCustomerRequest {
  isActive: boolean;
}
