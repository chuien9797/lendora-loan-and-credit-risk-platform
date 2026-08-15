# Lendora - Cloud-Deployed Loan Origination and Credit Risk Platform

## Repository

```text
lendora
```

## Project Title

**Lendora - Enterprise Loan Origination and Credit Risk Assessment Platform**

## Project Overview

Lendora is a full-stack loan origination and credit risk assessment platform built with ASP.NET Core, Entity Framework Core, PostgreSQL, React, TypeScript, and AWS-ready architecture.

The system simulates a realistic lending workflow for a Malaysia-focused financial services environment. Customers can register, create loan applications, upload required documents, submit applications, review approved repayment plans, and accept loan offers. Bank staff can run automated mock bank checks, review documents, assess affordability, view risk scoring, manage credit limits, make underwriting decisions, and search customer/application records.

The project is designed to show practical backend, frontend, financial workflow, cloud deployment, and production-readiness skills. It is not a simple loan calculator. It models a loan origination process with role separation, automated checks, explainable scoring, approval controls, and repayment schedule generation.

## Main Goals

- Build a realistic portfolio project for fintech, banking, mortgage, credit scoring, insurance, and enterprise workflow roles.
- Demonstrate C#, ASP.NET Core, EF Core, PostgreSQL, React, TypeScript, JWT auth, role-based authorization, financial calculations, and AWS deployment readiness.
- Show a complete demo loop from customer application to bank review, approval, repayment schedule, and customer offer acceptance.
- Keep bank-only scoring and decision data hidden from customers while still giving customers a clear application and repayment experience.

## Current Development Position

Lendora has moved beyond the original backend-only stages and now includes a working React frontend flow.

Current stage:

```text
Stage 17: Full CI/CD pipeline with GitHub Actions, AWS ECR/ECS backend deploy, and S3/CloudFront frontend deploy
Next stage: Final presentation screenshots and deployment verification polish
```

Completed or mostly completed:

- Backend foundation
- JWT authentication and refresh tokens
- Role-based access for Customer, LoanOfficer, Underwriter, and Admin
- React frontend foundation
- Login/register/token handling
- Customer dashboard
- Application list and clickable rows
- Loan application create/edit/submit flow
- Required document upload flow
- Actual file upload support for PDF, JPG, PNG, and TIFF evidence files up to 10 MB
- Staff document review
- S3 document storage support for AWS deployment
- Amazon Textract OCR integration path for staff-run document extraction
- Malaysia-aligned loan product interest rates
- Affordability calculation and DSR
- Risk scoring
- Automated mock CTOS/CCRIS/credit bureau/internal bank checks
- Staff review workflow
- Approved credit limit controls
- Underwriter decision workflow
- Repayment schedule generation
- Customer repayment plan view
- Customer loan offer acceptance flow
- Staff customer/application search
- Admin freeze and underwriter/admin cancel controls
- Application audit trail
- Admin user management
- Admin loan product management
- AWS Terraform deployment stack for ECS, RDS PostgreSQL, S3, CloudFront, ECR, Secrets Manager, CloudWatch, and Textract permissions

Not yet completed:

- CI/CD
- Final screenshots
- Production-grade payslip OCR field extraction

## Target Users

### Customer

The customer can:

- Register and log in
- Create a draft loan application
- Enter personal, employment, income, loan purpose, phone number, and IC/MyKad or passport information
- Give consent for credit history checks, income/document verification, and personal data processing
- Upload required documents
- Submit an application
- View application status
- View approved repayment schedule
- See interest rate, offer amount, total principal, total interest, and monthly repayment breakdown
- Accept an approved loan offer
- See confirmation that funds will be transferred in 1-3 days

### Loan Officer

The loan officer can:

- View submitted applications
- Review application details
- Review uploaded documents
- Run automated bank checks
- View affordability and risk scoring results
- Update bank review details where allowed
- Search customers/applications by name, IC/passport, phone number, user id, application id, or email

### Underwriter

The underwriter can:

- View applications requiring manual review
- Review affordability, risk, bureau, CTOS, CCRIS, KYC, fraud, and internal behaviour signals
- Approve applications
- Reject applications
- Cancel new applications where appropriate
- Offer a lower loan amount or shorter term
- Add decision notes
- Generate repayment schedule through approval

Underwriters cannot approve or change frozen applications. Frozen cases require admin action.

