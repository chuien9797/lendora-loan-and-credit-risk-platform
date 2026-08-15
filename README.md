# Lendora Loan Origination and Credit Risk Workflow Platform

Lendora is a Malaysia-aligned loan origination and credit risk workflow platform built with ASP.NET Core, Entity Framework Core, PostgreSQL, React, TypeScript, Docker, and AWS infrastructure-as-code.

It models a realistic lending workflow rather than a simple loan calculator: customers submit applications and documents, bank workers review evidence and run automated checks, underwriters make controlled final decisions, and customers can review repayment schedules and accept approved offers.

## Highlights

- Role-based workflows for Customer, Loan Officer, Underwriter, and Admin.
- JWT authentication with refresh tokens and seeded demo users.
- Customer loan application draft, edit, submit, document upload, repayment schedule, and offer acceptance flow.
- Staff review queue, customer/application search, document review, OCR trigger, affordability, risk scoring, audit trail, and underwriting decision support.
- Admin user management and loan product management.
- Rule-based affordability, DSR, risk scoring, fraud/KYC flags, and human-approval-required decision support.
- PostgreSQL persistence with EF Core migrations.
- Local document storage for development and S3 document storage for AWS.
- AWS Terraform stack for ECS Fargate, ALB, RDS PostgreSQL, S3, CloudFront, ECR, Secrets Manager, CloudWatch Logs, and Textract permissions.

## Tech Stack

| Area | Technology |
| --- | --- |
| Backend | ASP.NET Core, C#, EF Core |
| Database | PostgreSQL |
| Frontend | React, TypeScript, Vite |
| Auth | ASP.NET Core Identity, JWT, refresh tokens |
| Cloud | AWS ECS Fargate, RDS, S3, CloudFront, ECR, Secrets Manager, CloudWatch |
| Infrastructure | Terraform |
| Documents/OCR | Local/S3 storage, Amazon Textract integration path |
| Testing | .NET tests, TypeScript compiler check |

## Demo Accounts

These are seeded in local development only. Do not use these passwords in a deployed environment.

| Role | Email | Password |
| --- | --- | --- |
| Customer | `customer@lendora.local` | `Customer12345` |
| Loan Officer | `officer@lendora.local` | `Officer12345` |
| Underwriter | `underwriter@lendora.local` | `Underwriter12345` |
| Admin | `admin@lendora.local` | `Admin12345` |

## Local Setup

Prerequisites:

- .NET 10 SDK
- Node.js
- Docker Desktop

Start PostgreSQL:

```powershell
cd C:\GitHub\lendora
docker compose up -d postgres
```

Restore, migrate, and run the API:

```powershell
cd C:\GitHub\lendora

$env:APPDATA='C:\GitHub\lendora\.appdata\appdata'
$env:DOTNET_CLI_HOME='C:\GitHub\lendora\.appdata\dotnet-home'
$env:NUGET_PACKAGES='C:\GitHub\lendora\.appdata\nuget-packages'

dotnet restore Lendora.slnx --configfile NuGet.Config
dotnet ef database update --project src\Lendora.Infrastructure --startup-project src\Lendora.Infrastructure
dotnet run --project src\Lendora.Api
```

Run the frontend in another PowerShell window:

```powershell
cd C:\GitHub\lendora\frontend\lendora-client
npm install
npm run dev
```

Open the Vite URL shown in the terminal.


## Known Limitations

- CTOS/CCRIS/credit bureau checks are mocked for portfolio/demo purposes.
- Payslip OCR field extraction is intentionally listed as future work. Textract can read text accurately, but robust salary extraction from arbitrary payslip templates needs forms/tables extraction, template mapping, field-level confidence, and human confirmation.
- Email notifications, offer expiry jobs, real disbursement tracking, and repayment collection are future improvements.
- Demo Terraform defaults prioritize affordability and are not hardened for production data retention.

## Project Positioning

Lendora is best presented as a cloud-ready fintech workflow project showing enterprise-style backend design, React application flows, role-based controls, underwriting logic, auditability, and AWS deployment readiness.
