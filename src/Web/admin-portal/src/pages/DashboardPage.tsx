import {
  Anchor,
  Badge,
  Card,
  Group,
  SimpleGrid,
  Skeleton,
  Stack,
  Text,
  ThemeIcon,
  Title,
  UnstyledButton,
} from '@mantine/core';
import {
  IconAlertTriangle,
  IconCategory,
  IconChecklist,
  IconHandGrab,
  IconPackage,
  IconPackages,
  IconStack2,
  IconTruckDelivery,
  type Icon,
} from '@tabler/icons-react';
import type { UseQueryResult } from '@tanstack/react-query';
import { Link } from 'react-router';
import { DataTable } from '@/components/data/DataTable';
import { StatusBadge } from '@/components/data/StatusBadge';
import { PageHeader } from '@/components/page/PageHeader';
import { useCategoriesQuery } from '@/features/categories/api';
import { useStockQuery } from '@/features/inventory/api';
import { getStockStatus } from '@/features/inventory/stockStatus';
import { useProductsQuery } from '@/features/products/api';
import type { PagedResult } from '@/lib/api/httpClient';
import { formatNumber } from '@/lib/format';

interface KpiCardProps {
  label: string;
  query: UseQueryResult<PagedResult<unknown>>;
  icon: Icon;
  color: string;
  to: string;
  hint: string;
}

function KpiCard({ label, query, icon: KpiIcon, color, to, hint }: KpiCardProps) {
  return (
    <UnstyledButton component={Link} to={to} className="kpi-card">
      <Card withBorder radius="md" p="lg" h="100%">
        <Group justify="space-between" align="flex-start" wrap="nowrap">
          <Stack gap={4}>
            <Text size="sm" c="dimmed" fw={600} tt="uppercase">
              {label}
            </Text>
            {query.isPending ? (
              <Skeleton height={36} width={80} />
            ) : (
              <Text fz={32} fw={800} lh={1.1} className="tabular">
                {query.isError ? '—' : formatNumber(query.data.totalCount)}
              </Text>
            )}
            <Text size="xs" c="dimmed">
              {hint}
            </Text>
          </Stack>
          <ThemeIcon color={color} variant="light" size={48} radius="md">
            <KpiIcon size={26} stroke={1.75} />
          </ThemeIcon>
        </Group>
      </Card>
    </UnstyledButton>
  );
}

const pipeline: { label: string; icon: Icon }[] = [
  { label: 'New', icon: IconChecklist },
  { label: 'Picking', icon: IconHandGrab },
  { label: 'Packed', icon: IconPackage },
  { label: 'Out for delivery', icon: IconTruckDelivery },
];

export function DashboardPage() {
  const products = useProductsQuery({ pageSize: 1 });
  const categories = useCategoriesQuery({ pageSize: 1, isActive: true });
  const stock = useStockQuery({ pageSize: 1 });
  const lowStock = useStockQuery({ lowStockOnly: true, pageSize: 5 });

  return (
    <>
      <PageHeader title="Dashboard" description="Today's overview of the warehouse." />

      <SimpleGrid cols={{ base: 1, xs: 2, lg: 4 }} spacing="md">
        <KpiCard
          label="Products"
          query={products}
          icon={IconPackages}
          color="green"
          to="/inventory/products"
          hint="Items in the catalog"
        />
        <KpiCard
          label="Active categories"
          query={categories}
          icon={IconCategory}
          color="blue"
          to="/inventory/categories"
          hint="Vegetables, fruits, containers…"
        />
        <KpiCard
          label="Tracked stock"
          query={stock}
          icon={IconStack2}
          color="teal"
          to="/inventory/stock"
          hint="Products with stock levels"
        />
        <KpiCard
          label="Stock alerts"
          query={lowStock}
          icon={IconAlertTriangle}
          color="yellow"
          to="/inventory/stock?lowStockOnly=true"
          hint="Low or out of stock"
        />
      </SimpleGrid>

      <SimpleGrid cols={{ base: 1, lg: 2 }} spacing="md" mt="md">
        <Card withBorder radius="md" p={0}>
          <Group justify="space-between" p="md">
            <Title order={4}>Needs restocking</Title>
            <Anchor component={Link} to="/inventory/stock?lowStockOnly=true" size="sm">
              View all
            </Anchor>
          </Group>
          {lowStock.isPending ? (
            <Skeleton height={200} m="md" />
          ) : lowStock.data?.items.length ? (
            <DataTable
              minWidth={360}
              rows={lowStock.data.items}
              getRowId={(s) => s.id}
              columns={[
                {
                  key: 'sku',
                  header: 'SKU',
                  render: (s) => (
                    <Text ff="monospace" fw={600} size="sm">
                      {s.sku}
                    </Text>
                  ),
                },
                {
                  key: 'qty',
                  header: 'On hand',
                  align: 'right',
                  render: (s) => <span className="tabular">{formatNumber(s.quantityOnHand)}</span>,
                },
                {
                  key: 'status',
                  header: 'Status',
                  render: (s) => {
                    const status = getStockStatus(s);
                    return <StatusBadge tone={status.tone} label={status.label} />;
                  },
                },
              ]}
            />
          ) : (
            <Text c="dimmed" size="sm" px="md" pb="md">
              All tracked items are above their reorder level.
            </Text>
          )}
        </Card>

        <Card withBorder radius="md">
          <Group justify="space-between" mb="md">
            <Title order={4}>Order pipeline</Title>
            <Badge variant="light" color="gray">
              Coming soon
            </Badge>
          </Group>
          <SimpleGrid cols={{ base: 2, sm: 4 }} spacing="sm">
            {pipeline.map(({ label, icon: StageIcon }) => (
              <Card key={label} withBorder radius="md" p="md" ta="center">
                <ThemeIcon variant="light" color="gray" size="lg" mx="auto" mb={6}>
                  <StageIcon size={20} />
                </ThemeIcon>
                <Text fz={24} fw={700} c="dimmed">
                  —
                </Text>
                <Text size="sm" c="dimmed">
                  {label}
                </Text>
              </Card>
            ))}
          </SimpleGrid>
          <Text size="sm" c="dimmed" mt="md">
            Live order counts and a Kanban board arrive with the Orders module.
          </Text>
        </Card>
      </SimpleGrid>
    </>
  );
}