### Admin

The admin can:

- View staff/customer application information
- Freeze ongoing loan processes
- Cancel or change decisions where higher control is required
- Search customer/application records
- View operational review queues
- Create, update, disable, and delete user accounts
- Assign a single operational role to each user account
- Create, update, enable/disable, and delete loan products where safe
- View audit trail entries on application details

## Core Workflow

```text
1. Customer registers/logs in
2. Customer creates a draft loan application
3. Customer fills personal, employment, income, loan, phone, and IC/passport details
4. Customer grants required consent
5. Customer uploads required documents
6. Customer submits application
7. System runs automated mock bank checks
8. System fills CTOS, CCRIS, bureau, internal behaviour, fraud, KYC, income verification, and recommended limit data
9. System calculates affordability and DSR
10. System calculates risk score and recommendation
11. Staff reviews documents and bank check result
12. Underwriter approves, rejects, cancels, or keeps manual review
13. If approved, system generates repayment schedule
14. Customer views repayment plan and interest rate
15. Customer accepts loan offer
16. System shows transfer pending message for 1-3 days
```

## Application Statuses

Current status model:

```text
Draft
Submitted
AssessmentInProgress
ManualReview
Approved
Rejected
Cancelled
Frozen
OfferAccepted
```

Status meaning:

- `Draft`: Customer can still edit.
- `Submitted`: Customer has submitted and application is locked.
- `AssessmentInProgress`: Affordability/risk work has started.
- `ManualReview`: Staff/underwriter review is required.
- `Approved`: Bank has approved an offer and generated repayment schedule.
- `Rejected`: Bank rejected the application.
- `Cancelled`: Underwriter/admin cancelled the application.
- `Frozen`: Admin froze the process; non-admin staff cannot approve/change it.
- `OfferAccepted`: Customer accepted the approved loan offer after reviewing the repayment plan.

## Malaysia-Aligned Loan Review Model

The customer does not manually enter a credit score because normal applicants usually do not know their credit score.

Instead, the customer provides:

- Name
- IC/MyKad number or passport number if not Malaysian
- Phone number
- Email
- Employment status
- Employer or business information
- Income
- Expenses
- Existing debt
- Loan amount
- Loan purpose
- Consent for credit, income/document verification, and personal data processing

The bank-side system checks:

- Mock credit bureau score
- Mock CTOS score
- Mock CCRIS/eCCRIS summary
- Internal account history score
- Behaviour score
- Fraud risk score
- KYC risk score
- Income verification status
- Recommended starting credit limit
- Approved credit limit
- Affordability and DSR
- Risk score and recommendation

Customer-visible information is separated from bank-worker-only scoring data.

## Important Business Rules

- Customers can only edit draft applications.
- Submitted applications are locked for customers.
- Required documents must be uploaded before submission.
- Required documents must be accepted before approval.
- Automated bank checks must be completed before approval.
- Affordability assessment must exist before approval.
- Risk assessment must exist before approval.
- Approval requires offered amount and offered term.
- Offered amount can be lower than the product minimum as a reduced bank offer.
- Offered amount cannot exceed the customer requested amount.
- Offered amount cannot exceed product maximum.
- Offered amount cannot exceed approved credit limit when an approved credit limit exists.
- Rejection requires a decision note.
- Cancel/freeze requires a decision note.
- Only underwriters or admins can cancel applications.
- Only admins can freeze ongoing loan processes.
- Frozen applications cannot be approved or changed by underwriters.
- Accepted loan offers are locked from normal staff changes.
- Repayment schedule is visible after approval and remains visible after customer acceptance.

## Main Modules

### 1. Authentication and Roles

Features:

- Register
- Login
- JWT authentication
- Refresh token
- Role-based authorization
- Seed default admin account

Roles:

```text
Customer
LoanOfficer
Underwriter
Admin
```

### 2. Loan Application Workflow

Features:

- Loan products
- Draft creation
- Draft update
- Submit application
- View my applications
- Staff review queue
- Application details page
- Staff customer/application search

Current application fields include:

- Applicant full name
- IC/MyKad number or passport number
- Phone number
- Email
- Loan product
- Loan purpose
- Loan amount
- Loan term
- Employment status
- Employer/business name
- Employer/business registration number
- Monthly income
- Monthly expenses
- Existing monthly debt
- Employment duration
- Number of dependents
- Residential status
- Required consent fields

