import { Alert, Badge, Group, NumberInput, Paper, SimpleGrid, Stack, Table, Text, Title } from '@mantine/core';
import { PageHeader } from '@/components/page/PageHeader';
import { useReorderSuggestionsQuery } from '@/features/inventory/api';
import { formatCurrency, formatNumber } from '@/lib/format';
import { useState } from 'react';
import { useProductsQuery, useProductProfitabilityQuery } from '../api';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';

export function ProductInsightsPage() {
  const { t } = useUiLanguage();
  const [lookbackDays, setLookbackDays] = useState(30);
  const [lowMarginThresholdPct, setLowMarginThresholdPct] = useState(10);

  const reorder = useReorderSuggestionsQuery({ lookbackDays });
  const profitability = useProductProfitabilityQuery(lookbackDays, lowMarginThresholdPct);
  const products = useProductsQuery({ page: 1, pageSize: 500 });

  const productsById = new Map((products.data?.items ?? []).map((product) => [product.id, product]));
  const needThisWeek = (reorder.data ?? []).filter((item) => item.needToOrderThisWeek);
  const supplierGroups = needThisWeek.reduce(
    (groups, item) => {
      const supplier = productsById.get(item.productId)?.suppliers.find((x) => x.isDefault)?.name ?? t('No default supplier');
      if (!groups[supplier]) {
        groups[supplier] = [];
      }

      groups[supplier].push(item);
      return groups;
    },
    {} as Record<string, typeof needThisWeek>,
  );

  return (
    <Stack gap="lg">
      <PageHeader
        title={t('Product insights')}
        description={t('Reorder intelligence and profitability in one place.')}
      />

      <Paper withBorder p="md" radius="md">
        <Group grow>
          <NumberInput
            label={t('Demand lookback (days)')}
            min={7}
            max={180}
            value={lookbackDays}
            onChange={(value) => setLookbackDays(typeof value === 'number' ? value : 30)}
          />
          <NumberInput
            label={t('Low margin threshold (%)')}
            min={0}
            max={100}
            value={lowMarginThresholdPct}
            onChange={(value) => setLowMarginThresholdPct(typeof value === 'number' ? value : 10)}
          />
        </Group>
      </Paper>

      <Title order={4}>{t('Need to order this week')}</Title>
      <Paper withBorder p="md" radius="md">
        {reorder.isPending ? (
          <Text c="dimmed">{t('Loading reorder suggestions...')}</Text>
        ) : reorder.isError ? (
          <Alert color="red">{reorder.error.message}</Alert>
        ) : needThisWeek.length === 0 ? (
          <Text c="dimmed">{t('No products need ordering this week.')}</Text>
        ) : (
          <Table striped highlightOnHover>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>{t('SKU')}</Table.Th>
                <Table.Th>{t('Product')}</Table.Th>
                <Table.Th>{t('On hand')}</Table.Th>
                <Table.Th>{t('Min/Max')}</Table.Th>
                <Table.Th>{t('Avg daily demand')}</Table.Th>
                <Table.Th>{t('Reorder point')}</Table.Th>
                <Table.Th>{t('Suggested qty')}</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {needThisWeek.map((item) => {
                const product = productsById.get(item.productId);
                return (
                  <Table.Tr key={item.productId}>
                    <Table.Td>{item.sku}</Table.Td>
                    <Table.Td>{product?.name ?? '—'}</Table.Td>
                    <Table.Td>{formatNumber(item.quantityOnHand)}</Table.Td>
                    <Table.Td>{`${formatNumber(item.minLevel)} / ${formatNumber(item.maxLevel)}`}</Table.Td>
                    <Table.Td>{formatNumber(item.avgDailyDemand)}</Table.Td>
                    <Table.Td>{formatNumber(item.reorderPoint)}</Table.Td>
                    <Table.Td>
                      <Badge color={item.suggestedOrderQuantity > 0 ? 'orange' : 'gray'}>
                        {formatNumber(item.suggestedOrderQuantity)}
                      </Badge>
                    </Table.Td>
                  </Table.Tr>
                );
              })}
            </Table.Tbody>
          </Table>
        )}
      </Paper>

      <Title order={4}>{t('Supplier grouped purchase suggestions')}</Title>
      <SimpleGrid cols={{ base: 1, md: 2 }}>
        {Object.entries(supplierGroups).map(([supplier, items]) => (
          <Paper key={supplier} withBorder p="md" radius="md">
            <Group justify="space-between" mb="xs">
              <Title order={5}>{supplier}</Title>
              <Badge>{items.length}</Badge>
            </Group>
            <Stack gap={4}>
              {items.map((item) => {
                const product = productsById.get(item.productId);
                return (
                  <Text key={item.productId} size="sm">
                    {item.sku} · {product?.name ?? '—'} · {t('Order')} {formatNumber(item.suggestedOrderQuantity)}
                  </Text>
                );
              })}
            </Stack>
          </Paper>
        ))}
      </SimpleGrid>

      <Title order={4}>{t('Product profitability dashboard')}</Title>
      <Paper withBorder p="md" radius="md">
        {profitability.isPending ? (
          <Text c="dimmed">{t('Loading profitability...')}</Text>
        ) : profitability.isError ? (
          <Alert color="red">{profitability.error.message}</Alert>
        ) : (
          <Stack gap="md">
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{t('SKU')}</Table.Th>
                  <Table.Th>{t('Product')}</Table.Th>
                  <Table.Th>{t('Category')}</Table.Th>
                  <Table.Th>{t('Buy')}</Table.Th>
                  <Table.Th>{t('Near')}</Table.Th>
                  <Table.Th>{t('Far')}</Table.Th>
                  <Table.Th>{t('Near margin %')}</Table.Th>
                  <Table.Th>{t('Far margin %')}</Table.Th>
                  <Table.Th>{t('Trend')}</Table.Th>
                  <Table.Th>{t('Alerts')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {profitability.data?.products.map((item) => (
                  <Table.Tr key={item.productId}>
                    <Table.Td>{item.sku}</Table.Td>
                    <Table.Td>{item.name}</Table.Td>
                    <Table.Td>{item.categoryName}</Table.Td>
                    <Table.Td>{item.buyPrice == null ? '—' : formatCurrency(item.buyPrice)}</Table.Td>
                    <Table.Td>{item.sellPriceNear == null ? '—' : formatCurrency(item.sellPriceNear)}</Table.Td>
                    <Table.Td>{item.sellPriceFar == null ? '—' : formatCurrency(item.sellPriceFar)}</Table.Td>
                    <Table.Td>{item.nearMarginPct == null ? '—' : `${item.nearMarginPct.toFixed(2)}%`}</Table.Td>
                    <Table.Td>{item.farMarginPct == null ? '—' : `${item.farMarginPct.toFixed(2)}%`}</Table.Td>
                    <Table.Td>{item.nearMarginTrendPct == null ? '—' : `${item.nearMarginTrendPct.toFixed(2)}%`}</Table.Td>
                    <Table.Td>
                      {item.isNegativeMargin ? (
                        <Badge color="red">{t('Negative margin')}</Badge>
                      ) : item.isLowMargin ? (
                        <Badge color="yellow">{t('Low margin')}</Badge>
                      ) : (
                        <Badge color="green">{t('Healthy')}</Badge>
                      )}
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>

            <Title order={5}>{t('Category profitability heatmap')}</Title>
            <SimpleGrid cols={{ base: 1, md: 2 }}>
              {profitability.data?.categories.map((category) => {
                const severity =
                  category.negativeMarginCount > 0
                    ? 'red'
                    : category.lowMarginCount > 0
                      ? 'yellow'
                      : 'green';
                return (
                  <Paper key={category.categoryId} withBorder p="sm" radius="md">
                    <Group justify="space-between">
                      <Text fw={600}>{category.categoryName}</Text>
                      <Badge color={severity}>
                        {category.negativeMarginCount > 0
                          ? t('Risk')
                          : category.lowMarginCount > 0
                            ? t('Watch')
                            : t('Healthy')}
                      </Badge>
                    </Group>
                    <Text size="sm" c="dimmed">
                      {t('Avg near margin')}: {category.avgNearMarginPct == null ? '—' : `${category.avgNearMarginPct.toFixed(2)}%`}
                    </Text>
                    <Text size="sm" c="dimmed">
                      {t('Avg far margin')}: {category.avgFarMarginPct == null ? '—' : `${category.avgFarMarginPct.toFixed(2)}%`}
                    </Text>
                    <Text size="sm" c="dimmed">
                      {t('Low margin')}: {category.lowMarginCount} · {t('Negative')}: {category.negativeMarginCount}
                    </Text>
                  </Paper>
                );
              })}
            </SimpleGrid>
          </Stack>
        )}
      </Paper>
    </Stack>
  );
}
