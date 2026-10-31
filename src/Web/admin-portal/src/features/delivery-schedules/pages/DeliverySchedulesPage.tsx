import {
  Alert,
  Badge,
  Button,
  Checkbox,
  Group,
  Modal,
  Skeleton,
  Stack,
  Table,
  Text,
  TextInput,
  Textarea,
} from '@mantine/core';
import { useDebouncedValue, useDisclosure } from '@mantine/hooks';
import { IconChecklist, IconPlus, IconPrinter, IconSearch, IconTruckDelivery } from '@tabler/icons-react';
import { useState } from 'react';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { DataTable, type DataTableColumn } from '@/components/data/DataTable';
import { EmptyState } from '@/components/data/EmptyState';
import { QueryState } from '@/components/data/QueryState';
import { TablePagination } from '@/components/data/TablePagination';
import { DataPanel } from '@/components/page/DataPanel';
import { DetailDrawer } from '@/components/page/DetailDrawer';
import { FilterBar } from '@/components/page/FilterBar';
import { PageHeader } from '@/components/page/PageHeader';
import { useInvoicesQuery } from '@/features/invoices/api';
import type { InvoiceSummary } from '@/features/invoices/types';
import { formatCurrency, formatDate } from '@/lib/format';
import { notifyError, notifySuccess } from '@/lib/forms';
import { useUrlParams } from '@/lib/useUrlParams';
import {
  useCreateDeliveryScheduleMutation,
  useDeliveryScheduleQuery,
  useDeliverySchedulesQuery,
  useMarkAllDeliveryItemsDeliveredMutation,
  useUpdateDeliveryScheduleItemStatusMutation,
} from '../api';
import type {
  DeliverySchedule,
  DeliveryScheduleItem,
  DeliveryScheduleItemStatus,
  DeliveryScheduleSummary,
} from '../types';

function localDateTimeNowValue() {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  const hours = String(now.getHours()).padStart(2, '0');
  const minutes = String(now.getMinutes()).padStart(2, '0');
  return `${year}-${month}-${day}T${hours}:${minutes}`;
}

function toIsoUtc(localDateTime: string) {
  return new Date(localDateTime).toISOString();
}

const scheduleColumns: DataTableColumn<DeliveryScheduleSummary>[] = [
  { key: 'name', header: 'Schedule', render: (item) => <Text fw={600}>{item.name}</Text> },
  { key: 'date', header: 'Date', render: (item) => formatDate(item.scheduledDate) },
  { key: 'totals', header: 'Invoices', render: (item) => item.totalInvoices.toString() },
  {
    key: 'status',
    header: 'Progress',
    render: (item) => (
      <Group gap={6}>
        <Badge color="green" variant="light">{`D ${item.deliveredCount}`}</Badge>
        <Badge color="yellow" variant="light">{`P ${item.pendingCount}`}</Badge>
        <Badge color="red" variant="light">{`U ${item.undeliverableCount}`}</Badge>
      </Group>
    ),
  },
];

