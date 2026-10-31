import {
  Alert,
  Badge,
  Button,
  Group,
  Select,
  Skeleton,
  Stack,
  Switch,
  Text,
  TextInput,
  Textarea,
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { useForm } from '@mantine/form';
import { IconTrash } from '@tabler/icons-react';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { ActiveBadge } from '@/components/data/StatusBadge';
import { DetailDrawer } from '@/components/page/DetailDrawer';
import { formatDateTime } from '@/lib/format';
import { handleSubmitError, notifyError, notifySuccess } from '@/lib/forms';
import {
  useCreateCustomerMutation,
  useCustomerQuery,
  useDeleteCustomerMutation,
  useUpdateCustomerMutation,
} from '../api';
import { customerTypeOptions, type Customer, type CustomerType } from '../types';

const FORM_ID = 'customer-form';

interface CustomerDrawerProps {
  customerId?: string;
  creating: boolean;
  onClose: () => void;
}

interface CustomerFormValues {
  name: string;
  address: string;
  phone: string;
  email: string;
  website: string;
  abn: string;
  customerType: CustomerType;
  isActive: boolean;
}

export function CustomerDrawer({ customerId, creating, onClose }: CustomerDrawerProps) {
  const { t } = useUiLanguage();
  const [confirmDeleteOpened, confirmDelete] = useDisclosure(false);
  const customer = useCustomerQuery(creating ? undefined : customerId);
  const deleteCustomer = useDeleteCustomerMutation();
  const data = customer.data;

  const confirmDeleteCustomer = async () => {
    if (!data) {
      return;
    }

    try {
      await deleteCustomer.mutateAsync(data.id);
      notifySuccess(t('Customer deleted'));
      confirmDelete.close();
      onClose();
    } catch (error) {
      notifyError(error, t('Could not delete customer'));
    }
  };

  const footer = (
    <>
      {!creating && data?.isActive && (
        <Button
          color="red"
          variant="light"
          leftSection={<IconTrash size={16} />}
          onClick={confirmDelete.open}
          mr="auto"
        >
          {t('Delete customer')}
        </Button>
      )}
      <Button variant="default" onClick={onClose}>
        {t('Cancel')}
      </Button>
      <Button type="submit" form={FORM_ID}>
        {t(creating ? 'Create customer' : 'Save changes')}
      </Button>
    </>
  );

  return (
    <DetailDrawer
      opened={creating || !!customerId}
      onClose={onClose}
      drawerSize={680}
      title={creating ? t('New customer') : (data?.name ?? t('Customer'))}
      subtitle={
        !creating && data ? `${t('Last updated')} ${formatDateTime(data.updatedAtUtc)}` : undefined
      }
      badge={!creating && data && <ActiveBadge isActive={data.isActive} />}
      footer={footer}
    >
      {creating ? (
        <CustomerForm onDone={onClose} />
      ) : customer.isPending ? (
        <Skeleton height={260} radius="md" />
      ) : customer.isError ? (
        <Alert color="red" title={t("Couldn't load customer")}>
          {customer.error.message}
        </Alert>
      ) : (
        <CustomerForm key={customer.data.id} customer={customer.data} onDone={onClose} />
      )}

      {confirmDeleteOpened && data && (
        <Alert color="red" variant="light" mt="md">
          <Stack gap="sm">
            <Text fw={600}>{t('Delete customer')}</Text>
            <Text size="sm">
              {t('Deleting a customer deactivates it and hides it from new invoices.')}
            </Text>
            <Group justify="flex-end">
              <Button variant="default" onClick={confirmDelete.close}>
                {t('Cancel')}
              </Button>
              <Button color="red" onClick={() => void confirmDeleteCustomer()} loading={deleteCustomer.isPending}>
                {t('Confirm delete')}
              </Button>
            </Group>
          </Stack>
        </Alert>
      )}
    </DetailDrawer>
  );
}

function CustomerForm({ customer, onDone }: { customer?: Customer; onDone: () => void }) {
  const { t } = useUiLanguage();
  const createCustomer = useCreateCustomerMutation();
  const updateCustomer = useUpdateCustomerMutation(customer?.id ?? '');
  const form = useForm<CustomerFormValues>({
    mode: 'controlled',
    initialValues: {
      name: customer?.name ?? '',
      address: customer?.address ?? '',
      phone: customer?.phone ?? '',
      email: customer?.email ?? '',
      website: customer?.website ?? '',
      abn: customer?.abn ?? '',
      customerType: (customer?.customerType as CustomerType | null) ?? 'Restaurant',
      isActive: customer?.isActive ?? true,
    },
    validate: {
      name: (value) => (value.trim() ? null : t('Customer name is required')),
      address: (value) => (value.trim() ? null : t('Customer address is required')),
      phone: (value) => (value.trim() ? null : t('Customer phone is required')),
      email: (value) =>
        value.trim() && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim())
          ? t('Enter a valid email')
          : null,
      website: (value) =>
        value.trim() && !/^(https?:\/\/)?[\w.-]+\.[a-z]{2,}(\/.*)?$/i.test(value.trim())
          ? t('Enter a valid website URL')
          : null,
      abn: (value) =>
        value.trim() && !/^\d{11}$/.test(value.trim()) ? t('ABN must be 11 digits') : null,
    },
  });

  const submit = form.onSubmit(async (values) => {
    const request = {
      name: values.name.trim(),
      address: values.address.trim(),
      phone: values.phone.trim(),
      email: values.email.trim() || null,
      website: values.website.trim() || null,
      abn: values.abn.trim() || null,
      customerType: values.customerType,
    };

    try {
      if (customer) {
        await updateCustomer.mutateAsync({ ...request, isActive: values.isActive });
        notifySuccess(t('Customer updated'));
      } else {
        await createCustomer.mutateAsync(request);
        notifySuccess(t('Customer created'));
      }
      onDone();
    } catch (error) {
      handleSubmitError(form, error, { 'Customer.NameAlreadyExists': 'name' });
    }
  });

  return (
    <form id={FORM_ID} onSubmit={submit} noValidate>
      <Stack gap="md">
        {!customer?.isActive && (
          <Badge color="gray" variant="light" w="fit-content">
            {t('Inactive')}
          </Badge>
        )}
        <TextInput label={t('Customer')} required {...form.getInputProps('name')} />
        <Textarea
          label={t('Customer address')}
          required
          autosize
          minRows={2}
          {...form.getInputProps('address')}
        />
        <TextInput label={t('Phone')} required {...form.getInputProps('phone')} />
        <Select
          label={t('Customer type')}
          allowDeselect={false}
          data={customerTypeOptions.map((type) => ({
            value: type,
            label: type === 'Other' ? t('Other (*)') : t(type),
          }))}
          {...form.getInputProps('customerType')}
        />
        <TextInput label={t('Email')} type="email" {...form.getInputProps('email')} />
        <TextInput label={t('Website')} placeholder="https://example.com" {...form.getInputProps('website')} />
        <TextInput label={t('ABN')} placeholder="12345678901" {...form.getInputProps('abn')} />
        {customer && (
          <Switch
            size="md"
            label={t('Active')}
            description={t('Inactive customers are hidden from invoice creation')}
            {...form.getInputProps('isActive', { type: 'checkbox' })}
          />
        )}
      </Stack>
    </form>
  );
}
