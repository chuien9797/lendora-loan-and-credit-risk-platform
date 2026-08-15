import {
  createContext,
  useContext,
  useEffect,
  useRef,
  useState,
  type ReactNode
} from "react";
import { apiRequest, apiRequestBlob, ApiClientError } from "../lib/api";

type User = {
  id: string;
  email: string;
  fullName: string;
  isActive: boolean;
  roles: string[];
};

type AuthResponse = {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: User;
};

type StoredSession = {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: User;
};

type RegisterInput = {
  fullName: string;
  email: string;
  password: string;
};

type LoginInput = {
  email: string;
  password: string;
};

type LoanProduct = {
  id: string;
  code: string;
  name: string;
  productType: number;
  minAmount: number;
  maxAmount: number;
  minTermMonths: number;
  maxTermMonths: number;
  interestRate: number;
  isActive: boolean;
};

type AdminUser = {
  id: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roles: string[];
};

type AdminUserPayload = {
  fullName: string;
  email: string;
  password?: string;
  roles: string[];
  isActive: boolean;
};

type AdminLoanProductPayload = {
  code: string;
  name: string;
  productType: number;
  minAmount: number;
  maxAmount: number;
  minTermMonths: number;
  maxTermMonths: number;
  interestRate: number;
  isActive: boolean;
};

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

type LoanApplicationDetails = LoanApplicationSummary & {
  employmentStatus: number;
  employerOrBusinessName: string;
  employerOrBusinessRegistrationNumber: string | null;
  monthlyIncome: number;
  monthlyExpenses: number;
  existingMonthlyDebt: number;
  hasCreditHistoryConsent: boolean;
  hasIncomeVerificationConsent: boolean;
  hasPersonalDataProcessingConsent: boolean;
  creditScore: number | null;
  creditScoreSource: string | null;
  creditScoreCheckedAtUtc: string | null;
  ccrisRecordSummary: string | null;
  ctosScore: number | null;
  internalAccountHistoryScore: number | null;
  behaviourScore: number | null;
  fraudRiskScore: number | null;
  kycRiskScore: number | null;
  incomeVerificationStatus: string | null;
  missedPaymentCount: number;
  recommendedInitialLimit: number | null;
  approvedLimit: number | null;
  isLimitLocked: boolean;
  limitDecisionReason: string | null;
  limitReviewedAtUtc: string | null;
  offeredAmount: number | null;
  offeredTermMonths: number | null;
  decisionNote: string | null;
  decisionedAtUtc: string | null;
  offerAcceptedAtUtc: string | null;
  employmentDurationMonths: number;
  numberOfDependents: number;
  residentialStatus: number;
};

type ApplicationDocument = {
  id: string;
  loanApplicationId: string;
  documentType: number;
  originalFileName: string;
  storedFileName: string | null;
  storagePath: string | null;
  fileSize: number;
  contentType: string;
  uploadedByUserId: string;
  uploadedAtUtc: string;
  submittedToBank: boolean;
  status: number;
  reviewNote: string | null;
  reviewedByUserId: string | null;
  reviewedAtUtc: string | null;
  ocrStatus: number;
  ocrProvider: string | null;
  ocrConfidence: number | null;
  ocrSuggestedMonthlyIncome: number | null;
  ocrSuggestedMonthlyExpenses: number | null;
  ocrSuggestedNationalIdNumber: string | null;
  ocrNationalIdMatchesApplication: boolean | null;
  ocrSuggestedAddress: string | null;
  ocrDocumentDate: string | null;
  ocrIsRecent: boolean | null;
  ocrVerificationStatus: string | null;
  ocrVerificationFindings: string | null;
  ocrSummary: string | null;
  ocrExtractedText: string | null;
  ocrFailureReason: string | null;
  ocrProcessedByUserId: string | null;
  ocrProcessedAtUtc: string | null;
};

type AffordabilityAssessment = {
  id: string;
  loanApplicationId: string;
  monthlyRepayment: number;
  totalRepayment: number;
  totalInterest: number;
  debtServiceRatio: number;
  disposableIncome: number;
  result: number;
  assessedAtUtc: string;
};

type RiskAssessment = {
  id: string;
  loanApplicationId: string;
  score: number;
  grade: number;
  recommendation: number;
  factors: string[];
  assessedAtUtc: string;
};