### 3. Document Upload and Review

The project now supports actual document upload, not only metadata.

Supported formats:

```text
PDF
JPG
JPEG
PNG
TIF
TIFF
```

Rules:

- Max file size is 10 MB per file.
- Required documents are checked before submission.
- Customer can upload documents during application flow.
- Customer can upload additional/replacement documents later if staff requests them.
- Staff can accept, reject, or request resubmission.

### 4. Automated Bank Checks

Because real CTOS/CCRIS APIs are not available in development, Lendora uses a mock automated bank-check engine.

Example endpoint:

```text
POST /api/loan-applications/{id}/bank-checks/run
```

The mock engine fills:

- Credit bureau score
- Credit score source
- CTOS score
- CCRIS/eCCRIS summary
- Internal account history score
- Behaviour score
- Fraud risk score
- KYC risk score
- Income verification status
- Missed payment count
- Recommended starting limit
- Approved credit limit
- Affordability result
- Risk result

### 5. Affordability

Affordability calculates:

```text
Monthly repayment
Total repayment
Total interest
Debt service ratio / DSR
Disposable income
Result: Pass, Caution, or Fail
```

Core formulas:

```text
Monthly interest rate = Annual interest rate / 12

Monthly repayment =
P * r * (1 + r)^n / ((1 + r)^n - 1)

DSR =
(Existing monthly debt + New monthly repayment) / Monthly income * 100

Disposable income =
Monthly income - Monthly expenses - Existing monthly debt - New monthly repayment

Total repayment =
Monthly repayment * Loan term months

Total interest =
Total repayment - Principal
```

Where:

```text
P = principal / loan amount
r = monthly interest rate
n = loan term in months
```

### 6. Risk Scoring

Risk scoring combines:

- Bureau score
- CTOS score
- CCRIS summary
- Internal account history
- Behaviour score
- Fraud risk
- KYC risk
- Income verification
- Employment stability
- Affordability result
- Debt pressure

The result includes:

- Risk score
- Risk grade
- Recommendation
- Explainable factors/reasons

### 7. Underwriter Workflow

Underwriter/admin can:

- Review application
- Review documents
- Review automated bank checks
- Review affordability
- Review risk scoring
- Set approved credit limit
- Approve application
- Reject application
- Cancel application
- Offer lower amount or shorter term

Admin can additionally freeze ongoing loan processes.

### 8. Repayment Schedule and Offer Acceptance

When an application is approved:

- The system generates repayment schedule items.
- Customer can view repayment plan.
- Customer can see interest rate.
- Customer can see total principal, total interest, and scheduled repayment total.
- Customer can accept the offer.
- After acceptance, the system shows that funds will be transferred in 1-3 days.

Repayment schedule uses:

```text
Approved offer amount
Approved offer term
Loan product interest rate
First due date based on approval date
```

### 9. Audit Logging

The system records workflow and staff actions in an application audit trail. Staff and admins can view chronological activity on the application details page.

Audit events include application submission, document upload/review, OCR extraction, automated checks, affordability/risk generation, bank review updates, underwriting decisions, repayment schedule generation, offer acceptance, cancellation, and freeze actions.

### 10. AWS Observability

AWS observability is designed around structured console logs collected by ECS and CloudWatch Logs. Recommended dashboard metrics:

- API request duration
- Failed requests
- Unhandled exceptions
- Database dependency failures
- Authentication failures
- Automated bank-check errors
- Affordability errors
- Risk scoring errors
- Underwriting decision errors
- Repayment schedule generation errors

Custom events:

```text
LoanApplicationSubmitted
AutomatedBankChecksCompleted
AffordabilityAssessmentGenerated
RiskAssessmentGenerated
UnderwritingDecisionMade
RepaymentScheduleGenerated
LoanOfferAccepted
ApplicationFrozen
```

## AWS Deployment Strategy

Planned AWS services:

| System Part | AWS Service |
| --- | --- |
| ASP.NET Core backend API | Amazon ECS on Fargate |
| React frontend | Amazon S3 + Amazon CloudFront |
| Database | Amazon RDS for PostgreSQL |
| Logs and monitoring | Amazon CloudWatch |
| Container registry | Amazon ECR |
| Secrets/configuration | AWS Secrets Manager or SSM Parameter Store |
| Uploaded documents | Amazon S3 |
| OCR | Amazon Textract |
| CI/CD | GitHub Actions |

