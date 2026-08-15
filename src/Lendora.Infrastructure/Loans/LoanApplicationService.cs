using Lendora.Application.Abstractions.Audit;
using Lendora.Application.Abstractions.Loans;
using Lendora.Application.Loans;
using Lendora.Application.Repayments;
using Lendora.Domain.Constants;
using Lendora.Domain.Entities;
using Lendora.Domain.Enums;
using Lendora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lendora.Infrastructure.Loans;

internal sealed class LoanApplicationService(ApplicationDbContext dbContext, IApplicationAuditService auditService) : ILoanApplicationService
{
    public async Task<IReadOnlyCollection<LoanProductDto>> GetLoanProductsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.LoanProducts
            .Where(product => product.IsActive)
            .OrderBy(product => product.Name)
            .Select(product => new LoanProductDto(
                product.Id,
                product.Code,
                product.Name,
                product.ProductType,
                product.MinAmount,
                product.MaxAmount,
                product.MinTermMonths,
                product.MaxTermMonths,
                product.InterestRate,
                product.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<LoanApplicationDto>> CreateDraftAsync(Guid customerId, CreateLoanApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.LoanProducts.FirstOrDefaultAsync(product => product.Id == request.LoanProductId && product.IsActive, cancellationToken);
        if (product is null)
        {
            return ServiceResult<LoanApplicationDto>.Failure("The selected loan product does not exist or is inactive.");
        }

        var validationErrors = ValidateRequest(request, product);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<LoanApplicationDto>.Failure(validationErrors.ToArray());
        }

        var application = new LoanApplication
        {
            CustomerId = customerId,
            LoanProductId = request.LoanProductId,
            ApplicantFullName = request.ApplicantFullName.Trim(),
            NationalIdNumber = request.NationalIdNumber.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            LoanPurpose = request.LoanPurpose.Trim(),
            EmploymentStatus = request.EmploymentStatus,
            EmployerOrBusinessName = request.EmployerOrBusinessName.Trim(),
            EmployerOrBusinessRegistrationNumber = request.EmployerOrBusinessRegistrationNumber?.Trim(),
            Status = LoanApplicationStatus.Draft,
            LoanAmount = request.LoanAmount,
            LoanTermMonths = request.LoanTermMonths,
            MonthlyIncome = request.MonthlyIncome,
            MonthlyExpenses = request.MonthlyExpenses,
            ExistingMonthlyDebt = request.ExistingMonthlyDebt,
            HasCreditHistoryConsent = request.HasCreditHistoryConsent,
            HasIncomeVerificationConsent = request.HasIncomeVerificationConsent,
            HasPersonalDataProcessingConsent = request.HasPersonalDataProcessingConsent,
            EmploymentDurationMonths = request.EmploymentDurationMonths,
            NumberOfDependents = request.NumberOfDependents,
            ResidentialStatus = request.ResidentialStatus
        };

        dbContext.LoanApplications.Add(application);
        await auditService.RecordAsync(
            application.Id,
            customerId,
            ApplicationRoles.Customer,
            "ApplicationCreated",
            "Customer created a draft loan application.",
            $"Requested {application.LoanAmount:0.##} for {application.LoanTermMonths} months.",
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<LoanApplicationDto>.Success(MapToDetails(application, product.Name));
    }

    public async Task<ServiceResult<LoanApplicationDto>> UpdateDraftAsync(Guid customerId, Guid applicationId, UpdateLoanApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .Include(candidate => candidate.LoanProduct)
            .FirstOrDefaultAsync(candidate => candidate.Id == applicationId && candidate.CustomerId == customerId, cancellationToken);

        if (application is null)
        {
            return ServiceResult<LoanApplicationDto>.Failure("Loan application not found.");
        }

        if (application.Status != LoanApplicationStatus.Draft)
        {
            return ServiceResult<LoanApplicationDto>.Failure("Only draft applications can be edited.");
        }

        var product = await dbContext.LoanProducts.FirstOrDefaultAsync(candidate => candidate.Id == request.LoanProductId && candidate.IsActive, cancellationToken);
        if (product is null)
        {
            return ServiceResult<LoanApplicationDto>.Failure("The selected loan product does not exist or is inactive.");
        }

        var validationErrors = ValidateRequest(request, product);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<LoanApplicationDto>.Failure(validationErrors.ToArray());
        }

        application.LoanProductId = request.LoanProductId;
        application.ApplicantFullName = request.ApplicantFullName.Trim();
        application.NationalIdNumber = request.NationalIdNumber.Trim();
        application.PhoneNumber = request.PhoneNumber.Trim();
        application.Email = request.Email.Trim().ToLowerInvariant();
        application.LoanPurpose = request.LoanPurpose.Trim();
        application.EmploymentStatus = request.EmploymentStatus;
        application.EmployerOrBusinessName = request.EmployerOrBusinessName.Trim();
        application.EmployerOrBusinessRegistrationNumber = request.EmployerOrBusinessRegistrationNumber?.Trim();
        application.LoanAmount = request.LoanAmount;
        application.LoanTermMonths = request.LoanTermMonths;
        application.MonthlyIncome = request.MonthlyIncome;
        application.MonthlyExpenses = request.MonthlyExpenses;
        application.ExistingMonthlyDebt = request.ExistingMonthlyDebt;
        application.HasCreditHistoryConsent = request.HasCreditHistoryConsent;
        application.HasIncomeVerificationConsent = request.HasIncomeVerificationConsent;
        application.HasPersonalDataProcessingConsent = request.HasPersonalDataProcessingConsent;
        application.EmploymentDurationMonths = request.EmploymentDurationMonths;
        application.NumberOfDependents = request.NumberOfDependents;
        application.ResidentialStatus = request.ResidentialStatus;
        application.UpdatedAtUtc = DateTime.UtcNow;

        await auditService.RecordAsync(
            application.Id,
            customerId,
            ApplicationRoles.Customer,
            "ApplicationUpdated",
            "Customer updated the draft loan application.",
            $"Requested {application.LoanAmount:0.##} for {application.LoanTermMonths} months.",
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<LoanApplicationDto>.Success(MapToDetails(application, product.Name));
    }

    public async Task<ServiceResult<LoanApplicationDto>> SubmitAsync(Guid customerId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .Include(candidate => candidate.LoanProduct)
            .Include(candidate => candidate.Documents)
            .FirstOrDefaultAsync(candidate => candidate.Id == applicationId && candidate.CustomerId == customerId, cancellationToken);

        if (application is null)
        {
            return ServiceResult<LoanApplicationDto>.Failure("Loan application not found.");
        }

        if (application.Status != LoanApplicationStatus.Draft)
        {
            return ServiceResult<LoanApplicationDto>.Failure("Only draft applications can be submitted.");
        }

        var validationErrors = ValidateApplication(application, application.LoanProduct);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<LoanApplicationDto>.Failure(validationErrors.ToArray());
        }

        var documentErrors = ValidateRequiredDocuments(application.Documents);
        if (documentErrors.Count > 0)
        {
            return ServiceResult<LoanApplicationDto>.Failure(documentErrors.ToArray());
        }

        application.Status = LoanApplicationStatus.Submitted;
        application.SubmittedAtUtc = DateTime.UtcNow;
        application.UpdatedAtUtc = DateTime.UtcNow;
        foreach (var document in application.Documents)
        {
            document.SubmittedToBank = true;
            document.UpdatedAtUtc = DateTime.UtcNow;
        }

        await auditService.RecordAsync(
            application.Id,
            customerId,
            ApplicationRoles.Customer,
            "ApplicationSubmitted",
            "Customer submitted the loan application.",
            $"Submitted with {application.Documents.Count} uploaded document(s).",
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<LoanApplicationDto>.Success(MapToDetails(application, application.LoanProduct!.Name));
    }

    public async Task<ServiceResult<LoanApplicationDto>> UpdateBankReviewAsync(Guid staffUserId, Guid applicationId, UpdateBankReviewRequest request, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .Include(candidate => candidate.LoanProduct)
            .Include(candidate => candidate.Documents)
            .Include(candidate => candidate.AffordabilityAssessment)
            .Include(candidate => candidate.RiskAssessment)
            .Include(candidate => candidate.RepaymentScheduleItems)
            .FirstOrDefaultAsync(candidate => candidate.Id == applicationId, cancellationToken);

        if (application is null)
        {
            return ServiceResult<LoanApplicationDto>.Failure("Loan application not found.");
        }

        if (application.Status == LoanApplicationStatus.Draft)
        {
            return ServiceResult<LoanApplicationDto>.Failure("Submit the application before bank review.");
        }

        if (application.Status is LoanApplicationStatus.Frozen or LoanApplicationStatus.OfferAccepted)
        {
            return ServiceResult<LoanApplicationDto>.Failure("Bank review cannot be changed after an application is frozen or the customer has accepted the offer.");
        }

        var validationErrors = ValidateBankReview(request, application.LoanProduct);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<LoanApplicationDto>.Failure(validationErrors.ToArray());
        }

        application.CreditScore = request.CreditScore;
        application.CreditScoreSource = request.CreditScoreSource?.Trim();
        application.CreditScoreCheckedAtUtc = request.CreditScore.HasValue ? DateTime.UtcNow : null;
        application.CcrisRecordSummary = request.CcrisRecordSummary?.Trim();
        application.CtosScore = request.CtosScore;
        application.InternalAccountHistoryScore = request.InternalAccountHistoryScore;
        application.BehaviourScore = request.BehaviourScore;
        application.FraudRiskScore = request.FraudRiskScore;
        application.KycRiskScore = request.KycRiskScore;
        application.IncomeVerificationStatus = request.IncomeVerificationStatus?.Trim();
        application.MissedPaymentCount = request.MissedPaymentCount;
        application.RecommendedInitialLimit = CalculateRecommendedInitialLimit(application);
        application.ApprovedLimit = request.ApprovedLimit;
        application.IsLimitLocked = request.IsLimitLocked;
        application.LimitDecisionReason = request.LimitDecisionReason?.Trim();
        application.LimitReviewedAtUtc = DateTime.UtcNow;
        application.LimitReviewedByUserId = staffUserId;
        application.UpdatedAtUtc = DateTime.UtcNow;

        await auditService.RecordAsync(
            application.Id,
            staffUserId,
            "Staff",
            "BankReviewUpdated",
            "Staff updated bank review and credit limit fields.",
            $"Approved limit: {FormatAmount(application.ApprovedLimit)}. Limit locked: {(application.IsLimitLocked ? "Yes" : "No")}.",
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<LoanApplicationDto>.Success(MapToDetails(application, application.LoanProduct!.Name, includeBankOnlyFields: true));
    }

    public async Task<ServiceResult<LoanApplicationDto>> UpdateDecisionAsync(Guid staffUserId, IReadOnlyCollection<string> roles, Guid applicationId, UpdateApplicationDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .Include(candidate => candidate.LoanProduct)
            .Include(candidate => candidate.AffordabilityAssessment)
            .Include(candidate => candidate.RiskAssessment)
            .Include(candidate => candidate.Documents)
            .FirstOrDefaultAsync(candidate => candidate.Id == applicationId, cancellationToken);

        if (application is null)
        {
            return ServiceResult<LoanApplicationDto>.Failure("Loan application not found.");
        }

        if (application.Status == LoanApplicationStatus.Draft)
        {
            return ServiceResult<LoanApplicationDto>.Failure("Submit the application before recording a bank decision.");
        }

        var validationErrors = ValidateDecision(request, application, application.LoanProduct, roles);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<LoanApplicationDto>.Failure(validationErrors.ToArray());
        }

        var previousStatus = application.Status;
        application.Status = request.Status;
        application.OfferedAmount = request.OfferedAmount;
        application.OfferedTermMonths = request.OfferedTermMonths;
        application.DecisionNote = request.DecisionNote?.Trim();
        application.DecisionedAtUtc = DateTime.UtcNow;
        application.DecisionedByUserId = staffUserId;
        application.UpdatedAtUtc = DateTime.UtcNow;

        if (request.Status == LoanApplicationStatus.Approved)
        {
            await ReplaceRepaymentScheduleAsync(application, cancellationToken);
        }
        else
        {
            await dbContext.RepaymentScheduleItems
                .Where(item => item.LoanApplicationId == application.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }

        await auditService.RecordAsync(
            application.Id,
            staffUserId,
            GetPrimaryStaffRole(roles),
            "DecisionUpdated",
            $"Application decision changed from {previousStatus} to {application.Status}.",
            $"Offer amount: {FormatAmount(application.OfferedAmount)}. Offer term: {FormatTerm(application.OfferedTermMonths)}. Note: {FormatText(application.DecisionNote)}",
            cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<LoanApplicationDto>.Failure("Application decision could not be saved because the record changed while you were reviewing it. Refresh the application and try again.");
        }

        return ServiceResult<LoanApplicationDto>.Success(MapToDetails(application, application.LoanProduct!.Name, includeBankOnlyFields: true));
    }

    public async Task<ServiceResult<LoanApplicationDto>> AcceptOfferAsync(Guid customerId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .Include(candidate => candidate.LoanProduct)
            .FirstOrDefaultAsync(candidate => candidate.Id == applicationId && candidate.CustomerId == customerId, cancellationToken);

        if (application is null)
        {
            return ServiceResult<LoanApplicationDto>.Failure("Loan application not found.");
        }

        if (application.Status != LoanApplicationStatus.Approved)
        {
            return ServiceResult<LoanApplicationDto>.Failure("Only approved loan offers can be accepted.");
        }

        if (application.OfferedAmount is null || application.OfferedTermMonths is null)
        {
            return ServiceResult<LoanApplicationDto>.Failure("Loan offer amount and term must be available before accepting.");
        }

        application.Status = LoanApplicationStatus.OfferAccepted;
        application.OfferAcceptedAtUtc = DateTime.UtcNow;
        application.UpdatedAtUtc = DateTime.UtcNow;

        await auditService.RecordAsync(
            application.Id,
            customerId,
            ApplicationRoles.Customer,
            "OfferAccepted",
            "Customer accepted the approved loan offer.",
            $"Offer amount: {FormatAmount(application.OfferedAmount)}. Offer term: {FormatTerm(application.OfferedTermMonths)}.",
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<LoanApplicationDto>.Success(MapToDetails(application, application.LoanProduct!.Name));
    }

    public async Task<ServiceResult<bool>> DeleteDraftAsync(Guid customerId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .FirstOrDefaultAsync(candidate => candidate.Id == applicationId && candidate.CustomerId == customerId, cancellationToken);

        if (application is null)
        {
            return ServiceResult<bool>.Failure("Loan application not found.");
        }

        if (application.Status != LoanApplicationStatus.Draft)
        {
            return ServiceResult<bool>.Failure("Only draft applications can be deleted.");
        }

        dbContext.LoanApplications.Remove(application);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<IReadOnlyCollection<LoanApplicationSummaryDto>> GetMyApplicationsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.LoanApplications
            .AsNoTracking()
            .Include(application => application.LoanProduct)
            .Where(application => application.CustomerId == customerId)
            .OrderByDescending(application => application.CreatedAtUtc)
            .Select(application => MapToSummary(application, application.LoanProduct!.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<LoanApplicationDto>> GetApplicationAsync(Guid userId, IReadOnlyCollection<string> roles, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .AsNoTracking()
            .Include(candidate => candidate.LoanProduct)
            .FirstOrDefaultAsync(candidate => candidate.Id == applicationId, cancellationToken);

        if (application is null)
        {
            return ServiceResult<LoanApplicationDto>.Failure("Loan application not found.");
        }

        var isStaff = roles.Contains(ApplicationRoles.Admin) || roles.Contains(ApplicationRoles.LoanOfficer) || roles.Contains(ApplicationRoles.Underwriter);
        if (!isStaff && application.CustomerId != userId)
        {
            return ServiceResult<LoanApplicationDto>.Failure("You do not have access to this loan application.");
        }

        return ServiceResult<LoanApplicationDto>.Success(MapToDetails(application, application.LoanProduct!.Name, includeBankOnlyFields: isStaff));
    }

    public async Task<IReadOnlyCollection<LoanApplicationSummaryDto>> GetReviewQueueAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.LoanApplications
            .AsNoTracking()
            .Include(application => application.LoanProduct)
            .Where(application => application.Status == LoanApplicationStatus.Submitted ||
                application.Status == LoanApplicationStatus.AssessmentInProgress ||
                application.Status == LoanApplicationStatus.ManualReview ||
                application.Status == LoanApplicationStatus.Frozen)
            .OrderBy(application => application.SubmittedAtUtc ?? application.CreatedAtUtc)
            .Select(application => MapToSummary(application, application.LoanProduct!.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<LoanApplicationSummaryDto>> SearchApplicationsAsync(string query, CancellationToken cancellationToken = default)
    {
        var trimmedQuery = query.Trim();
        if (trimmedQuery.Length < 2)
        {
            return [];
        }

        var normalizedQuery = trimmedQuery.ToLower();
        var hasGuidQuery = Guid.TryParse(trimmedQuery, out var guidQuery);

        return await dbContext.LoanApplications
            .AsNoTracking()
            .Include(application => application.LoanProduct)
            .Where(application =>
                application.ApplicantFullName.ToLower().Contains(normalizedQuery) ||
                application.Email.ToLower().Contains(normalizedQuery) ||
                application.NationalIdNumber.ToLower().Contains(normalizedQuery) ||
                application.PhoneNumber.ToLower().Contains(normalizedQuery) ||
                (hasGuidQuery && (application.Id == guidQuery || application.CustomerId == guidQuery)))
            .OrderByDescending(application => application.SubmittedAtUtc ?? application.CreatedAtUtc)
            .Take(50)
            .Select(application => MapToSummary(application, application.LoanProduct!.Name))
            .ToListAsync(cancellationToken);
    }

    private static LoanApplicationSummaryDto MapToSummary(LoanApplication application, string loanProductName) =>
        new(
            application.Id,
            application.CustomerId,
            application.LoanProductId,
            loanProductName,
            application.ApplicantFullName,
            application.NationalIdNumber,
            application.PhoneNumber,
            application.Email,
            application.LoanPurpose,
            application.Status,
            application.LoanAmount,
            application.LoanTermMonths,
            application.LoanProduct?.InterestRate ?? 0m,
            application.CreatedAtUtc,
            application.SubmittedAtUtc);

    private static LoanApplicationDto MapToDetails(LoanApplication application, string loanProductName, bool includeBankOnlyFields = false) =>
        new(
            application.Id,
            application.CustomerId,
            application.LoanProductId,
            loanProductName,
            application.LoanProduct?.InterestRate ?? 0m,
            application.ApplicantFullName,
            application.NationalIdNumber,
            application.PhoneNumber,
            application.Email,
            application.LoanPurpose,
            application.EmploymentStatus,
            application.EmployerOrBusinessName,
            application.EmployerOrBusinessRegistrationNumber,
            application.Status,
            application.LoanAmount,
            application.LoanTermMonths,
            application.MonthlyIncome,
            application.MonthlyExpenses,
            application.ExistingMonthlyDebt,
            application.HasCreditHistoryConsent,
            application.HasIncomeVerificationConsent,
            application.HasPersonalDataProcessingConsent,
            includeBankOnlyFields ? application.CreditScore : null,
            includeBankOnlyFields ? application.CreditScoreSource : null,
            includeBankOnlyFields ? application.CreditScoreCheckedAtUtc : null,
            includeBankOnlyFields ? application.CcrisRecordSummary : null,
            includeBankOnlyFields ? application.CtosScore : null,
            includeBankOnlyFields ? application.InternalAccountHistoryScore : null,
            includeBankOnlyFields ? application.BehaviourScore : null,
            includeBankOnlyFields ? application.FraudRiskScore : null,
            includeBankOnlyFields ? application.KycRiskScore : null,
            includeBankOnlyFields ? application.IncomeVerificationStatus : null,
            includeBankOnlyFields ? application.MissedPaymentCount : 0,
            includeBankOnlyFields ? application.RecommendedInitialLimit : null,
            includeBankOnlyFields ? application.ApprovedLimit : null,
            includeBankOnlyFields && application.IsLimitLocked,
            includeBankOnlyFields ? application.LimitDecisionReason : null,
            includeBankOnlyFields ? application.LimitReviewedAtUtc : null,
            application.OfferedAmount,
            application.OfferedTermMonths,
            application.DecisionNote,
            application.DecisionedAtUtc,
            application.OfferAcceptedAtUtc,
            application.EmploymentDurationMonths,
            application.NumberOfDependents,
            application.ResidentialStatus,
            application.CreatedAtUtc,
            application.SubmittedAtUtc);

    private static List<string> ValidateRequest(CreateLoanApplicationRequest request, LoanProduct product)
    {
        var application = new LoanApplication
        {
            LoanProduct = product,
            ApplicantFullName = request.ApplicantFullName,
            NationalIdNumber = request.NationalIdNumber,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            LoanPurpose = request.LoanPurpose,
            EmployerOrBusinessName = request.EmployerOrBusinessName,
            LoanAmount = request.LoanAmount,
            LoanTermMonths = request.LoanTermMonths,
            MonthlyIncome = request.MonthlyIncome,
            MonthlyExpenses = request.MonthlyExpenses,
            ExistingMonthlyDebt = request.ExistingMonthlyDebt,
            HasCreditHistoryConsent = request.HasCreditHistoryConsent,
            HasIncomeVerificationConsent = request.HasIncomeVerificationConsent,
            HasPersonalDataProcessingConsent = request.HasPersonalDataProcessingConsent,
            EmploymentDurationMonths = request.EmploymentDurationMonths,
            NumberOfDependents = request.NumberOfDependents
        };

        return ValidateApplication(application, product);
    }

    private static List<string> ValidateRequest(UpdateLoanApplicationRequest request, LoanProduct product)
    {
        var application = new LoanApplication
        {
            LoanProduct = product,
            ApplicantFullName = request.ApplicantFullName,
            NationalIdNumber = request.NationalIdNumber,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            LoanPurpose = request.LoanPurpose,
            EmployerOrBusinessName = request.EmployerOrBusinessName,
            LoanAmount = request.LoanAmount,
            LoanTermMonths = request.LoanTermMonths,
            MonthlyIncome = request.MonthlyIncome,
            MonthlyExpenses = request.MonthlyExpenses,
            ExistingMonthlyDebt = request.ExistingMonthlyDebt,
            HasCreditHistoryConsent = request.HasCreditHistoryConsent,
            HasIncomeVerificationConsent = request.HasIncomeVerificationConsent,
            HasPersonalDataProcessingConsent = request.HasPersonalDataProcessingConsent,
            EmploymentDurationMonths = request.EmploymentDurationMonths,
            NumberOfDependents = request.NumberOfDependents
        };

        return ValidateApplication(application, product);
    }

    private static List<string> ValidateApplication(LoanApplication application, LoanProduct? product)
    {
        var errors = new List<string>();

        if (product is null)
        {
            errors.Add("Loan product is required.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(application.ApplicantFullName))
        {
            errors.Add("Applicant full name is required.");
        }

        if (string.IsNullOrWhiteSpace(application.NationalIdNumber))
        {
            errors.Add("IC/MyKad number or passport number is required.");
        }

        if (string.IsNullOrWhiteSpace(application.PhoneNumber))
        {
            errors.Add("Phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(application.Email))
        {
            errors.Add("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(application.LoanPurpose))
        {
            errors.Add("Loan purpose is required.");
        }

        if (string.IsNullOrWhiteSpace(application.EmployerOrBusinessName))
        {
            errors.Add("Employer or business name is required.");
        }

        if (application.LoanAmount < product.MinAmount || application.LoanAmount > product.MaxAmount)
        {
            errors.Add($"Loan amount must be between {product.MinAmount:0.##} and {product.MaxAmount:0.##}.");
        }

        if (application.LoanTermMonths < product.MinTermMonths || application.LoanTermMonths > product.MaxTermMonths)
        {
            errors.Add($"Loan term must be between {product.MinTermMonths} and {product.MaxTermMonths} months.");
        }

        if (application.MonthlyIncome < 0)
        {
            errors.Add("Monthly income cannot be negative.");
        }

        if (RequiresPositiveIncome(application.EmploymentStatus) && application.MonthlyIncome <= 0)
        {
            errors.Add("Monthly income must be greater than 0 for employed and self-employed applicants.");
        }

        if (application.MonthlyExpenses < 0)
        {
            errors.Add("Monthly expenses cannot be negative.");
        }

        if (application.ExistingMonthlyDebt < 0)
        {
            errors.Add("Existing monthly debt cannot be negative.");
        }

        if (!application.HasCreditHistoryConsent)
        {
            errors.Add("Credit history check consent is required before continuing.");
        }

        if (!application.HasIncomeVerificationConsent)
        {
            errors.Add("Income and document verification consent is required before continuing.");
        }

        if (!application.HasPersonalDataProcessingConsent)
        {
            errors.Add("Personal data processing consent is required before continuing.");
        }

        if (application.EmploymentDurationMonths < 0)
        {
            errors.Add("Employment duration cannot be negative.");
        }

        if (application.NumberOfDependents < 0)
        {
            errors.Add("Number of dependents cannot be negative.");
        }

        return errors;
    }

    private static bool RequiresPositiveIncome(EmploymentStatus employmentStatus) =>
        employmentStatus is EmploymentStatus.Employed or EmploymentStatus.SelfEmployed;

    private static List<string> ValidateBankReview(UpdateBankReviewRequest request, LoanProduct? product)
    {
        var errors = new List<string>();

        if (request.CreditScore.HasValue && (request.CreditScore.Value < 300 || request.CreditScore.Value > 850))
        {
            errors.Add("Credit score must be between 300 and 850.");
        }

        if (request.BehaviourScore.HasValue && (request.BehaviourScore.Value < 0 || request.BehaviourScore.Value > 100))
        {
            errors.Add("Behaviour score must be between 0 and 100.");
        }

        if (request.CtosScore.HasValue && (request.CtosScore.Value < 300 || request.CtosScore.Value > 850))
        {
            errors.Add("CTOS score must be between 300 and 850.");
        }

        if (request.InternalAccountHistoryScore.HasValue && (request.InternalAccountHistoryScore.Value < 0 || request.InternalAccountHistoryScore.Value > 100))
        {
            errors.Add("Internal account history score must be between 0 and 100.");
        }

        if (request.FraudRiskScore.HasValue && (request.FraudRiskScore.Value < 0 || request.FraudRiskScore.Value > 100))
        {
            errors.Add("Fraud risk score must be between 0 and 100.");
        }

        if (request.KycRiskScore.HasValue && (request.KycRiskScore.Value < 0 || request.KycRiskScore.Value > 100))
        {
            errors.Add("KYC risk score must be between 0 and 100.");
        }

        if (request.MissedPaymentCount < 0)
        {
            errors.Add("Missed payment count cannot be negative.");
        }

        if (request.ApprovedLimit is < 0)
        {
            errors.Add("Approved limit cannot be negative.");
        }

        if (request.ApprovedLimit.HasValue && product is not null && request.ApprovedLimit > product.MaxAmount)
        {
            errors.Add($"Approved limit cannot exceed the product maximum of {product.MaxAmount:0.##}.");
        }

        if (request.IsLimitLocked && string.IsNullOrWhiteSpace(request.LimitDecisionReason))
        {
            errors.Add("A limit decision reason is required when the limit is locked.");
        }

        return errors;
    }

    private static decimal CalculateRecommendedInitialLimit(LoanApplication application)
    {
        var incomeBasedLimit = application.MonthlyIncome * 2m;
        var requestBasedLimit = application.LoanAmount * 0.3m;
        var baseLimit = Math.Min(incomeBasedLimit, requestBasedLimit);

        if (!application.CreditScore.HasValue ||
            application.CreditScore.Value < 620 ||
            application.BehaviourScore is < 45 ||
            application.InternalAccountHistoryScore is < 45 ||
            application.FraudRiskScore is > 50 ||
            application.KycRiskScore is > 50 ||
            application.MissedPaymentCount > 0)
        {
            baseLimit *= 0.5m;
        }

        if (application.CreditScore.HasValue && application.CreditScore.Value >= 750 && application.BehaviourScore is >= 75 && application.MissedPaymentCount == 0)
        {
            baseLimit *= 1.25m;
        }

        return Math.Round(Math.Max(500m, Math.Min(baseLimit, application.LoanAmount)), 2);
    }

    private static string GetPrimaryStaffRole(IReadOnlyCollection<string> roles)
    {
        if (roles.Contains(ApplicationRoles.Admin))
        {
            return ApplicationRoles.Admin;
        }

        if (roles.Contains(ApplicationRoles.Underwriter))
        {
            return ApplicationRoles.Underwriter;
        }

        if (roles.Contains(ApplicationRoles.LoanOfficer))
        {
            return ApplicationRoles.LoanOfficer;
        }

        return "Staff";
    }

    private static string FormatAmount(decimal? amount) =>
        amount.HasValue ? amount.Value.ToString("0.##") : "Not set";

    private static string FormatTerm(int? termMonths) =>
        termMonths.HasValue ? $"{termMonths.Value} months" : "Not set";

    private static string FormatText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "None" : value;

    private static List<string> ValidateDecision(UpdateApplicationDecisionRequest request, LoanApplication application, LoanProduct? product, IReadOnlyCollection<string> roles)
    {
        var errors = new List<string>();
        var allowedStatuses = new[]
        {
            LoanApplicationStatus.ManualReview,
            LoanApplicationStatus.Approved,
            LoanApplicationStatus.Rejected,
            LoanApplicationStatus.Cancelled,
            LoanApplicationStatus.Frozen
        };

        if (!allowedStatuses.Contains(request.Status))
        {
            errors.Add("Decision status must be manual review, approved, rejected, cancelled, or frozen.");
        }

        var isAdmin = roles.Contains(ApplicationRoles.Admin);
        var isUnderwriter = roles.Contains(ApplicationRoles.Underwriter);

        if (application.Status == LoanApplicationStatus.Frozen && !isAdmin)
        {
            errors.Add("Frozen applications can only be changed by an admin.");
        }

        if (application.Status == LoanApplicationStatus.OfferAccepted && !isAdmin)
        {
            errors.Add("Accepted loan offers can only be changed by an admin.");
        }

        if (request.Status == LoanApplicationStatus.Cancelled && !isAdmin && !isUnderwriter)
        {
            errors.Add("Only underwriters or admins can cancel an application.");
        }

        if (request.Status == LoanApplicationStatus.Frozen && !isAdmin)
        {
            errors.Add("Only admins can freeze an ongoing loan process.");
        }

        if ((request.Status == LoanApplicationStatus.Approved || request.Status == LoanApplicationStatus.Rejected) &&
            !isAdmin &&
            !isUnderwriter)
        {
            errors.Add("Only underwriters or admins can approve or reject an application.");
        }

        if (request.OfferedAmount is < 0)
        {
            errors.Add("Offered amount cannot be negative.");
        }

        if (request.OfferedAmount.HasValue && product is not null && request.OfferedAmount.Value > product.MaxAmount)
        {
            errors.Add($"Offered amount cannot exceed the product maximum of {product.MaxAmount:0.##}.");
        }

        if (request.OfferedAmount.HasValue && request.OfferedAmount.Value > application.LoanAmount)
        {
            errors.Add("Offered amount cannot exceed the customer's requested amount.");
        }

        if (request.OfferedTermMonths.HasValue && product is not null &&
            (request.OfferedTermMonths.Value < product.MinTermMonths || request.OfferedTermMonths.Value > product.MaxTermMonths))
        {
            errors.Add($"Offered term must be between {product.MinTermMonths} and {product.MaxTermMonths} months.");
        }

        if (request.Status == LoanApplicationStatus.Rejected && string.IsNullOrWhiteSpace(request.DecisionNote))
        {
            errors.Add("A decision note is required when rejecting an application.");
        }

        if ((request.Status == LoanApplicationStatus.Cancelled || request.Status == LoanApplicationStatus.Frozen) &&
            string.IsNullOrWhiteSpace(request.DecisionNote))
        {
            errors.Add("A decision note is required when cancelling or freezing an application.");
        }

        if (request.Status == LoanApplicationStatus.Approved)
        {
            if (request.OfferedAmount is null || request.OfferedAmount <= 0)
            {
                errors.Add("Offered amount is required before approving an application.");
            }

            if (request.OfferedTermMonths is null || request.OfferedTermMonths <= 0)
            {
                errors.Add("Offered term is required before approving an application.");
            }

            if (!HasCompletedAutomatedChecks(application))
            {
                errors.Add("Automated bank checks must be completed before approving an application.");
            }

            if (application.AffordabilityAssessment is null)
            {
                errors.Add("Affordability assessment must be completed before approving an application.");
            }

            if (application.RiskAssessment is null)
            {
                errors.Add("Risk assessment must be completed before approving an application.");
            }

            if (application.ApprovedLimit.HasValue &&
                request.OfferedAmount.HasValue &&
                request.OfferedAmount.Value > application.ApprovedLimit.Value)
            {
                errors.Add("Offered amount cannot exceed the approved credit limit.");
            }

            var documentErrors = RequiredDocumentReviewErrors(application);
            if (documentErrors.Count > 0)
            {
                errors.AddRange(documentErrors);
            }
        }

        return errors;
    }

    private static bool HasCompletedAutomatedChecks(LoanApplication application)
    {
        return application.CreditScoreSource == "Mock bureau check" &&
            application.CreditScore.HasValue &&
            application.CtosScore.HasValue &&
            !string.IsNullOrWhiteSpace(application.CcrisRecordSummary) &&
            application.InternalAccountHistoryScore.HasValue &&
            application.BehaviourScore.HasValue &&
            application.FraudRiskScore.HasValue &&
            application.KycRiskScore.HasValue &&
            !string.IsNullOrWhiteSpace(application.IncomeVerificationStatus) &&
            application.RecommendedInitialLimit.HasValue;
    }

    private static List<string> RequiredDocumentReviewErrors(LoanApplication application)
    {
        var errors = new List<string>();
        var acceptedDocuments = application.Documents
            .Where(document => document.Status == ApplicationDocumentStatus.Accepted)
            .ToArray();

        var documentStatuses = acceptedDocuments
            .GroupBy(document => document.DocumentType)
            .ToDictionary(group => group.Key, group => group.Count());

        var hasIncomeDocuments = documentStatuses.GetValueOrDefault(ApplicationDocumentType.Payslip) >= 3 &&
            documentStatuses.GetValueOrDefault(ApplicationDocumentType.BankStatement) >= 3;

        if (documentStatuses.GetValueOrDefault(ApplicationDocumentType.IdDocument) < 1)
        {
            errors.Add("An accepted ID document is required before approving an application.");
        }

        if (documentStatuses.GetValueOrDefault(ApplicationDocumentType.ProofOfAddress) < 1)
        {
            errors.Add("An accepted proof of address is required before approving an application.");
        }

        if (!hasIncomeDocuments)
        {
            errors.Add("Three accepted payslips and three accepted bank statements are required before approving an application.");
        }

        var requiredEvidenceDocuments = acceptedDocuments
            .Where(document => document.DocumentType is
                ApplicationDocumentType.IdDocument or
                ApplicationDocumentType.ProofOfAddress or
                ApplicationDocumentType.Payslip or
                ApplicationDocumentType.BankStatement)
            .ToArray();

        if (requiredEvidenceDocuments.Any(document => document.OcrStatus != DocumentOcrStatus.Extracted))
        {
            errors.Add("OCR must be completed for all accepted ID, address, payslip, and bank statement evidence before approving an application.");
        }

        if (requiredEvidenceDocuments.Any(document => string.Equals(document.OcrVerificationStatus, "Review", StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("OCR verification findings must be resolved before approving an application.");
        }

        if (requiredEvidenceDocuments.Any(document =>
                document.DocumentType is (ApplicationDocumentType.ProofOfAddress or ApplicationDocumentType.Payslip or ApplicationDocumentType.BankStatement) &&
                document.OcrIsRecent == false))
        {
            errors.Add("Address, payslip, and bank statement evidence must be within the recent 3-month window before approving an application.");
        }

        var verifiedPayslipIncomes = acceptedDocuments
            .Where(document => document.DocumentType == ApplicationDocumentType.Payslip)
            .Select(document => document.OcrSuggestedMonthlyIncome)
            .Where(value => value is > 0)
            .Select(value => value!.Value)
            .ToArray();

        if (application.MonthlyIncome > 0 && verifiedPayslipIncomes.Length >= 3)
        {
            var averagePayslipIncome = verifiedPayslipIncomes.Average();
            var variance = Math.Abs(averagePayslipIncome - application.MonthlyIncome) / application.MonthlyIncome;
            if (variance > 0.15m)
            {
                errors.Add("Average OCR payslip income differs from declared monthly income by more than 15%.");
            }
        }

        return errors;
    }

    private async Task ReplaceRepaymentScheduleAsync(LoanApplication application, CancellationToken cancellationToken)
    {
        var offeredAmount = application.OfferedAmount ?? application.LoanAmount;
        var offeredTermMonths = application.OfferedTermMonths ?? application.LoanTermMonths;
        var firstDueDate = DateOnly.FromDateTime((application.DecisionedAtUtc ?? DateTime.UtcNow).Date.AddMonths(1));
        var schedule = RepaymentScheduleCalculator.Calculate(new RepaymentScheduleCalculationInput(
            offeredAmount,
            offeredTermMonths,
            application.LoanProduct!.InterestRate,
            firstDueDate));

        await dbContext.RepaymentScheduleItems
            .Where(item => item.LoanApplicationId == application.Id)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var item in schedule)
        {
            dbContext.RepaymentScheduleItems.Add(new RepaymentScheduleItem
            {
                LoanApplicationId = application.Id,
                InstallmentNumber = item.InstallmentNumber,
                DueDate = item.DueDate,
                OpeningBalance = item.OpeningBalance,
                ScheduledPayment = item.ScheduledPayment,
                PrincipalAmount = item.PrincipalAmount,
                InterestAmount = item.InterestAmount,
                ClosingBalance = item.ClosingBalance
            });
        }
    }

    private static List<string> ValidateRequiredDocuments(IEnumerable<ApplicationDocument> documents)
    {
        var errors = new List<string>();
        var documentTypes = documents.Select(document => document.DocumentType).ToHashSet();

        if (!documentTypes.Contains(ApplicationDocumentType.IdDocument))
        {
            errors.Add("ID document is required before submission.");
        }

        if (!documentTypes.Contains(ApplicationDocumentType.ProofOfAddress))
        {
            errors.Add("Proof of address is required before submission.");
        }

        var payslipCount = documents.Count(document => document.DocumentType == ApplicationDocumentType.Payslip);
        var bankStatementCount = documents.Count(document => document.DocumentType == ApplicationDocumentType.BankStatement);

        if (payslipCount < 3)
        {
            errors.Add("Income stability evidence requires at least three recent monthly payslips before submission.");
        }

        if (bankStatementCount < 3)
        {
            errors.Add("Cashflow evidence requires at least three recent monthly bank statements before submission.");
        }

        return errors;
    }
}
