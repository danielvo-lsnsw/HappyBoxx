export interface InvoiceLine {
  id: string;
  productId: string;
  priceTier: 'Near' | 'Far';
  description: string;
  unit: string;
  quantity: number;
  unitPriceInclusive: number;
  amountInclusive: number;
}

export interface InvoiceSummary {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  customerId: string;
  customerName: string;
  total: number;
  paidOn: string | null;
}

export interface Invoice extends InvoiceSummary {
  customerAddress: string;
  customerPhone: string | null;
  customerEmail: string | null;
  subtotal: number;
  gstAmount: number;
  paymentReference: string | null;
  paymentNote: string | null;
  lines: InvoiceLine[];
}

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

export interface CreateCustomerRequest {
  name: string;
  address: string;
  phone: string;
  email?: string | null;
  website?: string | null;
  abn?: string | null;
  customerType?: string | null;
}

export interface CreateInvoiceRequest {
  invoiceNumber: string;
  invoiceDate: string;
  customerId: string;
  lines: Array<{
    productId: string;
    priceTier: 'Near' | 'Far';
    description: string;
    unit: string;
    quantity: number;
    unitPriceInclusive: number;
  }>;
}

export interface DuplicateInvoiceDraft {
  customerId: string;
  lines: Array<{
    productId: string;
    priceTier: 'Near' | 'Far';
    description: string;
    unit: string;
    quantity: number;
    unitPriceInclusive: number;
  }>;
}
