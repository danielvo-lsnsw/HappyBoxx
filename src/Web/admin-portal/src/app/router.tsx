import { Center, Loader } from '@mantine/core';
import { createBrowserRouter } from 'react-router';
import { NotFoundPage } from '@/pages/NotFoundPage';
import { CustomerRegistrationPage } from '@/pages/CustomerRegistrationPage';
import { AcceptStaffInvitationPage } from '@/pages/AcceptStaffInvitationPage';
import { AuthenticationGate } from './auth/AuthProvider';
import { AppLayout } from './layout/AppLayout';
import { flattenLeaves } from './navigation';

const comingSoon = () =>
  import('@/pages/ComingSoonPage').then((m) => ({ Component: m.ComingSoonPage }));

const plannedRoutes = flattenLeaves()
  .filter((item) => item.planned && item.path)
  .map((item) => ({ path: item.path, lazy: comingSoon }));

export const router = createBrowserRouter([
  {
    path: '/register',
    element: <CustomerRegistrationPage />,
  },
  {
    path: '/accept-invitation',
    element: <AcceptStaffInvitationPage />,
  },
  {
    path: '/',
    element: (
      <AuthenticationGate>
        <AppLayout />
      </AuthenticationGate>
    ),
    hydrateFallbackElement: (
      <Center h="100dvh">
        <Loader />
      </Center>
    ),
    children: [
      {
        index: true,
        lazy: () => import('@/pages/DashboardPage').then((m) => ({ Component: m.DashboardPage })),
      },
      {
        path: 'inventory/products',
        lazy: () =>
          import('@/features/products/pages/ProductsPage').then((m) => ({
            Component: m.ProductsPage,
          })),
      },
      {
        path: 'inventory/categories',
        lazy: () =>
          import('@/features/categories/pages/CategoriesPage').then((m) => ({
            Component: m.CategoriesPage,
          })),
      },
      {
        path: 'inventory/stock',
        lazy: () =>
          import('@/features/inventory/pages/StockPage').then((m) => ({ Component: m.StockPage })),
      },
      {
        path: 'admin/staff',
        lazy: () =>
          import('@/pages/StaffAccessPage').then((m) => ({ Component: m.StaffAccessPage })),
      },
      ...plannedRoutes,
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);
