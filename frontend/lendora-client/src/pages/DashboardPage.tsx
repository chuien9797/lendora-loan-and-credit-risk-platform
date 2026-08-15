import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";
import { ApiClientError } from "../lib/api";

type LoanApplicationSummary = {
  id: string;
  customerId: string;
  loanProductId: string;
  loanProductName: string;
  loanProductInterestRate: number;
  applicantFullName: string;
  nationalIdNumber: string;
  phoneNumber: string;
  email: string;
  loanPurpose: string;
  status: number;
  loanAmount: number;
  loanTermMonths: number;
  createdAtUtc: string;
  submittedAtUtc: string | null;
};

function statusClass(status: number) {
  switch (status) {
    case 1:
      return "status-blue";
    case 2:
    case 3:
      return "status-amber";
    case 4:
      return "status-red";
    case 5:
    case 9:
      return "status-green";
    case 6:
    case 7:
    case 8:
      return "status-red";
    default:
      return "status-blue";
  }
}

function statusLabel(status: number) {
  switch (status) {
    case 1:
      return "Draft";
    case 2:
      return "Submitted";
    case 3:
      return "In review";
    case 4:
      return "Manual review";
    case 5:
      return "Approved";
    case 6:
      return "Rejected";
    case 7:
      return "Cancelled";
    case 8:
      return "Frozen";
    case 9:
      return "Offer accepted";
    default:
      return "Unknown";
  }
}

function formatCurrency(amount: number) {
  return new Intl.NumberFormat("en-MY", {
    style: "currency",
    currency: "MYR",
    maximumFractionDigits: 0
  }).format(amount);
}

function formatDate(value: string | null) {
  if (!value) {
    return "Not submitted";
  }

  return new Intl.DateTimeFormat("en-MY", {
    day: "2-digit",
    month: "short",
    year: "numeric"
  }).format(new Date(value));
}

export function DashboardPage() {
  const navigate = useNavigate();
  const { user, isCustomer, apiGetMyApplications, apiGetReviewQueue } = useAuth();
  const [applications, setApplications] = useState<LoanApplicationSummary[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    async function run() {
      setLoading(true);
      setError(null);

      try {
        const data = isCustomer
          ? await apiGetMyApplications()
          : await apiGetReviewQueue();
        if (!cancelled) {
          setApplications(data);
        }
      } catch (requestError) {
        if (!cancelled) {
          if (requestError instanceof ApiClientError) {
            setError(requestError.errors[0] ?? requestError.message);
          } else {
            setError("Could not load dashboard data.");
          }
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void run();

    return () => {
      cancelled = true;
    };
  }, [isCustomer]);

  const total = applications.length;
  const drafts = applications.filter((application) => application.status === 1).length;
  const submitted = applications.filter((application) =>
    application.status === 2 || application.status === 3 || application.status === 4
  ).length;
  const approved = applications.filter((application) => application.status === 5 || application.status === 9).length;

  function openApplication(id: string) {
    navigate(`/applications/${id}`);
  }

  function handleRowKeyDown(event: React.KeyboardEvent<HTMLTableRowElement>, id: string) {
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      openApplication(id);
    }
  }

  return (
    <div className="page-grid">
      <section className="stats-grid">
        <article className="stat-card">
          <span>{isCustomer ? "My applications" : "Queue size"}</span>
          <strong>{loading ? "--" : total}</strong>
          <small>{isCustomer ? "Across all statuses" : "Cases awaiting staff action"}</small>
        </article>
        <article className="stat-card">
          <span>{isCustomer ? "Drafts" : "Submitted"}</span>
          <strong>{loading ? "--" : isCustomer ? drafts : submitted}</strong>
          <small>{isCustomer ? "Still editable" : "Ready to assess"}</small>
        </article>
        <article className="stat-card">
          <span>{isCustomer ? "In progress" : "Manual review"}</span>
          <strong>
            {loading
              ? "--"
              : isCustomer
                ? submitted
                : applications.filter((application) => application.status === 4).length}
          </strong>
          <small>{isCustomer ? "Submitted or under review" : "Cases needing closer review"}</small>
        </article>
        <article className="stat-card">
          <span>{isCustomer ? "Approved" : "Signed in as"}</span>
          <strong>{loading ? "--" : isCustomer ? approved : user?.roles[0] ?? "User"}</strong>
          <small>{isCustomer ? "Completed successfully" : user?.fullName ?? ""}</small>
        </article>
      </section>

      <section className="two-col-grid">
        <section className="card">
          <div className="card-header">
            <div>
              <span className="eyebrow">{isCustomer ? "My applications" : "Review queue"}</span>
              <h3>{isCustomer ? "Current application statuses" : "Cases waiting for action"}</h3>
            </div>
            <Link to="/applications" className="text-link">
              View all
            </Link>
          </div>

          {error ? <div className="form-error">{error}</div> : null}

          <table className="data-table">
            <thead>
              <tr>
                <th>{isCustomer ? "Product" : "Applicant"}</th>
                <th>Amount</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={3}>Loading...</td>
                </tr>
              ) : applications.length === 0 ? (
                <tr>
                  <td colSpan={3}>
                    {isCustomer
                      ? "No applications yet. Start your first draft."
                      : "No applications are currently waiting for review."}
                  </td>
                </tr>
              ) : (
                applications.slice(0, 5).map((application) => (
                  <tr
                    key={application.id}
                    className="clickable-row"
                    tabIndex={0}
                    role="link"
                    onClick={() => openApplication(application.id)}
                    onKeyDown={(event) => handleRowKeyDown(event, application.id)}
                  >
                    <td>
                      <span className="table-link">
                        {isCustomer ? application.loanProductName : application.applicantFullName}
                      </span>
                    </td>
                    <td>{formatCurrency(application.loanAmount)}</td>
                    <td>
                      <span className={`status-pill ${statusClass(application.status)}`}>
                        {statusLabel(application.status)}
                      </span>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </section>

        <section className="card">
          <div className="card-header">
            <div>
              <span className="eyebrow">{isCustomer ? "Activity" : "Staff view"}</span>
              <h3>{isCustomer ? "Latest submission dates" : "Role-aware access summary"}</h3>
            </div>
          </div>

          <div className="bar-list">
            {loading ? (
              <p>Loading summary...</p>
            ) : isCustomer ? (
              applications.slice(0, 4).map((application) => (
                <div className="metric-bar" key={application.id}>
                  <span>{application.loanProductName}</span>
                  <div className="metric-track">
                    <div className="metric-fill fill-blue fill-100" />
                  </div>
                  <strong>{formatDate(application.submittedAtUtc ?? application.createdAtUtc)}</strong>
                </div>
              ))
            ) : (
              <>
                <div className="metric-bar">
                  <span>Current role</span>
                  <div className="metric-track">
                    <div className="metric-fill fill-navy fill-100" />
                  </div>
                  <strong>{user?.roles.join(", ") ?? "Unknown"}</strong>
                </div>
                <div className="metric-bar">
                  <span>Applications visible</span>
                  <div className="metric-track">
                    <div className="metric-fill fill-green fill-100" />
                  </div>
                  <strong>{applications.length}</strong>
                </div>
              </>
            )}
          </div>
        </section>
      </section>
    </div>
  );
}
