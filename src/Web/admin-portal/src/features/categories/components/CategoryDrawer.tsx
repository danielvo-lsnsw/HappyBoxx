import { Alert, Button, Skeleton, Stack, Switch, Textarea, TextInput } from '@mantine/core';
import { useForm } from '@mantine/form';
import { ActiveBadge } from '@/components/data/StatusBadge';
import { DetailDrawer } from '@/components/page/DetailDrawer';
import { formatDateTime } from '@/lib/format';
import { handleSubmitError, notifySuccess } from '@/lib/forms';
import { useCategoryQuery, useCreateCategoryMutation, useUpdateCategoryMutation } from '../api';
import type { Category } from '../types';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';

const FORM_ID = 'category-form';

interface CategoryDrawerProps {
  categoryId?: string;
  creating: boolean;
  onClose: () => void;
}

export function CategoryDrawer({ categoryId, creating, onClose }: CategoryDrawerProps) {
  const { t } = useUiLanguage();
  const category = useCategoryQuery(creating ? undefined : categoryId);
  const data = category.data;

  const footer = (creating || data) && (
    <>
      <Button variant="default" onClick={onClose}>
        {t('Cancel')}
      </Button>
      <Button type="submit" form={FORM_ID}>
        {t(creating ? 'Create category' : 'Save changes')}
      </Button>
    </>
  );

  return (
    <DetailDrawer
      opened={creating || !!categoryId}
      onClose={onClose}
      title={creating ? t('New category') : (data?.name ?? t('Category'))}
      subtitle={
        !creating && data ? `${t('Last updated')} ${formatDateTime(data.updatedAtUtc)}` : undefined
      }
      badge={!creating && data && <ActiveBadge isActive={data.isActive} />}
      footer={footer}
    >
      {creating ? (
        <CategoryForm onDone={onClose} />
      ) : category.isPending ? (
        <Skeleton height={200} radius="md" />
      ) : category.isError ? (
        <Alert color="red" title={t("Couldn't load category")}>
          {category.error.message}
        </Alert>
      ) : (
        <CategoryForm key={category.data.id} category={category.data} onDone={onClose} />
      )}
    </DetailDrawer>
  );
}

function CategoryForm({ category, onDone }: { category?: Category; onDone: () => void }) {
  const { t } = useUiLanguage();
  const createCategory = useCreateCategoryMutation();
  const updateCategory = useUpdateCategoryMutation(category?.id ?? '');
  const form = useForm({
    mode: 'controlled',
    initialValues: {
      name: category?.name ?? '',
      description: category?.description ?? '',
      isActive: category?.isActive ?? true,
    },
    validate: { name: (value) => (value.trim() ? null : t('Name is required')) },
  });

  const submit = form.onSubmit(async (values) => {
    const request = {
      name: values.name.trim(),
      description: values.description.trim() || null,
    };
    try {
      if (category) {
        await updateCategory.mutateAsync({ ...request, isActive: values.isActive });
        notifySuccess(`${request.name} ${t('was updated')}`);
      } else {
        await createCategory.mutateAsync(request);
        notifySuccess(`${request.name} ${t('was created')}`, t('Category created'));
      }
      onDone();
    } catch (error) {
      handleSubmitError(form, error, { 'Category.NameAlreadyExists': 'name' });
    }
  });

  return (
    <form id={FORM_ID} onSubmit={submit} noValidate>
      <Stack gap="md">
        <TextInput
          label={t('Name')}
          placeholder={t('e.g. Leafy greens')}
          required
          {...form.getInputProps('name')}
        />
        <Textarea
          label={t('Description')}
          autosize
          minRows={2}
          maxRows={5}
          {...form.getInputProps('description')}
        />
        {category && (
          <Switch
            size="md"
            label={t('Active')}
            description={t('Inactive categories are hidden from buyers')}
            {...form.getInputProps('isActive', { type: 'checkbox' })}
          />
        )}
      </Stack>
    </form>
  );
}
