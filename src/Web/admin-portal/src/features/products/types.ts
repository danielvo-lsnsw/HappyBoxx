export type UnitOfMeasure = 'Piece' | 'Kilogram' | 'Gram' | 'Bunch' | 'Pack' | 'Box' | 'Carton';

export interface Product {
  id: string;
  sku: string;
  name: string;
  description: string | null;
  categoryId: string;
  categoryName: string;
  unit: UnitOfMeasure;
  price: number;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateProductRequest {
  sku: string;
  name: string;
  description?: string | null;
  categoryId: string;
  unit: UnitOfMeasure;
  price: number;
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
}