export function DeliverySchedulesPage() {
  const { t } = useUiLanguage();
  const url = useUrlParams();
  const page = url.getNumber('page', 1);
  const pageSize = url.getNumber('pageSize', 20);
  const search = url.get('search') ?? '';
  const [debouncedSearch] = useDebouncedValue(search.trim(), 300);
  const selectedScheduleId = url.get('scheduleId');
  const schedules = useDeliverySchedulesQuery(page, pageSize, debouncedSearch);
  const scheduleDetail = useDeliveryScheduleQuery(selectedScheduleId);
  const [createModalOpened, createModal] = useDisclosure(false);
  const [invoiceSearch, setInvoiceSearch] = useState('');
  const [selectedInvoiceIds, setSelectedInvoiceIds] = useState<string[]>([]);
  const [scheduleDate, setScheduleDate] = useState(() => new Date().toISOString().slice(0, 10));
  const createSchedule = useCreateDeliveryScheduleMutation();
  const invoiceCandidates = useInvoicesQuery(1, 100, invoiceSearch);
  const hasActiveFilters = !!search;

  const createFromSelection = async () => {
    if (selectedInvoiceIds.length === 0) {
      notifyError(new Error(t('Select at least one invoice')));
      return;
    }

    try {
      const schedule = await createSchedule.mutateAsync({
        invoiceIds: selectedInvoiceIds,
        scheduledDate: scheduleDate,
      });
      notifySuccess(t('Delivery schedule created'));
      setSelectedInvoiceIds([]);
      createModal.close();
      url.set({ scheduleId: schedule.id, page: undefined }, { push: true });
    } catch (error) {
      notifyError(error, t('Could not create delivery schedule'));
    }
  };

  return (
    <>
      <PageHeader
        title={t('Delivery schedules')}
        description={t('Create routes from invoices and track delivery completion.')}
        actions={
          <Button leftSection={<IconPlus size={18} />} onClick={createModal.open}>
            {t('Create schedule from invoices')}
          </Button>
        }
      />

      <FilterBar
        hasActiveFilters={hasActiveFilters}
        onReset={() => url.set({ search: undefined, page: undefined })}
      >
        <TextInput
          aria-label={t('Search schedules')}
          placeholder={t('Search schedule')}
          leftSection={<IconSearch size={18} />}
          value={search}
          onChange={(event) => url.set({ search: event.currentTarget.value, page: undefined })}
          w={280}
        />
      </FilterBar>

      <DataPanel>
        <QueryState
          query={schedules}
          isEmpty={(data) => data.items.length === 0}
          empty={
            <EmptyState
              icon={IconTruckDelivery}
              title={t('No delivery schedules yet')}
              description={t('Select invoices to build a driver checklist for delivery runs.')}
              action={<Button onClick={createModal.open}>{t('Create schedule from invoices')}</Button>}
            />
          }
        >
          {(data) => (
            <>
              <DataTable
                columns={scheduleColumns.map((column) => ({
                  ...column,
                  header: typeof column.header === 'string' ? t(column.header) : column.header,
                }))}
                rows={data.items}
                getRowId={(item) => item.id}
                selectedId={selectedScheduleId}
                onRowClick={(item) => url.set({ scheduleId: item.id }, { push: true })}
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

      <DeliveryScheduleDrawer
        schedule={scheduleDetail.data}
        loading={scheduleDetail.isPending}
        error={scheduleDetail.isError ? scheduleDetail.error.message : undefined}
        onClose={() => url.set({ scheduleId: undefined })}
      />

      <Modal opened={createModalOpened} onClose={createModal.close} title={t('Create schedule from invoices')} centered size={720}>
        <Stack>
          <Group grow>
            <TextInput
              label={t('Schedule date')}
              type="date"
              value={scheduleDate}
              onChange={(event) => setScheduleDate(event.currentTarget.value)}
            />
            <TextInput
              label={t('Search invoices')}
              placeholder={t('Invoice number or customer')}
              leftSection={<IconSearch size={16} />}
              value={invoiceSearch}
              onChange={(event) => setInvoiceSearch(event.currentTarget.value)}
            />
          </Group>
          <QueryState
            query={invoiceCandidates}
            isEmpty={(data) => data.items.length === 0}
            empty={<Text size="sm" c="dimmed">{t('No invoices match your search')}</Text>}
          >
            {(data) => (
              <Stack gap={6} mah={320} style={{ overflowY: 'auto' }}>
                {data.items.map((invoice) => (
                  <InvoicePickRow
                    key={invoice.id}
                    invoice={invoice}
                    checked={selectedInvoiceIds.includes(invoice.id)}
                    onChange={(checked) => {
                      setSelectedInvoiceIds((current) =>
                        checked
                          ? current.includes(invoice.id)
                            ? current
                            : [...current, invoice.id]
                          : current.filter((id) => id !== invoice.id),
                      );
                    }}
                  />
                ))}
              </Stack>
            )}
          </QueryState>
          <Group justify="space-between">
            <Text size="sm" c="dimmed">
              {t('Selected invoices')}: {selectedInvoiceIds.length}
            </Text>
            <Group>
              <Button variant="default" onClick={createModal.close}>
                {t('Cancel')}
              </Button>
              <Button onClick={() => void createFromSelection()} loading={createSchedule.isPending}>
                {t('Create schedule')}
              </Button>
            </Group>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}

function DeliveryScheduleDrawer({
  schedule,
  loading,
  error,
  onClose,
}: {
  schedule?: DeliverySchedule;
  loading: boolean;
  error?: string;
  onClose: () => void;
}) {
  const { t } = useUiLanguage();
  const scheduleId = schedule?.id ?? '';
  const updateItem = useUpdateDeliveryScheduleItemStatusMutation(scheduleId);
  const markAll = useMarkAllDeliveryItemsDeliveredMutation(scheduleId);
  const [itemModalOpened, itemModal] = useDisclosure(false);
  const [allModalOpened, allModal] = useDisclosure(false);
  const [activeItem, setActiveItem] = useState<DeliveryScheduleItem>();
  const [itemAction, setItemAction] = useState<DeliveryScheduleItemStatus>('Delivered');
  const [deliveredAtLocal, setDeliveredAtLocal] = useState(localDateTimeNowValue());
  const [reason, setReason] = useState('');

  const printSchedule = () => {
    document.body.classList.add('printing-delivery-schedule');
    window.print();
    document.body.classList.remove('printing-delivery-schedule');
  };

  const openItemModal = (item: DeliveryScheduleItem, action: DeliveryScheduleItemStatus) => {
    setActiveItem(item);
    setItemAction(action);
    setDeliveredAtLocal(localDateTimeNowValue());
    setReason(item.undeliveredReason ?? '');
    itemModal.open();
  };

  const submitItemUpdate = async () => {
    if (!schedule || !activeItem) {
      return;
    }

    if (itemAction === 'Undeliverable' && !reason.trim()) {
      notifyError(new Error(t('Reason is required for undeliverable orders')));
      return;
    }

    try {
      await updateItem.mutateAsync({
        itemId: activeItem.id,
        status: itemAction,
        deliveredAtUtc: itemAction === 'Delivered' ? toIsoUtc(deliveredAtLocal) : undefined,
        reasonNote: itemAction === 'Undeliverable' ? reason.trim() : undefined,
      });
      notifySuccess(t('Delivery status updated'));
      itemModal.close();
    } catch (error) {
      notifyError(error, t('Could not update delivery status'));
    }
  };

  const submitMarkAll = async () => {
    if (!schedule) {
      return;
    }

    try {
      await markAll.mutateAsync({ deliveredAtUtc: toIsoUtc(deliveredAtLocal) });
      notifySuccess(t('All invoices marked delivered'));
      allModal.close();
    } catch (error) {
      notifyError(error, t('Could not mark all delivered'));
    }
  };

  return (
    <>
      <DetailDrawer
        opened={!!schedule}
        onClose={onClose}
        title={schedule?.name ?? t('Delivery schedule')}
        subtitle={schedule && `${formatDate(schedule.scheduledDate)} · ${schedule.items.length} ${t('invoices')}`}
        drawerSize={1080}
        footer={
          schedule && (
            <>
              <Button variant="default" onClick={onClose}>
                {t('Close')}
              </Button>
              <Button
                variant="default"
                leftSection={<IconChecklist size={16} />}
                onClick={() => {
                  setDeliveredAtLocal(localDateTimeNowValue());
                  allModal.open();
                }}
              >
                {t('Tick all delivered')}
              </Button>
              <Button leftSection={<IconPrinter size={16} />} onClick={printSchedule}>
                {t('Print list')}
              </Button>
            </>
          )
        }
      >
        {loading && <Skeleton height={300} radius="md" />}
        {error && (
          <Alert color="red" title={t("Couldn't load delivery schedule")}>
            {error}
          </Alert>
        )}
        {schedule && (
          <>
            <Table withTableBorder withColumnBorders>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{t('Invoice number')}</Table.Th>
                  <Table.Th>{t('Customer')}</Table.Th>
                  <Table.Th>{t('Address')}</Table.Th>
                  <Table.Th>{t('Phone')}</Table.Th>
                  <Table.Th>{t('Total (incl. GST)')}</Table.Th>
                  <Table.Th>{t('Status')}</Table.Th>
                  <Table.Th>{t('Delivered time')}</Table.Th>
                  <Table.Th>{t('Reason note')}</Table.Th>
                  <Table.Th>{t('Actions')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {schedule.items.map((item) => (
                  <Table.Tr key={item.id}>
                    <Table.Td>{item.invoiceNumber}</Table.Td>
                    <Table.Td>{item.customerName}</Table.Td>
                    <Table.Td>{item.customerAddress}</Table.Td>
                    <Table.Td>{item.customerPhone ?? '—'}</Table.Td>
                    <Table.Td ta="right">{formatCurrency(item.total)}</Table.Td>
                    <Table.Td>
                      <Badge
                        color={
                          item.status === 'Delivered'
                            ? 'green'
                            : item.status === 'Undeliverable'
                              ? 'red'
                              : 'yellow'
                        }
                        variant="light"
                      >
                        {item.status === 'Delivered'
                          ? t('Delivered')
                          : item.status === 'Undeliverable'
                            ? t('Undeliverable')
                            : t('Pending')}
                      </Badge>
                    </Table.Td>
                    <Table.Td>{item.deliveredAtUtc ? formatDate(item.deliveredAtUtc) : '—'}</Table.Td>
                    <Table.Td>{item.undeliveredReason ?? '—'}</Table.Td>
                    <Table.Td>
                      <Group gap={6} wrap="nowrap">
                        <Button
                          size="compact-xs"
                          variant="light"
                          onClick={() => openItemModal(item, 'Delivered')}
                        >
                          {t('Tick off')}
                        </Button>
                        <Button
                          size="compact-xs"
                          color="red"
                          variant="light"
                          onClick={() => openItemModal(item, 'Undeliverable')}
                        >
                          {t('Cannot deliver')}
                        </Button>
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
            <DeliverySchedulePrintDocument schedule={schedule} />
          </>
        )}
      </DetailDrawer>

      <Modal opened={itemModalOpened} onClose={itemModal.close} title={t('Update delivery status')} centered size={500}>
        <Stack>
          {itemAction === 'Delivered' ? (
            <TextInput
              label={t('Delivered time')}
              type="datetime-local"
              value={deliveredAtLocal}
              onChange={(event) => setDeliveredAtLocal(event.currentTarget.value)}
            />
          ) : (
            <Textarea
              label={t('Reason note')}
              value={reason}
              onChange={(event) => setReason(event.currentTarget.value)}
              minRows={3}
              autosize
            />
          )}
          <Group justify="flex-end">
            <Button variant="default" onClick={itemModal.close}>
              {t('Cancel')}
            </Button>
            <Button onClick={() => void submitItemUpdate()} loading={updateItem.isPending}>
              {t('Save')}
            </Button>
          </Group>
        </Stack>
      </Modal>

      <Modal opened={allModalOpened} onClose={allModal.close} title={t('Tick all delivered')} centered size={500}>
        <Stack>
          <TextInput
            label={t('Delivered time')}
            type="datetime-local"
            value={deliveredAtLocal}
            onChange={(event) => setDeliveredAtLocal(event.currentTarget.value)}
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={allModal.close}>
              {t('Cancel')}
            </Button>
            <Button onClick={() => void submitMarkAll()} loading={markAll.isPending}>
              {t('Confirm')}
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}

function DeliverySchedulePrintDocument({ schedule }: { schedule: DeliverySchedule }) {
  const { t } = useUiLanguage();

  return (
    <Stack className="delivery-schedule-print-area" gap="lg">
      <Group justify="space-between">
        <Stack gap={2}>
          <Text fw={800} size="xl">
            {schedule.name}
          </Text>
          <Text size="sm">
            {t('Scheduled date')}: {formatDate(schedule.scheduledDate)}
          </Text>
          <Text size="sm">
            {t('Created')}: {formatDate(schedule.createdAtUtc)}
          </Text>
        </Stack>
      </Group>

      <Table withTableBorder withColumnBorders>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>{t('Done')}</Table.Th>
            <Table.Th>{t('Invoice number')}</Table.Th>
            <Table.Th>{t('Customer')}</Table.Th>
            <Table.Th>{t('Address')}</Table.Th>
            <Table.Th>{t('Phone')}</Table.Th>
            <Table.Th>{t('Total (incl. GST)')}</Table.Th>
            <Table.Th>{t('Status')}</Table.Th>
            <Table.Th>{t('Delivered time')}</Table.Th>
            <Table.Th>{t('Reason note')}</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {schedule.items.map((item) => (
            <Table.Tr key={item.id}>
              <Table.Td>{item.status === 'Delivered' ? '✓' : '□'}</Table.Td>
              <Table.Td>{item.invoiceNumber}</Table.Td>
              <Table.Td>{item.customerName}</Table.Td>
              <Table.Td>{item.customerAddress}</Table.Td>
              <Table.Td>{item.customerPhone ?? '—'}</Table.Td>
              <Table.Td ta="right">{formatCurrency(item.total)}</Table.Td>
              <Table.Td>{item.status === 'Undeliverable' ? t('Undeliverable') : item.status === 'Delivered' ? t('Delivered') : t('Pending')}</Table.Td>
              <Table.Td>{item.deliveredAtUtc ? formatDate(item.deliveredAtUtc) : '—'}</Table.Td>
              <Table.Td>{item.undeliveredReason ?? '—'}</Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>
    </Stack>
  );
}

function InvoicePickRow({
  invoice,
  checked,
  onChange,
}: {
  invoice: InvoiceSummary;
  checked: boolean;
  onChange: (checked: boolean) => void;
}) {
  return (
    <Group justify="space-between" px="xs" py={6} style={{ borderBottom: '1px solid var(--mantine-color-gray-2)' }}>
      <Checkbox checked={checked} onChange={(event) => onChange(event.currentTarget.checked)} />
      <Stack gap={2} style={{ flex: 1 }}>
        <Text size="sm" fw={600}>
          {invoice.invoiceNumber} · {invoice.customerName}
        </Text>
        <Text size="xs" c="dimmed">
          {formatDate(invoice.invoiceDate)} · {formatCurrency(invoice.total)}
        </Text>
      </Stack>
    </Group>
  );
}
