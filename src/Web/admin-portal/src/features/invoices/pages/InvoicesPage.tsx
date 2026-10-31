import { Button, Checkbox, Group, Text, TextInput } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconPlus, IconRoute, IconSearch } from '@tabler/icons-react';
import { useState } from 'react';
import { useNavigate } from 'react-router';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { DataTable } from '@/components/data/DataTable';
import { EmptyState } from '@/components/data/EmptyState';
import { QueryState } from '@/components/data/QueryState';
import { TablePagination } from '@/components/data/TablePagination';
import { DataPanel } from '@/components/page/DataPanel';
import { FilterBar } from '@/components/page/FilterBar';
import { PageHeader } from '@/components/page/PageHeader';
import { formatCurrency, formatDate } from '@/lib/format';
import { notifyError, notifySuccess } from '@/lib/forms';
import { useUrlParams } from '@/lib/useUrlParams';
import { useCreateDeliveryScheduleMutation } from '@/features/delivery-schedules/api';
import { useInvoicesQuery } from '../api';
import { InvoiceDrawer } from '../components/InvoiceDrawer';
import { InvoiceFormDrawer } from '../components/InvoiceFormDrawer';
import type { DuplicateInvoiceDraft } from '../types';

export function InvoicesPage() {
  const { t } = useUiLanguage();
  const navigate = useNavigate();
  const url = useUrlParams();
  const page = url.getNumber('page', 1);
  const pageSize = url.getNumber('pageSize', 20);
  const search = url.get('search') ?? '';
  const [debouncedSearch] = useDebouncedValue(search.trim(), 300);
  const [selectedInvoiceId, setSelectedInvoiceId] = useState<string>();
  const [selectedInvoiceIds, setSelectedInvoiceIds] = useState<string[]>([]);
  const [createOpened, setCreateOpened] = useState(false);
  const [draft, setDraft] = useState<DuplicateInvoiceDraft | null>(null);
  const invoices = useInvoicesQuery(page, pageSize, debouncedSearch);
  const createSchedule = useCreateDeliveryScheduleMutation();

  const hasActiveFilters = !!search;

  const createDeliverySchedule = async () => {
    if (selectedInvoiceIds.length === 0) {
      notifyError(new Error(t('Select at least one invoice')));
      return;
    }

    try {
      const schedule = await createSchedule.mutateAsync({ invoiceIds: selectedInvoiceIds });
      notifySuccess(t('Delivery schedule created'));
      setSelectedInvoiceIds([]);
      navigate(`/logistics/delivery-schedules?scheduleId=${schedule.id}`);
    } catch (error) {
      notifyError(error, t('Could not create delivery schedule'));
    }
  };

  return (
    <>
      <PageHeader
        title={t('Invoices')}
        description={t('Create and print customer tax invoices.')}
        actions={
          <Group>
            <Button
              variant="default"
              leftSection={<IconRoute size={18} />}
              disabled={selectedInvoiceIds.length === 0}
              loading={createSchedule.isPending}
              onClick={() => void createDeliverySchedule()}
            >
              {t('Create delivery schedule')} ({selectedInvoiceIds.length})
            </Button>
            <Button
              leftSection={<IconPlus size={18} />}
              onClick={() => {
                setDraft(null);
                setCreateOpened(true);
              }}
            >
              {t('New invoice')}
            </Button>
          </Group>
        }
      />

      <FilterBar
        hasActiveFilters={hasActiveFilters}
        onReset={() => url.set({ search: undefined, page: undefined })}
      >
        <TextInput
          aria-label={t('Search invoices')}
          placeholder={t('Invoice number or customer')}
          leftSection={<IconSearch size={18} />}
          value={search}
          onChange={(event) => url.set({ search: event.currentTarget.value, page: undefined })}
          w={280}
        />
      </FilterBar>

      <DataPanel>
        <QueryState
          query={invoices}
          isEmpty={(data) => data.items.length === 0}
          empty={
            <EmptyState
              icon={IconPlus}
              title={t(hasActiveFilters ? 'No invoices match your search' : 'No invoices yet')}
              description={
                hasActiveFilters
                  ? undefined
                  : t('Create an invoice to record a delivery and print a customer copy.')
              }
              action={
                !hasActiveFilters && (
                  <Button
                    onClick={() => {
                      setDraft(null);
                      setCreateOpened(true);
                    }}
                  >
                    {t('New invoice')}
                  </Button>
                )
              }
            />
          }
        >
          {(data) => (
            <>
              <DataTable
                columns={[
                  {
                    key: 'select',
                    header: (
                      <Checkbox
                        aria-label={t('Select all invoices on page')}
                        checked={
                          data.items.length > 0 &&
                          data.items.every((invoice) => selectedInvoiceIds.includes(invoice.id))
                        }
                        indeterminate={
                          data.items.some((invoice) => selectedInvoiceIds.includes(invoice.id)) &&
                          !data.items.every((invoice) => selectedInvoiceIds.includes(invoice.id))
                        }
                        onChange={(event) => {
                          const checked = event.currentTarget.checked;
                          setSelectedInvoiceIds((current) => {
                            const pageIds = data.items.map((invoice) => invoice.id);
                            if (checked) {
                              return Array.from(new Set([...current, ...pageIds]));
                            }
                            return current.filter((id) => !pageIds.includes(id));
                          });
                        }}
                      />
                    ),
                    width: 40,
                    render: (invoice) => (
                      <Checkbox
                        aria-label={`${t('Select invoice')} ${invoice.invoiceNumber}`}
                        checked={selectedInvoiceIds.includes(invoice.id)}
                        onClick={(event) => event.stopPropagation()}
                        onChange={(event) => {
                          const checked = event.currentTarget.checked;
                          setSelectedInvoiceIds((current) =>
                            checked
                              ? current.includes(invoice.id)
                                ? current
                                : [...current, invoice.id]
                              : current.filter((id) => id !== invoice.id),
                          );
                        }}
                      />
                    ),
                  },
                  {
                    key: 'number',
                    header: t('Invoice number'),
                    render: (invoice) => <Text fw={600}>{invoice.invoiceNumber}</Text>,
                  },
                  {
                    key: 'date',
                    header: t('Date'),
                    render: (invoice) => formatDate(invoice.invoiceDate),
                  },
                  {
                    key: 'customer',
                    header: t('Customer'),
                    render: (invoice) => invoice.customerName,
                  },
                  {
                    key: 'total',
                    header: t('Total (incl. GST)'),
                    align: 'right',
                    render: (invoice) => (
                      <Text fw={700} className="tabular">
                        {formatCurrency(invoice.total)}
                      </Text>
                    ),
                  },
                ]}
                rows={data.items}
                getRowId={(invoice) => invoice.id}
                onRowClick={(invoice) => setSelectedInvoiceId(invoice.id)}
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

      <InvoiceFormDrawer
        opened={createOpened}
        onClose={() => {
          setCreateOpened(false);
          setDraft(null);
        }}
        initialDraft={draft}
      />
      <InvoiceDrawer
        invoiceId={selectedInvoiceId}
        onClose={() => setSelectedInvoiceId(undefined)}
        onDuplicate={(copyDraft) => {
          setDraft(copyDraft);
          setCreateOpened(true);
          setSelectedInvoiceId(undefined);
        }}
      />
    </>
  );
}