type RepaymentScheduleItem = {
  id: string;
  loanApplicationId: string;
  installmentNumber: number;
  dueDate: string;
  openingBalance: number;
  scheduledPayment: number;
  principalAmount: number;
  interestAmount: number;
  closingBalance: number;
};

type ApplicationAuditLog = {
  id: string;
  loanApplicationId: string;
  actorUserId: string | null;
  actorRole: string;
  action: string;
  summary: string;
  details: string | null;
  createdAtUtc: string;
};

type AutomatedBankCheckResult = {
  application: LoanApplicationDetails;
  affordability: AffordabilityAssessment;
  risk: RiskAssessment;
  providerNotes: string[];
};

type LoanApplicationPayload = {
  loanProductId: string;
  applicantFullName: string;
  nationalIdNumber: string;
  phoneNumber: string;
  email: string;
  loanPurpose: string;
  employmentStatus: number;
  employerOrBusinessName: string;
  employerOrBusinessRegistrationNumber: string | null;
  loanAmount: number;
  loanTermMonths: number;
  monthlyIncome: number;
  monthlyExpenses: number;
  existingMonthlyDebt: number;
  hasCreditHistoryConsent: boolean;
  hasIncomeVerificationConsent: boolean;
  hasPersonalDataProcessingConsent: boolean;
  employmentDurationMonths: number;
  numberOfDependents: number;
  residentialStatus: number;
};

type BankReviewPayload = {
  creditScore: number | null;
  creditScoreSource: string | null;
  ccrisRecordSummary: string | null;
  ctosScore: number | null;
  internalAccountHistoryScore: number | null;
  behaviourScore: number | null;
  fraudRiskScore: number | null;
  kycRiskScore: number | null;
  incomeVerificationStatus: string | null;
  missedPaymentCount: number;
  approvedLimit: number | null;
  isLimitLocked: boolean;
  limitDecisionReason: string | null;
};

type DocumentMetadataPayload = {
  documentType: number;
  originalFileName: string;
  fileSize: number;
  contentType: string;
};

type ReviewDocumentPayload = {
  status: number;
  reviewNote: string | null;
};

type ApplicationDecisionPayload = {
  status: number;
  offeredAmount: number | null;
  offeredTermMonths: number | null;
  decisionNote: string | null;
};

type AuthContextValue = {
  isReady: boolean;
  session: StoredSession | null;
  user: User | null;
  primaryRole: string | null;
  isCustomer: boolean;
  isStaff: boolean;
  isUnderwriter: boolean;
  isAdmin: boolean;
  login: (input: LoginInput) => Promise<void>;
  register: (input: RegisterInput) => Promise<void>;
  logout: () => void;
  requestPasswordReset: (email: string) => Promise<string>;
  apiGetLoanProducts: () => Promise<LoanProduct[]>;
  apiGetMyApplications: () => Promise<LoanApplicationSummary[]>;
  apiGetReviewQueue: () => Promise<LoanApplicationSummary[]>;
  apiSearchApplications: (query: string) => Promise<LoanApplicationSummary[]>;
  apiGetApplication: (id: string) => Promise<LoanApplicationDetails>;
  apiCreateDraft: (payload: LoanApplicationPayload) => Promise<LoanApplicationDetails>;
  apiUpdateDraft: (id: string, payload: LoanApplicationPayload) => Promise<LoanApplicationDetails>;
  apiSubmitApplication: (id: string) => Promise<LoanApplicationDetails>;
  apiAcceptOffer: (id: string) => Promise<LoanApplicationDetails>;
  apiUpdateBankReview: (id: string, payload: BankReviewPayload) => Promise<LoanApplicationDetails>;
  apiUpdateDecision: (id: string, payload: ApplicationDecisionPayload) => Promise<LoanApplicationDetails>;
  apiDeleteDraft: (id: string) => Promise<void>;
  apiGetDocuments: (applicationId: string) => Promise<ApplicationDocument[]>;
  apiAddDocumentMetadata: (applicationId: string, payload: DocumentMetadataPayload) => Promise<ApplicationDocument>;
  apiUploadDocument: (applicationId: string, documentType: number, file: File) => Promise<ApplicationDocument>;
  apiReviewDocument: (documentId: string, payload: ReviewDocumentPayload) => Promise<ApplicationDocument>;
  apiRunDocumentOcr: (documentId: string) => Promise<ApplicationDocument>;
  apiDownloadDocument: (documentId: string) => Promise<Blob>;
  apiRunBankChecks: (applicationId: string) => Promise<AutomatedBankCheckResult>;
  apiGetAffordability: (applicationId: string) => Promise<AffordabilityAssessment>;
  apiGenerateAffordability: (applicationId: string) => Promise<AffordabilityAssessment>;
  apiGetRisk: (applicationId: string) => Promise<RiskAssessment>;
  apiGenerateRisk: (applicationId: string) => Promise<RiskAssessment>;
  apiGetRepaymentSchedule: (applicationId: string) => Promise<RepaymentScheduleItem[]>;
  apiGetApplicationAuditLogs: (applicationId: string) => Promise<ApplicationAuditLog[]>;
  apiAdminGetUsers: () => Promise<AdminUser[]>;
  apiAdminCreateUser: (payload: AdminUserPayload & { password: string }) => Promise<AdminUser>;
  apiAdminUpdateUser: (id: string, payload: AdminUserPayload) => Promise<AdminUser>;
  apiAdminDeleteUser: (id: string) => Promise<void>;
  apiAdminGetLoanProducts: () => Promise<LoanProduct[]>;
  apiAdminCreateLoanProduct: (payload: AdminLoanProductPayload) => Promise<LoanProduct>;
  apiAdminUpdateLoanProduct: (id: string, payload: AdminLoanProductPayload) => Promise<LoanProduct>;
  apiAdminDeleteLoanProduct: (id: string) => Promise<void>;
};

