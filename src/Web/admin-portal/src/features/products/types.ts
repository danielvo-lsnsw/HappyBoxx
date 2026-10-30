import type { UnitOfMeasure } from '@/lib/units';

export type { UnitOfMeasure };

export interface Product {
  id: string;
  sku: string;
  legacyCode: string | null;
  name: string;
  description: string | null;
  categoryId: string;
  categoryName: string;
  unit: UnitOfMeasure;
  packDetails: string | null;
  sellPriceNear: number | null;
  sellPriceFar: number | null;
  buyPrice: number | null;
  priceEffectiveDate: string | null;
  suppliers: ProductSupplier[];
  price: number;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface ProductSupplier {
  id: string;
  name: string;
  isDefault: boolean;
  lastBuyPrice: number | null;
  lastBuyPriceAtUtc: string | null;
}

export interface ProductChangeHistoryItem {
  id: string;
  fieldName: string;
  oldValue: string | null;
  newValue: string | null;
  changedBy: string;
  changedAtUtc: string;
}

export interface ProductProfitabilityItem {
  productId: string;
  sku: string;
  name: string;
  categoryId: string;
  categoryName: string;
  buyPrice: number | null;
  sellPriceNear: number | null;
  sellPriceFar: number | null;
  nearMarginPct: number | null;
  farMarginPct: number | null;
  nearMarginTrendPct: number | null;
  farMarginTrendPct: number | null;
  isLowMargin: boolean;
  isNegativeMargin: boolean;
}

export interface CategoryProfitabilitySummary {
  categoryId: string;
  categoryName: string;
  avgNearMarginPct: number | null;
  avgFarMarginPct: number | null;
  productCount: number;
  lowMarginCount: number;
  negativeMarginCount: number;
}

export interface ProductProfitabilityDashboard {
  generatedAtUtc: string;
  lowMarginThresholdPct: number;
  products: ProductProfitabilityItem[];
  categories: CategoryProfitabilitySummary[];
}

export interface CreateProductRequest {
  sku: string;
  legacyCode?: string | null;
  name: string;
  description?: string | null;
  categoryId: string;
  unit: UnitOfMeasure;
  packDetails?: string | null;
  sellPriceNear?: number | null;
  sellPriceFar?: number | null;
  buyPrice?: number | null;
  priceEffectiveDate?: string | null;
  supplierNames?: string[];
  defaultSupplierName?: string | null;
}

export interface UpdateProductRequest extends Omit<CreateProductRequest, 'sku'> {
  isActive: boolean;
}

export interface ListProductsParams {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: string;
  isActive?: boolean;
  sortBy?: 'sku' | 'name' | 'category' | 'unit' | 'near' | 'far' | 'status';
  sortDir?: 'asc' | 'desc';
}

export interface BulkUpdateProductsRequest {
  productIds: string[];
  isActive?: boolean;
  categoryId?: string;
  unit?: UnitOfMeasure;
  supplierNames?: string[];
  defaultSupplierName?: string | null;
}

export interface BulkAdjustProductPricesRequest {
  productIds: string[];
  mode: 'Percentage' | 'Absolute';
  nearChange?: number | null;
  farChange?: number | null;
}

export interface BulkActionResponse {
  updatedCount: number;
}

export interface CsvImportError {
  rowNumber: number;
  message: string;
}

export interface UpsertProductsFromCsvResponse {
  createdCount: number;
  updatedCount: number;
  errorCount: number;
  errors: CsvImportError[];
}
