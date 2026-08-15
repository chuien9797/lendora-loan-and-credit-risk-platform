import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";
import { ApiClientError } from "../lib/api";

type LoanApplicationSummary = Awaited<
  ReturnType<ReturnType<typeof useApplicationServices>["loadApplications"]>
>[number];

function useApplicationServices() {
  const auth = useAuth();

  return {
    async loadApplications() {
      return auth.isCustomer
        ? await auth.apiGetMyApplications()
        : await auth.apiGetReviewQueue();
    },
    deleteDraft: auth.apiDeleteDraft,
    submitApplication: auth.apiSubmitApplication
  };
}

function statusClass(status: number) {
  switch (status) {
    case 1:
      return "status-blue";
    case 2:
    case 3:
      return "status-amber";
    case 4:
    case 6:
    case 7:
    case 8:
      return "status-red";
    case 5:
    case 9:
      return "status-green";
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
      return "Assessment";
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

export function ApplicationsPage() {
  const navigate = useNavigate();
  const { isCustomer } = useAuth();
  const { loadApplications, deleteDraft, submitApplication } = useApplicationServices();
  const [applications, setApplications] = useState<LoanApplicationSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  async function refresh() {
    setLoading(true);
    setError(null);

    try {
      setApplications(await loadApplications());
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Could not load applications.");
      }
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void refresh();
  }, [isCustomer]);

  async function handleDelete(id: string) {
    setBusyId(id);
    setError(null);

    try {
      await deleteDraft(id);
      await refresh();
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Delete failed.");
      }
    } finally {
      setBusyId(null);
    }
  }

  async function handleSubmit(id: string) {
    setBusyId(id);
    setError(null);

    try {
      await submitApplication(id);
      await refresh();
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Submit failed.");
      }
    } finally {
      setBusyId(null);
    }
  }

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
      <section className="card">
        <div className="card-header">
          <div>
            <span className="eyebrow">{isCustomer ? "Applications" : "Review queue"}</span>
            <h3>{isCustomer ? "Customer application pipeline" : "Staff review pipeline"}</h3>
            <p>
              {isCustomer
                ? "Edit drafts, submit complete applications, and track every status."
                : "See submitted applications that are ready for operational review."}
            </p>
          </div>
          <button className="secondary-button button-compact" type="button" onClick={() => void refresh()}>
            Refresh
          </button>
        </div>

        {error ? <div className="form-error">{error}</div> : null}

        <div className="table-card">
          <table className="data-table">
            <thead>
              <tr>
                <th>{isCustomer ? "Product" : "Applicant"}</th>
                {!isCustomer ? <th>IC/MyKad or passport</th> : null}
                {!isCustomer ? <th>Phone</th> : null}
                <th>Amount</th>
                <th>Status</th>
                <th>{isCustomer ? "Actions" : "Submitted"}</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={isCustomer ? 4 : 6}>Loading applications...</td>
                </tr>
              ) : applications.length === 0 ? (
                <tr>
                  <td colSpan={isCustomer ? 4 : 6}>
                    {isCustomer
                      ? "No applications yet. Start a new draft to begin."
                      : "No applications are waiting in the queue right now."}
                  </td>
                </tr>
              ) : (
                applications.map((application) => (
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
                    {!isCustomer ? <td>{application.nationalIdNumber || "Not set"}</td> : null}
                    {!isCustomer ? <td>{application.phoneNumber || "Not set"}</td> : null}
                    <td>{formatCurrency(application.loanAmount)}</td>
                    <td>
                      <span className={`status-pill ${statusClass(application.status)}`}>
                        {statusLabel(application.status)}
                      </span>
                    </td>
                    <td>
                      {isCustomer ? (
                        <div className="inline-actions">
                          {application.status === 1 ? (
                            <>
                              <Link
                                to={`/applications/${application.id}/edit`}
                                className="text-link"
                                onClick={(event) => event.stopPropagation()}
                              >
                                Edit
                              </Link>
                              <button
                                className="text-button"
                                type="button"
                                disabled={busyId === application.id}
                                onClick={(event) => {
                                  event.stopPropagation();
                                  void handleSubmit(application.id);
                                }}
                              >
                                Submit
                              </button>
                              <button
                                className="text-button text-button-danger"
                                type="button"
                                disabled={busyId === application.id}
                                onClick={(event) => {
                                  event.stopPropagation();
                                  void handleDelete(application.id);
                                }}
                              >
                                Delete
                              </button>
                            </>
                          ) : (
                            <span className="table-note">View locked application</span>
                          )}
                        </div>
                      ) : (
                        <span className="table-note">
                          {application.submittedAtUtc
                            ? new Intl.DateTimeFormat("en-MY", {
                                day: "2-digit",
                                month: "short",
                                year: "numeric"
                              }).format(new Date(application.submittedAtUtc))
                            : "View details"}
                        </span>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
