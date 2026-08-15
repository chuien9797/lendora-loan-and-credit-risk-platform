import { FormEvent, useEffect, useState } from "react";
import { ApiClientError } from "../lib/api";
import { useAuth } from "../auth/AuthProvider";

type AdminUser = Awaited<ReturnType<ReturnType<typeof useAuth>["apiAdminGetUsers"]>>[number];

const roles = [
  {
    value: "Customer",
    label: "Customer",
    description: "Can create and track their own applications."
  },
  {
    value: "LoanOfficer",
    label: "Loan officer",
    description: "Can search customers and run intake review."
  },
  {
    value: "Underwriter",
    label: "Underwriter",
    description: "Can complete final underwriting decisions."
  },
  {
    value: "Admin",
    label: "Admin",
    description: "Can manage users, staff, customers, and loan products."
  }
];

const emptyForm = {
  fullName: "",
  email: "",
  password: "",
  roles: ["Customer"],
  isActive: true
};

export function AdminUsersPage() {
  const auth = useAuth();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void loadUsers();
  }, []);

  async function loadUsers() {
    setLoading(true);
    setError(null);
    try {
      setUsers(await auth.apiAdminGetUsers());
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  function beginEdit(user: AdminUser) {
    setEditingId(user.id);
    setForm({
      fullName: user.fullName,
      email: user.email,
      password: "",
      roles: user.roles,
      isActive: user.isActive
    });
    setMessage(null);
    setError(null);
  }

  function resetForm() {
    setEditingId(null);
    setForm(emptyForm);
  }

  function chooseRole(role: string) {
    setForm((current) => ({ ...current, roles: [role] }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setMessage(null);

    try {
      if (editingId) {
        await auth.apiAdminUpdateUser(editingId, {
          fullName: form.fullName,
          email: form.email,
          roles: form.roles,
          isActive: form.isActive
        });
        setMessage("User account updated.");
      } else {
        await auth.apiAdminCreateUser({
          fullName: form.fullName,
          email: form.email,
          password: form.password,
          roles: form.roles,
          isActive: form.isActive
        });
        setMessage("User account created.");
      }

      resetForm();
      await loadUsers();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  async function handleDisable(user: AdminUser) {
    setError(null);
    setMessage(null);
    try {
      await auth.apiAdminUpdateUser(user.id, {
        fullName: user.fullName,
        email: user.email,
        roles: user.roles,
        isActive: !user.isActive
      });
      setMessage(user.isActive ? "User account disabled." : "User account enabled.");
      await loadUsers();
    } catch (err) {
      setError(errorMessage(err));
    }
  }

  async function handleDelete(user: AdminUser) {
    setError(null);
    setMessage(null);
    try {
      await auth.apiAdminDeleteUser(user.id);
      setMessage("User account deleted.");
      await loadUsers();
    } catch (err) {
      setError(errorMessage(err));
    }
  }

  return (
    <div className="page-grid">
      <section className="card">
        <div className="card-header">
          <div>
            <span className="eyebrow">Admin</span>
            <h3>User management</h3>
            <p>Create, disable, delete, and assign one clear access level for each account.</p>
          </div>
          {editingId ? (
            <button className="secondary-button button-compact" type="button" onClick={resetForm}>
              New user
            </button>
          ) : null}
        </div>

        {error ? <div className="form-error">{error}</div> : null}
        {message ? <div className="form-success">{message}</div> : null}

        <form className="admin-form" onSubmit={handleSubmit}>
          <label className="field">
            <span className="field-label">Full name</span>
            <input className="field-control" value={form.fullName} onChange={(event) => setForm((current) => ({ ...current, fullName: event.target.value }))} />
          </label>
          <label className="field">
            <span className="field-label">Email</span>
            <input className="field-control" type="email" value={form.email} onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))} />
          </label>
          {!editingId ? (
            <label className="field">
              <span className="field-label">Temporary password</span>
              <input className="field-control" type="password" value={form.password} onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))} />
            </label>
          ) : null}
          <label className="field admin-toggle">
            <span className="field-label">Active</span>
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
          </label>
          <div className="role-picker field-span-full">
            {roles.map((role) => (
              <label key={role.value} className="role-choice">
                <input
                  type="radio"
                  name="admin-user-role"
                  checked={form.roles[0] === role.value}
                  onChange={() => chooseRole(role.value)}
                />
                <span>{role.label}</span>
                <small>{role.description}</small>
              </label>
            ))}
          </div>
          <div className="form-actions">
            <button className="primary-button button-compact" type="submit" disabled={saving}>
              {saving ? "Saving..." : editingId ? "Save user" : "Create user"}
            </button>
          </div>
        </form>
      </section>

      <section className="card table-card">
        <table className="data-table compact-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Roles</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={5}>Loading accounts...</td></tr>
            ) : users.length === 0 ? (
              <tr><td colSpan={5}>No accounts found.</td></tr>
            ) : users.map((user) => (
              <tr key={user.id}>
                <td>{user.fullName}</td>
                <td>{user.email}</td>
                <td>{user.roles.join(", ")}</td>
                <td><span className={`status-pill ${user.isActive ? "status-green" : "status-red"}`}>{user.isActive ? "Active" : "Disabled"}</span></td>
                <td>
                  <div className="inline-actions">
                    <button className="text-button" type="button" onClick={() => beginEdit(user)}>Edit</button>
                    <button className="text-button" type="button" onClick={() => void handleDisable(user)}>{user.isActive ? "Disable" : "Enable"}</button>
                    <button className="text-button text-button-danger" type="button" onClick={() => void handleDelete(user)}>Delete</button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </div>
  );
}

function errorMessage(error: unknown) {
  if (error instanceof ApiClientError) {
    return error.errors.length > 0 ? error.errors.join(" ") : error.message;
  }

  return "Something went wrong. Try again.";
}
