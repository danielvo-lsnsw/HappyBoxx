import { useEffect, useState } from 'react';
import {
  Button,
  Center,
  Container,
  Paper,
  Select,
  Stack,
  Text,
  TextInput,
  Title,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { useAuth } from '@/app/auth/AuthContext';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { authMode } from '@/app/auth/authConfig';
import { httpClient } from '@/lib/api/httpClient';

interface ClaimInvitationValues {
  invitationCode: string;
  displayName: string;
}

export function AcceptStaffInvitationPage() {
  const { t } = useUiLanguage();
  const { user, signUp, signOut, signInAsDevelopmentRole } = useAuth();
  const [localEmail, setLocalEmail] = useState(user?.email ?? '');
  const [localRole, setLocalRole] = useState<'Admin' | 'Order Creator'>(
    user?.role === 'Admin' ? 'Admin' : 'Order Creator',
  );
  const [invitationCode] = useState(
    () =>
      new URLSearchParams(window.location.hash.slice(1)).get('code') ??
      sessionStorage.getItem('happyboxx:staff-invitation-code') ??
      '',
  );
  const [error, setError] = useState<string>();
  const [claimed, setClaimed] = useState(false);
  const form = useForm<ClaimInvitationValues>({
    initialValues: { invitationCode, displayName: '' },
    validate: {
      invitationCode: (value) => (value.trim() ? null : t('Invitation code is required')),
      displayName: (value) => (value.trim() ? null : t('Name is required')),
    },
  });

  useEffect(() => {
    if (window.location.hash) {
      window.history.replaceState(null, '', `${window.location.pathname}${window.location.search}`);
    }
    if (invitationCode) {
      sessionStorage.setItem('happyboxx:staff-invitation-code', invitationCode);
    }
  }, [invitationCode]);

  const submit = form.onSubmit(async (values) => {
    setError(undefined);
    try {
      const claimedAccount = await httpClient.post<{ role: string }>(
        '/identity/staff/invitations/claim',
        {
          invitationCode: values.invitationCode.trim(),
          displayName: values.displayName.trim(),
        },
      );
      if (user?.isDevelopmentIdentity) {
        const assignedRole = claimedAccount.role === 'Admin' ? 'Admin' : 'Order Creator';
        signInAsDevelopmentRole(assignedRole, user.email);
      }
      sessionStorage.removeItem('happyboxx:staff-invitation-code');
      setClaimed(true);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : t('Invitation could not be claimed'));
    }
  });

  return (
    <Center mih="100dvh" p="md">
      <Container size={460} w="100%">
        <Paper withBorder p="xl" radius="md">
          <Stack gap="md">
            <div>
              <Title order={2}>{t('Accept staff invitation')}</Title>
              <Text c="dimmed" size="sm" mt="xs">
                {t('Verify the invited email with Entra before claiming the invitation.')}
              </Text>
            </div>
            {claimed ? (
              <>
                <Text>{t('Invitation claimed. Sign in again to load your assigned access.')}</Text>
                <Button onClick={signOut}>{t('Sign in again')}</Button>
              </>
            ) : !user ? (
              authMode === 'development' ? (
                <>
                  <Text size="sm" fw={600} c="orange.8">
                    {t('Local test identities only. No email or OTP is verified.')}
                  </Text>
                  <TextInput
                    label={t('Invited email')}
                    type="email"
                    required
                    value={localEmail}
                    onChange={(event) => setLocalEmail(event.currentTarget.value)}
                  />
                  <Select
                    label={t('Invited role')}
                    data={[
                      { value: 'Order Creator', label: t('Order Creator') },
                      { value: 'Admin', label: t('Admin') },
                    ]}
                    value={localRole}
                    allowDeselect={false}
                    onChange={(role) => role && setLocalRole(role as 'Admin' | 'Order Creator')}
                  />
                  <Button
                    disabled={!/^\S+@\S+\.\S+$/.test(localEmail.trim())}
                    onClick={() => signInAsDevelopmentRole(localRole, localEmail)}
                  >
                    {t('Continue as local invitee')}
                  </Button>
                </>
              ) : (
                <Button
                  onClick={() => {
                    if (invitationCode) {
                      sessionStorage.setItem('happyboxx:staff-invitation-code', invitationCode);
                    }
                    signUp();
                  }}
                >
                  {t('Verify email and continue')}
                </Button>
              )
            ) : user.role === 'Customer' ? (
              <>
                {user.isDevelopmentIdentity && (
                  <DevelopmentIdentitySwitcher
                    email={localEmail || user.email}
                    role={localRole}
                    onEmailChange={setLocalEmail}
                    onRoleChange={setLocalRole}
                    onApply={() => signInAsDevelopmentRole(localRole, localEmail)}
                  />
                )}
                <Text c="red">{t('Customer accounts cannot claim staff invitations.')}</Text>
              </>
            ) : (
              <form onSubmit={submit} noValidate>
                <Stack gap="sm">
                  <Text size="sm" fw={600}>
                    {user.email}
                  </Text>
                  {authMode === 'development' && user.isDevelopmentIdentity && (
                    <DevelopmentIdentitySwitcher
                      email={localEmail || user.email}
                      role={localRole}
                      onEmailChange={setLocalEmail}
                      onRoleChange={setLocalRole}
                      onApply={() => signInAsDevelopmentRole(localRole, localEmail)}
                    />
                  )}
                  <TextInput
                    label={t('Invitation code')}
                    required
                    {...form.getInputProps('invitationCode')}
                  />
                  <TextInput
                    label={t('Full name')}
                    required
                    {...form.getInputProps('displayName')}
                  />
                  {error && (
                    <Text role="alert" c="red">
                      {error}
                    </Text>
                  )}
                  <Button type="submit">{t('Claim invitation')}</Button>
                </Stack>
              </form>
            )}
          </Stack>
        </Paper>
      </Container>
    </Center>
  );
}

function DevelopmentIdentitySwitcher({
  email,
  role,
  onEmailChange,
  onRoleChange,
  onApply,
}: {
  email: string;
  role: 'Admin' | 'Order Creator';
  onEmailChange: (email: string) => void;
  onRoleChange: (role: 'Admin' | 'Order Creator') => void;
  onApply: () => void;
}) {
  const { t } = useUiLanguage();

  return (
    <Paper withBorder p="sm" radius="sm">
      <Stack gap="xs">
        <Text size="sm" c="orange.8">
          {t('Local test identity only')}
        </Text>
        <TextInput
          label={t('Invited email')}
          type="email"
          value={email}
          onChange={(event) => onEmailChange(event.currentTarget.value)}
        />
        <Select
          label={t('Invited role')}
          data={[
            { value: 'Order Creator', label: t('Order Creator') },
            { value: 'Admin', label: t('Admin') },
          ]}
          value={role}
          allowDeselect={false}
          onChange={(value) => value && onRoleChange(value as 'Admin' | 'Order Creator')}
        />
        <Button size="sm" disabled={!/^\S+@\S+\.\S+$/.test(email.trim())} onClick={onApply}>
          {t('Switch local identity')}
        </Button>
      </Stack>
    </Paper>
  );
}