const storageKey = "lendora.auth.session";
const AuthContext = createContext<AuthContextValue | null>(null);

function readStoredSession(): StoredSession | null {
  const raw = localStorage.getItem(storageKey);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as StoredSession;
  } catch {
    localStorage.removeItem(storageKey);
    return null;
  }
}

function writeStoredSession(session: StoredSession | null) {
  if (!session) {
    localStorage.removeItem(storageKey);
    return;
  }

  localStorage.setItem(storageKey, JSON.stringify(session));
}

function rolePriority(role: string) {
  switch (role) {
    case "Admin":
      return 0;
    case "Underwriter":
      return 1;
    case "LoanOfficer":
      return 2;
    case "Customer":
      return 3;
    default:
      return 99;
  }
}

function getPrimaryRole(user: User | null) {
  if (!user || user.roles.length === 0) {
    return null;
  }

  return [...user.roles].sort((left, right) => rolePriority(left) - rolePriority(right))[0];
}

function toStoredSession(response: AuthResponse): StoredSession {
  return {
    accessToken: response.accessToken,
    accessTokenExpiresAtUtc: response.accessTokenExpiresAtUtc,
    refreshToken: response.refreshToken,
    refreshTokenExpiresAtUtc: response.refreshTokenExpiresAtUtc,
    user: response.user
  };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isReady, setIsReady] = useState(false);
  const [session, setSession] = useState<StoredSession | null>(() => readStoredSession());
  const sessionRef = useRef<StoredSession | null>(session);

  useEffect(() => {
    sessionRef.current = session;
    writeStoredSession(session);
  }, [session]);

  useEffect(() => {
    let cancelled = false;

    async function restoreSession() {
      const current = sessionRef.current;
      if (!current) {
        if (!cancelled) {
          setIsReady(true);
        }
        return;
      }

      try {
        const restoredSession = await refreshWithToken(current.refreshToken);
        if (!cancelled) {
          setSession(restoredSession);
        }
      } catch {
        if (!cancelled) {
          setSession(null);
        }
      } finally {
        if (!cancelled) {
          setIsReady(true);
        }
      }
    }

    void restoreSession();

    return () => {
      cancelled = true;
    };
  }, []);

  async function refreshWithToken(refreshToken: string) {
    const response = await apiRequest<AuthResponse>("/auth/refresh-token", {
      method: "POST",
      body: JSON.stringify({ refreshToken })
    });

    return toStoredSession(response.data);
  }

  async function authenticatedRequest<T>(path: string, options: RequestInit = {}) {
    const current = sessionRef.current;
    if (!current) {
      throw new ApiClientError("You need to sign in to continue.", 401);
    }

    try {
      const response = await apiRequest<T>(path, {
        ...options,
        accessToken: current.accessToken
      });

      return response.data;
    } catch (error) {
      if (!(error instanceof ApiClientError) || error.statusCode !== 401) {
        throw error;
      }

      let refreshed: StoredSession;

      try {
        refreshed = await refreshWithToken(current.refreshToken);
        setSession(refreshed);
      } catch (refreshError) {
        setSession(null);
        throw refreshError;
      }

      const retry = await apiRequest<T>(path, {
        ...options,
        accessToken: refreshed.accessToken
      });

      return retry.data;
    }
  }

  async function authenticatedBlobRequest(path: string, options: RequestInit = {}) {
    const current = sessionRef.current;
    if (!current) {
      throw new ApiClientError("You need to sign in to continue.", 401);
    }

    try {
      return await apiRequestBlob(path, {
        ...options,
        accessToken: current.accessToken
      });
    } catch (error) {
      if (!(error instanceof ApiClientError) || error.statusCode !== 401) {
        throw error;
      }

      let refreshed: StoredSession;

      try {
        refreshed = await refreshWithToken(current.refreshToken);
        setSession(refreshed);
      } catch (refreshError) {
        setSession(null);
        throw refreshError;
      }

      return apiRequestBlob(path, {
        ...options,
        accessToken: refreshed.accessToken
      });
    }
  }

  async function login(input: LoginInput) {
    const response = await apiRequest<AuthResponse>("/auth/login", {
      method: "POST",
      body: JSON.stringify(input)
    });

    setSession(toStoredSession(response.data));
  }

  async function register(input: RegisterInput) {
    const response = await apiRequest<AuthResponse>("/auth/register", {
      method: "POST",
      body: JSON.stringify(input)
    });

    setSession(toStoredSession(response.data));
  }

  function logout() {
    setSession(null);
  }

  async function requestPasswordReset(email: string) {
    const response = await apiRequest<object>("/auth/forgot-password", {
      method: "POST",
      body: JSON.stringify({ email })
    });

    return response.message;
  }

  const user = session?.user ?? null;
  const primaryRole = getPrimaryRole(user);
  const isCustomer = primaryRole === "Customer";
  const isUnderwriter = primaryRole === "Underwriter";
  const isAdmin = primaryRole === "Admin";
  const isStaff = primaryRole === "Admin" || primaryRole === "LoanOfficer" || primaryRole === "Underwriter";

  const value: AuthContextValue = {
    isReady,
    session,
    user,
    primaryRole,
    isCustomer,
    isStaff,
    isUnderwriter,
    isAdmin,
    login,
    register,
    logout,
    requestPasswordReset,
    apiGetLoanProducts: () => authenticatedRequest<LoanProduct[]>("/loan-products"),
    apiGetMyApplications: () =>
      authenticatedRequest<LoanApplicationSummary[]>("/loan-applications/me"),
    apiGetReviewQueue: () =>
      authenticatedRequest<LoanApplicationSummary[]>("/loan-applications/review-queue"),
    apiSearchApplications: (query) =>
      authenticatedRequest<LoanApplicationSummary[]>(
        `/loan-applications/search?query=${encodeURIComponent(query)}`
      ),
    apiGetApplication: (id) =>
      authenticatedRequest<LoanApplicationDetails>(`/loan-applications/${id}`),
    apiCreateDraft: (payload) =>
      authenticatedRequest<LoanApplicationDetails>("/loan-applications", {
        method: "POST",
        body: JSON.stringify(payload)
      }),
    apiUpdateDraft: (id, payload) =>
      authenticatedRequest<LoanApplicationDetails>(`/loan-applications/${id}`, {
        method: "PUT",
        body: JSON.stringify(payload)
      }),
    apiSubmitApplication: (id) =>
      authenticatedRequest<LoanApplicationDetails>(`/loan-applications/${id}/submit`, {
        method: "POST"
      }),
    apiAcceptOffer: (id) =>
      authenticatedRequest<LoanApplicationDetails>(`/loan-applications/${id}/accept-offer`, {
        method: "POST"
      }),
    apiUpdateBankReview: (id, payload) =>
      authenticatedRequest<LoanApplicationDetails>(`/loan-applications/${id}/bank-review`, {
        method: "PATCH",
        body: JSON.stringify(payload)
      }),
    apiUpdateDecision: (id, payload) =>
      authenticatedRequest<LoanApplicationDetails>(`/loan-applications/${id}/decision`, {
        method: "PATCH",
        body: JSON.stringify(payload)
      }),
    apiDeleteDraft: async (id) => {
      await authenticatedRequest<object>(`/loan-applications/${id}`, {
        method: "DELETE"
      });
    },
    apiGetDocuments: (applicationId) =>
      authenticatedRequest<ApplicationDocument[]>(`/loan-applications/${applicationId}/documents`),
    apiAddDocumentMetadata: (applicationId, payload) =>
      authenticatedRequest<ApplicationDocument>(`/loan-applications/${applicationId}/documents/metadata`, {
        method: "POST",
        body: JSON.stringify(payload)
      }),
    apiUploadDocument: (applicationId, documentType, file) => {
      const formData = new FormData();
      formData.set("documentType", String(documentType));
      formData.set("file", file);

      return authenticatedRequest<ApplicationDocument>(`/loan-applications/${applicationId}/documents`, {
        method: "POST",
        body: formData
      });
    },
    apiReviewDocument: (documentId, payload) =>
      authenticatedRequest<ApplicationDocument>(`/documents/${documentId}/review`, {
        method: "PATCH",
        body: JSON.stringify(payload)
      }),
    apiRunDocumentOcr: (documentId) =>
      authenticatedRequest<ApplicationDocument>(`/documents/${documentId}/ocr`, {
        method: "POST"
      }),
    apiDownloadDocument: (documentId) =>
      authenticatedBlobRequest(`/documents/${documentId}/download`),
    apiRunBankChecks: (applicationId) =>
      authenticatedRequest<AutomatedBankCheckResult>(`/loan-applications/${applicationId}/bank-checks/run`, {
        method: "POST"
      }),
    apiGetAffordability: (applicationId) =>
      authenticatedRequest<AffordabilityAssessment>(`/loan-applications/${applicationId}/affordability-assessment`),
    apiGenerateAffordability: (applicationId) =>
      authenticatedRequest<AffordabilityAssessment>(`/loan-applications/${applicationId}/affordability-assessment`, {
        method: "POST"
      }),
    apiGetRisk: (applicationId) =>
      authenticatedRequest<RiskAssessment>(`/loan-applications/${applicationId}/risk-assessment`),
    apiGenerateRisk: (applicationId) =>
      authenticatedRequest<RiskAssessment>(`/loan-applications/${applicationId}/risk-assessment`, {
        method: "POST"
      }),
    apiGetRepaymentSchedule: (applicationId) =>
      authenticatedRequest<RepaymentScheduleItem[]>(`/loan-applications/${applicationId}/repayment-schedule`),
    apiGetApplicationAuditLogs: (applicationId) =>
      authenticatedRequest<ApplicationAuditLog[]>(`/loan-applications/${applicationId}/audit-logs`)
    ,
    apiAdminGetUsers: () => authenticatedRequest<AdminUser[]>("/admin/users"),
    apiAdminCreateUser: (payload) =>
      authenticatedRequest<AdminUser>("/admin/users", {
        method: "POST",
        body: JSON.stringify(payload)
      }),
    apiAdminUpdateUser: (id, payload) =>
      authenticatedRequest<AdminUser>(`/admin/users/${id}`, {
        method: "PUT",
        body: JSON.stringify(payload)
      }),
    apiAdminDeleteUser: async (id) => {
      await authenticatedRequest<object>(`/admin/users/${id}`, {
        method: "DELETE"
      });
    },
    apiAdminGetLoanProducts: () => authenticatedRequest<LoanProduct[]>("/admin/loan-products"),
    apiAdminCreateLoanProduct: (payload) =>
      authenticatedRequest<LoanProduct>("/admin/loan-products", {
        method: "POST",
        body: JSON.stringify(payload)
      }),
    apiAdminUpdateLoanProduct: (id, payload) =>
      authenticatedRequest<LoanProduct>(`/admin/loan-products/${id}`, {
        method: "PUT",
        body: JSON.stringify(payload)
      }),
    apiAdminDeleteLoanProduct: async (id) => {
      await authenticatedRequest<object>(`/admin/loan-products/${id}`, {
        method: "DELETE"
      });
    }
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider.");
  }

  return context;
}
