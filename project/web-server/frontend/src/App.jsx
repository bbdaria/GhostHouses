import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider } from './authContext.jsx';
import NavBar from './components/NavBar.jsx';
import ProtectedRoute from './components/ProtectedRoute.jsx';
import AddBuildingPage from './pages/AddBuildingPage.jsx';
import BuildingDetailsPage from './pages/BuildingDetailsPage.jsx';
import BuildingsListPage from './pages/BuildingsListPage.jsx';
import DashboardPage from './pages/DashboardPage.jsx';
import LoginPage from './pages/LoginPage.jsx';
import LogsPage from './pages/LogsPage.jsx';
import UserDetailsPage from './pages/UserDetailsPage.jsx';
import UserListPage from './pages/UserListPage.jsx';

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <NavBar />
        <main className="container">
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route element={<ProtectedRoute />}>
              <Route path="/" element={<Navigate to="/dashboard" replace />} />
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/buildings" element={<BuildingsListPage />} />
              <Route path="/buildings/:id" element={<BuildingDetailsPage />} />
              <Route element={<ProtectedRoute allowedRoles={['Editor', 'Admin']} />}>
                <Route path="/buildings/add" element={<AddBuildingPage />} />
              </Route>
              <Route path="/logs" element={<LogsPage />} />
              <Route element={<ProtectedRoute allowedRoles={['Admin']} />}>
                <Route path="/users" element={<UserListPage />} />
                <Route path="/users/:id" element={<UserDetailsPage />} />
              </Route>
            </Route>
          </Routes>
        </main>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
