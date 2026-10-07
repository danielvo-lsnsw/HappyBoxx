import { createBrowserRouter } from 'react-router';
import { AdminLayout } from '@/components/layout/AdminLayout';
import { CategoriesPage } from '@/features/categories/pages/CategoriesPage';
import { StockPage } from '@/features/inventory/pages/StockPage';
import { ProductsPage } from '@/features/products/pages/ProductsPage';
import { DashboardPage } from '@/pages/DashboardPage';
import { NotFoundPage } from '@/pages/NotFoundPage';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AdminLayout />,
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'categories', element: <CategoriesPage /> },
      { path: 'products', element: <ProductsPage /> },
      { path: 'stock', element: <StockPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);
