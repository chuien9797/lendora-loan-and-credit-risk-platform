using System.Globalization;
using System.Text.RegularExpressions;
using Lendora.Application.Abstractions.Audit;
using Lendora.Application.Abstractions.Documents;
using Lendora.Application.Documents;
using Lendora.Application.Loans;
using Lendora.Domain.Entities;
using Lendora.Domain.Enums;
using Lendora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lendora.Infrastructure.Documents;

internal sealed partial class DocumentOcrService(
    ApplicationDbContext dbContext,
    IDocumentTextExtractor textExtractor,
    IApplicationAuditService auditService,
    IOptions<DocumentOcrOptions> options) : IDocumentOcrService
{
    private const int MaxStoredTextLength = 12000;
    private static readonly IReadOnlySet<string> SupportedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/tiff"
    };

    public async Task<ServiceResult<ApplicationDocumentDto>> ExtractAsync(
        Guid reviewerId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await dbContext.ApplicationDocuments
            .Include(candidate => candidate.LoanApplication)
            .FirstOrDefaultAsync(candidate => candidate.Id == documentId, cancellationToken);

        if (document is null)
        {
            return ServiceResult<ApplicationDocumentDto>.Failure("Document metadata not found.");
        }

        if (!SupportedContentTypes.Contains(document.ContentType))
        {
            MarkOcrFailure(document, DocumentOcrStatus.NotSupported, "OCR supports PDF, JPG, PNG, and TIFF files only.");
            await SaveFailureAsync(document, reviewerId, cancellationToken);
            return ServiceResult<ApplicationDocumentDto>.Success(MapToDto(document));
        }

        var extraction = await textExtractor.ExtractAsync(document, cancellationToken);
        if (!extraction.Succeeded || extraction.Data is null)
        {
            MarkOcrFailure(document, DocumentOcrStatus.Failed, extraction.Errors.FirstOrDefault() ?? "OCR extraction failed.");
            await SaveFailureAsync(document, reviewerId, cancellationToken);
            return ServiceResult<ApplicationDocumentDto>.Success(MapToDto(document));
        }

        var parsed = ParseSuggestions(document, extraction.Data.Lines);
        var extractedText = string.Join(Environment.NewLine, extraction.Data.Lines);

        document.OcrStatus = DocumentOcrStatus.Extracted;
        document.OcrProvider = options.Value.Provider;
        document.OcrConfidence = extraction.Data.Confidence;
        document.OcrSuggestedMonthlyIncome = parsed.MonthlyIncome;
        document.OcrSuggestedMonthlyExpenses = parsed.MonthlyExpenses;
        document.OcrSuggestedNationalIdNumber = parsed.NationalIdNumber;
        document.OcrNationalIdMatchesApplication = parsed.NationalIdMatchesApplication;
        document.OcrSuggestedAddress = parsed.Address;
        document.OcrDocumentDate = parsed.DocumentDate;
        document.OcrIsRecent = parsed.IsRecent;
        document.OcrVerificationStatus = parsed.VerificationStatus;
        document.OcrVerificationFindings = parsed.VerificationFindings;
        document.OcrSummary = BuildSummary(document.DocumentType, parsed, extraction.Data.Lines.Count, extraction.Data.Confidence);
        document.OcrExtractedText = extractedText.Length > MaxStoredTextLength
            ? extractedText[..MaxStoredTextLength]
            : extractedText;
        document.OcrFailureReason = null;
        document.OcrProcessedByUserId = reviewerId;
        document.OcrProcessedAtUtc = DateTime.UtcNow;
        document.UpdatedAtUtc = DateTime.UtcNow;

        await auditService.RecordAsync(
            document.LoanApplicationId,
            reviewerId,
            "Staff",
            "DocumentOcrExtracted",
            $"Staff ran OCR extraction for {document.DocumentType}.",
            $"Provider: {document.OcrProvider}. Confidence: {document.OcrConfidence?.ToString("0.##", CultureInfo.InvariantCulture) ?? "n/a"}. Suggested income: {FormatMoney(document.OcrSuggestedMonthlyIncome)}. Suggested expenses: {FormatMoney(document.OcrSuggestedMonthlyExpenses)}. ID match: {FormatMatch(document.OcrNationalIdMatchesApplication)}.",
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ApplicationDocumentDto>.Success(MapToDto(document));
    }

    private async Task SaveFailureAsync(ApplicationDocument document, Guid reviewerId, CancellationToken cancellationToken)
    {
        document.OcrProvider = options.Value.Provider;
        document.OcrProcessedByUserId = reviewerId;
        document.OcrProcessedAtUtc = DateTime.UtcNow;
        document.UpdatedAtUtc = DateTime.UtcNow;

        await auditService.RecordAsync(
            document.LoanApplicationId,
            reviewerId,
            "Staff",
            "DocumentOcrFailed",
            $"OCR extraction could not be completed for {document.DocumentType}.",
            document.OcrFailureReason,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void MarkOcrFailure(ApplicationDocument document, DocumentOcrStatus status, string reason)
    {
        document.OcrStatus = status;
        document.OcrFailureReason = reason;
        document.OcrSuggestedMonthlyIncome = null;
        document.OcrSuggestedMonthlyExpenses = null;
        document.OcrSuggestedNationalIdNumber = null;
        document.OcrNationalIdMatchesApplication = null;
        document.OcrSuggestedAddress = null;
        document.OcrDocumentDate = null;
        document.OcrIsRecent = null;
        document.OcrVerificationStatus = null;
        document.OcrVerificationFindings = null;
        document.OcrSummary = null;
        document.OcrExtractedText = null;
        document.OcrConfidence = null;
    }

    private static ParsedOcrSuggestions ParseSuggestions(ApplicationDocument document, IReadOnlyCollection<string> lines)
    {
        decimal? income = null;
        decimal? expenses = null;
        string? nationalIdNumber = null;
        bool? nationalIdMatchesApplication = null;
        string? address = null;
        var documentDate = ExtractDocumentDate(lines);
        bool? isRecent = documentDate is null
            ? null
            : documentDate.Value >= DateTime.UtcNow.Date.AddMonths(-3);
        var largestAmount = lines
            .Select(ExtractLargestAmount)
            .Where(amount => amount is > 0)
            .DefaultIfEmpty()
            .Max();
        var orderedLines = lines.ToArray();

        if (document.DocumentType == ApplicationDocumentType.Payslip)
        {
            income = ExtractPayslipMonthlyIncome(
                orderedLines,
                document.LoanApplication?.MonthlyIncome);
        }
        else if (document.DocumentType == ApplicationDocumentType.BankStatement)
        {
            income = ExtractAmountNearKeywords(orderedLines, 100m, "salary", "payroll", "income", "credit", "deposit", "giro");
            expenses = ExtractAmountNearKeywords(orderedLines, 100m, "expense", "expenses", "debit", "withdrawal", "payment", "transfer", "card");
        }

        foreach (var line in orderedLines)
        {
            var normalized = line.ToLowerInvariant();
            var amount = ExtractLargestAmount(line);
            if (amount is null)
            {
                continue;
            }

            if (income is null &&
                document.DocumentType != ApplicationDocumentType.Payslip &&
                ContainsAny(normalized, "gross salary", "net salary", "basic salary", "monthly salary", "monthly income", "income", "salary", "wages"))
            {
                income = amount;
                continue;
            }

            if (expenses is null &&
                document.DocumentType != ApplicationDocumentType.Payslip &&
                ContainsAny(normalized, "expense", "expenses", "commitment", "commitments", "deduction", "deductions", "repayment", "instalment", "installment", "debit"))
            {
                expenses = amount;
            }
        }

        if (document.DocumentType == ApplicationDocumentType.IdDocument)
        {
            nationalIdNumber = ExtractNationalId(lines);
            if (!string.IsNullOrWhiteSpace(nationalIdNumber) && document.LoanApplication is not null)
            {
                nationalIdMatchesApplication = NormalizeIdentifier(nationalIdNumber) == NormalizeIdentifier(document.LoanApplication.NationalIdNumber);
            }
        }

        if (document.DocumentType == ApplicationDocumentType.ProofOfAddress)
        {
            address = ExtractAddress(lines);
        }

        if (document.DocumentType is ApplicationDocumentType.PropertyValuation or ApplicationDocumentType.TaxDocument or ApplicationDocumentType.InsuranceDocument &&
            income is null)
        {
            income = largestAmount;
        }

        var verification = BuildVerification(document, lines, income, expenses, nationalIdNumber, nationalIdMatchesApplication, address, documentDate, isRecent);

        return new ParsedOcrSuggestions(
            income,
            expenses,
            nationalIdNumber,
            nationalIdMatchesApplication,
            address,
            documentDate,
            isRecent,
            verification.Status,
            verification.Findings);
    }

    private static decimal? ExtractLargestAmount(string line)
    {
        return ExtractAmounts(line)
            .Select(value => (decimal?)value)
            .DefaultIfEmpty()
            .Max();
    }

    private static decimal? ExtractAmountNearKeywords(IReadOnlyList<string> lines, decimal minimumAmount, params string[] keywords)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            if (!ContainsAny(lines[index].ToLowerInvariant(), keywords))
            {
                continue;
            }

            var candidateLines = new[]
            {
                lines[index],
                index + 1 < lines.Count ? lines[index + 1] : string.Empty,
                index + 2 < lines.Count ? lines[index + 2] : string.Empty,
                index > 0 ? lines[index - 1] : string.Empty
            };

            var amount = candidateLines
                .Select(ExtractLargestAmount)
                .Where(value => value is not null && value.Value >= minimumAmount)
                .DefaultIfEmpty()
                .Max();

            if (amount is not null)
            {
                return amount;
            }
        }

        return null;
    }

    private static decimal? ExtractPayslipMonthlyIncome(IReadOnlyList<string> lines, decimal? declaredMonthlyIncome)
    {
        var candidates = new List<PayslipAmountCandidate>();

        for (var index = 0; index < lines.Count; index++)
        {
            var currentLine = lines[index];
            var normalized = currentLine.ToLowerInvariant();
            var labelScore = PayslipSalaryLabelScore(normalized);
            if (labelScore == 0)
            {
                continue;
            }

            var candidateLines = new[]
            {
                (Line: currentLine, Distance: 0),
                (Line: index + 1 < lines.Count ? lines[index + 1] : string.Empty, Distance: 1),
                (Line: index + 2 < lines.Count ? lines[index + 2] : string.Empty, Distance: 2),
                (Line: index > 0 ? lines[index - 1] : string.Empty, Distance: 1)
            };

            foreach (var (line, distance) in candidateLines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var amountLine = line.ToLowerInvariant();
                foreach (var amount in ExtractAmounts(line))
                {
                    if (!IsPlausibleMonthlyIncomeCandidate(amount, declaredMonthlyIncome))
                    {
                        continue;
                    }

                    var score = labelScore - (distance * 10) + PayslipAmountScore(amount, declaredMonthlyIncome);
                    if (ContainsAny(amountLine, "ytd", "year to date", "annual", "yearly", "taxable", "employee no", "employee number", "account no", "account number", "epf no", "socso no", "tax no", "reference"))
                    {
                        score -= 80;
                    }

                    candidates.Add(new PayslipAmountCandidate(amount, score));
                }
            }
        }

        return candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Amount)
            .Select(candidate => (decimal?)candidate.Amount)
            .FirstOrDefault();
    }

    private static int PayslipSalaryLabelScore(string line)
    {
        if (ContainsAny(line, "net pay", "net salary", "net income", "take home"))
        {
            return 120;
        }

        if (ContainsAny(line, "gross pay", "gross salary", "gross income", "basic pay", "basic salary", "monthly salary", "monthly income"))
        {
            return 100;
        }

        if (ContainsAny(line, "total earnings", "total income", "total salary", "salary", "wages"))
        {
            return 75;
        }

        return 0;
    }

    private static int PayslipAmountScore(decimal amount, decimal? declaredMonthlyIncome)
    {
        if (declaredMonthlyIncome is null or <= 0)
        {
            return amount <= 50_000m ? 20 : -100;
        }

        var variance = Math.Abs(amount - declaredMonthlyIncome.Value) / declaredMonthlyIncome.Value;
        return variance switch
        {
            <= 0.05m => 120,
            <= 0.15m => 90,
            <= 0.30m => 50,
            <= 0.50m => 10,
            _ => -120
        };
    }

    private static bool IsPlausibleMonthlyIncomeCandidate(decimal amount, decimal? declaredMonthlyIncome)
    {
        if (amount < 500m)
        {
            return false;
        }

        if (declaredMonthlyIncome is > 0)
        {
            return amount <= Math.Max(50_000m, declaredMonthlyIncome.Value * 1.8m);
        }

        return amount <= 50_000m;
    }

    private static IEnumerable<decimal> ExtractAmounts(string line)
    {
        return MoneyAmountRegex()
            .Matches(line)
            .Where(match => match.Index + match.Length >= line.Length || line[match.Index + match.Length] != '%')
            .Select(match => match.Value.Replace("RM", string.Empty, StringComparison.OrdinalIgnoreCase).Replace(",", string.Empty).Trim())
            .Select(value => decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed) ? parsed : (decimal?)null)
            .Where(value => value is > 0)
            .Select(value => value!.Value);
    }

    private static bool ContainsAny(string text, params string[] keywords) =>
        keywords.Any(text.Contains);

    private static string BuildSummary(ApplicationDocumentType documentType, ParsedOcrSuggestions parsed, int lineCount, decimal? confidence)
    {
        var parts = new List<string>
        {
            $"OCR read {lineCount} text lines"
        };

        if (confidence is not null)
        {
            parts.Add($"average confidence {confidence:0.##}%");
        }

        if (parsed.DocumentDate is not null)
        {
            parts.Add($"document date {parsed.DocumentDate.Value:dd MMM yyyy}");
        }

        if (parsed.IsRecent is not null)
        {
            parts.Add(parsed.IsRecent.Value ? "document is within the recent 3-month window" : "document is older than the recent 3-month window");
        }

        if (documentType == ApplicationDocumentType.Payslip)
        {
            parts.Add(parsed.MonthlyIncome is null
                ? "monthly income was not confidently identified"
                : $"suggested monthly income is RM {parsed.MonthlyIncome.Value:N0}");
        }
        else if (documentType == ApplicationDocumentType.BankStatement)
        {
            parts.Add(parsed.MonthlyIncome is null
                ? "monthly income was not confidently identified"
                : $"suggested monthly income is RM {parsed.MonthlyIncome.Value:N0}");

            parts.Add(parsed.MonthlyExpenses is null
                ? "monthly expenses were not confidently identified"
                : $"suggested monthly expenses are RM {parsed.MonthlyExpenses.Value:N0}");
        }

        if (documentType == ApplicationDocumentType.IdDocument && string.IsNullOrWhiteSpace(parsed.NationalIdNumber))
        {
            parts.Add("ID number was not confidently identified");
        }
        else if (!string.IsNullOrWhiteSpace(parsed.NationalIdNumber))
        {
            parts.Add($"extracted ID number {parsed.NationalIdNumber}");
        }

        if (parsed.NationalIdMatchesApplication is not null)
        {
            parts.Add(parsed.NationalIdMatchesApplication.Value
                ? "ID number matches the application"
                : "ID number does not match the application");
        }

        if (documentType == ApplicationDocumentType.ProofOfAddress && string.IsNullOrWhiteSpace(parsed.Address))
        {
            parts.Add("address was not confidently identified");
        }
        else if (!string.IsNullOrWhiteSpace(parsed.Address))
        {
            parts.Add($"possible address: {parsed.Address}");
        }

        parts.Add("values are suggestions only and require human verification");
        return string.Join("; ", parts) + ".";
    }

    private static VerificationResult BuildVerification(
        ApplicationDocument document,
        IReadOnlyCollection<string> lines,
        decimal? income,
        decimal? expenses,
        string? nationalIdNumber,
        bool? nationalIdMatchesApplication,
        string? address,
        DateTime? documentDate,
        bool? isRecent)
    {
        var findings = new List<string>();

        switch (document.DocumentType)
        {
            case ApplicationDocumentType.IdDocument:
                if (string.IsNullOrWhiteSpace(nationalIdNumber))
                {
                    findings.Add("ID/passport number was not detected.");
                }
                else if (nationalIdMatchesApplication == true)
                {
                    findings.Add("Detected ID/passport number matches the application.");
                }
                else
                {
                    findings.Add("Detected ID/passport number does not match the application.");
                }
                break;

            case ApplicationDocumentType.ProofOfAddress:
                findings.Add(string.IsNullOrWhiteSpace(address)
                    ? "Address text was not confidently detected."
                    : "Address text candidate was detected for staff comparison.");
                if (documentDate is not null)
                {
                    findings.Add(isRecent == true ? "Proof of address appears recent." : "Proof of address may be older than 3 months.");
                }
                break;

            case ApplicationDocumentType.Payslip:
                findings.Add(income is null ? "Monthly salary was not detected." : "Monthly salary candidate was detected.");
                findings.Add(documentDate is null
                    ? "Payslip month/date was not detected."
                    : isRecent == true
                        ? "Payslip appears to be within the recent 3-month window."
                        : "Payslip appears older than the recent 3-month window.");
                break;

            case ApplicationDocumentType.BankStatement:
                findings.Add(income is null ? "Income credit was not detected." : "Income credit candidate was detected.");
                findings.Add(expenses is null ? "Expense/debit pattern was not detected." : "Expense/debit candidate was detected.");
                findings.Add(documentDate is null
                    ? "Statement date was not detected."
                    : isRecent == true
                        ? "Bank statement appears recent."
                        : "Bank statement may be older than 3 months.");
                break;

            case ApplicationDocumentType.EmploymentLetter:
                findings.Add(ContainsAnyLines(lines, "employment", "employed", "position", "salary", "basic pay", "designation")
                    ? "Employment-related wording was detected."
                    : "Employment-related wording was not confidently detected.");
                break;

            case ApplicationDocumentType.PropertyValuation:
                findings.Add(income is null ? "Valuation amount was not detected." : "Valuation amount candidate was detected.");
                break;

            case ApplicationDocumentType.TaxDocument:
                findings.Add(income is null ? "Taxable income/amount was not detected." : "Tax/income amount candidate was detected.");
                break;

            case ApplicationDocumentType.InsuranceDocument:
                findings.Add(income is null ? "Premium/insured amount was not detected." : "Insurance amount candidate was detected.");
                break;
        }

        var hasHighRiskFinding = findings.Any(finding =>
            finding.Contains("not detected", StringComparison.OrdinalIgnoreCase) ||
            finding.Contains("does not match", StringComparison.OrdinalIgnoreCase) ||
            finding.Contains("older than", StringComparison.OrdinalIgnoreCase));

        return new VerificationResult(
            hasHighRiskFinding ? "Review" : "Pass",
            string.Join(" ", findings));
    }

    private static string FormatMoney(decimal? value) =>
        value is null ? "n/a" : $"RM {value.Value:N2}";

    private static string FormatMatch(bool? value) =>
        value is null ? "n/a" : value.Value ? "match" : "mismatch";

    private static string? ExtractNationalId(IReadOnlyCollection<string> lines)
    {
        foreach (var line in lines)
        {
            var match = NationalIdRegex().Match(line);
            if (match.Success)
            {
                return match.Value.Trim();
            }
        }

        return null;
    }

    private static string? ExtractAddress(IReadOnlyCollection<string> lines)
    {
        var addressLines = lines
            .Select(line => line.Trim())
            .Where(line => line.Length >= 8)
            .Where(line => AddressKeywordRegex().IsMatch(line) || PostcodeRegex().IsMatch(line))
            .Take(4)
            .ToArray();

        return addressLines.Length == 0 ? null : string.Join(", ", addressLines);
    }

    private static string NormalizeIdentifier(string value) =>
        IdentifierCleanupRegex().Replace(value, string.Empty).ToUpperInvariant();

    private static DateTime? ExtractDocumentDate(IReadOnlyCollection<string> lines)
    {
        foreach (var line in lines)
        {
            var numericDate = NumericDateRegex().Match(line);
            if (numericDate.Success &&
                DateTime.TryParseExact(numericDate.Value, ["dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedNumericDate))
            {
                return DateTime.SpecifyKind(parsedNumericDate.Date, DateTimeKind.Utc);
            }

            var monthYear = MonthYearRegex().Match(line);
            if (monthYear.Success)
            {
                var month = MonthNumber(monthYear.Groups["month"].Value);
                if (month is not null && int.TryParse(monthYear.Groups["year"].Value, out var year))
                {
                    return new DateTime(year, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
                }
            }
        }

        return null;
    }

    private static int? MonthNumber(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "jan" or "january" => 1,
            "feb" or "february" => 2,
            "mar" or "march" => 3,
            "apr" or "april" => 4,
            "may" => 5,
            "jun" or "june" => 6,
            "jul" or "july" => 7,
            "aug" or "august" => 8,
            "sep" or "sept" or "september" => 9,
            "oct" or "october" => 10,
            "nov" or "november" => 11,
            "dec" or "december" => 12,
            _ => null
        };
    }

    private static bool ContainsAnyLines(IReadOnlyCollection<string> lines, params string[] keywords) =>
        lines.Any(line => keywords.Any(keyword => line.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

    private static ApplicationDocumentDto MapToDto(ApplicationDocument document) =>
        new(
            document.Id,
            document.LoanApplicationId,
            document.DocumentType,
            document.OriginalFileName,
            document.StoredFileName,
            document.StoragePath,
            document.FileSize,
            document.ContentType,
            document.UploadedByUserId,
            document.UploadedAtUtc,
            document.SubmittedToBank,
            document.Status,
            document.ReviewNote,
            document.ReviewedByUserId,
            document.ReviewedAtUtc,
            document.OcrStatus,
            document.OcrProvider,
            document.OcrConfidence,
            document.OcrSuggestedMonthlyIncome,
            document.OcrSuggestedMonthlyExpenses,
            document.OcrSuggestedNationalIdNumber,
            document.OcrNationalIdMatchesApplication,
            document.OcrSuggestedAddress,
            document.OcrDocumentDate,
            document.OcrIsRecent,
            document.OcrVerificationStatus,
            document.OcrVerificationFindings,
            document.OcrSummary,
            document.OcrExtractedText,
            document.OcrFailureReason,
            document.OcrProcessedByUserId,
            document.OcrProcessedAtUtc);

    [GeneratedRegex(@"(?<![\d.])(?:RM\s*)?(?:\d{1,3}(?:,\d{3})+|\d+)(?:\.\d{1,2})?(?![\d.%])", RegexOptions.IgnoreCase)]
    private static partial Regex MoneyAmountRegex();

    [GeneratedRegex(@"\b[A-Z]{0,2}\d{6,12}[A-Z]{0,2}\b", RegexOptions.IgnoreCase)]
    private static partial Regex NationalIdRegex();

    [GeneratedRegex(@"\b(jalan|jln|lorong|persiaran|taman|kampung|kg|no\.?|unit|block|blok|apartment|residence|street|road|postcode|poskod)\b", RegexOptions.IgnoreCase)]
    private static partial Regex AddressKeywordRegex();

    [GeneratedRegex(@"\b\d{5}\b")]
    private static partial Regex PostcodeRegex();

    [GeneratedRegex(@"\b\d{1,2}[/-]\d{1,2}[/-]\d{4}\b")]
    private static partial Regex NumericDateRegex();

    [GeneratedRegex(@"\b(?<month>jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t|tember)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)\s+(?<year>20\d{2})\b", RegexOptions.IgnoreCase)]
    private static partial Regex MonthYearRegex();

    [GeneratedRegex(@"[^A-Z0-9]", RegexOptions.IgnoreCase)]
    private static partial Regex IdentifierCleanupRegex();

    private sealed record ParsedOcrSuggestions(
        decimal? MonthlyIncome,
        decimal? MonthlyExpenses,
        string? NationalIdNumber,
        bool? NationalIdMatchesApplication,
        string? Address,
        DateTime? DocumentDate,
        bool? IsRecent,
        string VerificationStatus,
        string VerificationFindings);

    private sealed record VerificationResult(string Status, string Findings);

    private sealed record PayslipAmountCandidate(decimal Amount, int Score);
}
