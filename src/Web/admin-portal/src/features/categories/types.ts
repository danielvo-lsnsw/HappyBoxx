export interface Category {
  id: string;
  code: string;
  name: string;
  description: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateCategoryRequest {
  code: string;
  name: string;
  description?: string | null;
}

export interface UpdateCategoryRequest {
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
}

export interface ListCategoriesParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
}
