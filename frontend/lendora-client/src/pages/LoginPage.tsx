import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";
import { ApiClientError } from "../lib/api";

export function LoginPage() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [email, setEmail] = useState("customer@lendora.local");
  const [password, setPassword] = useState("Customer12345");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      await login({ email, password });
      navigate("/", { replace: true });
    } catch (submissionError) {
      if (submissionError instanceof ApiClientError) {
        setError(submissionError.errors[0] ?? submissionError.message);
      } else {
        setError("Sign in failed. Please try again.");
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="auth-flow auth-flow-centered auth-screen">
      <div className="auth-stage">
        <div className="logo-mark" aria-hidden="true">
          <div className="logo-icon">
            <span className="logo-icon-dot" />
          </div>
          <div className="logo-name">Lendora</div>
        </div>

        <div className="card auth-card auth-card-narrow">
          <div className="card-header-centered">
            <h2>Sign in</h2>
            <p>Use your Lendora account</p>
          </div>

          <form className="form-grid auth-form-compact" onSubmit={handleSubmit}>
            <label className="field">
              <span className="field-label">Email</span>
              <input
                className="field-control"
                type="email"
                placeholder="james.davies@email.com"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
              />
            </label>

            <label className="field">
              <span className="field-label-row">
                <span className="field-label-inline">Password</span>
                <Link to="/forgot-password" className="text-link">
                  Forgot?
                </Link>
              </span>
              <input
                className="field-control"
                type="password"
                placeholder="Enter your password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
              />
            </label>

            <label className="remember-row">
              <input type="checkbox" defaultChecked />
              <span>Remember me</span>
            </label>

            {error ? <div className="form-error">{error}</div> : null}

            <button className="primary-button auth-primary-outline" type="submit" disabled={submitting}>
              {submitting ? "Signing in..." : "Sign in"}
            </button>
          </form>

          <div className="auth-helper">
            Try `customer@lendora.local`, `officer@lendora.local`, `underwriter@lendora.local`, or `admin@lendora.local`.
          </div>

          <p className="auth-switch">
            Don&apos;t have an account? <Link to="/register">Create one -&gt;</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
