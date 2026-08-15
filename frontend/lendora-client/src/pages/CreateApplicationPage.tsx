import { useEffect, useState } from "react";
import { Navigate, useNavigate, useParams } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";
import { ApiClientError } from "../lib/api";

type LoanProduct = Awaited<ReturnType<ReturnType<typeof useApplicationFormServices>["loadProducts"]>>[number];
type LoanApplicationDetails = Awaited<
  ReturnType<ReturnType<typeof useApplicationFormServices>["loadApplication"]>
>;
type ApplicationDocument = Awaited<
  ReturnType<ReturnType<typeof useApplicationFormServices>["loadDocuments"]>
>[number];

type FormState = {
  loanProductId: string;
  applicantFullName: string;
  nationalIdNumber: string;
  phoneNumber: string;
  email: string;
  loanPurpose: string;
  employmentStatus: number;
  employerOrBusinessName: string;
  employerOrBusinessRegistrationNumber: string;
  loanAmount: string;
  loanTermMonths: string;
  monthlyIncome: string;
  monthlyExpenses: string;
  existingMonthlyDebt: string;
  hasCreditHistoryConsent: boolean;
  hasIncomeVerificationConsent: boolean;
  hasPersonalDataProcessingConsent: boolean;
  employmentDurationMonths: string;
  numberOfDependents: string;
  residentialStatus: number;
};

const employmentOptions = [
  { value: 1, label: "Employed" },
  { value: 2, label: "Self-employed" },
  { value: 3, label: "Unemployed" },
  { value: 4, label: "Student" },
  { value: 5, label: "Retired" }
];

const zeroIncomeEmploymentStatuses = new Set([3, 4]);

const residentialOptions = [
  { value: 1, label: "Owner" },
  { value: 2, label: "Mortgage" },
  { value: 3, label: "Tenant" },
  { value: 4, label: "Living with family" },
  { value: 5, label: "Other" }
];

const documentTypeOptions = [
  { value: 1, label: "ID document" },
  { value: 2, label: "Proof of address" },
  { value: 3, label: "Payslip" },
  { value: 4, label: "Bank statement" },
  { value: 5, label: "Employment letter" },
  { value: 6, label: "Property valuation" },
  { value: 7, label: "Tax document" },
  { value: 8, label: "Insurance document" }
];

const requiredDocumentSlots = [
  {
    key: "identity",
    label: "ID document",
    helper: "MyKad, MyPR, passport, or other government ID.",
    acceptedTypes: [1],
    defaultType: 1
  },
  {
    key: "address",
    label: "Proof of address",
    helper: "Utility bill, bank letter, or tenancy document.",
    acceptedTypes: [2],
    defaultType: 2
  },
  {
    key: "payslip-1",
    label: "Payslip month 1",
    helper: "Upload one of the latest 3 monthly payslips.",
    acceptedTypes: [3],
    defaultType: 3,
    occurrence: 1
  },
  {
    key: "payslip-2",
    label: "Payslip month 2",
    helper: "Upload the second recent monthly payslip.",
    acceptedTypes: [3],
    defaultType: 3,
    occurrence: 2
  },
  {
    key: "payslip-3",
    label: "Payslip month 3",
    helper: "Upload the third recent monthly payslip.",
    acceptedTypes: [3],
    defaultType: 3,
    occurrence: 3
  },
  {
    key: "bank-statement-1",
    label: "Bank statement month 1",
    helper: "Upload one of the latest 3 monthly bank statements.",
    acceptedTypes: [4],
    defaultType: 4,
    occurrence: 1
  },
  {
    key: "bank-statement-2",
    label: "Bank statement month 2",
    helper: "Upload the second recent monthly bank statement.",
    acceptedTypes: [4],
    defaultType: 4,
    occurrence: 2
  },
  {
    key: "bank-statement-3",
    label: "Bank statement month 3",
    helper: "Upload the third recent monthly bank statement.",
    acceptedTypes: [4],
    defaultType: 4,
    occurrence: 3
  }
];

