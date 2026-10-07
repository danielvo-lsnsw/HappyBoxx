export interface Category {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateCategoryRequest {
  name: string;
  description?: string | null;
}

export interface UpdateCategoryRequest {
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
