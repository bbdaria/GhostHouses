import { lazy, Suspense } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import AppLayout from './components/AppLayout.jsx';
import RequireAuth from './components/RequireAuth.jsx';
import RoleGate from './components/RoleGate.jsx';
import BuildingsPage from './pages/BuildingsPage.jsx';
import LogsPage from './pages/LogsPage.jsx';
import LoginPage from './pages/LoginPage.jsx';
import OtpPage from './pages/OtpPage.jsx';
import SettingsPage from './pages/SettingsPage.jsx';
import StreetsPage from './pages/StreetsPage.jsx';
import TemplateConverterPage from './pages/TemplateConverterPage.jsx';
import UserDetailsPage from './pages/UserDetailsPage.jsx';
import UsersListPage from './pages/UsersListPage.jsx';

const MapPage = lazy(() => import('./pages/MapPage.jsx'));

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/otp" element={<OtpPage />} />
      <Route element={<RequireAuth />}>
        <Route element={<AppLayout />}>
          <Route index element={<Navigate to="buildings" replace />} />
          <Route path="buildings" element={<BuildingsPage />} />
          <Route
            path="map"
            element={
              <Suspense fallback={<p className="muted">טוען מפה...</p>}>
                <MapPage />
              </Suspense>
            }
          />
          <Route path="streets" element={<StreetsPage />} />
          <Route path="logs" element={<LogsPage />} />
          <Route path="settings" element={<SettingsPage />} />
          <Route element={<RoleGate minRole="Admin" />}>
            <Route path="users" element={<UsersListPage />} />
            <Route path="users/:id" element={<UsersListPage />} />
            <Route path="converter" element={<TemplateConverterPage />} />
          </Route>
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
