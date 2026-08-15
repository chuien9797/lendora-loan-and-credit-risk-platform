import { useEffect, useState, type FormEvent, type KeyboardEvent } from "react";
import { useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";
import { ApiClientError } from "../lib/api";

type SearchResult = Awaited<ReturnType<ReturnType<typeof useSearchServices>["searchApplications"]>>[number];

function useSearchServices() {
  const auth = useAuth();

  return {
    isStaff: auth.isStaff,
    searchApplications: auth.apiSearchApplications
  };
}

function statusClass(status: number) {
  switch (status) {
    case 1:
      return "status-blue";
    case 2:
    case 3:
      return "status-amber";
    case 5:
    case 9:
      return "status-green";
    case 4:
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

export function CustomerSearchPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams, setSearchParams] = useSearchParams();
  const urlQuery = searchParams.get("query") ?? "";
  const { isStaff, searchApplications } = useSearchServices();
  const [query, setQuery] = useState(urlQuery);
  const [results, setResults] = useState<SearchResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasSearched, setHasSearched] = useState(false);

  useEffect(() => {
    setQuery(urlQuery);

    if (urlQuery.trim().length >= 2) {
      void runSearch(urlQuery);
    }
  }, [urlQuery]);

  if (!isStaff) {
    return <div className="form-error">Only bank workers can search customer applications.</div>;
  }

  async function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const trimmedQuery = query.trim();
    if (trimmedQuery !== urlQuery.trim()) {
      setSearchParams(trimmedQuery ? { query: trimmedQuery } : new URLSearchParams());
      return;
    }

    await runSearch(trimmedQuery);
  }

  async function runSearch(searchTerm: string) {
    const trimmedQuery = searchTerm.trim();
    setError(null);
    setHasSearched(true);

    if (trimmedQuery.length < 2) {
      setResults([]);
      setError("Enter at least 2 characters.");
      return;
    }

    setLoading(true);

    try {
      setResults(await searchApplications(trimmedQuery));
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Search failed.");
      }
    } finally {
      setLoading(false);
    }
  }

  function openApplication(id: string) {
    navigate(`/applications/${id}`, {
      state: {
        returnTo: `${location.pathname}${location.search}`,
        returnLabel: "Back to search"
      }
    });
  }

  function handleRowKeyDown(event: KeyboardEvent<HTMLTableRowElement>, id: string) {
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
            <span className="eyebrow">Bank worker only</span>
            <h3>Customer and application search</h3>
            <p>Search by customer name, IC/MyKad or passport, user id, application id, phone number, or email.</p>
          </div>
        </div>

        <form className="bank-review-form" onSubmit={handleSearch}>
          <label className="field field-span-full">
            <span className="field-label">Search</span>
            <input
              className="field-control"
              type="search"
              value={query}
              placeholder="Name, IC/passport, phone, user id, application id, or email"
              onChange={(event) => setQuery(event.target.value)}
            />
          </label>

          <div className="form-actions field-span-full">
            <button className="primary-button button-compact" type="submit" disabled={loading}>
              {loading ? "Searching..." : "Search"}
            </button>
          </div>
        </form>

        {error ? <div className="form-error">{error}</div> : null}

        <div className="table-card">
          <table className="data-table">
            <thead>
              <tr>
                <th>Applicant</th>
                <th>IC/passport</th>
                <th>Phone</th>
                <th>Amount</th>
                <th>Status</th>
                <th>Submitted</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={6}>Searching applications...</td>
                </tr>
              ) : results.length === 0 ? (
                <tr>
                  <td colSpan={6}>
                    {hasSearched ? "No matching applications found." : "Run a search to find customer applications."}
                  </td>
                </tr>
              ) : (
                results.map((application) => (
                  <tr
                    key={application.id}
                    className="clickable-row"
                    tabIndex={0}
                    role="link"
                    onClick={() => openApplication(application.id)}
                    onKeyDown={(event) => handleRowKeyDown(event, application.id)}
                  >
                    <td>
                      <span className="table-link">{application.applicantFullName}</span>
                      <div className="table-note">{application.email}</div>
                    </td>
                    <td>{application.nationalIdNumber || "Not set"}</td>
                    <td>{application.phoneNumber || "Not set"}</td>
                    <td>{formatCurrency(application.loanAmount)}</td>
                    <td>
                      <span className={`status-pill ${statusClass(application.status)}`}>
                        {statusLabel(application.status)}
                      </span>
                    </td>
                    <td>{formatDate(application.submittedAtUtc)}</td>
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
