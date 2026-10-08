import {
  Anchor,
  Button,
  Group,
  NumberInput,
  Paper,
  SimpleGrid,
  Skeleton,
  Stack,
  Text,
  Title,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { IconStack2 } from '@tabler/icons-react';
import { useState } from 'react';
import { Link } from 'react-router';
import { StatusBadge } from '@/components/data/StatusBadge';
import { UnitToggle } from '@/components/data/UnitToggle';
import { useCreateStockItemMutation, useStockByProductQuery } from '@/features/inventory/api';
import { getStockStatus } from '@/features/inventory/stockStatus';
import { formatDateTime } from '@/lib/format';
import { handleSubmitError, notifySuccess } from '@/lib/forms';
import { convertQuantity, formatQuantity, type UnitOfMeasure } from '@/lib/units';
import type { Product } from '../types';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { useAuth } from '@/app/auth/AuthContext';

export function ProductStockSection({ product }: { product: Product }) {
  const { t } = useUiLanguage();
  const { user } = useAuth();
  const isAdmin = user?.role === 'Admin';
  const stock = useStockByProductQuery(product.id);
  const [displayUnit, setDisplayUnit] = useState<UnitOfMeasure>(product.unit);

  if (stock.isPending) {
    return <Skeleton height={120} radius="md" />;
  }

  if (stock.isError) {
    return (
      <Text c="red" size="sm">
        {t("Couldn't load stock")}: {stock.error.message}
      </Text>
    );
  }

  if (!stock.data) {
    return isAdmin ? (
      <StartTrackingForm product={product} />
    ) : (
      <Text c="dimmed" size="sm">
        {t('Not tracked in inventory yet')}
      </Text>
    );
  }

  const item = stock.data;
  const status = getStockStatus(item);
  const show = (value: number) =>
    formatQuantity(convertQuantity(value, product.unit, displayUnit), displayUnit);

  return (
    <Paper withBorder p="md" radius="md">
      <Group justify="space-between" mb="sm">
        <Group gap="xs">
          <Title order={5}>{t('Stock')}</Title>
          <StatusBadge tone={status.tone} label={t(status.label)} />
        </Group>
        <UnitToggle baseUnit={product.unit} value={displayUnit} onChange={setDisplayUnit} />
      </Group>
      <SimpleGrid cols={2}>
        <div>
          <Text size="xs" c="dimmed" tt="uppercase" fw={600}>
            {t('On hand')}
          </Text>
          <Text size="xl" fw={700} className="tabular">
            {show(item.quantityOnHand)}
          </Text>
        </div>
        <div>
          <Text size="xs" c="dimmed" tt="uppercase" fw={600}>
            {t('Reorder level')}
          </Text>
          <Text size="xl" fw={700} className="tabular">
            {show(item.reorderLevel)}
          </Text>
        </div>
      </SimpleGrid>
      <Group justify="space-between" mt="md">
        <Text size="xs" c="dimmed">
          {t('Updated')} {formatDateTime(item.updatedAtUtc)}
        </Text>
        {isAdmin && (
          <Anchor component={Link} to={`/inventory/stock?productId=${product.id}`} size="sm">
            {t('Adjust stock →')}
          </Anchor>
        )}
      </Group>
    </Paper>
  );
}

function StartTrackingForm({ product }: { product: Product }) {
  const { t } = useUiLanguage();
  const createStock = useCreateStockItemMutation();
  const form = useForm({
    mode: 'controlled',
    initialValues: { initialQuantity: 0 as number | string, reorderLevel: 0 as number | string },
    validate: {
      initialQuantity: (v) => (v === '' || Number(v) < 0 ? t('Enter 0 or more') : null),
      reorderLevel: (v) => (v === '' || Number(v) < 0 ? t('Enter 0 or more') : null),
    },
  });

  const submit = form.onSubmit(async (values) => {
    try {
      await createStock.mutateAsync({
        productId: product.id,
        sku: product.sku,
        initialQuantity: Number(values.initialQuantity),
        reorderLevel: Number(values.reorderLevel),
      });
      notifySuccess(
        `${product.sku} ${t('is now tracked in inventory')}`,
        t('Stock tracking started'),
      );
    } catch (error) {
      handleSubmitError(form, error);
    }
  });

  return (
    <Paper withBorder p="md" radius="md">
      <form onSubmit={submit} noValidate>
        <Stack gap="sm">
          <Group gap="xs">
            <IconStack2 size={20} />
            <Title order={5}>{t('Not tracked in inventory yet')}</Title>
          </Group>
          <Text size="sm" c="dimmed">
            {t('Start tracking to record on-hand quantity and get low-stock alerts.')}
          </Text>
          <SimpleGrid cols={2}>
            <NumberInput
              label={t('Opening quantity')}
              min={0}
              decimalScale={3}
              {...form.getInputProps('initialQuantity')}
            />
            <NumberInput
              label={t('Reorder level')}
              min={0}
              decimalScale={3}
              {...form.getInputProps('reorderLevel')}
            />
          </SimpleGrid>
          <Group justify="flex-end">
            <Button type="submit" variant="light" loading={createStock.isPending}>
              {t('Start tracking')}
            </Button>
          </Group>
        </Stack>
      </form>
    </Paper>
  );
}
