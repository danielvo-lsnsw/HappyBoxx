import { Anchor, Button, Center, Container, Paper, Stack, Text, Title } from '@mantine/core';
import { IconAt, IconShieldLock } from '@tabler/icons-react';
import { Link } from 'react-router';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import type { HappyBoxxRole } from './AuthContext';
import { authMode } from './authConfig';

interface SignInPageProps {
  onSignIn: () => void;
  onDevelopmentSignIn: (role: Exclude<HappyBoxxRole, 'Unassigned'>) => void;
}

export function SignInPage({ onSignIn, onDevelopmentSignIn }: SignInPageProps) {
  const { t } = useUiLanguage();

  return (
    <Center mih="100dvh" p="md" bg="var(--mantine-color-body)">
      <Container size={420} w="100%">
        <Paper withBorder p="xl" radius="md">
          <Stack gap="md">
            <Center>
              <IconShieldLock size={36} stroke={1.75} />
            </Center>
            <div>
              <Title order={2} ta="center">
                {t('Sign in to HappyBoxx')}
              </Title>
              <Text c="dimmed" size="sm" ta="center" mt="xs">
                {t('Use your invited staff account to continue.')}
              </Text>
            </div>

            {authMode === 'development' ? (
              <>
                <Text size="sm" fw={600} ta="center" c="orange.8">
                  {t('Local test identities only. No email or OTP is verified.')}
                </Text>
                <Button
                  leftSection={<IconAt size={18} />}
                  onClick={() => onDevelopmentSignIn('Admin')}
                >
                  {t('Continue as local Admin')}
                </Button>
                <Button variant="default" onClick={() => onDevelopmentSignIn('Order Creator')}>
                  {t('Continue as local Order Creator')}
                </Button>
                <Button variant="subtle" onClick={() => onDevelopmentSignIn('Customer')}>
                  {t('Continue as local Customer')}
                </Button>
              </>
            ) : (
              <Button leftSection={<IconAt size={18} />} onClick={onSignIn}>
                {t('Sign in with email')}
              </Button>
            )}
            <Anchor component={Link} to="/register" ta="center" size="sm">
              {t('Create customer account')}
            </Anchor>
          </Stack>
        </Paper>
      </Container>
    </Center>
  );
}
