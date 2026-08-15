import { NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";

function getInitials(fullName: string) {
  return fullName
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");
}

export function AppShell() {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, primaryRole, isCustomer, isAdmin, logout } = useAuth();

  const navigation = isCustomer
    ? [
        { to: "/", label: "Dashboard" },
        { to: "/applications", label: "My applications" },
        { to: "/applications/new", label: "New application" }
      ]
    : [
        { to: "/", label: "Dashboard" },
        { to: "/applications", label: "Review queue" },
        { to: "/customers/search", label: "Customer search" },
        ...(isAdmin
          ? [
              { to: "/admin/users", label: "User management" },
              { to: "/admin/loan-products", label: "Loan products" }
            ]
          : [])
      ];

  const headerByRoute: Record<
    string,
    { title: string; subtitle: string; action: string; actionPath?: string }
  > = {
    "/": {
      title: isCustomer ? "My dashboard" : "Operations dashboard",
      subtitle: isCustomer
        ? "Track your applications, affordability, and next steps in one place."
        : "Monitor the review pipeline and keep lending decisions moving smoothly.",
      action: isCustomer ? "New application" : "Review queue",
      actionPath: isCustomer ? "/applications/new" : "/applications"
    },
    "/applications": {
      title: isCustomer ? "My applications" : "Review queue",
      subtitle: isCustomer
        ? "View statuses, continue drafts, and submit when everything is ready."
        : "See submitted applications that need staff attention.",
      action: isCustomer ? "New application" : "Refresh view",
      actionPath: isCustomer ? "/applications/new" : "/applications"
    },
    "/applications/new": {
      title: "Create application",
      subtitle: "Draft a request with clear sections before submitting it for review.",
      action: "My applications",
      actionPath: "/applications"
    },
    "/customers/search": {
      title: "Customer search",
      subtitle: "Find customer applications by name, IC/passport, phone, user id, or application id.",
      action: "Review queue",
      actionPath: "/applications"
    },
    "/admin/users": {
      title: "User management",
      subtitle: "Create, disable, delete, and assign role permissions for staff and customer accounts.",
      action: "Loan products",
      actionPath: "/admin/loan-products"
    },
    "/admin/loan-products": {
      title: "Loan products",
      subtitle: "Manage lending products, rates, active status, and application limits.",
      action: "User management",
      actionPath: "/admin/users"
    },
    "/applications/:id": {
      title: "Application details",
      subtitle: "Review the request, submit drafts, and track the locked submitted status.",
      action: "My applications",
      actionPath: "/applications"
    }
  };

  const headerByEditRoute = {
    title: "Edit application",
    subtitle: "Update your draft, save changes, or submit when you are ready.",
    action: "My applications",
    actionPath: "/applications"
  };

  const isApplicationDetailsRoute =
    location.pathname.startsWith("/applications/") &&
    !location.pathname.endsWith("/edit") &&
    location.pathname !== "/applications/new";

  const header = location.pathname.startsWith("/applications/") && location.pathname.endsWith("/edit")
    ? headerByEditRoute
    : isApplicationDetailsRoute
      ? headerByRoute["/applications/:id"]
      : headerByRoute[location.pathname] ?? headerByRoute["/"];

  function handlePrimaryAction() {
    if (!header.actionPath) {
      return;
    }

    navigate(header.actionPath);
  }

  function handleLogout() {
    logout();
    navigate("/login", { replace: true });
  }

  const fullName = user?.fullName ?? "Unknown User";
  const roleLabel = primaryRole ?? "User";

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="sidebar-logo">
          <div className="name">Lendora</div>
          <div className="tagline">Loan origination platform</div>
        </div>

        <div className="role-badge">
          <span className="role-dot" />
          <span className="role-label">{roleLabel}</span>
        </div>

        <div className="nav-heading">Workspace</div>
        <nav className="nav-list" aria-label="Main navigation">
          {navigation.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === "/"}
              className={({ isActive }) =>
                isActive ? "nav-link nav-link-active" : "nav-link"
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-footer">
          <div className="avatar">{getInitials(fullName)}</div>
          <div className="user-copy">
            <div className="sidebar-user">{fullName}</div>
            <div className="sidebar-user-role">{roleLabel}</div>
          </div>
        </div>
      </aside>

      <main className="main-panel">
        <header className="topbar">
          <div>
            <div className="topbar-title">{header.title}</div>
            <div className="topbar-subtitle">{header.subtitle}</div>
          </div>
          <div className="topbar-actions">
            <button className="icon-button" type="button" onClick={handleLogout}>
              Sign out
            </button>
            <button
              className="primary-button button-compact"
              type="button"
              onClick={handlePrimaryAction}
            >
              {header.action}
            </button>
          </div>
        </header>

        <section className="content-panel">
          <Outlet />
        </section>
      </main>
    </div>
  );
}
