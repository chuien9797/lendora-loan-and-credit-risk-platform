import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";
import { ApiClientError } from "../lib/api";

export function ForgotPasswordPage() {
  const { requestPasswordReset } = useAuth();
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    setMessage(null);

    try {
      const responseMessage = await requestPasswordReset(email);
      setMessage(responseMessage);
    } catch (submissionError) {
      if (submissionError instanceof ApiClientError) {
        setError(submissionError.errors[0] ?? submissionError.message);
      } else {
        setError("We could not process your request right now.");
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
            <div className="auth-lockup" aria-hidden="true">
              <span className="auth-lockup-icon" />
            </div>
            <h2>Reset password</h2>
            <p>We&apos;ll send a reset link to your email</p>
          </div>

          <form className="form-grid auth-form-compact" onSubmit={handleSubmit}>
            <label className="field">
              <span className="field-label">Email</span>
              <input
                className="field-control"
                type="email"
                placeholder="your@email.com"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
              />
            </label>

            {message ? <div className="form-success">{message}</div> : null}
            {error ? <div className="form-error">{error}</div> : null}

            <button className="primary-button auth-primary-outline" type="submit" disabled={submitting}>
              {submitting ? "Sending..." : "Send reset link"}
            </button>
          </form>

          <p className="auth-switch auth-switch-back">
            <Link to="/login">&lt;- Back to sign in</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
