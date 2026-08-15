import { useEffect, useState, type FormEvent } from "react";
import { Link, Navigate, useLocation, useNavigate, useParams } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";
import { ApiClientError } from "../lib/api";

type LoanApplicationDetails = Awaited<ReturnType<ReturnType<typeof useApplicationDetailsServices>["loadApplication"]>>;
type ApplicationDocument = Awaited<ReturnType<ReturnType<typeof useApplicationDetailsServices>["loadDocuments"]>>[number];
type AffordabilityAssessment = Awaited<ReturnType<ReturnType<typeof useApplicationDetailsServices>["loadAffordability"]>>;
type RiskAssessment = Awaited<ReturnType<ReturnType<typeof useApplicationDetailsServices>["loadRisk"]>>;
type RepaymentScheduleItem = Awaited<ReturnType<ReturnType<typeof useApplicationDetailsServices>["loadRepaymentSchedule"]>>[number];
type ApplicationAuditLog = Awaited<ReturnType<ReturnType<typeof useApplicationDetailsServices>["loadAuditLogs"]>>[number];

type DocumentFormState = {
  documentType: string;
  file: File | null;
};

type BankReviewFormState = {
  creditScore: string;
  creditScoreSource: string;
  ccrisRecordSummary: string;
  ctosScore: string;
  internalAccountHistoryScore: string;
  behaviourScore: string;
  fraudRiskScore: string;
  kycRiskScore: string;
  incomeVerificationStatus: string;
  missedPaymentCount: string;
  approvedLimit: string;
  isLimitLocked: boolean;
  limitDecisionReason: string;
};

type DecisionFormState = {
  status: string;
  offeredAmount: string;
  offeredTermMonths: string;
  decisionNote: string;
};

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

const reviewStatusOptions = [
  { value: 2, label: "Accepted" },
  { value: 3, label: "Rejected" },
  { value: 4, label: "Resubmission required" }
];

const acceptedUploadTypes = ".pdf,.jpg,.jpeg,.png,.tif,.tiff";
const maxFileSizeBytes = 10 * 1024 * 1024;
const policyThresholdSummary = "18x income limit, 70% debt-load ceiling, fraud/KYC 70+ trigger, 2+ missed-payment trigger, accepted required documents.";