## Updated Development Stages

### Stage 1 - Backend Foundation

Status: completed.

Deliverables:

- ASP.NET Core API
- Clean project structure
- EF Core
- PostgreSQL-ready database
- Swagger
- Global exception middleware
- Standard API response wrapper

### Stage 2 - Authentication and Roles

Status: completed.

Deliverables:

- ASP.NET Core Identity
- JWT
- Refresh tokens
- Customer/LoanOfficer/Underwriter/Admin roles
- Default admin seeding
- Protected endpoints

### Stage 3 - Loan Application Backend Workflow

Status: completed.

Deliverables:

- Loan products
- Loan application entity
- Draft/update/submit flow
- Customer application list
- Staff review queue

### Stage 4 - Frontend Foundation

Status: completed.

Deliverables:

- React/Vite/TypeScript frontend
- Login/register pages
- Token handling
- Protected routes
- Role-aware layout

### Stage 5 - Customer Loan Application UI

Status: completed.

Deliverables:

- Customer dashboard
- My applications page
- Create application page
- Application details page
- Draft/edit/submit flow
- Clickable application rows

### Stage 6 - Document Upload

Status: completed.

Deliverables:

- Required document step
- Actual file upload
- File type validation
- 10 MB max file size
- Staff review
- Additional document upload after submission

### Stage 7 - Affordability

Status: completed.

Deliverables:

- Monthly repayment
- Total repayment
- Total interest
- DSR
- Disposable income
- Pass/Caution/Fail result
- Unit tests

### Stage 8 - Automated Bank Checks and Risk Scoring

Status: completed.

Deliverables:

- Mock CTOS/CCRIS/bureau/internal checks
- Automated bank-check endpoint
- Risk score
- Risk recommendation
- Explainable factors
- Bank-only visibility
- Unit tests

### Stage 9 - Underwriter Workflow

Status: completed.

Deliverables:

- Staff review queue
- Bank review controls
- Underwriter decision form
- Approval/rejection/manual review
- Cancel/freeze status handling
- Role-based restrictions
- Customer/staff search

### Stage 10 - Repayment Schedule and Offer Acceptance

Status: completed.

Deliverables:

- Repayment schedule generated after approval
- Customer repayment schedule view
- Interest rate shown to applicant
- Customer offer acceptance confirmation panel
- Transfer pending message after acceptance
- Accepted offer status

### Stage 11 - Audit Logging

Status: completed.

Deliverables:

- Audit log entity
- Audit logging service
- Log core application actions
- Admin audit log endpoint
- Audit log UI on staff/admin side

### Stage 12 - AWS Observability Readiness

Status: completed.

Deliverables:

- Structured console logging retained for CloudWatch collection
- CloudWatch-focused observability plan documented
- AWS deployment configuration documented

### Stage 13 - AWS Deployment

Status: completed for demo deployment.

Deliverables:

- API Dockerfile
- Frontend Dockerfile and Nginx static hosting config
- AWS deployment guide
- Terraform stack for ECS/Fargate, ECR, RDS PostgreSQL, S3, CloudFront, CloudWatch, Secrets Manager, and Textract permissions
- S3/CloudFront frontend deployment
- ECR/ECS API deployment

### Stage 14 - Admin Management and Document Evidence Controls

Status: completed.

Deliverables:

- Admin user management
- Admin loan product management
- Single-role account assignment
- Required 3 payslips and 3 bank statements before submission
- Staff-only OCR visibility
- Secure document open/download

### Stage 15 - OCR and Rule-Based Decision Support

Status: completed for demo; advanced payslip accuracy deferred to future work.

Deliverables:

- Staff-triggered OCR endpoint
- Textract-backed extraction path
- OCR status, confidence, document date, recency, and verification findings
- Rule-based fraud/anomaly flags
- Rule-based underwriter recommendation
- Human final approval always required
- Known limitation documented for complex payslip field extraction

### Stage 16 - Portfolio Documentation Polish

Status: completed.

Deliverables:

- GitHub README
- Updated proposal
- Demo flow
- Known limitations and future-work framing
- AWS deployment notes

### Stage 17 - CI/CD and Screenshots

Status: current.

Deliverables:

- GitHub Actions workflow for backend restore, build, test, publish, and AWS deployment
- GitHub Actions workflow for frontend install, build, S3 upload, and CloudFront invalidation
- Deployment jobs gated by successful backend and frontend validation
- GitHub OIDC-based AWS role assumption instead of long-term AWS access keys
- README and AWS deployment documentation for required GitHub secrets, variables, and IAM permissions

## Recommended Demo Flow

```text
1. Customer logs in
2. Customer creates application
3. Customer uploads required documents
4. Customer submits application
5. Staff searches or opens the submitted application
6. Staff reviews documents and runs OCR where configured
7. Staff runs automated bank checks
8. Underwriter reviews affordability, risk, decision support, and audit trail
9. Underwriter approves with an offered amount and term
10. System generates repayment schedule
11. Customer views interest rate and repayment plan
12. Customer accepts offer
13. Customer sees transfer pending in 1-3 days
14. Admin manages staff/customer accounts and loan products
15. Admin freezes a case to show control restrictions
```

## Recommended README Screenshots

- Login page
- Customer dashboard
- Create loan application form
- Document upload step
- Application details page
- Staff review queue
- Automated bank checks panel
- Affordability/risk panels
- Underwriter decision panel
- Repayment schedule and offer acceptance panel
- Staff customer search page
- Audit trail panel
- Admin user management page
- Admin loan product management page
- AWS deployed frontend/backend
- Future: CloudWatch dashboard screenshot
- Future: GitHub Actions pipeline

## Future Improvements

- Real CTOS/CCRIS integration
- Advanced payslip OCR accuracy: current OCR can read document text, but payslip salary extraction is still rule-based and can misidentify values on complex templates. Future work should use Textract forms/tables, payslip template mapping, confidence scoring per field, and a human confirmation step before extracted salary updates underwriting inputs.
- Three-month income evidence verification: strengthen validation so the system compares confirmed payslip month, employer, and monthly salary across the latest three payslips against the applicant-declared income before underwriting approval.
- Bank statement cashflow analytics: extract monthly credits, debits, recurring commitments, overdraft patterns, and ending balances from the latest three bank statements to produce a stronger spending/cashflow summary.
- Notification/email system
- Offer expiry background job
- Loan disbursement tracking
- Repayment payment status tracking
- Configurable risk rule editor
- CloudWatch dashboards
- PDF export for loan offer and repayment schedule
- Multi-tenant support

## Final GitHub Description

```text
Lendora is a cloud-deployed loan origination and credit risk assessment platform built with ASP.NET Core, Entity Framework Core, PostgreSQL, React, TypeScript, and AWS-ready architecture. It supports customer loan applications, document upload, JWT authentication, role-based access control, automated mock bank checks, Malaysia-aligned affordability assessment, credit risk scoring, underwriting workflows, repayment schedule generation, customer loan offer acceptance, staff search, audit logging, AWS observability readiness, and GitHub Actions CI/CD deployment.
```

## Resume Bullets

- Built Lendora, a full-stack loan origination and credit risk assessment platform using ASP.NET Core, Entity Framework Core, PostgreSQL, React, TypeScript, and AWS-ready architecture, supporting customer loan applications, document upload, role-based access control, affordability checks, risk scoring, underwriting decisions, repayment schedules, and customer offer acceptance.

- Implemented a Malaysia-aligned lending workflow with customer consent capture, mock CTOS/CCRIS/credit bureau checks, internal behaviour scoring, DSR affordability calculation, approved credit limit controls, underwriter approval/rejection/cancellation, admin freeze controls, and bank-worker-only risk visibility.

- Designed a production-style portfolio system with JWT authentication, refresh tokens, EF Core migrations, automated financial calculations, explainable risk recommendations, polished React pages, staff search, repayment schedule generation, audit logging, CloudWatch monitoring readiness, AWS deployment, and GitHub Actions CI/CD.

## Final Positioning

Lendora should be positioned as:

```text
A Malaysia-aligned, cloud-ready loan origination and credit risk workflow platform, not a simple loan calculator or prediction app.
```

Strongest selling points:

- Real lending workflow
- Customer/staff/admin role separation
- Automated mock bank checks
- Affordability and DSR calculation
- Explainable risk scoring
- Underwriter decision process
- Approved credit limit vs loan offer amount controls
- Repayment schedule generation
- Customer loan offer acceptance
- Staff search
- Audit logging
- Admin user and loan product management
- AWS deployment and GitHub Actions CI/CD
