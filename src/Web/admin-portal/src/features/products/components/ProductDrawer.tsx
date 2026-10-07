import { Alert, Button, Divider, Skeleton, Stack, Text, Title } from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';
import { ActiveBadge } from '@/components/data/StatusBadge';
import { DetailDrawer } from '@/components/page/DetailDrawer';
import { useAllCategoriesQuery } from '@/features/categories/api';
import type { Category } from '@/features/categories/types';
import { notifySuccess } from '@/lib/forms';
import { useCreateProductMutation, useProductQuery, useUpdateProductMutation } from '../api';
import type { Product } from '../types';
import { ProductForm, type ProductFormValues } from './ProductForm';
import { ProductStockSection } from './ProductStockSection';

const FORM_ID = 'product-form';

const emptyProductForm = (categoryId?: string): ProductFormValues => ({
  sku: '',
  name: '',
  description: '',
  categoryId: categoryId ?? null,
  unit: 'Kilogram',
  price: '',
  isActive: true,
});

interface ProductDrawerProps {
  productId?: string;
  creating: boolean;
  defaultCategoryId?: string;
  onClose: () => void;
  onCreated: (product: Product) => void;
}

export function ProductDrawer({
  productId,
  creating,
  defaultCategoryId,
  onClose,
  onCreated,
}: ProductDrawerProps) {
  const opened = creating || !!productId;
  const product = useProductQuery(creating ? undefined : productId);
  const categories = useAllCategoriesQuery();
  const categoryList = categories.data?.items ?? [];

  if (creating) {
    return (
      <DetailDrawer
        opened={opened}
        onClose={onClose}
        title="New product"
        subtitle="Add an item to the catalog"
        footer={<FormFooter onCancel={onClose} label="Create product" />}
      >
        <CreateProduct
          categories={categoryList}
          defaultCategoryId={defaultCategoryId}
          onCreated={onCreated}
        />
      </DetailDrawer>
    );
  }

  const data = product.data;

  return (
    <DetailDrawer
      opened={opened}
      onClose={onClose}
      title={data?.name ?? 'Product'}
      subtitle={data && `${data.sku} · ${data.categoryName}`}
      badge={data && <ActiveBadge isActive={data.isActive} />}
      footer={data && <FormFooter onCancel={onClose} label="Save changes" />}
    >
      {product.isPending && <Skeleton height={320} radius="md" />}
      {product.isError && (
        <Alert color="red" title="Couldn't load product">
          {product.error.message}
        </Alert>
      )}
      {data && (
        <Stack gap="lg">
          <ProductStockSection product={data} />
          <Divider label="Details" labelPosition="left" />
          <EditProduct key={data.id} product={data} categories={categoryList} />
          <Divider label="Batches, supplier & price history" labelPosition="left" />
          <Alert variant="light" color="gray" icon={<IconInfoCircle />}>
            <Text size="sm">
              Batch numbers, supplier details, storage requirements and price history will appear
              here once the Purchasing module is live.
            </Text>
          </Alert>
        </Stack>
      )}
    </DetailDrawer>
  );
}

function FormFooter({ onCancel, label }: { onCancel: () => void; label: string }) {
  return (
    <>
      <Button variant="default" onClick={onCancel}>
        Cancel
      </Button>
      <Button type="submit" form={FORM_ID}>
        {label}
      </Button>
    </>
  );
}

function CreateProduct({
  categories,
  defaultCategoryId,
  onCreated,
}: {
  categories: Category[];
  defaultCategoryId?: string;
  onCreated: (product: Product) => void;
}) {
  const createProduct = useCreateProductMutation();

  const handleSubmit = async (values: ProductFormValues) => {
    const created = await createProduct.mutateAsync({
      sku: values.sku.trim(),
      name: values.name.trim(),
      description: values.description.trim() || null,
      categoryId: values.categoryId ?? '',
      unit: values.unit,
      price: Number(values.price),
    });
    notifySuccess(`${created.name} was added to the catalog`, 'Product created');
    onCreated(created);
  };

  return (
    <Stack gap="md">
      <Title order={5}>Product details</Title>
      <ProductForm
        id={FORM_ID}
        mode="create"
        initialValues={emptyProductForm(defaultCategoryId)}
        categories={categories}
        onSubmit={handleSubmit}
      />
    </Stack>
  );
}

function EditProduct({ product, categories }: { product: Product; categories: Category[] }) {
  const updateProduct = useUpdateProductMutation(product.id);

  const handleSubmit = async (values: ProductFormValues) => {
    const updated = await updateProduct.mutateAsync({
      name: values.name.trim(),
      description: values.description.trim() || null,
      categoryId: values.categoryId ?? '',
      unit: values.unit,
      price: Number(values.price),
      isActive: values.isActive,
    });
    notifySuccess(`${updated.name} was updated`);
  };

  return (
    <ProductForm
      id={FORM_ID}
      mode="edit"
      categories={categories}
      initialValues={{
        sku: product.sku,
        name: product.name,
        description: product.description ?? '',
        categoryId: product.categoryId,
        unit: product.unit,
        price: product.price,
        isActive: product.isActive,
      }}
      onSubmit={handleSubmit}
    />
  );
}