const acceptedUploadTypes = ".pdf,.jpg,.jpeg,.png,.tif,.tiff";
const maxFileSizeBytes = 10 * 1024 * 1024;

function useApplicationFormServices() {
  const auth = useAuth();

  return {
    loadProducts: auth.apiGetLoanProducts,
    loadApplication: auth.apiGetApplication,
    loadDocuments: auth.apiGetDocuments,
    createDraft: auth.apiCreateDraft,
    updateDraft: auth.apiUpdateDraft,
    submitApplication: auth.apiSubmitApplication,
    uploadDocument: auth.apiUploadDocument
  };
}

function toFormState(userName: string, email: string): FormState {
  return {
    loanProductId: "",
    applicantFullName: userName,
    nationalIdNumber: "",
    phoneNumber: "",
    email,
    loanPurpose: "",
    employmentStatus: 1,
    employerOrBusinessName: "",
    employerOrBusinessRegistrationNumber: "",
    loanAmount: "",
    loanTermMonths: "",
    monthlyIncome: "",
    monthlyExpenses: "",
    existingMonthlyDebt: "",
    hasCreditHistoryConsent: false,
    hasIncomeVerificationConsent: false,
    hasPersonalDataProcessingConsent: false,
    employmentDurationMonths: "24",
    numberOfDependents: "0",
    residentialStatus: 2
  };
}

function toPayload(state: FormState) {
  return {
    loanProductId: state.loanProductId,
    applicantFullName: state.applicantFullName,
    nationalIdNumber: state.nationalIdNumber,
    phoneNumber: state.phoneNumber,
    email: state.email,
    loanPurpose: state.loanPurpose,
    employmentStatus: state.employmentStatus,
    employerOrBusinessName: state.employerOrBusinessName,
    employerOrBusinessRegistrationNumber: state.employerOrBusinessRegistrationNumber.trim() || null,
    loanAmount: Number(state.loanAmount),
    loanTermMonths: Number(state.loanTermMonths),
    monthlyIncome: Number(state.monthlyIncome),
    monthlyExpenses: Number(state.monthlyExpenses),
    existingMonthlyDebt: Number(state.existingMonthlyDebt),
    hasCreditHistoryConsent: state.hasCreditHistoryConsent,
    hasIncomeVerificationConsent: state.hasIncomeVerificationConsent,
    hasPersonalDataProcessingConsent: state.hasPersonalDataProcessingConsent,
    employmentDurationMonths: Number(state.employmentDurationMonths),
    numberOfDependents: Number(state.numberOfDependents),
    residentialStatus: state.residentialStatus
  };
}

function fromApplication(details: LoanApplicationDetails): FormState {
  return {
    loanProductId: details.loanProductId,
    applicantFullName: details.applicantFullName,
    nationalIdNumber: details.nationalIdNumber,
    phoneNumber: details.phoneNumber,
    email: details.email,
    loanPurpose: details.loanPurpose,
    employmentStatus: details.employmentStatus,
    employerOrBusinessName: details.employerOrBusinessName,
    employerOrBusinessRegistrationNumber: details.employerOrBusinessRegistrationNumber ?? "",
    loanAmount: String(details.loanAmount),
    loanTermMonths: String(details.loanTermMonths),
    monthlyIncome: String(details.monthlyIncome),
    monthlyExpenses: String(details.monthlyExpenses),
    existingMonthlyDebt: String(details.existingMonthlyDebt),
    hasCreditHistoryConsent: details.hasCreditHistoryConsent,
    hasIncomeVerificationConsent: details.hasIncomeVerificationConsent,
    hasPersonalDataProcessingConsent: details.hasPersonalDataProcessingConsent,
    employmentDurationMonths: String(details.employmentDurationMonths),
    numberOfDependents: String(details.numberOfDependents),
    residentialStatus: details.residentialStatus
  };
}

