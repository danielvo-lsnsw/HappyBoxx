import { Button, SegmentedControl, Text, TextInput, Tooltip } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconDownload, IconSearch, IconStack2 } from '@tabler/icons-react';
import { DataTable, type DataTableColumn } from '@/components/data/DataTable';
import { EmptyState } from '@/components/data/EmptyState';
import { QueryState } from '@/components/data/QueryState';
import { StatusBadge } from '@/components/data/StatusBadge';
import { TablePagination } from '@/components/data/TablePagination';
import { DataPanel } from '@/components/page/DataPanel';
import { FilterBar } from '@/components/page/FilterBar';
import { PageHeader } from '@/components/page/PageHeader';
import { downloadCsv, toCsv } from '@/lib/csv';
import { formatDateTime, formatNumber } from '@/lib/format';
import { useUrlParams } from '@/lib/useUrlParams';
import { useStockQuery } from '../api';
import { StockDrawer } from '../components/StockDrawer';
import { getStockStatus } from '../stockStatus';
import type { StockItem } from '../types';

const columns: DataTableColumn<StockItem>[] = [
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
    key: 'onHand',
    header: 'On hand',
    align: 'right',
    width: 140,
    render: (s) => (
      <Text fw={700} size="sm" className="tabular">
        {formatNumber(s.quantityOnHand)}
      </Text>
    ),
  },
  {
    key: 'reorder',
    header: 'Reorder level',
    align: 'right',
    width: 140,
    render: (s) => <span className="tabular">{formatNumber(s.reorderLevel)}</span>,
  },
  {
    key: 'status',
    header: 'Status',
    width: 150,
    render: (s) => {
      const status = getStockStatus(s);
      return <StatusBadge tone={status.tone} label={status.label} />;
    },
  },
  {
    key: 'updated',
    header: 'Last updated',
    width: 200,
    render: (s) => (
      <Text size="sm" c="dimmed">
        {formatDateTime(s.updatedAtUtc)}
      </Text>
    ),
  },
];

export function StockPage() {
  const url = useUrlParams();
  const page = url.getNumber('page', 1);
  const pageSize = url.getNumber('pageSize', 20);
  const sku = url.get('sku') ?? '';
  const lowStockOnly = url.get('lowStockOnly') === 'true';
  const productId = url.get('productId');
  const [debouncedSku] = useDebouncedValue(sku.trim(), 300);

  const stock = useStockQuery({
    page,
    pageSize,
    sku: debouncedSku || undefined,
    lowStockOnly: lowStockOnly || undefined,
  });

  const hasActiveFilters = !!sku || lowStockOnly;

  const exportCsv = () =>
    downloadCsv(
      'stock-levels.csv',
      toCsv(stock.data?.items ?? [], [
        { header: 'SKU', value: (s) => s.sku },
        { header: 'On hand', value: (s) => s.quantityOnHand },
        { header: 'Reorder level', value: (s) => s.reorderLevel },
        { header: 'Status', value: (s) => getStockStatus(s).label },
        { header: 'Updated (UTC)', value: (s) => s.updatedAtUtc },
      ]),
    );

  return (
    <>
      <PageHeader
        title="Stock Levels"
        description="On-hand quantities across the warehouse. Click a row to receive or remove stock."
        actions={
          <Tooltip label="Exports the rows currently shown">
            <Button
              variant="default"
              leftSection={<IconDownload size={18} />}
              onClick={exportCsv}
              disabled={!stock.data?.items.length}
            >
              Export
            </Button>
          </Tooltip>
        }
      />

      <FilterBar
        hasActiveFilters={hasActiveFilters}
        onReset={() => url.set({ sku: undefined, lowStockOnly: undefined, page: undefined })}
      >
        <TextInput
          aria-label="Search SKU"
          placeholder="Search SKU"
          leftSection={<IconSearch size={18} />}
          value={sku}
          onChange={(e) => url.set({ sku: e.currentTarget.value, page: undefined })}
          w={240}
        />
        <SegmentedControl
          aria-label="Stock filter"
          value={lowStockOnly ? 'low' : 'all'}
          onChange={(value) =>
            url.set({ lowStockOnly: value === 'low' ? true : undefined, page: undefined })
          }
          data={[
            { value: 'all', label: 'All items' },
            { value: 'low', label: 'Low / out of stock' },
          ]}
        />
      </FilterBar>

      <DataPanel>
        <QueryState
          query={stock}
          isEmpty={(data) => data.items.length === 0}
          empty={
            <EmptyState
              icon={IconStack2}
              title={
                hasActiveFilters ? 'No stock items match your filters' : 'No tracked stock yet'
              }
              description={
                hasActiveFilters
                  ? undefined
                  : 'Open a product and choose "Start tracking" to record its stock level.'
              }
            />
          }
        >
          {(data) => (
            <>
              <DataTable
                columns={columns}
                rows={data.items}
                getRowId={(s) => s.productId}
                selectedId={productId}
                onRowClick={(s) => url.set({ productId: s.productId }, { push: true })}
              />
              <TablePagination
                page={data.page}
                pageSize={data.pageSize}
                totalCount={data.totalCount}
                totalPages={data.totalPages}
                onPageChange={(value) => url.set({ page: value })}
                onPageSizeChange={(value) => url.set({ pageSize: value, page: undefined })}
              />
            </>
          )}
        </QueryState>
      </DataPanel>

      <StockDrawer productId={productId} onClose={() => url.set({ productId: undefined })} />
    </>
  );
}
