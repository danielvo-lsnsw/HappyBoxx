import { Button, SegmentedControl, Text, TextInput } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconPlus, IconSearch, IconUsers } from '@tabler/icons-react';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { DataTable, type DataTableColumn } from '@/components/data/DataTable';
import { EmptyState } from '@/components/data/EmptyState';
import { QueryState } from '@/components/data/QueryState';
import { ActiveBadge } from '@/components/data/StatusBadge';
import { TablePagination } from '@/components/data/TablePagination';
import { DataPanel } from '@/components/page/DataPanel';
import { FilterBar } from '@/components/page/FilterBar';
import { PageHeader } from '@/components/page/PageHeader';
import { formatDateTime } from '@/lib/format';
import { useUrlParams } from '@/lib/useUrlParams';
import { useCustomersQuery } from '../api';
import { CustomerDrawer } from '../components/CustomerDrawer';
import type { Customer } from '../types';

const columns: DataTableColumn<Customer>[] = [
  {
    key: 'name',
    header: 'Customer',
    render: (customer) => (
      <Text fw={600} size="sm">
        {customer.name}
      </Text>
    ),
  },
  {
    key: 'type',
    header: 'Customer type',
    render: (customer) => customer.customerType ?? 'Other',
  },
  {
    key: 'phone',
    header: 'Phone',
    render: (customer) => customer.phone ?? '—',
  },
  {
    key: 'email',
    header: 'Email',
    render: (customer) => customer.email ?? '—',
  },
  {
    key: 'status',
    header: 'Status',
    width: 120,
    render: (customer) => <ActiveBadge isActive={customer.isActive} />,
  },
  {
    key: 'updatedAt',
    header: 'Last updated',
    width: 210,
    render: (customer) => formatDateTime(customer.updatedAtUtc),
  },
];

export function CustomersPage() {
  const { t } = useUiLanguage();
  const url = useUrlParams();
  const page = url.getNumber('page', 1);
  const pageSize = url.getNumber('pageSize', 20);
  const search = url.get('search') ?? '';
  const status = url.get('status') ?? 'active';
  const customerId = url.get('customerId');
  const creating = url.get('create') === '1';
  const [debouncedSearch] = useDebouncedValue(search.trim(), 300);

  const customers = useCustomersQuery({
    page,
    pageSize,
    search: debouncedSearch || undefined,
    isActive: status === 'all' ? undefined : status === 'active',
  });

  const hasActiveFilters = !!search || status !== 'active';

  return (
    <>
      <PageHeader
        title={t('Customers')}
        description={t('Manage customer profiles used for invoices.')}
        actions={
          <Button leftSection={<IconPlus size={18} />} onClick={() => url.set({ create: 1, customerId: undefined }, { push: true })}>
            {t('New customer')}
          </Button>
        }
      />

      <FilterBar
        hasActiveFilters={hasActiveFilters}
        onReset={() =>
          url.set({ search: undefined, status: undefined, page: undefined, pageSize: undefined })
        }
      >
        <TextInput
          aria-label={t('Search customers')}
          placeholder={t('Search customers')}
          leftSection={<IconSearch size={18} />}
          value={search}
          onChange={(event) => url.set({ search: event.currentTarget.value, page: undefined })}
          w={300}
        />
        <SegmentedControl
          aria-label={t('Status')}
          value={status}
          onChange={(value) =>
            url.set({ status: value === 'active' ? undefined : value, page: undefined })
          }
          data={[
            { value: 'active', label: t('Active') },
            { value: 'inactive', label: t('Inactive') },
            { value: 'all', label: t('All') },
          ]}
        />
      </FilterBar>

      <DataPanel>
        <QueryState
          query={customers}
          isEmpty={(data) => data.items.length === 0}
          empty={
            <EmptyState
              icon={IconUsers}
              title={t(hasActiveFilters ? 'No customers match your filters' : 'No customers yet')}
              description={t(
                hasActiveFilters
                  ? 'Try a different search or status filter.'
                  : 'Create customers before creating invoices.',
              )}
              action={
                !hasActiveFilters && (
                  <Button onClick={() => url.set({ create: 1 }, { push: true })}>
                    {t('New customer')}
                  </Button>
                )
              }
            />
          }
        >
          {(data) => (
            <>
              <DataTable
                columns={columns.map((column) => ({
                  ...column,
                  header: typeof column.header === 'string' ? t(column.header) : column.header,
                }))}
                rows={data.items}
                getRowId={(customer) => customer.id}
                selectedId={customerId}
                onRowClick={(customer) =>
                  url.set({ customerId: customer.id, create: undefined }, { push: true })
                }
                minWidth={700}
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

      <CustomerDrawer
        customerId={customerId ?? undefined}
        creating={creating}
        onClose={() => url.set({ customerId: undefined, create: undefined })}
      />
    </>
  );
}
