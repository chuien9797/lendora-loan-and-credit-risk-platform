import { Navigate, Route, Routes } from "react-router-dom";
import { ProtectedRoute, PublicOnlyRoute, RoleRoute } from "./components/ProtectedRoute";
import { AppShell } from "./components/AppShell";
import { AuthLayout } from "./components/AuthLayout";
import { DashboardPage } from "./pages/DashboardPage";
import { ApplicationsPage } from "./pages/ApplicationsPage";
import { ApplicationDetailsPage } from "./pages/ApplicationDetailsPage";
import { CreateApplicationPage } from "./pages/CreateApplicationPage";
import { CustomerSearchPage } from "./pages/CustomerSearchPage";
import { AdminUsersPage } from "./pages/AdminUsersPage";
import { AdminLoanProductsPage } from "./pages/AdminLoanProductsPage";
import { LoginPage } from "./pages/LoginPage";
import { RegisterPage } from "./pages/RegisterPage";
import { ForgotPasswordPage } from "./pages/ForgotPasswordPage";

export function App() {
  return (
    <Routes>
      <Route element={<PublicOnlyRoute />}>
        <Route element={<AuthLayout />}>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        </Route>
      </Route>

      <Route element={<ProtectedRoute />}>
        <Route element={<AppShell />}>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/applications" element={<ApplicationsPage />} />
          <Route element={<RoleRoute allow="customer" />}>
            <Route path="/applications/new" element={<CreateApplicationPage />} />
            <Route path="/applications/:id/edit" element={<CreateApplicationPage />} />
          </Route>
          <Route path="/applications/:id" element={<ApplicationDetailsPage />} />
          <Route element={<RoleRoute allow="staff" />}>
            <Route path="/customers/search" element={<CustomerSearchPage />} />
          </Route>
          <Route element={<RoleRoute allow="admin" />}>
            <Route path="/admin/users" element={<AdminUsersPage />} />
            <Route path="/admin/loan-products" element={<AdminLoanProductsPage />} />
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  );
}
