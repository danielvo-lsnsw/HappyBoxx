import {
  Avatar,
  Badge,
  Group,
  Menu,
  Text,
  UnstyledButton,
  useComputedColorScheme,
  useMantineColorScheme,
} from '@mantine/core';
import {
  IconBuildingWarehouse,
  IconCheck,
  IconChevronDown,
  IconClockPlay,
  IconLogout,
  IconMoon,
  IconSettings,
  IconSun,
} from '@tabler/icons-react';

const soon = (
  <Badge size="xs" variant="light" color="gray">
    Soon
  </Badge>
);

// Placeholder identity until authentication is added.
const currentUser = { name: 'Admin User', role: 'Warehouse manager', initials: 'AU' };
const currentWarehouse = 'Main Warehouse';

export function UserMenu() {
  const { setColorScheme } = useMantineColorScheme();
  const isDark = useComputedColorScheme('light') === 'dark';

  return (
    <Menu position="bottom-end" width={260} shadow="md">
      <Menu.Target>
        <UnstyledButton className="user-trigger" aria-label="Account menu">
          <Group gap="xs" wrap="nowrap">
            <Avatar color="green" radius="xl" size={36}>
              {currentUser.initials}
            </Avatar>
            <div className="user-trigger-text">
              <Text size="sm" fw={600} lh={1.2}>
                {currentUser.name}
              </Text>
              <Text size="xs" c="dimmed" lh={1.2}>
                {currentWarehouse}
              </Text>
            </div>
            <IconChevronDown size={16} />
          </Group>
        </UnstyledButton>
      </Menu.Target>
      <Menu.Dropdown>
        <Menu.Label>
          {currentUser.name} · {currentUser.role}
        </Menu.Label>
        <Menu.Divider />
        <Menu.Label>Warehouse</Menu.Label>
        <Menu.Item
          leftSection={<IconBuildingWarehouse size={18} />}
          rightSection={<IconCheck size={16} />}
        >
          {currentWarehouse}
        </Menu.Item>
        <Menu.Item disabled rightSection={soon}>
          Switch warehouse
        </Menu.Item>
        <Menu.Divider />
        <Menu.Item leftSection={<IconClockPlay size={18} />} rightSection={soon} disabled>
          Clock in / out
        </Menu.Item>
        <Menu.Item
          leftSection={isDark ? <IconSun size={18} /> : <IconMoon size={18} />}
          onClick={() => setColorScheme(isDark ? 'light' : 'dark')}
        >
          {isDark ? 'Light mode' : 'Dark mode'}
        </Menu.Item>
        <Menu.Item leftSection={<IconSettings size={18} />} rightSection={soon} disabled>
          Account settings
        </Menu.Item>
        <Menu.Divider />
        <Menu.Item leftSection={<IconLogout size={18} />} color="red" disabled>
          Sign out
        </Menu.Item>
      </Menu.Dropdown>
    </Menu>
  );
}
