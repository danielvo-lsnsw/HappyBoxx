import { useState } from 'react';
import {
  Alert,
  Button,
  Code,
  CopyButton,
  Group,
  Paper,
  Select,
  SimpleGrid,
  Stack,
  Table,
  Text,
  TextInput,
  Title,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { IconCopy, IconUserPlus } from '@tabler/icons-react';
import { PageHeader } from '@/components/page/PageHeader';
import { useAuth } from '@/app/auth/AuthContext';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { formatDateTime } from '@/lib/format';
import { notifyError, notifySuccess } from '@/lib/forms';
import {
  useChangeStaffRoleMutation,
  useCreateStaffInvitationMutation,
  useDisableStaffAccountMutation,
  useRevokeStaffInvitationMutation,
  useStaffAccountsQuery,
  useStaffInvitationsQuery,
} from '@/features/identity/api';
import type { StaffRole } from '@/features/identity/types';

const roleOptions = [
  { value: 'Admin', label: 'Admin' },
  { value: 'Order Creator', label: 'Order Creator' },
];

export function StaffAccessPage() {
  const { t } = useUiLanguage();
  const { user } = useAuth();
  const [createdInvitationLink, setCreatedInvitationLink] = useState<string>();
  const [invitationEmailSent, setInvitationEmailSent] = useState(false);
  const accounts = useStaffAccountsQuery();
  const invitations = useStaffInvitationsQuery();
  const createInvitation = useCreateStaffInvitationMutation();
  const form = useForm<{ email: string; role: StaffRole }>({
    initialValues: { email: '', role: 'Order Creator' },
    validate: {
      email: (value) => (/^\S+@\S+\.\S+$/.test(value) ? null : t('Enter a valid email')),
    },
  });

  const submitInvitation = form.onSubmit(async (values) => {
    try {
      const invitation = await createInvitation.mutateAsync(values);
      setCreatedInvitationLink(invitation.invitationUrl ?? undefined);
      setInvitationEmailSent(invitation.emailSent);
      form.reset();
      notifySuccess(
        invitation.emailSent
          ? t('Invitation email sent.')
          : t('Invitation created. Share the secure link with the staff member.'),
      );
    } catch (error) {
      notifyError(error);
    }
  });

  if (user?.role !== 'Admin') {
    return <Alert color="red" title={t('Admin access required')} />;
  }

  return (
    <>
      <PageHeader
        title={t('Staff access')}
        description={t('Invite staff and manage application roles.')}
      />
      <Stack gap="lg">
        <Paper withBorder p="md" radius="md">
          <Stack gap="md">
            <Title order={4}>{t('Invite staff')}</Title>
            <form onSubmit={submitInvitation} noValidate>
              <Group align="flex-end">
                <TextInput
                  label={t('Email')}
                  type="email"
                  required
                  w={280}
                  {...form.getInputProps('email')}
                />
                <Select
                  label={t('Role')}
                  data={roleOptions}
                  allowDeselect={false}
                  w={200}
                  {...form.getInputProps('role')}
                />
                <Button
                  type="submit"
                  leftSection={<IconUserPlus size={18} />}
                  loading={createInvitation.isPending}
                >
                  {t('Create invitation')}
                </Button>
              </Group>
            </form>
            {invitationEmailSent && (
              <Alert color="green" title={t('Invitation email sent.')}>
                {t('The one-time link was sent to the invited address.')}
              </Alert>
            )}
            {createdInvitationLink && (
              <Alert color="yellow" title={t('Copy this one-time invitation link')}>
                <Group justify="space-between" wrap="wrap">
                  <Code>{createdInvitationLink}</Code>
                  <CopyButton value={createdInvitationLink} timeout={1500}>
                    {({ copied, copy }) => (
                      <Button variant="default" leftSection={<IconCopy size={16} />} onClick={copy}>
                        {copied ? t('Copied') : t('Copy invitation link')}
                      </Button>
                    )}
                  </CopyButton>
                </Group>
                <Text size="sm" mt="xs">
                  {t(
                    'The link is single-use and expires in 7 days. Send it only to the invited email address.',
                  )}
                </Text>
              </Alert>
            )}
          </Stack>
        </Paper>

        <Paper withBorder p="md" radius="md">
          <Stack gap="md">
            <Title order={4}>{t('Staff accounts')}</Title>
            {accounts.isError && <Alert color="red" title={t('Could not load staff accounts')} />}
            {accounts.data && (
              <Table.ScrollContainer minWidth={600}>
                <Table striped highlightOnHover>
                  <Table.Thead>
                    <Table.Tr>
                      <Table.Th>{t('Name')}</Table.Th>
                      <Table.Th>{t('Email')}</Table.Th>
                      <Table.Th>{t('Role')}</Table.Th>
                      <Table.Th>{t('Status')}</Table.Th>
                      <Table.Th />
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {accounts.data.map((account) => (
                      <StaffAccountRow
                        key={account.id}
                        account={account}
                        isCurrent={account.email === user.email}
                      />
                    ))}
                  </Table.Tbody>
                </Table>
              </Table.ScrollContainer>
            )}
          </Stack>
        </Paper>

        <Paper withBorder p="md" radius="md">
          <Stack gap="md">
            <Title order={4}>{t('Invitations')}</Title>
            {invitations.isError && <Alert color="red" title={t('Could not load invitations')} />}
            {invitations.data && (
              <SimpleGrid cols={{ base: 1, sm: 2 }}>
                {invitations.data.map((invitation) => (
                  <InvitationCard key={invitation.id} invitation={invitation} />
                ))}
              </SimpleGrid>
            )}
          </Stack>
        </Paper>
      </Stack>
    </>
  );
}

function StaffAccountRow({
  account,
  isCurrent,
}: {
  account: { id: string; email: string; displayName: string; role: StaffRole; status: string };
  isCurrent: boolean;
}) {
  const { t } = useUiLanguage();
  const changeRole = useChangeStaffRoleMutation(account.id);
  const disable = useDisableStaffAccountMutation(account.id);

  return (
    <Table.Tr>
      <Table.Td>{account.displayName}</Table.Td>
      <Table.Td>{account.email}</Table.Td>
      <Table.Td>
        <Select
          data={roleOptions}
          value={account.role}
          allowDeselect={false}
          disabled={account.status !== 'Active' || changeRole.isPending}
          onChange={(role) => role && changeRole.mutate({ role: role as StaffRole })}
          w={180}
          aria-label={`${t('Role')} ${account.email}`}
        />
      </Table.Td>
      <Table.Td>{t(account.status)}</Table.Td>
      <Table.Td>
        <Button
          size="xs"
          color="red"
          variant="subtle"
          disabled={isCurrent || account.status !== 'Active' || disable.isPending}
          loading={disable.isPending}
          onClick={() => disable.mutate()}
        >
          {t('Disable')}
        </Button>
      </Table.Td>
    </Table.Tr>
  );
}

function InvitationCard({
  invitation,
}: {
  invitation: {
    id: string;
    email: string;
    role: StaffRole;
    expiresAtUtc: string;
    claimedAtUtc: string | null;
    revokedAtUtc: string | null;
  };
}) {
  const { t } = useUiLanguage();
  const revoke = useRevokeStaffInvitationMutation(invitation.id);
  const state = invitation.claimedAtUtc
    ? t('Claimed')
    : invitation.revokedAtUtc
      ? t('Revoked')
      : new Date(invitation.expiresAtUtc) <= new Date()
        ? t('Expired')
        : t('Pending');

  return (
    <Paper withBorder p="sm" radius="sm">
      <Group justify="space-between" align="flex-start" wrap="nowrap">
        <Stack gap={2}>
          <Text fw={600}>{invitation.email}</Text>
          <Text size="sm" c="dimmed">
            {t(invitation.role)} · {state}
          </Text>
          <Text size="xs" c="dimmed">
            {t('Expires')} {formatDateTime(invitation.expiresAtUtc)}
          </Text>
        </Stack>
        <Button
          size="xs"
          variant="subtle"
          color="red"
          disabled={state !== t('Pending') || revoke.isPending}
          loading={revoke.isPending}
          onClick={() => revoke.mutate()}
        >
          {t('Revoke')}
        </Button>
      </Group>
    </Paper>
  );
}
