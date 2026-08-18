import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AppLayout } from '../AppLayout';

const mediaQueryMocks = vi.hoisted(() => ({
  isMobile: false,
}));

vi.mock('@mui/material', async () => {
  const actual = await vi.importActual<typeof import('@mui/material')>('@mui/material');
  return {
    ...actual,
    useMediaQuery: () => mediaQueryMocks.isMobile,
  };
});

vi.mock('../../hooks/useAccessibleNavigation', () => ({
  useAccessibleNavigation: () => ({
    visibleItems: [
      { path: '/', label: 'Главная', end: true },
      { path: '/projects', label: 'Проекты' },
      { path: '/references', label: 'Справочники' },
    ],
  }),
}));

vi.mock('../../hooks/usePersistLastRoute', () => ({
  usePersistLastRoute: () => undefined,
}));

vi.mock('../../store/useAuthStore', () => ({
  useAuthStore: (sel: (s: { user: { roleName: string } }) => unknown) =>
    sel({ user: { roleName: 'SuperAdmin' } }),
}));

vi.mock('../../store/useDictionaryStore', () => ({
  useDictionaryStore: (sel: (s: { fetchDictionaries: () => void }) => unknown) =>
    sel({ fetchDictionaries: vi.fn() }),
}));

vi.mock('../../components/auth/UserMenu', () => ({
  UserMenu: () => <div data-testid="user-menu">User</div>,
}));

vi.mock('../../services/apiClient', () => ({
  default: {
    post: vi.fn().mockResolvedValue({}),
    get: vi.fn().mockResolvedValue({ data: { sessionExpirationMinutes: 60 } }),
  },
  refreshSessionHeartbeat: vi.fn().mockResolvedValue('ok'),
}));

function renderLayout(path = '/') {
  const theme = createTheme();
  return render(
    <ThemeProvider theme={theme}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route element={<AppLayout />}>
            <Route path="/" element={<div data-testid="home-page">Home content</div>} />
            <Route path="/projects" element={<div data-testid="projects-page">Projects content</div>} />
            <Route path="/references" element={<div data-testid="references-page">References content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </ThemeProvider>,
  );
}

describe('AppLayout mobile nav', () => {
  beforeEach(() => {
    mediaQueryMocks.isMobile = false;
  });

  afterEach(() => {
    cleanup();
  });

  it('shows permanent sidebar on desktop without hamburger', () => {
    mediaQueryMocks.isMobile = false;
    renderLayout();
    expect(screen.getAllByTestId('app-nav-list')).toHaveLength(1);
    expect(screen.queryByTestId('mobile-nav-toggle')).not.toBeInTheDocument();
  });

  it('shows hamburger, opens drawer, navigates and closes', () => {
    mediaQueryMocks.isMobile = true;
    renderLayout();

    expect(screen.getByTestId('mobile-nav-toggle')).toBeInTheDocument();

    fireEvent.click(screen.getByTestId('mobile-nav-toggle'));
    const nav = screen.getByTestId('app-nav-list');
    expect(within(nav).getByRole('link', { name: 'Проекты' })).toBeVisible();

    fireEvent.click(within(nav).getByRole('link', { name: 'Справочники' }));
    expect(screen.getByTestId('references-page')).toBeInTheDocument();
    // keepMounted оставляет Modal в DOM; при закрытии — aria-hidden.
    expect(document.querySelector('.MuiModal-root')).toHaveAttribute('aria-hidden', 'true');
  });
});
