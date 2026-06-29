import React from 'react';
import ReactDOM from 'react-dom/client';
import { QueryClientProvider } from '@tanstack/react-query';
import {
  ClientSideRowModelModule,
  CommunityFeaturesModule,
  GridCoreModule,
  ModuleRegistry,
} from 'ag-grid-community';
import { EnterpriseCoreModule, LicenseManager, RowGroupingModule } from 'ag-grid-enterprise';
import { CssBaseline, ThemeProvider, createTheme } from '@mui/material';
import App from './App';
import { queryClient } from './services/queryClient';
import { wpColors } from './theme/wpColors';

const agGridLicenseKey = import.meta.env.VITE_AG_GRID_LICENSE_KEY as string | undefined;
if (agGridLicenseKey?.trim()) {
  LicenseManager.setLicenseKey(agGridLicenseKey.trim());
}

// AG Grid v32+: treeData и группировка требуют enterprise RowGroupingModule.
ModuleRegistry.registerModules([
  GridCoreModule,
  ClientSideRowModelModule,
  CommunityFeaturesModule,
  EnterpriseCoreModule,
  RowGroupingModule,
]);

const theme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: wpColors.accent, dark: wpColors.accentDark, light: wpColors.accentHover },
    secondary: { main: wpColors.sidebar },
    background: { default: wpColors.pageBg, paper: wpColors.cardBg },
    text: { primary: wpColors.heading, secondary: wpColors.muted },
  },
  typography: {
    fontFamily: '"Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
    h5: { color: wpColors.heading, fontWeight: 600 },
    h6: { color: wpColors.heading, fontWeight: 600 },
  },
  components: {
    MuiButton: {
      styleOverrides: {
        containedPrimary: {
          textTransform: 'none',
          boxShadow: 'none',
          '&:hover': { boxShadow: 'none', bgcolor: wpColors.accentDark },
        },
      },
    },
    MuiTabs: {
      styleOverrides: {
        root: { minHeight: 48, bgcolor: wpColors.pageBg },
        indicator: { display: 'none' },
      },
    },
    MuiTab: {
      styleOverrides: {
        root: {
          minHeight: 48,
          minWidth: 120,
          textTransform: 'none',
          fontSize: 13,
          color: wpColors.tabInactive,
          bgcolor: wpColors.pageBg,
          borderRadius: 0,
          '&.Mui-selected': {
            color: '#FFFFFF',
            bgcolor: wpColors.accent,
          },
          '&:hover': {
            bgcolor: wpColors.accentHover,
            color: wpColors.heading,
          },
          '&.Mui-selected:hover': {
            bgcolor: wpColors.accentDark,
            color: '#FFFFFF',
          },
        },
      },
    },
  },
});

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <App />
      </ThemeProvider>
    </QueryClientProvider>
  </React.StrictMode>,
);
