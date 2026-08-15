namespace Lendora.Domain.Enums;

public enum LoanApplicationStatus
{
    Draft = 1,
    Submitted = 2,
    AssessmentInProgress = 3,
    ManualReview = 4,
    Approved = 5,
    Rejected = 6,
    Cancelled = 7,
    Frozen = 8,
    OfferAccepted = 9
}
