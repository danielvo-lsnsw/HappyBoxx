import {
  IconApple,
  IconArrowBackUp,
  IconBox,
  IconBuildingWarehouse,
  IconCarrot,
  IconCategory,
  IconChartBar,
  IconClipboardList,
  IconHandGrab,
  IconHourglass,
  IconLayoutDashboard,
  IconLeaf,
  IconPackages,
  IconRoute,
  IconShieldCheck,
  IconShoppingCart,
  IconSignature,
  IconStack2,
  IconSteeringWheel,
  IconTruckDelivery,
  IconTruckLoading,
  IconUsers,
  type Icon,
} from '@tabler/icons-react';
import { useMemo } from 'react';
import { useLocation } from 'react-router';

export interface NavItem {
  label: string;
  /** Route for leaves; may include a query string (e.g. category filters). */
  path?: string;
  icon?: Icon;
  description?: string;
  /** Module not built yet – rendered as "Coming soon". */
  planned?: boolean;
  children?: NavItem[];
}

const productsInCategory = (category: string) =>
  `/inventory/products?category=${encodeURIComponent(category)}`;

/** Single source of truth for sidebar, breadcrumbs, global search and placeholder routes. */
export const navigation: NavItem[] = [
  {
    label: 'Dashboard',
    path: '/',
    icon: IconLayoutDashboard,
    description: 'Key metrics and alerts',
  },
  {
    label: 'Inventory',
    icon: IconBuildingWarehouse,
    children: [
      {
        label: 'All Products',
        path: '/inventory/products',
        icon: IconPackages,
        description: 'Everything in the catalog',
      },
      {
        label: 'Fresh Produce',
        icon: IconLeaf,
        children: [
          { label: 'Vegetables', path: productsInCategory('Vegetables'), icon: IconCarrot },
          { label: 'Fruits', path: productsInCategory('Fruits'), icon: IconApple },
        ],
      },
      {
        label: 'Packaging',
        icon: IconBox,
        children: [
          { label: 'Containers', path: productsInCategory('Food Containers'), icon: IconBox },
        ],
      },
      {
        label: 'Stock Levels',
        path: '/inventory/stock',
        icon: IconStack2,
        description: 'On-hand quantities and low-stock alerts',
      },
      {
        label: 'Categories',
        path: '/inventory/categories',
        icon: IconCategory,
        description: 'Organise the catalog',
      },
      {
        label: 'Spoilage & Quality Control',
        path: '/inventory/quality',
        icon: IconShieldCheck,
        description: 'Record spoilage, inspections and write-offs',
        planned: true,
      },
    ],
  },
  {
    label: 'Orders',
    icon: IconClipboardList,
    children: [
      {
        label: 'Pending',
        path: '/orders/pending',
        icon: IconHourglass,
        description: 'New orders awaiting confirmation',
        planned: true,
      },
      {
        label: 'Processing / Picking',
        path: '/orders/picking',
        icon: IconHandGrab,
        description: 'Orders being picked and packed',
        planned: true,
      },
      {
        label: 'Ready for Dispatch',
        path: '/orders/dispatch',
        icon: IconTruckLoading,
        description: 'Packed orders waiting for a driver',
        planned: true,
      },
      {
        label: 'Returns',
        path: '/orders/returns',
        icon: IconArrowBackUp,
        description: 'Returned and rejected goods',
        planned: true,
      },
    ],
  },
  {
    label: 'Logistics & Delivery',
    icon: IconTruckDelivery,
    children: [
      {
        label: 'Route Planning',
        path: '/logistics/routes',
        icon: IconRoute,
        description: 'Plan delivery runs',
        planned: true,
      },
      {
        label: 'Driver Dispatch',
        path: '/logistics/dispatch',
        icon: IconSteeringWheel,
        description: 'Assign drivers and vehicles',
        planned: true,
      },
      {
        label: 'Proof of Delivery',
        path: '/logistics/proof-of-delivery',
        icon: IconSignature,
        description: 'Signatures and photos from drop-offs',
        planned: true,
      },
    ],
  },
  {
    label: 'Purchasing & Suppliers',
    path: '/purchasing',
    icon: IconShoppingCart,
    description: 'Incoming stock from farms and manufacturers',
    planned: true,
  },
  {
    label: 'Customers',
    path: '/customers',
    icon: IconUsers,
    description: 'Wholesale (B2B) and retail (B2C) buyers',
    planned: true,
  },
  {
    label: 'Reports & Analytics',
    path: '/reports',
    icon: IconChartBar,
    description: 'Financials, yield and waste reporting',
    planned: true,
  },
];

export function flattenLeaves(items: NavItem[] = navigation): NavItem[] {
  return items.flatMap((item) => (item.children ? flattenLeaves(item.children) : [item]));
}

/** -1 when the item doesn't match; otherwise its specificity (number of matched query params). */
function matchScore(itemPath: string, pathname: string, search: string): number {
  const target = new URL(itemPath, 'http://local');
  if (target.pathname !== pathname) {
    return -1;
  }
  const current = new URLSearchParams(search);
  let score = 0;
  for (const [key, value] of target.searchParams) {
    if (current.get(key) !== value) {
      return -1;
    }
    score++;
  }
  return score;
}

function findTrail(items: NavItem[], pathname: string, search: string) {
  let best: { trail: NavItem[]; score: number } = { trail: [], score: -1 };

  const visit = (nodes: NavItem[], ancestors: NavItem[]) => {
    for (const node of nodes) {
      const trail = [...ancestors, node];
      if (node.children) {
        visit(node.children, trail);
      } else if (node.path) {
        const score = matchScore(node.path, pathname, search);
        if (score > best.score) {
          best = { trail, score };
        }
      }
    }
  };

  visit(items, []);
  return best.trail;
}

export function useActiveNavigation() {
  const { pathname, search } = useLocation();

  return useMemo(() => {
    const trail = findTrail(navigation, pathname, search);
    return { trail, active: trail.at(-1) };
  }, [pathname, search]);
}
