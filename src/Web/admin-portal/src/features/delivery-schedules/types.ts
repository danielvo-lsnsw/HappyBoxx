export type DeliveryScheduleItemStatus = 'Pending' | 'Delivered' | 'Undeliverable';

export interface DeliveryScheduleSummary {
  id: string;
  name: string;
  scheduledDate: string;
  totalInvoices: number;
  deliveredCount: number;
  undeliverableCount: number;
  pendingCount: number;
  createdAtUtc: string;
}

export interface DeliveryScheduleItem {
  id: string;
  invoiceId: string;
  invoiceNumber: string;
  invoiceDate: string;
  customerName: string;
  customerAddress: string;
  customerPhone: string | null;
  total: number;
  status: DeliveryScheduleItemStatus;
  deliveredAtUtc: string | null;
  undeliveredReason: string | null;
}

export interface DeliverySchedule {
  id: string;
  name: string;
  scheduledDate: string;
  createdAtUtc: string;
  items: DeliveryScheduleItem[];
}
