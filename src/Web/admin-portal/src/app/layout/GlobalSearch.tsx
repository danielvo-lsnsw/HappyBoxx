import { Badge, Group, Kbd, Text, UnstyledButton } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { Spotlight, spotlight } from '@mantine/spotlight';
import { IconPackage, IconSearch } from '@tabler/icons-react';
import { useState } from 'react';
import { useNavigate } from 'react-router';
import { flattenLeaves } from '@/app/navigation';
import { useProductsQuery } from '@/features/products/api';
import { formatCurrency } from '@/lib/format';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';

const pages = flattenLeaves().filter((item) => item.path);

function matchesQuery(query: string, ...values: (string | undefined)[]) {
  const q = query.toLowerCase();
  return values.some((value) => value?.toLowerCase().includes(q));
}

export function GlobalSearchTrigger() {
  const { t } = useUiLanguage();

  return (
    <UnstyledButton
      className="search-trigger"
      onClick={spotlight.open}
      aria-label={t('Open search')}
    >
      <Group gap="xs" wrap="nowrap">
        <IconSearch size={18} stroke={1.75} />
        <Text size="sm" c="dimmed" truncate>
          {t('Search SKUs, products, orders, customers…')}
        </Text>
      </Group>
      <Kbd size="xs" visibleFrom="md">
        Ctrl + K
      </Kbd>
    </UnstyledButton>
  );
}

/** Global Ctrl/⌘+K search across pages and catalog data. */
export function GlobalSearch() {
  const { t } = useUiLanguage();
  const navigate = useNavigate();
  const [query, setQuery] = useState('');
  const [debouncedQuery] = useDebouncedValue(query.trim(), 250);
  const searchProducts = debouncedQuery.length >= 2;
  const products = useProductsQuery(
    { search: debouncedQuery, pageSize: 6 },
    { enabled: searchProducts },
  );

  const pageResults = query.trim()
    ? pages.filter((p) =>
        matchesQuery(
          query.trim(),
          p.label,
          p.description,
          t(p.label),
          p.description ? t(p.description) : undefined,
        ),
      )
    : pages.filter((p) => !p.planned);
  const productResults = searchProducts ? (products.data?.items ?? []) : [];

  const go = (path: string) => void navigate(path);

  return (
    <Spotlight.Root
      query={query}
      onQueryChange={setQuery}
      shortcut={['mod + K', '/']}
      scrollable
      maxHeight={480}
    >
      <Spotlight.Search
        placeholder={t('Search SKUs, products, pages…')}
        leftSection={<IconSearch size={20} stroke={1.75} />}
      />
      <Spotlight.ActionsList>
        {productResults.length > 0 && (
          <Spotlight.ActionsGroup label={t('Products')}>
            {productResults.map((product) => (
              <Spotlight.Action
                key={product.id}
                label={product.name}
                description={`${product.sku} · ${product.categoryName}`}
                leftSection={<IconPackage size={20} stroke={1.75} />}
                rightSection={
                  <Text size="sm" className="tabular">
                    {formatCurrency(product.price)}
                  </Text>
                }
                onClick={() => go(`/inventory/products?productId=${product.id}`)}
              />
            ))}
          </Spotlight.ActionsGroup>
        )}
        {pageResults.length > 0 && (
          <Spotlight.ActionsGroup label={t('Go to')}>
            {pageResults.map((page) => {
              const PageIcon = page.icon;
              return (
                <Spotlight.Action
                  key={page.path}
                  label={t(page.label)}
                  description={page.description ? t(page.description) : undefined}
                  leftSection={PageIcon && <PageIcon size={20} stroke={1.75} />}
                  rightSection={
                    page.planned && (
                      <Badge size="xs" variant="light" color="gray">
                        {t('Soon')}
                      </Badge>
                    )
                  }
                  onClick={() => page.path && go(page.path)}
                />
              );
            })}
          </Spotlight.ActionsGroup>
        )}
        {productResults.length === 0 && pageResults.length === 0 && (
          <Spotlight.Empty>
            {products.isFetching
              ? t('Searching…')
              : t('No results. Order, batch and customer search arrives with those modules.')}
          </Spotlight.Empty>
        )}
      </Spotlight.ActionsList>
    </Spotlight.Root>
  );
}
