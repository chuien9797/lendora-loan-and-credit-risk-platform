import { FormEvent, useEffect, useState } from "react";
import { useAuth } from "../auth/AuthProvider";
import { ApiClientError } from "../lib/api";

type LoanProduct = Awaited<ReturnType<ReturnType<typeof useAuth>["apiAdminGetLoanProducts"]>>[number];

const emptyForm = {
  code: "",
  name: "",
  productType: "1",
  minAmount: "1000",
  maxAmount: "25000",
  minTermMonths: "12",
  maxTermMonths: "60",
  interestRate: "0.0799",
  isActive: true
};

export function AdminLoanProductsPage() {
  const auth = useAuth();
  const [products, setProducts] = useState<LoanProduct[]>([]);
  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void loadProducts();
  }, []);

  async function loadProducts() {
    setLoading(true);
    setError(null);
    try {
      setProducts(await auth.apiAdminGetLoanProducts());
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  function beginEdit(product: LoanProduct) {
    setEditingId(product.id);
    setForm({
      code: product.code,
      name: product.name,
      productType: String(product.productType),
      minAmount: String(product.minAmount),
      maxAmount: String(product.maxAmount),
      minTermMonths: String(product.minTermMonths),
      maxTermMonths: String(product.maxTermMonths),
      interestRate: String(product.interestRate),
      isActive: product.isActive
    });
    setMessage(null);
    setError(null);
  }

  function resetForm() {
    setEditingId(null);
    setForm(emptyForm);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setMessage(null);

    const payload = {
      code: form.code,
      name: form.name,
      productType: Number(form.productType),
      minAmount: Number(form.minAmount),
      maxAmount: Number(form.maxAmount),
      minTermMonths: Number(form.minTermMonths),
      maxTermMonths: Number(form.maxTermMonths),
      interestRate: Number(form.interestRate),
      isActive: form.isActive
    };

    try {
      if (editingId) {
        await auth.apiAdminUpdateLoanProduct(editingId, payload);
        setMessage("Loan product updated.");
      } else {
        await auth.apiAdminCreateLoanProduct(payload);
        setMessage("Loan product created.");
      }

      resetForm();
      await loadProducts();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  async function handleToggle(product: LoanProduct) {
    try {
      await auth.apiAdminUpdateLoanProduct(product.id, {
        code: product.code,
        name: product.name,
        productType: product.productType,
        minAmount: product.minAmount,
        maxAmount: product.maxAmount,
        minTermMonths: product.minTermMonths,
        maxTermMonths: product.maxTermMonths,
        interestRate: product.interestRate,
        isActive: !product.isActive
      });
      setMessage(product.isActive ? "Loan product disabled." : "Loan product enabled.");
      await loadProducts();
    } catch (err) {
      setError(errorMessage(err));
    }
  }

  async function handleDelete(product: LoanProduct) {
    try {
      await auth.apiAdminDeleteLoanProduct(product.id);
      setMessage("Loan product deleted.");
      await loadProducts();
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
            <h3>Loan products</h3>
            <p>Add, disable, and adjust lending products before customers create applications.</p>
          </div>
          {editingId ? (
            <button className="secondary-button button-compact" type="button" onClick={resetForm}>New product</button>
          ) : null}
        </div>

        {error ? <div className="form-error">{error}</div> : null}
        {message ? <div className="form-success">{message}</div> : null}

        <form className="admin-form admin-product-form" onSubmit={handleSubmit}>
          <label className="field"><span className="field-label">Code</span><input className="field-control" value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value }))} /></label>
          <label className="field"><span className="field-label">Name</span><input className="field-control" value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} /></label>
          <label className="field">
            <span className="field-label">Product type</span>
            <select className="field-control" value={form.productType} onChange={(event) => setForm((current) => ({ ...current, productType: event.target.value }))}>
              <option value="1">Personal loan</option>
              <option value="2">Car loan</option>
              <option value="3">Mortgage</option>
              <option value="4">Business loan</option>
            </select>
          </label>
          <label className="field"><span className="field-label">Min amount</span><input className="field-control" type="number" value={form.minAmount} onChange={(event) => setForm((current) => ({ ...current, minAmount: event.target.value }))} /></label>
          <label className="field"><span className="field-label">Max amount</span><input className="field-control" type="number" value={form.maxAmount} onChange={(event) => setForm((current) => ({ ...current, maxAmount: event.target.value }))} /></label>
          <label className="field"><span className="field-label">Min term</span><input className="field-control" type="number" value={form.minTermMonths} onChange={(event) => setForm((current) => ({ ...current, minTermMonths: event.target.value }))} /></label>
          <label className="field"><span className="field-label">Max term</span><input className="field-control" type="number" value={form.maxTermMonths} onChange={(event) => setForm((current) => ({ ...current, maxTermMonths: event.target.value }))} /></label>
          <label className="field"><span className="field-label">Interest rate</span><input className="field-control" type="number" step="0.0001" value={form.interestRate} onChange={(event) => setForm((current) => ({ ...current, interestRate: event.target.value }))} /></label>
          <label className="field admin-toggle"><span className="field-label">Active</span><input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} /></label>
          <div className="form-actions">
            <button className="primary-button button-compact" type="submit" disabled={saving}>{saving ? "Saving..." : editingId ? "Save product" : "Create product"}</button>
          </div>
        </form>
      </section>

      <section className="card table-card">
        <table className="data-table compact-table">
          <thead>
            <tr><th>Code</th><th>Name</th><th>Range</th><th>Term</th><th>Rate</th><th>Status</th><th>Actions</th></tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={7}>Loading loan products...</td></tr>
            ) : products.map((product) => (
              <tr key={product.id}>
                <td>{product.code}</td>
                <td>{product.name}</td>
                <td>RM {product.minAmount.toLocaleString()} - RM {product.maxAmount.toLocaleString()}</td>
                <td>{product.minTermMonths}-{product.maxTermMonths} months</td>
                <td>{(product.interestRate * 100).toFixed(2)}%</td>
                <td><span className={`status-pill ${product.isActive ? "status-green" : "status-red"}`}>{product.isActive ? "Active" : "Disabled"}</span></td>
                <td>
                  <div className="inline-actions">
                    <button className="text-button" type="button" onClick={() => beginEdit(product)}>Edit</button>
                    <button className="text-button" type="button" onClick={() => void handleToggle(product)}>{product.isActive ? "Disable" : "Enable"}</button>
                    <button className="text-button text-button-danger" type="button" onClick={() => void handleDelete(product)}>Delete</button>
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
