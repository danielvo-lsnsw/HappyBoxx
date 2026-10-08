import { useState } from 'react';
import {
  Anchor,
  Button,
  Center,
  Container,
  Paper,
  Stack,
  Text,
  TextInput,
  Title,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { Link } from 'react-router';
import { useAuth } from '@/app/auth/AuthContext';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { httpClient } from '@/lib/api/httpClient';

interface RegistrationValues {
  displayName: string;
  phoneNumber: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  region: string;
  postalCode: string;
  country: string;
}

export function CustomerRegistrationPage() {
  const { t } = useUiLanguage();
  const { user, signUp } = useAuth();
  const [error, setError] = useState<string>();
  const [registered, setRegistered] = useState(false);
  const form = useForm<RegistrationValues>({
    initialValues: {
      displayName: '',
      phoneNumber: '',
      addressLine1: '',
      addressLine2: '',
      city: '',
      region: '',
      postalCode: '',
      country: '',
    },
    validate: {
      displayName: (value) => (value.trim() ? null : t('Name is required')),
      phoneNumber: (value) => (value.trim() ? null : t('Phone number is required')),
      addressLine1: (value) => (value.trim() ? null : t('Address is required')),
      city: (value) => (value.trim() ? null : t('City is required')),
      postalCode: (value) => (value.trim() ? null : t('Postal code is required')),
      country: (value) =>
        /^[a-z]{2}$/i.test(value.trim()) ? null : t('Enter a 2-letter country code'),
    },
  });

  const submit = form.onSubmit(async (values) => {
    setError(undefined);
    try {
      await httpClient.post('/identity/customers/register', {
        ...values,
        addressLine2: values.addressLine2.trim() || null,
        region: values.region.trim() || null,
      });
      setRegistered(true);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : t('Registration failed'));
    }
  });

  const isStaff = user?.role === 'Admin' || user?.role === 'Order Creator';
  const isCustomer = user?.role === 'Customer';

  return (
    <Center mih="100dvh" p="md">
      <Container size={520} w="100%">
        <Paper withBorder p="xl" radius="md">
          <Stack gap="md">
            <div>
              <Title order={2}>{t('Customer registration')}</Title>
              <Text c="dimmed" size="sm" mt="xs">
                {t('Your email is verified by Entra before this profile can be activated.')}
              </Text>
            </div>

            {registered ? (
              <>
                <Text>{t('Your customer account is active.')}</Text>
                <Text size="sm" c="dimmed">
                  {user?.email}
                </Text>
              </>
            ) : !user ? (
              <Button onClick={signUp}>{t('Verify email and continue')}</Button>
            ) : isStaff ? (
              <Text c="red">{t('Staff accounts cannot register as customers.')}</Text>
            ) : isCustomer && !user.isDevelopmentIdentity ? (
              <Text>{t('Your customer account is already active.')}</Text>
            ) : (
              <form onSubmit={submit} noValidate>
                <Stack gap="sm">
                  <Text size="sm" fw={600}>
                    {user.email}
                  </Text>
                  <TextInput
                    label={t('Full name')}
                    required
                    {...form.getInputProps('displayName')}
                  />
                  <TextInput
                    label={t('Phone number')}
                    type="tel"
                    required
                    {...form.getInputProps('phoneNumber')}
                  />
                  <TextInput
                    label={t('Address line 1')}
                    required
                    {...form.getInputProps('addressLine1')}
                  />
                  <TextInput label={t('Address line 2')} {...form.getInputProps('addressLine2')} />
                  <TextInput label={t('City')} required {...form.getInputProps('city')} />
                  <TextInput label={t('State / Region')} {...form.getInputProps('region')} />
                  <TextInput
                    label={t('Postal code')}
                    required
                    {...form.getInputProps('postalCode')}
                  />
                  <TextInput
                    label={t('Country code')}
                    placeholder="US"
                    required
                    {...form.getInputProps('country')}
                  />
                  {error && (
                    <Text role="alert" c="red">
                      {error}
                    </Text>
                  )}
                  <Button type="submit">{t('Create customer account')}</Button>
                </Stack>
              </form>
            )}

            <Anchor component={Link} to="/">
              {t('Staff sign-in')}
            </Anchor>
          </Stack>
        </Paper>
      </Container>
    </Center>
  );
}
