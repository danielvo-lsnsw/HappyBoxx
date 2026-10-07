import type { StatusTone } from '@/components/data/StatusBadge';
import type { StockItem } from './types';

export function getStockStatus(item: Pick<StockItem, 'quantityOnHand' | 'isLowStock'>): {
  tone: StatusTone;
  label: string;
} {
  if (item.quantityOnHand <= 0) {
    return { tone: 'danger', label: 'Out of stock' };
  }
  if (item.isLowStock) {
    return { tone: 'warning', label: 'Low stock' };
  }
  return { tone: 'success', label: 'In stock' };
}
