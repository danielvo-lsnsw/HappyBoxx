import {
  ActionIcon,
  Badge,
  Button,
  Card,
  createTheme,
  Drawer,
  NavLink,
  NumberInput,
  Paper,
  SegmentedControl,
  Select,
  Table,
  Textarea,
  TextInput,
  type MantineThemeOverride,
} from '@mantine/core';

/** Visual language – see docs/UI_GUIDELINES.md §5–6. */
export const theme: MantineThemeOverride = createTheme({
  // Fresh-produce green; shade 7 (#15803d) gives ≥4.5:1 contrast with white text.
  colors: {
    brand: [
      '#ecfdf3',
      '#d1fadf',
      '#a6f4c5',
      '#6ce9a6',
      '#32d583',
      '#16a34a',
      '#15803d',
      '#15803d',
      '#166534',
      '#14532d',
    ],
  },
  primaryColor: 'brand',
  primaryShade: { light: 7, dark: 7 },
  defaultRadius: 'md',
  cursorType: 'pointer',
  fontFamily:
    'Inter, system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
  fontFamilyMonospace: '"JetBrains Mono", "Cascadia Mono", Consolas, monospace',
  headings: { fontWeight: '700' },
  components: {
    // Touch targets: ≥44px for gloved hands on warehouse tablets.
    Button: Button.extend({ defaultProps: { size: 'md' } }),
    TextInput: TextInput.extend({ defaultProps: { size: 'md' } }),
    Textarea: Textarea.extend({ defaultProps: { size: 'md' } }),
    Select: Select.extend({ defaultProps: { size: 'md' } }),
    NumberInput: NumberInput.extend({ defaultProps: { size: 'md' } }),
    SegmentedControl: SegmentedControl.extend({ defaultProps: { size: 'md' } }),
    ActionIcon: ActionIcon.extend({ defaultProps: { size: 44, variant: 'subtle', color: 'gray' } }),
    NavLink: NavLink.extend({ styles: { root: { minHeight: 44, borderRadius: 8 } } }),
    Badge: Badge.extend({ defaultProps: { radius: 'sm' } }),
    Card: Card.extend({ defaultProps: { shadow: 'xs' } }),
    Paper: Paper.extend({ defaultProps: { shadow: 'xs' } }),
    Table: Table.extend({ defaultProps: { fz: 'sm' } }),
    Drawer: Drawer.extend({ defaultProps: { position: 'right' } }),
  },
});
