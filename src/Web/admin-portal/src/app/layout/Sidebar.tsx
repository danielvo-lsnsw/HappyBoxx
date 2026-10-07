import {
  ActionIcon,
  Badge,
  Box,
  Divider,
  Menu,
  NavLink,
  ScrollArea,
  Stack,
  Tooltip,
} from '@mantine/core';
import { IconLayoutSidebarLeftCollapse, IconLayoutSidebarLeftExpand } from '@tabler/icons-react';
import { Link } from 'react-router';
import { navigation, useActiveNavigation, type NavItem } from '@/app/navigation';

interface SidebarProps {
  /** Icon-rail mode (desktop only). */
  collapsed: boolean;
  onToggleCollapsed: () => void;
  onNavigate: () => void;
}

const soonBadge = (
  <Badge size="xs" variant="light" color="gray">
    Soon
  </Badge>
);

export function Sidebar({ collapsed, onToggleCollapsed, onNavigate }: SidebarProps) {
  const { trail, active } = useActiveNavigation();

  return (
    <Stack h="100%" gap={0}>
      <ScrollArea flex={1} type="auto" px={collapsed ? 'xs' : 'sm'} py="sm">
        {collapsed ? (
          <RailItems items={navigation} trail={trail} onNavigate={onNavigate} />
        ) : (
          <NavTree
            items={navigation}
            trail={trail}
            active={active}
            onNavigate={onNavigate}
            depth={0}
          />
        )}
      </ScrollArea>
      <Divider />
      <Box p="xs" visibleFrom="sm">
        {collapsed ? (
          <Tooltip label="Expand sidebar" position="right">
            <ActionIcon
              onClick={onToggleCollapsed}
              aria-label="Expand sidebar"
              mx="auto"
              display="flex"
            >
              <IconLayoutSidebarLeftExpand size={22} stroke={1.75} />
            </ActionIcon>
          </Tooltip>
        ) : (
          <NavLink
            component="button"
            label="Collapse sidebar"
            leftSection={<IconLayoutSidebarLeftCollapse size={20} stroke={1.75} />}
            onClick={onToggleCollapsed}
          />
        )}
      </Box>
    </Stack>
  );
}

interface NavTreeProps {
  items: NavItem[];
  trail: NavItem[];
  active?: NavItem;
  onNavigate: () => void;
  depth: number;
}

function NavTree({ items, trail, active, onNavigate, depth }: NavTreeProps) {
  return items.map((item) => {
    const ItemIcon = item.icon;
    const icon = ItemIcon && <ItemIcon size={depth === 0 ? 20 : 18} stroke={1.75} />;

    if (item.children) {
      return (
        <NavLink
          key={item.label}
          label={item.label}
          leftSection={icon}
          defaultOpened={trail.includes(item)}
          childrenOffset={depth === 0 ? 'md' : 'sm'}
          fw={depth === 0 ? 600 : undefined}
        >
          <NavTree
            items={item.children}
            trail={trail}
            active={active}
            onNavigate={onNavigate}
            depth={depth + 1}
          />
        </NavLink>
      );
    }

    return (
      <NavLink
        key={item.label}
        component={Link}
        to={item.path ?? '/'}
        label={item.label}
        leftSection={icon}
        rightSection={item.planned ? soonBadge : undefined}
        active={item === active}
        onClick={onNavigate}
        fw={depth === 0 ? 600 : undefined}
      />
    );
  });
}

function RailItems({
  items,
  trail,
  onNavigate,
}: {
  items: NavItem[];
  trail: NavItem[];
  onNavigate: () => void;
}) {
  return (
    <Stack gap={4} align="center">
      {items.map((item) => {
        const ItemIcon = item.icon;
        const isActive = trail.includes(item);
        const iconNode = ItemIcon && <ItemIcon size={22} stroke={1.75} />;

        if (!item.children) {
          return (
            <Tooltip key={item.label} label={item.label} position="right" withArrow>
              <ActionIcon
                component={Link}
                to={item.path ?? '/'}
                size={48}
                variant={isActive ? 'light' : 'subtle'}
                color={isActive ? undefined : 'gray'}
                aria-label={item.label}
                onClick={onNavigate}
              >
                {iconNode}
              </ActionIcon>
            </Tooltip>
          );
        }

        return (
          <Menu
            key={item.label}
            position="right-start"
            offset={12}
            trigger="click-hover"
            width={240}
          >
            <Menu.Target>
              <ActionIcon
                size={48}
                variant={isActive ? 'light' : 'subtle'}
                color={isActive ? undefined : 'gray'}
                aria-label={item.label}
              >
                {iconNode}
              </ActionIcon>
            </Menu.Target>
            <Menu.Dropdown>
              <Menu.Label>{item.label}</Menu.Label>
              <RailMenuItems items={item.children} onNavigate={onNavigate} />
            </Menu.Dropdown>
          </Menu>
        );
      })}
    </Stack>
  );
}

function RailMenuItems({ items, onNavigate }: { items: NavItem[]; onNavigate: () => void }) {
  return items.map((item) => {
    if (item.children) {
      return (
        <Box key={item.label}>
          <Menu.Divider />
          <Menu.Label>{item.label}</Menu.Label>
          <RailMenuItems items={item.children} onNavigate={onNavigate} />
        </Box>
      );
    }
    const ItemIcon = item.icon;
    return (
      <Menu.Item
        key={item.label}
        component={Link}
        to={item.path ?? '/'}
        leftSection={ItemIcon && <ItemIcon size={18} stroke={1.75} />}
        rightSection={item.planned ? soonBadge : undefined}
        onClick={onNavigate}
      >
        {item.label}
      </Menu.Item>
    );
  });
}
