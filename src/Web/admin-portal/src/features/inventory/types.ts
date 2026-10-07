export interface StockItem {
  id: string;
  productId: string;
  sku: string;
  quantityOnHand: number;
  reorderLevel: number;
  isLowStock: boolean;
  updatedAtUtc: string;
}

export interface AdjustStockRequest {
  quantityChange: number;
}

export interface CreateStockItemRequest {
  productId: string;
  sku: string;
  initialQuantity: number;
  reorderLevel: number;
}

export interface ListStockParams {
  page?: number;
  pageSize?: number;
  sku?: string;
  lowStockOnly?: boolean;
}
