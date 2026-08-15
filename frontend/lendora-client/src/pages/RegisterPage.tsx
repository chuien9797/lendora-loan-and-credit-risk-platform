import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";
import { ApiClientError } from "../lib/api";

export function RegisterPage() {
  const navigate = useNavigate();
  const { register } = useAuth();
  const [firstName, setFirstName] = useState("James");
  const [lastName, setLastName] = useState("Davies");
  const [email, setEmail] = useState("james.davies@email.com");
  const [password, setPassword] = useState("Customer12345");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      await register({
        fullName: `${firstName} ${lastName}`.trim(),
        email,
        password
      });

      navigate("/", { replace: true });
    } catch (submissionError) {
      if (submissionError instanceof ApiClientError) {
        setError(submissionError.errors[0] ?? submissionError.message);
      } else {
        setError("Registration failed. Please try again.");
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

        <div className="auth-steps" aria-label="Registration progress">
          <span className="auth-step auth-step-done" />
          <span className="auth-step auth-step-active" />
          <span className="auth-step" />
        </div>

        <div className="card auth-card auth-card-narrow">
          <div className="card-header-centered">
            <h2>Create account</h2>
            <p>Step 2 of 3 - Personal details</p>
          </div>

          <div className="notice-card">
            Registering as a customer. Staff accounts are set up by an admin.
          </div>

          <form className="form-grid auth-form-compact" onSubmit={handleSubmit}>
            <div className="two-inputs">
              <label className="field">
                <span className="field-label">First name</span>
                <input
                  className="field-control"
                  type="text"
                  placeholder="James"
                  value={firstName}
                  onChange={(event) => setFirstName(event.target.value)}
                />
              </label>

              <label className="field">
                <span className="field-label">Last name</span>
                <input
                  className="field-control"
                  type="text"
                  placeholder="Davies"
                  value={lastName}
                  onChange={(event) => setLastName(event.target.value)}
                />
              </label>
            </div>

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
              <span className="field-label">Password</span>
              <input
                className="field-control"
                type="password"
                placeholder="Create a strong password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
              />
              <div className="password-strength" aria-hidden="true">
                <span className="strength-bar strength-red" />
                <span className="strength-bar strength-amber" />
                <span className="strength-bar strength-amber" />
                <span className="strength-bar strength-green" />
              </div>
              <span className="strength-label">Strong password</span>
            </label>

            {error ? <div className="form-error">{error}</div> : null}

            <button className="primary-button auth-primary-outline" type="submit" disabled={submitting}>
              {submitting ? "Creating account..." : "Continue ->"}
            </button>
          </form>

          <p className="auth-switch">
            Already have an account? <Link to="/login">Sign in -&gt;</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
