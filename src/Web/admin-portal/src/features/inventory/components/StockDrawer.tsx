import {
  Alert,
  Anchor,
  Button,
  Chip,
  Group,
  NumberInput,
  Paper,
  SegmentedControl,
  SimpleGrid,
  Skeleton,
  Stack,
  Text,
  Title,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { IconMinus, IconPlus } from '@tabler/icons-react';
import { Link } from 'react-router';
import { StatusBadge } from '@/components/data/StatusBadge';
import { DetailDrawer } from '@/components/page/DetailDrawer';
import { formatDateTime, formatNumber } from '@/lib/format';
import { handleSubmitError, notifySuccess } from '@/lib/forms';
import { useAdjustStockMutation, useStockByProductQuery } from '../api';
import { getStockStatus } from '../stockStatus';
import type { StockItem } from '../types';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { useAuth } from '@/app/auth/AuthContext';

const QUICK_AMOUNTS = [1, 5, 10, 25, 50];

interface StockDrawerProps {
  productId?: string;
  onClose: () => void;
}

export function StockDrawer({ productId, onClose }: StockDrawerProps) {
  const { t } = useUiLanguage();
  const { user } = useAuth();
  const isAdmin = user?.role === 'Admin';
  const stock = useStockByProductQuery(productId);
  const item = stock.data;
  const status = item && getStockStatus(item);

  return (
    <DetailDrawer
      opened={!!productId}
      onClose={onClose}
      title={item?.sku ?? t('Stock')}
      subtitle={item && `${t('Updated')} ${formatDateTime(item.updatedAtUtc)}`}
      badge={status && <StatusBadge tone={status.tone} label={t(status.label)} />}
    >
      {stock.isPending && <Skeleton height={300} radius="md" />}
      {stock.isError && (
        <Alert color="red" title={t("Couldn't load stock")}>
          {stock.error.message}
        </Alert>
      )}
      {stock.isSuccess && !item && (
        <Alert color="yellow" title={t('Not tracked')}>
          {t("This product isn't tracked in inventory yet.")}
        </Alert>
      )}
      {item && (
        <Stack gap="lg">
          <SimpleGrid cols={2}>
            <Stat label={t('On hand')} value={formatNumber(item.quantityOnHand)} />
            <Stat label={t('Reorder level')} value={formatNumber(item.reorderLevel)} />
          </SimpleGrid>
          {isAdmin && <AdjustStockForm key={item.id} item={item} />}
          <Anchor component={Link} to={`/inventory/products?productId=${item.productId}`} size="sm">
            {t('View product details →')}
          </Anchor>
        </Stack>
      )}
    </DetailDrawer>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <Paper withBorder p="md" radius="md">
      <Text size="xs" c="dimmed" tt="uppercase" fw={600}>
        {label}
      </Text>
      <Text fz={28} fw={700} className="tabular">
        {value}
      </Text>
    </Paper>
  );
}

function AdjustStockForm({ item }: { item: StockItem }) {
  const { t } = useUiLanguage();
  const adjust = useAdjustStockMutation(item.productId);
  const form = useForm({
    mode: 'controlled',
    initialValues: { direction: 'receive', quantity: '' as number | string },
    validate: {
      quantity: (value, values) => {
        const qty = Number(value);
        if (value === '' || qty <= 0) return t('Enter a quantity greater than 0');
        if (values.direction === 'remove' && qty > item.quantityOnHand)
          return `${t('Only')} ${formatNumber(item.quantityOnHand)} ${t('on hand')}`;
        return null;
      },
    },
  });

  const receiving = form.values.direction === 'receive';

  const submit = form.onSubmit(async (values) => {
    const qty = Number(values.quantity);
    try {
      const updated = await adjust.mutateAsync({ quantityChange: receiving ? qty : -qty });
      notifySuccess(
        `${item.sku}: ${receiving ? '+' : '−'}${formatNumber(qty)} → ${formatNumber(updated.quantityOnHand)} ${t('on hand')}`,
        t('Stock updated'),
      );
      form.setFieldValue('quantity', '');
    } catch (error) {
      handleSubmitError(form, error, { 'Stock.Insufficient': 'quantity' });
    }
  });

  return (
    <Paper withBorder p="md" radius="md">
      <form onSubmit={submit} noValidate>
        <Stack gap="md">
          <Title order={5}>{t('Adjust stock')}</Title>
          <SegmentedControl
            fullWidth
            size="lg"
            data={[
              { value: 'receive', label: t('Receive (+)') },
              { value: 'remove', label: t('Remove (−)') },
            ]}
            {...form.getInputProps('direction')}
          />
          <NumberInput
            size="lg"
            label={t('Quantity')}
            min={0}
            decimalScale={3}
            placeholder="0"
            {...form.getInputProps('quantity')}
          />
          <Group gap="xs">
            {QUICK_AMOUNTS.map((amount) => (
              <Chip
                key={amount}
                size="md"
                checked={Number(form.values.quantity) === amount}
                onChange={() => form.setFieldValue('quantity', amount)}
              >
                {amount}
              </Chip>
            ))}
          </Group>
          <Button
            type="submit"
            size="lg"
            fullWidth
            color={receiving ? 'brand' : 'red.8'}
            leftSection={receiving ? <IconPlus size={20} /> : <IconMinus size={20} />}
            loading={adjust.isPending}
          >
            {t(receiving ? 'Receive stock' : 'Remove stock')}
          </Button>
        </Stack>
      </form>
    </Paper>
  );
}
