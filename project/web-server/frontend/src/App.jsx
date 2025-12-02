import { Navigate, Route, Routes } from 'react-router-dom';
import AppLayout from './components/AppLayout.jsx';
import RequireAuth from './components/RequireAuth.jsx';
import RoleGate from './components/RoleGate.jsx';
import BuildingsPage from './pages/BuildingsPage.jsx';
import LogsPage from './pages/LogsPage.jsx';
import LoginPage from './pages/LoginPage.jsx';
import OtpPage from './pages/OtpPage.jsx';
import UserDetailsPage from './pages/UserDetailsPage.jsx';
import UsersListPage from './pages/UsersListPage.jsx';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/otp" element={<OtpPage />} />
      <Route element={<RequireAuth />}>
        <Route element={<AppLayout />}>
          <Route index element={<Navigate to="buildings" replace />} />
          <Route path="buildings" element={<BuildingsPage />} />
          <Route path="logs" element={<LogsPage />} />
          <Route element={<RoleGate minRole="Admin" />}>
            <Route path="users" element={<UsersListPage />} />
            <Route path="users/:id" element={<UsersListPage />} />
          </Route>
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
