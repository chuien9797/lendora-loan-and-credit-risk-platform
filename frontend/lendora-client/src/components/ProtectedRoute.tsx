import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";

export function ProtectedRoute() {
  const { isReady, session } = useAuth();

  if (!isReady) {
    return <div className="auth-loading">Loading your workspace...</div>;
  }

  if (!session) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}

export function PublicOnlyRoute() {
  const { isReady, session } = useAuth();

  if (!isReady) {
    return <div className="auth-loading">Loading your workspace...</div>;
  }

  if (session) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}

type RoleRouteProps = {
  allow: "customer" | "staff" | "admin";
};

export function RoleRoute({ allow }: RoleRouteProps) {
  const { isReady, session, isCustomer, isStaff, isAdmin } = useAuth();

  if (!isReady) {
    return <div className="auth-loading">Loading your workspace...</div>;
  }

  if (!session) {
    return <Navigate to="/login" replace />;
  }

  const isAllowed =
    allow === "customer" ? isCustomer :
      allow === "staff" ? isStaff :
        isAdmin;

  if (!isAllowed) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