export function CreateApplicationPage() {
  const { user, isCustomer } = useAuth();
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const {
    loadProducts,
    loadApplication,
    loadDocuments,
    createDraft,
    updateDraft,
    submitApplication,
    uploadDocument
  } =
    useApplicationFormServices();
  const [products, setProducts] = useState<LoanProduct[]>([]);
  const [form, setForm] = useState<FormState>(() =>
    toFormState(user?.fullName ?? "", user?.email ?? "")
  );
  const [step, setStep] = useState<"details" | "documents">("details");
  const [draftId, setDraftId] = useState<string | null>(id ?? null);
  const [documents, setDocuments] = useState<ApplicationDocument[]>([]);
  const [selectedFiles, setSelectedFiles] = useState<Record<string, File | null>>({});
  const [selectedDocumentTypes, setSelectedDocumentTypes] = useState<Record<string, number>>({
    identity: 1,
    address: 2,
    income: 3
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uploadingSlot, setUploadingSlot] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function run() {
      setLoading(true);
      setError(null);

      try {
        const [loadedProducts, loadedApplication, loadedDocuments] = await Promise.all([
          loadProducts(),
          id ? loadApplication(id) : Promise.resolve(null),
          id ? loadDocuments(id) : Promise.resolve([])
        ]);

        if (cancelled) {
          return;
        }

        setProducts(loadedProducts);
        setDocuments(loadedDocuments);
        setForm(
          loadedApplication
            ? fromApplication(loadedApplication)
            : {
                ...toFormState(user?.fullName ?? "", user?.email ?? ""),
                loanProductId: loadedProducts[0]?.id ?? ""
              }
        );
      } catch (requestError) {
        if (!cancelled) {
          if (requestError instanceof ApiClientError) {
            setError(requestError.errors[0] ?? requestError.message);
          } else {
            setError("Could not load application form.");
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
  }, [id, user?.email, user?.fullName]);

  if (!isCustomer) {
    return <Navigate to="/applications" replace />;
  }

  function updateField<Key extends keyof FormState>(key: Key, value: FormState[Key]) {
    setForm((current) => {
      if (key === "employmentStatus") {
        const employmentStatus = value as number;
        return {
          ...current,
          employmentStatus,
          monthlyIncome: zeroIncomeEmploymentStatuses.has(employmentStatus) ? "0" : current.monthlyIncome
        };
      }

      return { ...current, [key]: value };
    });
  }

  function getSlotDocument(slot: (typeof requiredDocumentSlots)[number]) {
    const matchingDocuments = documents.filter((document) => slot.acceptedTypes.includes(document.documentType));
    const occurrence = "occurrence" in slot ? slot.occurrence : undefined;
    if (occurrence !== undefined) {
      return matchingDocuments[occurrence - 1];
    }

    return matchingDocuments[0];
  }

  function hasRequiredDocuments() {
    const hasIdentity = documents.some((document) => document.documentType === 1);
    const hasAddress = documents.some((document) => document.documentType === 2);
    const payslipCount = documents.filter((document) => document.documentType === 3).length;
    const bankStatementCount = documents.filter((document) => document.documentType === 4).length;

    return hasIdentity && hasAddress && payslipCount >= 3 && bankStatementCount >= 3;
  }

  function validateFile(file: File) {
    const allowedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".tif", ".tiff"];
    const extension = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();

    if (!allowedExtensions.includes(extension)) {
      return "Upload a PDF, JPG, PNG, or TIFF file.";
    }

    if (file.size > maxFileSizeBytes) {
      return "Each document must be 10 MB or smaller.";
    }

    return null;
  }

  async function saveDraft() {
    setSaving(true);
    setError(null);

    try {
      const payload = toPayload(form);
      const saved = isEditing && id
        ? await updateDraft(id, payload)
        : draftId
          ? await updateDraft(draftId, payload)
          : await createDraft(payload);

      setDraftId(saved.id);
      return saved;
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Could not save your application.");
      }
      return null;
    } finally {
      setSaving(false);
    }
  }

  async function handleSaveAndExit() {
    const saved = await saveDraft();
    if (saved) {
      navigate("/applications");
    }
  }

  async function handleNext() {
    const saved = await saveDraft();
    if (!saved) {
      return;
    }

    try {
      setDocuments(await loadDocuments(saved.id));
    } catch {
      setDocuments([]);
    }

    setStep("documents");
  }

  async function handleUpload(slotKey: string) {
    const currentDraftId = draftId;
    const file = selectedFiles[slotKey];
    if (!currentDraftId || !file) {
      setError("Choose a file before uploading.");
      return;
    }

    const fileError = validateFile(file);
    if (fileError) {
      setError(fileError);
      return;
    }

    setUploadingSlot(slotKey);
    setError(null);

    try {
      const uploaded = await uploadDocument(
        currentDraftId,
        selectedDocumentTypes[slotKey],
        file
      );
      setDocuments((current) => [uploaded, ...current]);
      setSelectedFiles((current) => ({ ...current, [slotKey]: null }));
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Could not upload this document.");
      }
    } finally {
      setUploadingSlot(null);
    }
  }

  async function handleSubmitApplication() {
    if (!draftId) {
      setError("Save the application details before submitting.");
      return;
    }

    if (!hasRequiredDocuments()) {
      setError("Upload the required documents before submitting.");
      return;
    }

    setSaving(true);
    setError(null);

    try {
      await submitApplication(draftId);
      navigate(`/applications/${draftId}`);
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Could not submit your application.");
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page-grid">
      <section className="card">
        <div className="card-header">
          <div>
            <span className="eyebrow">{isEditing ? "Edit draft" : "New application"}</span>
            <h3>{isEditing ? "Update your draft" : "Draft a loan request"}</h3>
            <p>
              {step === "details"
                ? "Start with the application details, then continue to required documents."
                : "Upload required documents before submitting the application."}
            </p>
          </div>
        </div>

        <div className="wizard-steps" aria-label="Application progress">
          <span className={step === "details" ? "wizard-step wizard-step-active" : "wizard-step wizard-step-done"}>
            1. Basic info
          </span>
          <span className={step === "documents" ? "wizard-step wizard-step-active" : "wizard-step"}>
            2. Documents
          </span>
        </div>

        {error ? <div className="form-error">{error}</div> : null}

        {loading ? (
          <p>Loading form...</p>
        ) : step === "details" ? (
          <form
            className="application-grid"
            onSubmit={(event) => {
              event.preventDefault();
              void handleNext();
            }}
          >
            <div className="section-title">Applicant details</div>

            <label className="field">
              <span className="field-label">Applicant full name</span>
              <input
                className="field-control"
                type="text"
                value={form.applicantFullName}
                onChange={(event) => updateField("applicantFullName", event.target.value)}
              />
            </label>

            <label className="field">
              <span className="field-label">IC/MyKad number or passport if not Malaysian</span>
              <input
                className="field-control"
                type="text"
                value={form.nationalIdNumber}
                onChange={(event) => updateField("nationalIdNumber", event.target.value)}
              />
            </label>

            <label className="field">
              <span className="field-label">Phone number</span>
              <input
                className="field-control"
                type="tel"
                value={form.phoneNumber}
                onChange={(event) => updateField("phoneNumber", event.target.value)}
              />
            </label>

            <label className="field">
              <span className="field-label">Email</span>
              <input
                className="field-control"
                type="email"
                value={form.email}
                onChange={(event) => updateField("email", event.target.value)}
              />
            </label>

            <label className="field">
              <span className="field-label">Employment status</span>
              <select
                className="field-control"
                value={form.employmentStatus}
                onChange={(event) => updateField("employmentStatus", Number(event.target.value))}
              >
                {employmentOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>

            <label className="field">
              <span className="field-label">Employer or business name</span>
              <input
                className="field-control"
                type="text"
                value={form.employerOrBusinessName}
                onChange={(event) => updateField("employerOrBusinessName", event.target.value)}
              />
            </label>

            <label className="field">
              <span className="field-label">Business registration number</span>
              <input
                className="field-control"
                type="text"
                value={form.employerOrBusinessRegistrationNumber}
                onChange={(event) => updateField("employerOrBusinessRegistrationNumber", event.target.value)}
              />
            </label>

            <label className="field">
              <span className="field-label">Residential status</span>
              <select
                className="field-control"
                value={form.residentialStatus}
                onChange={(event) => updateField("residentialStatus", Number(event.target.value))}
              >
                {residentialOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>

            <div className="section-title">Loan request</div>

            <label className="field">
              <span className="field-label">Loan product</span>
              <select
                className="field-control"
                value={form.loanProductId}
                onChange={(event) => updateField("loanProductId", event.target.value)}
              >
                {products.map((product) => (
                  <option key={product.id} value={product.id}>
                    {product.name}
                  </option>
                ))}
              </select>
            </label>

            <label className="field">
              <span className="field-label">Loan amount</span>
              <input
                className="field-control"
                type="number"
                value={form.loanAmount}
                onChange={(event) => updateField("loanAmount", event.target.value)}
              />
            </label>

            <label className="field">
              <span className="field-label">Loan term (months)</span>
              <input
                className="field-control"
                type="number"
                value={form.loanTermMonths}
                onChange={(event) => updateField("loanTermMonths", event.target.value)}
              />
            </label>

            <div className="section-title">Affordability</div>

            <label className="field">
              <span className="field-label">Monthly income</span>
              <input
                className="field-control"
                type="number"
                value={form.monthlyIncome}
                disabled={zeroIncomeEmploymentStatuses.has(form.employmentStatus)}
                onChange={(event) => updateField("monthlyIncome", event.target.value)}
              />
            </label>

            <label className="field">
              <span className="field-label">Monthly expenses</span>
              <input
                className="field-control"
                type="number"
                value={form.monthlyExpenses}
                onChange={(event) => updateField("monthlyExpenses", event.target.value)}
              />
            </label>

            <label className="field">
              <span className="field-label">Existing monthly debt</span>
              <input
                className="field-control"
                type="number"
                value={form.existingMonthlyDebt}
                onChange={(event) => updateField("existingMonthlyDebt", event.target.value)}
              />
            </label>

            <label className="field">
              <span className="field-label">Employment duration (months)</span>
              <input
                className="field-control"
                type="number"
                value={form.employmentDurationMonths}
                onChange={(event) => updateField("employmentDurationMonths", event.target.value)}
              />
            </label>

            <label className="field">
              <span className="field-label">Number of dependents</span>
              <input
                className="field-control"
                type="number"
                value={form.numberOfDependents}
                onChange={(event) => updateField("numberOfDependents", event.target.value)}
              />
            </label>

            <label className="field field-span-full">
              <span className="field-label">Loan purpose</span>
              <textarea
                className="field-control field-area"
                rows={4}
                value={form.loanPurpose}
                onChange={(event) => updateField("loanPurpose", event.target.value)}
              />
            </label>

            <label className="consent-card field-span-full">
              <input
                type="checkbox"
                checked={form.hasCreditHistoryConsent}
                onChange={(event) => updateField("hasCreditHistoryConsent", event.target.checked)}
              />
              <span>I consent to Lendora checking my credit history, including CCRIS/eCCRIS and credit bureau information.</span>
            </label>

            <label className="consent-card field-span-full">
              <input
                type="checkbox"
                checked={form.hasIncomeVerificationConsent}
                onChange={(event) => updateField("hasIncomeVerificationConsent", event.target.checked)}
              />
              <span>I consent to Lendora verifying my income, employment, payslip, EPF, bank statement, and uploaded documents.</span>
            </label>

            <label className="consent-card field-span-full">
              <input
                type="checkbox"
                checked={form.hasPersonalDataProcessingConsent}
                onChange={(event) => updateField("hasPersonalDataProcessingConsent", event.target.checked)}
              />
              <span>I consent to Lendora processing my personal data for loan assessment, fraud checks, KYC, and application servicing.</span>
            </label>

            <div className="form-actions">
              <button
                className="secondary-button button-compact"
                type="button"
                disabled={saving}
                onClick={() => void handleSaveAndExit()}
              >
                {saving ? "Saving..." : "Save draft"}
              </button>
              <button
                className="primary-button button-compact"
                type="submit"
                disabled={saving}
              >
                {saving ? "Saving..." : "Next"}
              </button>
            </div>
          </form>
        ) : (
          <div className="document-wizard">
            <div className="required-document-grid">
              {requiredDocumentSlots.map((slot) => {
                const uploadedDocument = getSlotDocument(slot);

                return (
                  <section className="required-document-card" key={slot.key}>
                    <div>
                      <span className="required-marker">Required</span>
                      <h4>{slot.label}</h4>
                      <p>{slot.helper}</p>
                    </div>

                    {slot.acceptedTypes.length > 1 ? (
                      <label className="field">
                        <span className="field-label">Document type</span>
                        <select
                          className="field-control"
                          value={selectedDocumentTypes[slot.key]}
                          onChange={(event) =>
                            setSelectedDocumentTypes((current) => ({
                              ...current,
                              [slot.key]: Number(event.target.value)
                            }))
                          }
                        >
                          {documentTypeOptions
                            .filter((option) => slot.acceptedTypes.includes(option.value))
                            .map((option) => (
                              <option key={option.value} value={option.value}>
                                {option.label}
                              </option>
                            ))}
                        </select>
                      </label>
                    ) : null}

                    <label className="file-picker">
                      <span>Choose file</span>
                      <input
                        type="file"
                        accept={acceptedUploadTypes}
                        onChange={(event) => {
                          const file = event.target.files?.[0] ?? null;
                          setSelectedFiles((current) => ({ ...current, [slot.key]: file }));
                          setSelectedDocumentTypes((current) => ({
                            ...current,
                            [slot.key]: slot.acceptedTypes.length === 1
                              ? slot.defaultType
                              : current[slot.key]
                          }));
                        }}
                      />
                    </label>

                    <div className="file-selection">
                      {selectedFiles[slot.key]?.name ?? uploadedDocument?.originalFileName ?? "No file selected"}
                    </div>

                    <button
                      className="secondary-button button-compact"
                      type="button"
                      disabled={uploadingSlot === slot.key}
                      onClick={() => void handleUpload(slot.key)}
                    >
                      {uploadingSlot === slot.key ? "Uploading..." : uploadedDocument ? "Upload replacement" : "Upload"}
                    </button>
                  </section>
                );
              })}
            </div>

            <div className="document-support-note">
              Supported formats: PDF, JPG, JPEG, PNG, TIFF. Income evidence requires 3 recent monthly payslips and 3 recent monthly bank statements. Max size: 10 MB per file.
            </div>

            <div className="form-actions">
              <button
                className="secondary-button button-compact"
                type="button"
                disabled={saving}
                onClick={() => setStep("details")}
              >
                Back
              </button>
              <button
                className="primary-button button-compact"
                type="button"
                disabled={saving || !hasRequiredDocuments()}
                onClick={() => void handleSubmitApplication()}
              >
                {saving ? "Submitting..." : "Submit application"}
              </button>
            </div>
          </div>
        )}
      </section>
    </div>
  );
}