function useApplicationDetailsServices() {
  const auth = useAuth();

  return {
    isCustomer: auth.isCustomer,
    isStaff: auth.isStaff,
    isUnderwriter: auth.isUnderwriter,
    isAdmin: auth.isAdmin,
    loadApplication: auth.apiGetApplication,
    submitApplication: auth.apiSubmitApplication,
    acceptOffer: auth.apiAcceptOffer,
    updateBankReview: auth.apiUpdateBankReview,
    updateDecision: auth.apiUpdateDecision,
    loadDocuments: auth.apiGetDocuments,
    uploadDocument: auth.apiUploadDocument,
    reviewDocument: auth.apiReviewDocument,
    runDocumentOcr: auth.apiRunDocumentOcr,
    downloadDocument: auth.apiDownloadDocument,
    runBankChecks: auth.apiRunBankChecks,
    loadAffordability: auth.apiGetAffordability,
    generateAffordability: auth.apiGenerateAffordability,
    loadRisk: auth.apiGetRisk,
    generateRisk: auth.apiGenerateRisk,
    loadRepaymentSchedule: auth.apiGetRepaymentSchedule,
    loadAuditLogs: auth.apiGetApplicationAuditLogs
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

function employmentLabel(status: number) {
  switch (status) {
    case 1:
      return "Employed";
    case 2:
      return "Self-employed";
    case 3:
      return "Unemployed";
    case 4:
      return "Student";
    case 5:
      return "Retired";
    default:
      return "Unknown";
  }
}

function residentialLabel(status: number) {
  switch (status) {
    case 1:
      return "Owner";
    case 2:
      return "Mortgage";
    case 3:
      return "Tenant";
    case 4:
      return "Living with family";
    case 5:
      return "Other";
    default:
      return "Unknown";
  }
}

function documentTypeLabel(type: number) {
  return documentTypeOptions.find((option) => option.value === type)?.label ?? "Unknown";
}

function documentStatusLabel(status: number) {
  switch (status) {
    case 1:
      return "Pending review";
    case 2:
      return "Accepted";
    case 3:
      return "Rejected";
    case 4:
      return "Resubmission required";
    default:
      return "Unknown";
  }
}

function documentStatusClass(status: number) {
  switch (status) {
    case 2:
      return "status-green";
    case 3:
    case 4:
      return "status-red";
    default:
      return "status-amber";
  }
}

function documentOcrStatusLabel(status: number) {
  switch (status) {
    case 1:
      return "Extracted";
    case 2:
      return "Failed";
    case 3:
      return "Not supported";
    default:
      return "Not run";
  }
}

function documentOcrStatusClass(status: number) {
  switch (status) {
    case 1:
      return "status-green";
    case 2:
    case 3:
      return "status-red";
    default:
      return "status-amber";
  }
}

function affordabilityResultLabel(result: number) {
  switch (result) {
    case 1:
      return "Pass";
    case 2:
      return "Caution";
    case 3:
      return "Fail";
    default:
      return "Unknown";
  }
}

function affordabilityResultClass(result: number) {
  switch (result) {
    case 1:
      return "status-green";
    case 2:
      return "status-amber";
    case 3:
      return "status-red";
    default:
      return "status-blue";
  }
}

function riskGradeLabel(grade: number) {
  switch (grade) {
    case 1:
      return "Low";
    case 2:
      return "Medium";
    case 3:
      return "High";
    default:
      return "Unknown";
  }
}

function riskGradeClass(grade: number) {
  switch (grade) {
    case 1:
      return "status-green";
    case 2:
      return "status-amber";
    case 3:
      return "status-red";
    default:
      return "status-blue";
  }
}

function riskRecommendationLabel(recommendation: number) {
  switch (recommendation) {
    case 1:
      return "Approval candidate";
    case 2:
      return "Manual review";
    case 3:
      return "Decline";
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

function formatCurrencyWithPence(amount: number) {
  return new Intl.NumberFormat("en-MY", {
    style: "currency",
    currency: "MYR",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(amount);
}

function formatPercent(value: number) {
  return `${value.toFixed(2)}%`;
}

function formatAnnualRate(value: number) {
  const percent = value > 1 ? value : value * 100;
  return `${percent.toFixed(2)}% p.a.`;
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

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat("en-MY", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit"
  }).format(new Date(value));
}

function formatOptionalCurrency(amount: number | null) {
  return amount === null ? "Not set" : formatCurrency(amount);
}

function formatOptionalNumber(value: number | null) {
  return value === null ? "Not set" : String(value);
}

function formatOfferAmount(application: LoanApplicationDetails) {
  if (application.offeredAmount !== null) {
    return formatCurrency(application.offeredAmount);
  }

  return application.status === 1 ? "Not set" : "Pending";
}

function formatOfferTerm(application: LoanApplicationDetails) {
  if (application.offeredTermMonths !== null) {
    return `${application.offeredTermMonths} months`;
  }

  return application.status === 1 ? "Not set" : "Pending";
}

function automatedProviderNotes(application: LoanApplicationDetails, notes: string[]) {
  if (notes.length > 0) {
    return notes;
  }

  if (application.creditScoreSource !== "Mock bureau check") {
    return [];
  }

  return [
    "Mock CTOS/credit bureau provider used for demo and development.",
    "CCRIS summary is simulated from declared income, existing debt, and application profile.",
    "Replace this provider with a real CTOS/CCRIS integration before production."
  ];
}

function hasAutomatedCheckResults(application: LoanApplicationDetails) {
  return application.creditScore !== null ||
    application.ctosScore !== null ||
    application.ccrisRecordSummary !== null ||
    application.internalAccountHistoryScore !== null ||
    application.behaviourScore !== null ||
    application.fraudRiskScore !== null ||
    application.kycRiskScore !== null ||
    application.incomeVerificationStatus !== null ||
    application.recommendedInitialLimit !== null;
}

function hasCompletedAutomatedCheckResults(application: LoanApplicationDetails) {
  return application.creditScoreSource === "Mock bureau check" &&
    application.creditScore !== null &&
    application.ctosScore !== null &&
    application.ccrisRecordSummary !== null &&
    application.internalAccountHistoryScore !== null &&
    application.behaviourScore !== null &&
    application.fraudRiskScore !== null &&
    application.kycRiskScore !== null &&
    application.incomeVerificationStatus !== null &&
    application.recommendedInitialLimit !== null;
}

function acceptedRequiredDocuments(documents: ApplicationDocument[]) {
  const acceptedDocuments = documents.filter((document) => document.status === 2);
  const acceptedTypes = new Set(acceptedDocuments.map((document) => document.documentType));
  const acceptedPayslipCount = acceptedDocuments.filter((document) => document.documentType === 3).length;
  const acceptedBankStatementCount = acceptedDocuments.filter((document) => document.documentType === 4).length;
  const hasAcceptedIncomeEvidence = acceptedPayslipCount >= 3 && acceptedBankStatementCount >= 3;

  return acceptedTypes.has(1) && acceptedTypes.has(2) && hasAcceptedIncomeEvidence;
}

function requiredDocumentEvidenceIssues(documents: ApplicationDocument[]) {
  const acceptedDocuments = documents.filter((document) => document.status === 2);
  const acceptedTypes = new Set(acceptedDocuments.map((document) => document.documentType));
  const acceptedPayslipCount = acceptedDocuments.filter((document) => document.documentType === 3).length;
  const acceptedBankStatementCount = acceptedDocuments.filter((document) => document.documentType === 4).length;
  const requiredEvidenceDocuments = acceptedDocuments.filter((document) => [1, 2, 3, 4].includes(document.documentType));
  const issues: string[] = [];

  if (!acceptedTypes.has(1)) {
    issues.push("Accepted ID document is missing.");
  }

  if (!acceptedTypes.has(2)) {
    issues.push("Accepted proof of address is missing.");
  }

  if (acceptedPayslipCount < 3) {
    issues.push("Three accepted recent monthly payslips are required to verify income stability.");
  }

  if (acceptedBankStatementCount < 3) {
    issues.push("Three accepted recent monthly bank statements are required to verify spending and cashflow.");
  }

  if (requiredEvidenceDocuments.some((document) => document.ocrStatus !== 1)) {
    issues.push("Run OCR on all accepted ID, address, payslip, and bank statement evidence.");
  }

  if (requiredEvidenceDocuments.some((document) => document.ocrVerificationStatus === "Review")) {
    issues.push("At least one accepted required document has OCR verification findings requiring review.");
  }

  if (requiredEvidenceDocuments.some((document) => [2, 3, 4].includes(document.documentType) && document.ocrIsRecent === false)) {
    issues.push("At least one address, payslip, or bank statement document appears older than 3 months.");
  }

  return issues;
}

function averageVerifiedPayslipIncome(documents: ApplicationDocument[]) {
  const incomes = documents
    .filter((document) => document.documentType === 3 && document.ocrStatus === 1 && document.ocrSuggestedMonthlyIncome !== null)
    .map((document) => document.ocrSuggestedMonthlyIncome as number)
    .filter((value) => value > 0);

  return {
    count: incomes.length,
    average: incomes.length === 0 ? null : incomes.reduce((total, value) => total + value, 0) / incomes.length
  };
}

function documentReadinessLabel(documents: ApplicationDocument[]) {
  if (requiredDocumentEvidenceIssues(documents).length === 0) {
    return "Ready";
  }

  if (documents.some((document) => document.status === 4)) {
    return "More documents requested";
  }

  if (documents.some((document) => document.status === 3)) {
    return "Rejected document";
  }

  return "Pending review";
}

function anomalyFlags(application: LoanApplicationDetails, documents: ApplicationDocument[]) {
  const flags = [];
  const payslipIncomeCheck = averageVerifiedPayslipIncome(documents);
  const monthlyDebtLoad = application.monthlyIncome <= 0
    ? 0
    : ((application.monthlyExpenses + application.existingMonthlyDebt) / application.monthlyIncome) * 100;

  if (application.loanAmount > application.monthlyIncome * 18) {
    flags.push("Requested loan amount is high compared with declared monthly income.");
  }

  if (monthlyDebtLoad > 70) {
    flags.push("Declared monthly expenses and debt consume more than 70% of income.");
  }

  if ((application.fraudRiskScore ?? 0) >= 70) {
    flags.push("Fraud risk score is elevated.");
  }

  if ((application.kycRiskScore ?? 0) >= 70) {
    flags.push("KYC risk score is elevated.");
  }

  if (application.missedPaymentCount >= 2) {
    flags.push("Multiple missed payments are present in the simulated credit profile.");
  }

  flags.push(...requiredDocumentEvidenceIssues(documents));

  if (application.monthlyIncome > 0 && payslipIncomeCheck.average !== null) {
    const incomeVariance = Math.abs(payslipIncomeCheck.average - application.monthlyIncome) / application.monthlyIncome;
    if (incomeVariance > 0.15) {
      flags.push(`${payslipIncomeCheck.count}/3 OCR payslip income average ${formatCurrency(payslipIncomeCheck.average)} differs from declared income ${formatCurrency(application.monthlyIncome)} by more than 15%.`);
    }
  }

  return flags;
}

function decisionSupport(application: LoanApplicationDetails, affordability: AffordabilityAssessment | null, risk: RiskAssessment | null, documents: ApplicationDocument[]) {
  const flags = anomalyFlags(application, documents);
  const suggestedOfferAmount = application.approvedLimit === null
    ? null
    : Math.min(application.loanAmount, application.approvedLimit);

  if (!hasCompletedAutomatedCheckResults(application) || !affordability || !risk) {
    return {
      action: "Run checks first",
      statusClass: "status-blue",
      summary: "The engine does not have enough verified inputs to recommend an underwriting action yet.",
      nextSteps: [
        "Run automated bank checks.",
        "Generate affordability and risk assessments.",
        "Review required documents before preparing any offer."
      ],
      suggestedOfferAmount
    };
  }

  const documentIssues = requiredDocumentEvidenceIssues(documents);
  if (documentIssues.length > 0) {
    return {
      action: "Hold for documents",
      statusClass: "status-amber",
      summary: "Risk may be acceptable, but required document evidence is not fully verified.",
      nextSteps: [
        documentIssues[0],
        "Do not prepare a final offer until document readiness is ready.",
        "Record the document issue in the decision note."
      ],
      suggestedOfferAmount
    };
  }

  if (risk.recommendation === 3 || affordability.result === 3) {
    return {
      action: "Recommend reject",
      statusClass: "status-red",
      summary: "The rule engine sees a failed affordability or high-risk outcome.",
      nextSteps: [
        "Check whether new verified evidence changes the result.",
        "If not, reject with a clear underwriter rationale.",
        "Do not override without documenting the reason."
      ],
      suggestedOfferAmount
    };
  }

  if (risk.recommendation === 2 || affordability.result === 2 || flags.length > 0) {
    return {
      action: "Manual review",
      statusClass: "status-amber",
      summary: "The case is not clean enough for an approval recommendation.",
      nextSteps: [
        "Review each flag and risk factor.",
        "Adjust the offer only if the approved credit limit and affordability support it.",
        "Document the underwriter rationale before final decision."
      ],
      suggestedOfferAmount
    };
  }

  return {
    action: "Prepare approval",
    statusClass: "status-green",
    summary: "Strong score, passing affordability, ready documents, and no high-priority flags. This is an approval candidate.",
    nextSteps: [
      "Prepare an offer at or below the suggested amount.",
      "Keep the requested term unless bank policy requires adjustment.",
      "Underwriter/admin must still click final approve and save the rationale."
    ],
    suggestedOfferAmount
  };
}

function decisionEvidence(application: LoanApplicationDetails, affordability: AffordabilityAssessment | null, risk: RiskAssessment | null, documents: ApplicationDocument[]) {
  const payslipIncomeCheck = averageVerifiedPayslipIncome(documents);
  const monthlyDebtLoad = application.monthlyIncome <= 0
    ? null
    : ((application.monthlyExpenses + application.existingMonthlyDebt) / application.monthlyIncome) * 100;

  return [
    risk
      ? `Risk: ${risk.score}/100, ${riskGradeLabel(risk.grade).toLowerCase()}, ${riskRecommendationLabel(risk.recommendation).toLowerCase()}.`
      : "Risk: not generated.",
    affordability
      ? `Affordability: ${affordabilityResultLabel(affordability.result).toLowerCase()}, DSR ${formatPercent(affordability.debtServiceRatio)}, disposable income ${formatCurrency(affordability.disposableIncome)}.`
      : "Affordability: not generated.",
    `Payslip income check: ${payslipIncomeCheck.average === null ? "not available until OCR finds payslip income" : `${payslipIncomeCheck.count}/3 OCR payslips, average ${formatCurrency(payslipIncomeCheck.average)} versus declared ${formatCurrency(application.monthlyIncome)}`}.`,
    `Debt load: ${monthlyDebtLoad === null ? "not available" : formatPercent(monthlyDebtLoad)} against the 70% flag threshold.`,
    `Documents: ${documentReadinessLabel(documents).toLowerCase()}.`,
    `Policy thresholds checked: ${policyThresholdSummary}`
  ];
}

function formatAuditDetails(details: string) {
  if (!details.trim().startsWith("{")) {
    return details;
  }

  try {
    const parsed = JSON.parse(details) as Record<string, unknown>;
    const factors = Array.isArray(parsed.Factors)
      ? parsed.Factors.filter((factor): factor is string => typeof factor === "string")
      : [];
    const score = typeof parsed.Score === "number" ? parsed.Score : null;
    const grade = typeof parsed.Grade === "number" ? riskGradeLabel(parsed.Grade) : String(parsed.Grade ?? "Unknown");
    const recommendation = typeof parsed.Recommendation === "number"
      ? riskRecommendationLabel(parsed.Recommendation)
      : String(parsed.Recommendation ?? "Unknown");
    const reasonText = factors.length > 0 ? ` Reasons: ${factors.slice(0, 3).join("; ")}.` : "";

    if (score !== null || parsed.Recommendation !== undefined || parsed.HumanApprovalRequired !== undefined) {
      return `Score ${score ?? "N/A"}, grade ${grade}, recommendation ${recommendation}. Human approval required.${reasonText}`;
    }
  } catch {
    return "Risk assessment details were saved in a legacy audit format.";
  }

  return details;
}

function formatFileSize(bytes: number) {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}

function sumSchedule(schedule: RepaymentScheduleItem[], selector: (item: RepaymentScheduleItem) => number) {
  return schedule.reduce((total, item) => total + selector(item), 0);
}

function DetailItem({ label, value }: { label: string; value: string }) {
  return (
    <div className="detail-item">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function toBankReviewForm(application: LoanApplicationDetails): BankReviewFormState {
  return {
    creditScore: application.creditScore === null ? "" : String(application.creditScore),
    creditScoreSource: application.creditScoreSource ?? "",
    ccrisRecordSummary: application.ccrisRecordSummary ?? "",
    ctosScore: application.ctosScore === null ? "" : String(application.ctosScore),
    internalAccountHistoryScore: application.internalAccountHistoryScore === null ? "" : String(application.internalAccountHistoryScore),
    behaviourScore: application.behaviourScore === null ? "" : String(application.behaviourScore),
    fraudRiskScore: application.fraudRiskScore === null ? "" : String(application.fraudRiskScore),
    kycRiskScore: application.kycRiskScore === null ? "" : String(application.kycRiskScore),
    incomeVerificationStatus: application.incomeVerificationStatus ?? "",
    missedPaymentCount: String(application.missedPaymentCount),
    approvedLimit: application.approvedLimit === null ? "" : String(application.approvedLimit),
    isLimitLocked: application.isLimitLocked,
    limitDecisionReason: application.limitDecisionReason ?? ""
  };
}

function toNullableNumber(value: string) {
  const trimmed = value.trim();
  return trimmed === "" ? null : Number(trimmed);
}

function toDecisionForm(application: LoanApplicationDetails): DecisionFormState {
  const decisionStatuses = [4, 5, 6, 7, 8, 9];

  return {
    status: String(decisionStatuses.includes(application.status) ? application.status : 4),
    offeredAmount: application.offeredAmount === null ? "" : String(application.offeredAmount),
    offeredTermMonths: application.offeredTermMonths === null ? "" : String(application.offeredTermMonths),
    decisionNote: application.decisionNote ?? ""
  };
}

export function ApplicationDetailsPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { id } = useParams();
  const {
    isCustomer,
    isStaff,
    isUnderwriter,
    isAdmin,
    loadApplication,
    submitApplication,
    acceptOffer,
    updateBankReview,
    updateDecision,
    loadDocuments,
    uploadDocument,
    reviewDocument,
    runDocumentOcr,
    downloadDocument,
    runBankChecks,
    loadAffordability,
    generateAffordability,
    loadRisk,
    generateRisk,
    loadRepaymentSchedule,
    loadAuditLogs
  } = useApplicationDetailsServices();
  const [application, setApplication] = useState<LoanApplicationDetails | null>(null);
  const [documents, setDocuments] = useState<ApplicationDocument[]>([]);
  const [affordability, setAffordability] = useState<AffordabilityAssessment | null>(null);
  const [risk, setRisk] = useState<RiskAssessment | null>(null);
  const [repaymentSchedule, setRepaymentSchedule] = useState<RepaymentScheduleItem[]>([]);
  const [auditLogs, setAuditLogs] = useState<ApplicationAuditLog[]>([]);
  const [documentForm, setDocumentForm] = useState<DocumentFormState>({
    documentType: "1",
    file: null
  });
  const [bankReviewForm, setBankReviewForm] = useState<BankReviewFormState>({
    creditScore: "",
    creditScoreSource: "",
    ccrisRecordSummary: "",
    ctosScore: "",
    internalAccountHistoryScore: "",
    behaviourScore: "",
    fraudRiskScore: "",
    kycRiskScore: "",
    incomeVerificationStatus: "",
    missedPaymentCount: "0",
    approvedLimit: "",
    isLimitLocked: false,
    limitDecisionReason: ""
  });
  const [decisionForm, setDecisionForm] = useState<DecisionFormState>({
    status: "4",
    offeredAmount: "",
    offeredTermMonths: "",
    decisionNote: ""
  });
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [assessing, setAssessing] = useState(false);
  const [scoringRisk, setScoringRisk] = useState(false);
  const [savingBankReview, setSavingBankReview] = useState(false);
  const [checkingBank, setCheckingBank] = useState(false);
  const [savingDecision, setSavingDecision] = useState(false);
  const [savingDocument, setSavingDocument] = useState(false);
  const [reviewingDocumentId, setReviewingDocumentId] = useState<string | null>(null);
  const [extractingDocumentId, setExtractingDocumentId] = useState<string | null>(null);
  const [providerNotes, setProviderNotes] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function run() {
      if (!id) {
        return;
      }

      setLoading(true);
      setError(null);

      try {
        const [data, loadedDocuments] = await Promise.all([
          loadApplication(id),
          loadDocuments(id)
        ]);
        let loadedAffordability: AffordabilityAssessment | null = null;

        if (isStaff) {
          try {
            loadedAffordability = await loadAffordability(id);
          } catch {
            loadedAffordability = null;
          }
        }

        let loadedRisk: RiskAssessment | null = null;

        if (isStaff) {
          try {
            loadedRisk = await loadRisk(id);
          } catch {
            loadedRisk = null;
          }
        }

        let loadedRepaymentSchedule: RepaymentScheduleItem[] = [];
        if (data.status === 5 || data.status === 9) {
          try {
            loadedRepaymentSchedule = await loadRepaymentSchedule(id);
          } catch {
            loadedRepaymentSchedule = [];
          }
        }

        let loadedAuditLogs: ApplicationAuditLog[] = [];
        if (isStaff) {
          try {
            loadedAuditLogs = await loadAuditLogs(id);
          } catch {
            loadedAuditLogs = [];
          }
        }

        if (!cancelled) {
          setApplication(data);
          setBankReviewForm(toBankReviewForm(data));
          setDecisionForm(toDecisionForm(data));
          setDocuments(loadedDocuments);
          setAffordability(loadedAffordability);
          setRisk(loadedRisk);
          setRepaymentSchedule(loadedRepaymentSchedule);
          setAuditLogs(loadedAuditLogs);
        }
      } catch (requestError) {
        if (!cancelled) {
          if (requestError instanceof ApiClientError) {
            setError(requestError.errors[0] ?? requestError.message);
          } else {
            setError("Could not load this application.");
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
  }, [id]);

  if (!id) {
    return <Navigate to="/applications" replace />;
  }

  async function refreshAuditLogs(applicationId: string) {
    if (!isStaff) {
      return;
    }

    try {
      setAuditLogs(await loadAuditLogs(applicationId));
    } catch {
      setAuditLogs([]);
    }
  }

  async function handleSubmit() {
    if (!application) {
      return;
    }

    setSubmitting(true);
    setError(null);

    try {
      const submitted = await submitApplication(application.id);
      setApplication(submitted);
      setBankReviewForm(toBankReviewForm(submitted));
      setDecisionForm(toDecisionForm(submitted));
      await refreshAuditLogs(submitted.id);
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Could not submit this application.");
      }
    } finally {
      setSubmitting(false);
    }
  }

  async function handleRunAffordability() {
    if (!application) {
      return;
    }

    setAssessing(true);
    setError(null);

    try {
      const assessment = await generateAffordability(application.id);
      const refreshedApplication = await loadApplication(application.id);
      setAffordability(assessment);
      setApplication(refreshedApplication);
      setBankReviewForm(toBankReviewForm(refreshedApplication));
      setDecisionForm(toDecisionForm(refreshedApplication));
      setRisk(null);
      await refreshAuditLogs(refreshedApplication.id);
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Could not run affordability assessment.");
      }
    } finally {
      setAssessing(false);
    }
  }

  async function handleRunRisk() {
    if (!application) {
      return;
    }

    setScoringRisk(true);
    setError(null);

    try {
      const assessment = await generateRisk(application.id);
      const refreshedApplication = await loadApplication(application.id);
      setRisk(assessment);
      setApplication(refreshedApplication);
      setBankReviewForm(toBankReviewForm(refreshedApplication));
      setDecisionForm(toDecisionForm(refreshedApplication));
      await refreshAuditLogs(refreshedApplication.id);
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Could not run risk assessment.");
      }
    } finally {
      setScoringRisk(false);
    }
  }

  async function handleAddDocument(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!application) {
      return;
    }

    if (!documentForm.file) {
      setError("Choose a file before uploading.");
      return;
    }

    const fileError = validateFile(documentForm.file);
    if (fileError) {
      setError(fileError);
      return;
    }

    setSavingDocument(true);
    setError(null);

    try {
      const created = await uploadDocument(application.id, Number(documentForm.documentType), documentForm.file);
      setDocuments((current) => [created, ...current]);
      setDocumentForm({
        documentType: "1",
        file: null
      });
      await refreshAuditLogs(application.id);
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Could not add document metadata.");
      }
    } finally {
      setSavingDocument(false);
    }
  }

  async function handleBankReviewSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!application) {
      return;
    }

    setSavingBankReview(true);
    setError(null);

    try {
      const updated = await updateBankReview(application.id, {
        creditScore: toNullableNumber(bankReviewForm.creditScore),
        creditScoreSource: bankReviewForm.creditScoreSource.trim() || null,
        ccrisRecordSummary: bankReviewForm.ccrisRecordSummary.trim() || null,
        ctosScore: toNullableNumber(bankReviewForm.ctosScore),
        internalAccountHistoryScore: toNullableNumber(bankReviewForm.internalAccountHistoryScore),
        behaviourScore: toNullableNumber(bankReviewForm.behaviourScore),
        fraudRiskScore: toNullableNumber(bankReviewForm.fraudRiskScore),
        kycRiskScore: toNullableNumber(bankReviewForm.kycRiskScore),
        incomeVerificationStatus: bankReviewForm.incomeVerificationStatus.trim() || null,
        missedPaymentCount: Number(bankReviewForm.missedPaymentCount || 0),
        approvedLimit: toNullableNumber(bankReviewForm.approvedLimit),
        isLimitLocked: bankReviewForm.isLimitLocked,
        limitDecisionReason: bankReviewForm.limitDecisionReason.trim() || null
      });
      setApplication(updated);
      setBankReviewForm(toBankReviewForm(updated));
      setDecisionForm(toDecisionForm(updated));
      setRisk(null);
      await refreshAuditLogs(updated.id);
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Could not save bank review.");
      }
    } finally {
      setSavingBankReview(false);
    }
  }

  async function handleRunBankChecks() {
    if (!application) {
      return;
    }

    setCheckingBank(true);
    setError(null);

    try {
      const result = await runBankChecks(application.id);
      if (!hasCompletedAutomatedCheckResults(result.application)) {
        throw new Error("Automated bank checks returned an incomplete result. Confirm the API was restarted with the latest backend code, then run the checks again.");
      }

      setApplication(result.application);
      setBankReviewForm(toBankReviewForm(result.application));
      setDecisionForm(toDecisionForm(result.application));
      setAffordability(result.affordability);
      setRisk(result.risk);
      setProviderNotes(result.providerNotes);
      await refreshAuditLogs(result.application.id);
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else if (requestError instanceof Error) {
        setError(requestError.message);
      } else {
        setError("Could not run automated bank checks.");
      }
    } finally {
      setCheckingBank(false);
    }
  }

  async function handleDecisionSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!application) {
      return;
    }

    setSavingDecision(true);
    setError(null);

    try {
      const updated = await updateDecision(application.id, {
        status: Number(decisionForm.status),
        offeredAmount: toNullableNumber(decisionForm.offeredAmount),
        offeredTermMonths: toNullableNumber(decisionForm.offeredTermMonths),
        decisionNote: decisionForm.decisionNote.trim() || null
      });
      setApplication(updated);
      setBankReviewForm(toBankReviewForm(updated));
      setDecisionForm(toDecisionForm(updated));
      if (updated.status === 5 || updated.status === 9) {
        setRepaymentSchedule(await loadRepaymentSchedule(updated.id));
      } else {
        setRepaymentSchedule([]);
      }
      await refreshAuditLogs(updated.id);
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Could not save application decision.");
      }
    } finally {
      setSavingDecision(false);
    }
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

  async function handleAcceptOffer() {
    if (!application) {
      return;
    }

    setSubmitting(true);
    setError(null);

    try {
      const updated = await acceptOffer(application.id);
      setApplication(updated);
      setDecisionForm(toDecisionForm(updated));
      setRepaymentSchedule(await loadRepaymentSchedule(updated.id));
      await refreshAuditLogs(updated.id);
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Could not accept this loan offer.");
      }
    } finally {
      setSubmitting(false);
    }
  }

  async function handleReviewDocument(documentId: string, status: number) {
    const reviewNote = status === 2 ? null : window.prompt("Add a review note")?.trim();
    if (status !== 2 && !reviewNote) {
      return;
    }

    setReviewingDocumentId(documentId);
    setError(null);

    try {
      const reviewed = await reviewDocument(documentId, {
        status,
        reviewNote: reviewNote || null
      });
      setDocuments((current) =>
        current.map((document) => document.id === reviewed.id ? reviewed : document)
      );
      await refreshAuditLogs(reviewed.loanApplicationId);
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Could not review this document.");
      }
    } finally {
      setReviewingDocumentId(null);
    }
  }

  async function handleRunDocumentOcr(documentId: string) {
    setExtractingDocumentId(documentId);
    setError(null);

    try {
      const extracted = await runDocumentOcr(documentId);
      setDocuments((current) =>
        current.map((document) => document.id === extracted.id ? extracted : document)
      );
      await refreshAuditLogs(extracted.loanApplicationId);
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Could not run OCR extraction for this document.");
      }
    } finally {
      setExtractingDocumentId(null);
    }
  }

  async function handleOpenDocument(document: ApplicationDocument) {
    setError(null);

    try {
      const blob = await downloadDocument(document.id);
      const url = URL.createObjectURL(blob);
      window.open(url, "_blank", "noopener,noreferrer");
      window.setTimeout(() => URL.revokeObjectURL(url), 60000);
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Could not open this document.");
      }
    }
  }

  async function handleDownloadDocument(document: ApplicationDocument) {
    setError(null);

    try {
      const blob = await downloadDocument(document.id);
      const url = URL.createObjectURL(blob);
      const link = window.document.createElement("a");
      link.href = url;
      link.download = document.originalFileName;
      link.click();
      window.setTimeout(() => URL.revokeObjectURL(url), 60000);
    } catch (requestError) {
      if (requestError instanceof ApiClientError) {
        setError(requestError.errors[0] ?? requestError.message);
      } else {
        setError("Could not download this document.");
      }
    }
  }

  const isDraft = application?.status === 1;
  const canMakeFinalDecision = isUnderwriter || isAdmin;
  const decisionLocked = application?.status === 1 || application?.status === 9 || (application?.status === 8 && !isAdmin);
  const finalDecisionFieldsLocked = decisionLocked || !canMakeFinalDecision;
  const bankReviewLocked = application?.status === 1 || application?.status === 9 || application?.status === 8;
  const approveSelected = decisionForm.status === "5";
  const navigationState = location.state as { returnTo?: string; returnLabel?: string } | null;
  const backTarget = navigationState?.returnTo ?? "/applications";
  const backLabel = navigationState?.returnLabel ?? "Back to applications";
  const approvalReady = application
    ? hasCompletedAutomatedCheckResults(application) &&
      acceptedRequiredDocuments(documents) &&
      affordability !== null &&
      risk !== null &&
      decisionForm.offeredAmount.trim() !== "" &&
      decisionForm.offeredTermMonths.trim() !== ""
    : false;
  const reviewFlags = application ? anomalyFlags(application, documents) : [];
  const support = application ? decisionSupport(application, affordability, risk, documents) : null;
  const evidenceItems = application ? decisionEvidence(application, affordability, risk, documents) : [];

  return (
    <div className="page-grid">
      <section className="card">
        <div className="card-header">
          <div>
            <span className="eyebrow">Application details</span>
            <h3>{application?.loanProductName ?? "Loan application"}</h3>
            <p>Review the request, submit drafts, and confirm locked applications after submission.</p>
          </div>
          <Link to={backTarget} className="secondary-button button-compact">
            {backLabel}
          </Link>
        </div>

        {error ? <div className="form-error">{error}</div> : null}

        {loading ? (
          <p>Loading application...</p>
        ) : !application ? (
          <div className="empty-card">
            <div>
              <span className="empty-icon">N/A</span>
              <p>This application could not be found.</p>
            </div>
          </div>
        ) : (
          <>
            <div className="details-hero">
              <div>
                <span className={`status-pill ${statusClass(application.status)}`}>
                  {statusLabel(application.status)}
                </span>
                <h2>{formatCurrency(application.loanAmount)}</h2>
                <p>
                  {application.loanTermMonths} months for {application.loanPurpose || "a customer loan request"}
                </p>
              </div>
              <div className="details-actions">
                {isCustomer && isDraft ? (
                  <>
                    <button
                      className="primary-button button-compact"
                      type="button"
                      disabled={submitting}
                      onClick={() => void handleSubmit()}
                    >
                      {submitting ? "Submitting..." : "Submit application"}
                    </button>
                    <button
                      className="secondary-button button-compact"
                      type="button"
                      onClick={() => navigate(`/applications/${application.id}/edit`)}
                    >
                      Edit draft
                    </button>
                  </>
                ) : (
                  <span className="locked-note">Locked after submission</span>
                )}
              </div>
            </div>

            <div className="details-grid">
              <section className="detail-section">
                <h4>Applicant</h4>
                <DetailItem label="Full name" value={application.applicantFullName} />
                <DetailItem label="IC/MyKad or passport" value={application.nationalIdNumber} />
                <DetailItem label="Phone number" value={application.phoneNumber} />
                <DetailItem label="Email" value={application.email} />
                <DetailItem label="Employment" value={employmentLabel(application.employmentStatus)} />
                <DetailItem label="Residential status" value={residentialLabel(application.residentialStatus)} />
              </section>

              <section className="detail-section">
                <h4>Loan request</h4>
                <DetailItem label="Product" value={application.loanProductName} />
                <DetailItem label="Amount" value={formatCurrency(application.loanAmount)} />
                <DetailItem label="Term" value={`${application.loanTermMonths} months`} />
                <DetailItem label="Loan offer amount" value={formatOfferAmount(application)} />
                <DetailItem label="Offered term" value={formatOfferTerm(application)} />
                <DetailItem label="Created" value={formatDate(application.createdAtUtc)} />
              </section>

              <section className="detail-section">
                <h4>Affordability inputs</h4>
                <DetailItem label="Monthly income" value={formatCurrency(application.monthlyIncome)} />
                <DetailItem label="Monthly expenses" value={formatCurrency(application.monthlyExpenses)} />
                <DetailItem label="Existing debt" value={formatCurrency(application.existingMonthlyDebt)} />
                <DetailItem label="Dependents" value={String(application.numberOfDependents)} />
                <DetailItem label="Employer/business" value={application.employerOrBusinessName} />
              </section>

              <section className="detail-section">
                <h4>{isStaff ? "Bank-only review" : "Checks"}</h4>
                <DetailItem label="Credit history consent" value={application.hasCreditHistoryConsent ? "Granted" : "Not granted"} />
                <DetailItem label="Income verification consent" value={application.hasIncomeVerificationConsent ? "Granted" : "Not granted"} />
                <DetailItem label="Personal data consent" value={application.hasPersonalDataProcessingConsent ? "Granted" : "Not granted"} />
                {isStaff ? (
                  <>
                    <DetailItem label="Credit score" value={formatOptionalNumber(application.creditScore)} />
                    <DetailItem label="CTOS score" value={formatOptionalNumber(application.ctosScore)} />
                    <DetailItem label="Behaviour score" value={formatOptionalNumber(application.behaviourScore)} />
                  </>
                ) : null}
                <DetailItem
                  label="Employment duration"
                  value={`${application.employmentDurationMonths} months`}
                />
                <DetailItem label="Submitted" value={formatDate(application.submittedAtUtc)} />
              </section>
            </div>

            {isStaff ? (
              <section className="ai-assist-panel">
                <div className="card-header">
                  <div>
                    <span className="eyebrow">AI assistance</span>
                    <h3>Underwriting decision support</h3>
                    <p>Rule-based recommendation, offer guardrails, review flags, and explainable next steps.</p>
                  </div>
                  <span className="status-pill status-blue">Advisory only</span>
                </div>

                <div className="ai-assist-grid">
                  <div className="ai-assist-box ai-assist-primary">
                    <span>Recommended action</span>
                    <div className="decision-action-row">
                      <strong>{support?.action}</strong>
                      <span className={`status-pill ${support?.statusClass ?? "status-blue"}`}>Rule outcome</span>
                    </div>
                    <p>{support?.summary}</p>
                    {support && support.suggestedOfferAmount !== null ? (
                      <p>Suggested offer cap: {formatCurrency(support.suggestedOfferAmount)} over {application.loanTermMonths} months.</p>
                    ) : (
                      <p>Suggested offer cap is unavailable until the approved credit limit is set.</p>
                    )}
                  </div>
                  <div className="ai-assist-box">
                    <span>Final controls</span>
                    <ul className="factor-list">
                      {(support?.nextSteps ?? []).map((step) => (
                        <li key={step}>{step}</li>
                      ))}
                    </ul>
                  </div>
                  <div className="ai-assist-box">
                    <span>Flags to review</span>
                    {reviewFlags.length > 0 ? (
                      <ul className="factor-list">
                        {reviewFlags.map((flag) => (
                          <li key={flag}>{flag}</li>
                        ))}
                      </ul>
                    ) : (
                      <p>No high-priority rule flags from current application data.</p>
                    )}
                  </div>
                  <div className="ai-assist-box">
                    <span>Evidence used</span>
                    <ul className="factor-list">
                      {evidenceItems.map((item) => (
                        <li key={item}>{item}</li>
                      ))}
                    </ul>
                  </div>
                </div>
              </section>
            ) : null}

            {isStaff ? (
              <section className="bank-review-panel">
                <div className="card-header">
                  <div>
                    <span className="eyebrow">Bank worker only</span>
                    <h3>Automated bank checks</h3>
                    <p>Run this to complete mock CTOS, bureau, CCRIS, internal behaviour, affordability, and risk checks before approving.</p>
                  </div>
                  {application.status === 1 ? (
                    <span className="locked-note">Submit before checks</span>
                  ) : (
                    <button
                      className="primary-button button-compact"
                      type="button"
                      disabled={checkingBank || application.status === 8 || application.status === 9}
                      onClick={() => void handleRunBankChecks()}
                    >
                      {checkingBank ? "Running checks..." : "Run automated checks"}
                    </button>
                  )}
                </div>

                {automatedProviderNotes(application, providerNotes).length > 0 ? (
                  <ul className="factor-list bank-check-notes">
                    {automatedProviderNotes(application, providerNotes).map((note) => (
                      <li key={note}>{note}</li>
                    ))}
                  </ul>
                ) : null}

                {hasCompletedAutomatedCheckResults(application) ? (
                  <div className="automated-results-grid">
                    <section className="automated-result-section">
                      <h4>Bureau and CCRIS</h4>
                      <DetailItem label="Credit bureau score" value={formatOptionalNumber(application.creditScore)} />
                      <DetailItem label="Score source" value={application.creditScoreSource ?? "Not checked"} />
                      <DetailItem label="Checked at" value={formatDate(application.creditScoreCheckedAtUtc)} />
                      <DetailItem label="CTOS score" value={formatOptionalNumber(application.ctosScore)} />
                      <DetailItem label="CCRIS/eCCRIS summary" value={application.ccrisRecordSummary ?? "Not checked"} />
                    </section>

                    <section className="automated-result-section">
                      <h4>Internal behaviour</h4>
                      <DetailItem label="Internal account history" value={formatOptionalNumber(application.internalAccountHistoryScore)} />
                      <DetailItem label="Behaviour score" value={formatOptionalNumber(application.behaviourScore)} />
                      <DetailItem label="Missed payments" value={String(application.missedPaymentCount)} />
                      <DetailItem label="Income verification" value={application.incomeVerificationStatus ?? "Not checked"} />
                    </section>

                    <section className="automated-result-section">
                      <h4>KYC and limit</h4>
                      <DetailItem label="Fraud risk score" value={formatOptionalNumber(application.fraudRiskScore)} />
                      <DetailItem label="KYC risk score" value={formatOptionalNumber(application.kycRiskScore)} />
                      <DetailItem label="Recommended starting limit" value={formatOptionalCurrency(application.recommendedInitialLimit)} />
                      <DetailItem label="Approved credit limit" value={formatOptionalCurrency(application.approvedLimit)} />
                      <DetailItem label="Limit status" value={application.isLimitLocked ? "Locked or reduced" : "Active"} />
                    </section>

                    <section className="automated-result-section">
                      <h4>Affordability and risk</h4>
                      <DetailItem label="Affordability" value={affordability ? affordabilityResultLabel(affordability.result) : "Not calculated"} />
                      <DetailItem label="DSR" value={affordability ? formatPercent(affordability.debtServiceRatio) : "Not calculated"} />
                      <DetailItem label="Monthly repayment" value={affordability ? formatCurrencyWithPence(affordability.monthlyRepayment) : "Not calculated"} />
                      <DetailItem label="Risk score" value={risk ? String(risk.score) : "Not scored"} />
                      <DetailItem label="Recommendation" value={risk ? riskRecommendationLabel(risk.recommendation) : "Not scored"} />
                    </section>
                  </div>
                ) : hasAutomatedCheckResults(application) ? (
                  <div className="empty-inline">
                    Bank check data is incomplete or from an older/manual review. Run automated checks again after restarting the API.
                  </div>
                ) : (
                  <div className="empty-inline">No automated bank checks have been run yet.</div>
                )}

                <div className="card-header bank-review-subheader">
                  <div>
                    <span className="eyebrow">Staff override</span>
                    <h3>Review and limit controls</h3>
                    <p>Adjust generated values only when verified documents, bureau reports, or bank policy require an override.</p>
                  </div>
                </div>

                <form className="bank-review-form" onSubmit={handleBankReviewSubmit}>
                  <label className="field">
                    <span className="field-label">Credit bureau score</span>
                    <input
                      className="field-control"
                      type="number"
                      min="300"
                      max="850"
                      value={bankReviewForm.creditScore}
                      disabled={bankReviewLocked}
                      onChange={(event) =>
                        setBankReviewForm((current) => ({ ...current, creditScore: event.target.value }))
                      }
                    />
                  </label>

                  <label className="field">
                    <span className="field-label">Score source</span>
                    <input
                      className="field-control"
                      type="text"
                      placeholder="CTOS, CCRIS, internal"
                      value={bankReviewForm.creditScoreSource}
                      disabled={bankReviewLocked}
                      onChange={(event) =>
                        setBankReviewForm((current) => ({ ...current, creditScoreSource: event.target.value }))
                      }
                    />
                  </label>

                  <label className="field">
                    <span className="field-label">CTOS score</span>
                    <input
                      className="field-control"
                      type="number"
                      min="300"
                      max="850"
                      value={bankReviewForm.ctosScore}
                      disabled={bankReviewLocked}
                      onChange={(event) =>
                        setBankReviewForm((current) => ({ ...current, ctosScore: event.target.value }))
                      }
                    />
                  </label>

                  <label className="field field-span-full">
                    <span className="field-label">CCRIS/eCCRIS record summary</span>
                    <textarea
                      className="field-control field-area"
                      rows={3}
                      value={bankReviewForm.ccrisRecordSummary}
                      disabled={bankReviewLocked}
                      onChange={(event) =>
                        setBankReviewForm((current) => ({ ...current, ccrisRecordSummary: event.target.value }))
                      }
                    />
                  </label>

                  <label className="field">
                    <span className="field-label">Income verification</span>
                    <input
                      className="field-control"
                      type="text"
                      placeholder="Verified, pending, failed"
                      value={bankReviewForm.incomeVerificationStatus}
                      disabled={bankReviewLocked}
                      onChange={(event) =>
                        setBankReviewForm((current) => ({ ...current, incomeVerificationStatus: event.target.value }))
                      }
                    />
                  </label>

                  <label className="field">
                    <span className="field-label">Internal account history</span>
                    <input
                      className="field-control"
                      type="number"
                      min="0"
                      max="100"
                      value={bankReviewForm.internalAccountHistoryScore}
                      disabled={bankReviewLocked}
                      onChange={(event) =>
                        setBankReviewForm((current) => ({ ...current, internalAccountHistoryScore: event.target.value }))
                      }
                    />
                  </label>

                  <label className="field">
                    <span className="field-label">Behaviour score</span>
                    <input
                      className="field-control"
                      type="number"
                      min="0"
                      max="100"
                      value={bankReviewForm.behaviourScore}
                      disabled={bankReviewLocked}
                      onChange={(event) =>
                        setBankReviewForm((current) => ({ ...current, behaviourScore: event.target.value }))
                      }
                    />
                  </label>

                  <label className="field">
                    <span className="field-label">Fraud risk score</span>
                    <input
                      className="field-control"
                      type="number"
                      min="0"
                      max="100"
                      value={bankReviewForm.fraudRiskScore}
                      disabled={bankReviewLocked}
                      onChange={(event) =>
                        setBankReviewForm((current) => ({ ...current, fraudRiskScore: event.target.value }))
                      }
                    />
                  </label>

                  <label className="field">
                    <span className="field-label">KYC risk score</span>
                    <input
                      className="field-control"
                      type="number"
                      min="0"
                      max="100"
                      value={bankReviewForm.kycRiskScore}
                      disabled={bankReviewLocked}
                      onChange={(event) =>
                        setBankReviewForm((current) => ({ ...current, kycRiskScore: event.target.value }))
                      }
                    />
                  </label>

                  <label className="field">
                    <span className="field-label">Missed payments</span>
                    <input
                      className="field-control"
                      type="number"
                      min="0"
                      value={bankReviewForm.missedPaymentCount}
                      disabled={bankReviewLocked}
                      onChange={(event) =>
                        setBankReviewForm((current) => ({ ...current, missedPaymentCount: event.target.value }))
                      }
                    />
                  </label>

                  <label className="field">
                    <span className="field-label">Approved credit limit</span>
                    <input
                      className="field-control"
                      type="number"
                      min="0"
                      value={bankReviewForm.approvedLimit}
                      disabled={bankReviewLocked}
                      onChange={(event) =>
                        setBankReviewForm((current) => ({ ...current, approvedLimit: event.target.value }))
                      }
                    />
                  </label>

                  <label className="consent-card bank-review-toggle">
                    <input
                      type="checkbox"
                      checked={bankReviewForm.isLimitLocked}
                      disabled={bankReviewLocked}
                      onChange={(event) =>
                        setBankReviewForm((current) => ({ ...current, isLimitLocked: event.target.checked }))
                      }
                    />
                    <span>Lock or reduce limit</span>
                  </label>

                  <label className="field field-span-full">
                    <span className="field-label">Limit decision reason</span>
                    <textarea
                      className="field-control field-area"
                      rows={3}
                      value={bankReviewForm.limitDecisionReason}
                      disabled={bankReviewLocked}
                      onChange={(event) =>
                        setBankReviewForm((current) => ({ ...current, limitDecisionReason: event.target.value }))
                      }
                    />
                  </label>

                  <div className="details-grid field-span-full">
                    <DetailItem label="Recommended starting limit" value={formatOptionalCurrency(application.recommendedInitialLimit)} />
                    <DetailItem label="Current approved credit limit" value={formatOptionalCurrency(application.approvedLimit)} />
                    <DetailItem label="Limit status" value={application.isLimitLocked ? "Locked or reduced" : "Active"} />
                    <DetailItem label="Last limit review" value={formatDate(application.limitReviewedAtUtc)} />
                  </div>

                  <div className="form-actions">
                    <button
                      className="primary-button button-compact"
                      type="submit"
                      disabled={savingBankReview || bankReviewLocked}
                    >
                      {savingBankReview ? "Saving..." : "Save bank review"}
                    </button>
                  </div>
                </form>
              </section>
            ) : null}

            {isStaff ? (
              <section className="bank-review-panel">
                <div className="card-header">
                  <div>
                    <span className="eyebrow">Underwriter decision</span>
                    <h3>Application outcome</h3>
                    <p>
                      {canMakeFinalDecision
                        ? "Approve, reject, cancel, or offer a lower amount or shorter term. Use Run automated checks first to complete affordability and risk."
                        : "Loan officers can move applications into manual review after intake checks. Final approve or reject decisions are reserved for underwriters and admins."}
                    </p>
                  </div>
                </div>

                <div className="underwriter-checklist">
                  <DetailItem label="Required documents" value={documentReadinessLabel(documents)} />
                  <DetailItem label="Automated checks" value={hasCompletedAutomatedCheckResults(application) ? "Complete" : "Incomplete"} />
                  <DetailItem label="Affordability" value={affordability ? affordabilityResultLabel(affordability.result) : "Not calculated"} />
                  <DetailItem label="Risk scoring" value={risk ? riskRecommendationLabel(risk.recommendation) : "Not scored"} />
                  <DetailItem label="Offer amount" value={decisionForm.offeredAmount.trim() === "" ? "Required for approval" : formatCurrency(Number(decisionForm.offeredAmount))} />
                  <DetailItem label="Offer term" value={decisionForm.offeredTermMonths.trim() === "" ? "Required for approval" : `${decisionForm.offeredTermMonths} months`} />
                </div>

                {approveSelected && !approvalReady ? (
                  <div className="form-error">
                    Approval is locked until required documents are accepted and Run automated checks has completed affordability, risk scoring, offered amount, and offered term.
                  </div>
                ) : null}

                {application.status === 8 && !isAdmin ? (
                  <div className="form-error">
                    This application is frozen. Only an admin can change or unfreeze the decision.
                  </div>
                ) : null}

                {application.status === 9 ? (
                  <div className="empty-inline">
                    The customer has accepted this loan offer. Decision fields are locked.
                  </div>
                ) : null}

                <form className="bank-review-form" onSubmit={handleDecisionSubmit}>
                  <label className="field">
                    <span className="field-label">Decision</span>
                    <select
                      className="field-control"
                      value={decisionForm.status}
                      disabled={decisionLocked}
                      onChange={(event) =>
                        setDecisionForm((current) => ({ ...current, status: event.target.value }))
                      }
                    >
                      <option value="4">Manual review</option>
                      {canMakeFinalDecision ? <option value="5">Approve</option> : null}
                      {canMakeFinalDecision ? <option value="6">Reject</option> : null}
                      {isUnderwriter || isAdmin ? <option value="7">Cancel application</option> : null}
                      {isAdmin ? <option value="8">Freeze ongoing process</option> : null}
                      {application.status === 9 ? <option value="9">Offer accepted</option> : null}
                    </select>
                  </label>

                  <label className="field">
                    <span className="field-label">Loan offer amount</span>
                    <input
                      className="field-control"
                      type="number"
                      min="0"
                      value={decisionForm.offeredAmount}
                      disabled={finalDecisionFieldsLocked}
                      onChange={(event) =>
                        setDecisionForm((current) => ({ ...current, offeredAmount: event.target.value }))
                      }
                    />
                  </label>

                  <label className="field">
                    <span className="field-label">Offered term months</span>
                    <input
                      className="field-control"
                      type="number"
                      min="1"
                      value={decisionForm.offeredTermMonths}
                      disabled={finalDecisionFieldsLocked}
                      onChange={(event) =>
                        setDecisionForm((current) => ({ ...current, offeredTermMonths: event.target.value }))
                      }
                    />
                  </label>

                  <label className="field field-span-full">
                    <span className="field-label">Decision note</span>
                    <textarea
                      className="field-control field-area"
                      rows={3}
                      value={decisionForm.decisionNote}
                      disabled={decisionLocked}
                      onChange={(event) =>
                        setDecisionForm((current) => ({ ...current, decisionNote: event.target.value }))
                      }
                    />
                  </label>

                  <div className="details-grid field-span-full">
                    <DetailItem label="Current status" value={statusLabel(application.status)} />
                    <DetailItem label="Decision date" value={formatDate(application.decisionedAtUtc)} />
                  </div>

                  <div className="form-actions">
                    <button
                      className="primary-button button-compact"
                      type="submit"
                      disabled={savingDecision || decisionLocked || (approveSelected && !approvalReady)}
                    >
                      {savingDecision ? "Saving..." : "Save decision"}
                    </button>
                  </div>
                </form>
              </section>
            ) : null}

            {isStaff ? (
              <section className="affordability-panel">
              <div className="card-header">
                <div>
                  <span className="eyebrow">Affordability</span>
                  <h3>Repayment assessment</h3>
                  <p>Estimate repayment pressure from income, expenses, current debt, and product interest.</p>
                </div>
                {application.status === 1 ? (
                  <span className="locked-note">Submit before assessment</span>
                ) : (
                  <button
                    className="primary-button button-compact"
                    type="button"
                    disabled={assessing || application.status === 8 || application.status === 9}
                    onClick={() => void handleRunAffordability()}
                  >
                    {assessing ? "Running..." : affordability ? "Re-run assessment" : "Run assessment"}
                  </button>
                )}
              </div>

              {affordability ? (
                <>
                  <div className="affordability-summary">
                    <span className={`status-pill ${affordabilityResultClass(affordability.result)}`}>
                      {affordabilityResultLabel(affordability.result)}
                    </span>
                    <strong>{formatCurrencyWithPence(affordability.monthlyRepayment)}</strong>
                    <span>Estimated monthly repayment</span>
                  </div>

                  <div className="details-grid">
                    <DetailItem label="Total repayment" value={formatCurrencyWithPence(affordability.totalRepayment)} />
                    <DetailItem label="Total interest" value={formatCurrencyWithPence(affordability.totalInterest)} />
                    <DetailItem label="DSR" value={formatPercent(affordability.debtServiceRatio)} />
                    <DetailItem label="Disposable income" value={formatCurrencyWithPence(affordability.disposableIncome)} />
                  </div>
                </>
              ) : (
                <div className="empty-inline">
                  {application.status === 1
                    ? "Affordability can be calculated after the draft is submitted."
                    : "No affordability assessment has been generated yet."}
                </div>
              )}
              </section>
            ) : null}

            {isStaff ? (
              <section className="risk-panel">
                <div className="card-header">
                  <div>
                    <span className="eyebrow">Risk scoring</span>
                    <h3>Credit risk assessment</h3>
                    <p>Score bureau profile, behaviour, debt pressure, income stability, and affordability result.</p>
                  </div>
                  {application.status === 1 ? (
                    <span className="locked-note">Submit before scoring</span>
                  ) : !affordability ? (
                    <span className="locked-note">Run affordability first</span>
                  ) : (
                    <button
                      className="primary-button button-compact"
                      type="button"
                      disabled={scoringRisk || application.status === 8 || application.status === 9}
                      onClick={() => void handleRunRisk()}
                    >
                      {scoringRisk ? "Scoring..." : risk ? "Re-run risk" : "Run risk"}
                    </button>
                  )}
                </div>

                {risk ? (
                  <>
                    <div className="risk-summary">
                      <div className="risk-score">{risk.score}</div>
                      <div>
                        <span className={`status-pill ${riskGradeClass(risk.grade)}`}>
                          {riskGradeLabel(risk.grade)} risk
                        </span>
                        <strong>{riskRecommendationLabel(risk.recommendation)}</strong>
                        <span>System recommendation</span>
                      </div>
                    </div>

                    <ul className="factor-list">
                      {risk.factors.map((factor) => (
                        <li key={factor}>{factor}</li>
                      ))}
                    </ul>
                  </>
                ) : (
                  <div className="empty-inline">
                    {!affordability
                      ? "Risk scoring can be calculated after affordability is available."
                      : "No risk assessment has been generated yet."}
                  </div>
                )}
              </section>
            ) : null}

            {application.status === 5 || application.status === 9 ? (
              <section className="repayment-panel">
                <div className="card-header">
                  <div>
                    <span className="eyebrow">Repayment schedule</span>
                    <h3>Approved loan repayment plan</h3>
                    <p>Monthly principal and interest breakdown generated from the approved offer.</p>
                  </div>
                </div>

                {repaymentSchedule.length > 0 ? (
                  <>
                    <div className="details-grid">
                      <DetailItem label="Interest rate" value={formatAnnualRate(application.loanProductInterestRate)} />
                      <DetailItem label="Loan offer amount" value={formatOptionalCurrency(application.offeredAmount)} />
                      <DetailItem label="Installments" value={String(repaymentSchedule.length)} />
                      <DetailItem label="Total scheduled" value={formatCurrencyWithPence(sumSchedule(repaymentSchedule, (item) => item.scheduledPayment))} />
                      <DetailItem label="Total principal" value={formatCurrencyWithPence(sumSchedule(repaymentSchedule, (item) => item.principalAmount))} />
                      <DetailItem label="Total interest" value={formatCurrencyWithPence(sumSchedule(repaymentSchedule, (item) => item.interestAmount))} />
                    </div>

                    {isCustomer && application.status === 5 ? (
                      <div className="accept-offer-panel">
                        <div>
                          <span className="eyebrow">Confirmation required</span>
                          <h3>Accept loan offer</h3>
                          <p>Confirm only after reviewing the offer amount, interest rate, monthly payment, and repayment schedule.</p>
                        </div>
                        <button
                          className="primary-button button-compact"
                          type="button"
                          disabled={submitting}
                          onClick={() => void handleAcceptOffer()}
                        >
                          {submitting ? "Accepting..." : "Accept offer"}
                        </button>
                      </div>
                    ) : null}

                    {isCustomer && application.status === 9 ? (
                      <div className="accept-offer-panel accept-offer-success">
                        <div>
                          <span className="eyebrow">Offer accepted</span>
                          <h3>Transfer pending</h3>
                          <p>Your loan offer has been accepted. The money will be transferred to your account in 1-3 days.</p>
                        </div>
                        <DetailItem label="Accepted on" value={formatDate(application.offerAcceptedAtUtc)} />
                      </div>
                    ) : null}

                    <div className="table-card repayment-table-card">
                      <table className="data-table">
                        <thead>
                          <tr>
                            <th>No.</th>
                            <th>Due date</th>
                            <th>Opening</th>
                            <th>Payment</th>
                            <th>Principal</th>
                            <th>Interest</th>
                            <th>Closing</th>
                          </tr>
                        </thead>
                        <tbody>
                          {repaymentSchedule.map((item) => (
                            <tr key={item.id}>
                              <td>{item.installmentNumber}</td>
                              <td>{formatDate(item.dueDate)}</td>
                              <td>{formatCurrencyWithPence(item.openingBalance)}</td>
                              <td>{formatCurrencyWithPence(item.scheduledPayment)}</td>
                              <td>{formatCurrencyWithPence(item.principalAmount)}</td>
                              <td>{formatCurrencyWithPence(item.interestAmount)}</td>
                              <td>{formatCurrencyWithPence(item.closingBalance)}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </>
                ) : (
                  <div className="empty-inline">Repayment schedule will appear after the approved decision is saved.</div>
                )}
              </section>
            ) : null}

            {isStaff ? (
              <section className="audit-panel">
                <div className="card-header">
                  <div>
                    <span className="eyebrow">Audit trail</span>
                    <h3>Application activity</h3>
                    <p>Chronological record of workflow changes and staff actions for this application.</p>
                  </div>
                </div>

                {auditLogs.length === 0 ? (
                  <div className="empty-inline">No audit events have been recorded yet.</div>
                ) : (
                  <ol className="audit-timeline">
                    {auditLogs.map((log) => (
                      <li key={log.id} className="audit-event">
                        <div className="audit-event-marker" aria-hidden="true" />
                        <div className="audit-event-body">
                          <div className="audit-event-header">
                            <strong>{log.summary}</strong>
                            <span>{formatDateTime(log.createdAtUtc)}</span>
                          </div>
                          <div className="audit-event-meta">
                            <span>{log.action}</span>
                            <span>{log.actorRole}</span>
                          </div>
                          {log.details ? <p>{formatAuditDetails(log.details)}</p> : null}
                        </div>
                      </li>
                    ))}
                  </ol>
                )}
              </section>
            ) : null}

            <section className="document-panel">
              <div className="card-header">
                <div>
                  <span className="eyebrow">Document metadata</span>
                  <h3>Application documents</h3>
                  <p>Review uploaded files, run OCR extraction, and treat extracted values as human-verified suggestions only.</p>
                </div>
              </div>

              {isCustomer ? (
                <form className="document-form" onSubmit={handleAddDocument}>
                  <label className="field">
                    <span className="field-label">Document type</span>
                    <select
                      className="field-control"
                      value={documentForm.documentType}
                      onChange={(event) =>
                        setDocumentForm((current) => ({ ...current, documentType: event.target.value }))
                      }
                    >
                      {documentTypeOptions.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                  </label>

                  <label className="file-picker">
                    <span>Choose file</span>
                    <input
                      type="file"
                      accept={acceptedUploadTypes}
                      onChange={(event) =>
                        setDocumentForm((current) => ({
                          ...current,
                          file: event.target.files?.[0] ?? null
                        }))
                      }
                    />
                  </label>

                  <div className="file-selection">
                    {documentForm.file?.name ?? "No file selected"}
                  </div>

                  <button className="primary-button button-compact" type="submit" disabled={savingDocument}>
                    {savingDocument ? "Uploading..." : "Upload document"}
                  </button>
                </form>
              ) : null}

              <div className="table-card">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Document</th>
                      <th>File</th>
                      <th>Status</th>
                      {isStaff ? <th>OCR extraction</th> : null}
                      <th>{isStaff ? "Review" : "Uploaded"}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {documents.length === 0 ? (
                      <tr>
                        <td colSpan={isStaff ? 5 : 4}>No document metadata has been added yet.</td>
                      </tr>
                    ) : (
                      documents.map((document) => (
                        <tr key={document.id}>
                          <td>{documentTypeLabel(document.documentType)}</td>
                          <td>
                            <strong>{document.originalFileName}</strong>
                            <span className="table-subtext">
                              {document.contentType} - {formatFileSize(document.fileSize)}
                            </span>
                            <span className="table-subtext">
                              {document.submittedToBank ? "Submitted to bank" : "Draft attachment"}
                            </span>
                            <div className="inline-actions">
                              <button
                                className="text-button"
                                type="button"
                                onClick={() => void handleOpenDocument(document)}
                              >
                                Open
                              </button>
                              <button
                                className="text-button"
                                type="button"
                                onClick={() => void handleDownloadDocument(document)}
                              >
                                Download
                              </button>
                            </div>
                          </td>
                          <td>
                            <span className={`status-pill ${documentStatusClass(document.status)}`}>
                              {documentStatusLabel(document.status)}
                            </span>
                            {document.reviewNote ? (
                              <span className="table-subtext">{document.reviewNote}</span>
                            ) : null}
                          </td>
                          {isStaff ? (
                            <td>
                              <span className={`status-pill ${documentOcrStatusClass(document.ocrStatus)}`}>
                                {documentOcrStatusLabel(document.ocrStatus)}
                              </span>
                              {document.ocrSummary ? (
                                <span className="table-subtext">{document.ocrSummary}</span>
                              ) : document.ocrFailureReason ? (
                                <span className="table-subtext">{document.ocrFailureReason}</span>
                              ) : (
                                <span className="table-subtext">No extraction has been run.</span>
                              )}
                              {document.documentType === 3 && document.ocrSuggestedMonthlyIncome !== null ? (
                                <span className="table-subtext">
                                  Payslip income {formatCurrency(document.ocrSuggestedMonthlyIncome)}
                                </span>
                              ) : document.documentType === 4 && (document.ocrSuggestedMonthlyIncome !== null || document.ocrSuggestedMonthlyExpenses !== null) ? (
                                <span className="table-subtext">
                                  {document.ocrSuggestedMonthlyIncome !== null ? `Income credit ${formatCurrency(document.ocrSuggestedMonthlyIncome)}` : "Income credit not found"}
                                  {" | "}
                                  {document.ocrSuggestedMonthlyExpenses !== null ? `Debit/expense ${formatCurrency(document.ocrSuggestedMonthlyExpenses)}` : "Debit/expense not found"}
                                </span>
                              ) : document.documentType !== 3 && document.documentType !== 4 && (document.ocrSuggestedMonthlyIncome !== null || document.ocrSuggestedMonthlyExpenses !== null) ? (
                                <span className="table-subtext">
                                  {document.ocrSuggestedMonthlyIncome !== null ? `Amount ${formatCurrency(document.ocrSuggestedMonthlyIncome)}` : "Amount not found"}
                                </span>
                              ) : null}
                              {document.ocrSuggestedNationalIdNumber ? (
                                <span className="table-subtext">
                                  ID {document.ocrSuggestedNationalIdNumber}
                                  {document.ocrNationalIdMatchesApplication === null
                                    ? ""
                                    : document.ocrNationalIdMatchesApplication
                                      ? " matches application"
                                      : " does not match application"}
                                </span>
                              ) : null}
                              {document.ocrSuggestedAddress ? (
                                <span className="table-subtext">Address candidate: {document.ocrSuggestedAddress}</span>
                              ) : null}
                              {document.ocrDocumentDate ? (
                                <span className="table-subtext">
                                  Date {formatDate(document.ocrDocumentDate)}
                                  {document.ocrIsRecent === null ? "" : document.ocrIsRecent ? " | recent" : " | older than 3 months"}
                                </span>
                              ) : null}
                              {document.ocrVerificationStatus ? (
                                <span className="table-subtext">
                                  Verification: {document.ocrVerificationStatus}
                                </span>
                              ) : null}
                              {document.ocrVerificationFindings ? (
                                <span className="table-subtext">{document.ocrVerificationFindings}</span>
                              ) : null}
                              {document.ocrConfidence !== null ? (
                                <span className="table-note">Confidence {document.ocrConfidence.toFixed(2)}%</span>
                              ) : null}
                            </td>
                          ) : null}
                          <td>
                            {isStaff ? (
                              <div className="inline-actions">
                                <button
                                  className="text-button"
                                  type="button"
                                  disabled={extractingDocumentId === document.id}
                                  onClick={() => void handleRunDocumentOcr(document.id)}
                                >
                                  {extractingDocumentId === document.id ? "Reading..." : "Run OCR"}
                                </button>
                                {reviewStatusOptions.map((option) => (
                                  <button
                                    key={option.value}
                                    className="text-button"
                                    type="button"
                                    disabled={reviewingDocumentId === document.id}
                                    onClick={() => void handleReviewDocument(document.id, option.value)}
                                  >
                                    {option.label}
                                  </button>
                                ))}
                              </div>
                            ) : (
                              <span className="table-note">{formatDate(document.uploadedAtUtc)}</span>
                            )}
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </section>
          </>
        )}
      </section>
    </div>
  );
}
