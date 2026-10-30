export interface StockItem {
  id: string;
  productId: string;
  sku: string;
  quantityOnHand: number;
  minLevel: number;
  maxLevel: number;
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
  minLevel: number;
  maxLevel: number;
}

export interface UpdateStockPolicyRequest {
  minLevel: number;
  maxLevel: number;
}

export interface ReorderSuggestion {
  productId: string;
  sku: string;
  quantityOnHand: number;
  minLevel: number;
  maxLevel: number;
  avgDailyDemand: number;
  reorderPoint: number;
  suggestedOrderQuantity: number;
  needToOrderThisWeek: boolean;
}

export interface ListStockParams {
  page?: number;
  pageSize?: number;
  sku?: string;
  lowStockOnly?: boolean;
}

export interface ListReorderSuggestionsParams {
  lookbackDays?: number;
  needThisWeekOnly?: boolean;
}
