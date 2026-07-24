# TalentAI — AI-Powered Recruitment Platform

TalentAI is a full-stack recruitment platform that connects candidates, recruiters, hiring managers, and admins in one system. It supports job postings, applications, AI-assisted screening, interview scheduling, and automated communication (email/SMS notifications).

## Tech Stack

**Backend**
- ASP.NET Core (.NET) Web API
- Entity Framework Core + Pomelo (MySQL provider)
- MySQL database
- JWT-based authentication
- MailKit (email) + Twilio (SMS)
- Google Gemini API (AI features)

**Frontend**
- Angular
- TypeScript

## Project Structure

```
AI-Platform/
├── RecruitmentPlatform.API/       # Backend (.NET Web API)
│   ├── Controllers/                # API endpoints
│   ├── Models/                     # EF Core entity models
│   ├── DTOs/                       # Request/response objects
│   ├── Services/                   # Business logic (Email, SMS, Notifications)
│   ├── Interfaces/                 # Service contracts
│   ├── BackgroundServices/         # Scheduled jobs (interview reminders)
│   ├── Data/                       # AppDbContext (EF Core)
│   ├── appsettings.json            # Base config (placeholders only)
│   └── appsettings.Development.json # Local secrets (gitignored, not committed)
└── RecruitmentPlatform.Client/    # Frontend (Angular)
    └── src/app/
        ├── pages/                  # Feature pages (sign-in, dashboard, jobs, etc.)
        └── services/                # API + Auth services
```

## Features

- **Authentication** — Register/login with JWT, role-based access (`Candidate`, `Recruiter`, `HiringManager`, `Admin`)
- **Job Postings** — Recruiters can create, list, and manage job postings
- **Applications** — Candidates apply to jobs; recruiters review, shortlist, and update application status
- **AI Insights** — Gemini-powered chat/matching assistance
- **Interview Scheduling** — Interview records with automatic 24-hour reminder emails (background service)
- **Communication Service** — Email and SMS notifications for application status updates and interview reminders
- **Admin Dashboard** — User management and recruitment analytics
- **Candidate Profile** — Resume upload, skills, experience, education

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (matching the project's target framework)
- [Node.js](https://nodejs.org/) + npm
- [MySQL Server](https://dev.mysql.com/downloads/) (running locally or remotely)
- A Gmail account with an [App Password](https://myaccount.google.com/apppasswords) (for email notifications)
- A [Twilio](https://www.twilio.com/try-twilio) account (for SMS notifications, optional)

### 1. Clone the repository

```bash
git clone <https://github.com/Lakshan994/AI-Recruitment-Platform.git>
cd AI-Platform
```

### 2. Database setup

Create the MySQL database and required tables. Run the SQL scripts in `RecruitmentPlatform.API/database/` (if present) or ensure your schema matches the models in `RecruitmentPlatform.API/Models/`.

### 3. Backend configuration

The backend reads configuration from two files:
- `appsettings.json` — committed to git, should only contain **placeholder** values
- `appsettings.Development.json` — **not committed** (gitignored), contains your real local secrets

Create `RecruitmentPlatform.API/appsettings.Development.json` with your real values:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Jwt": {
    "Key": "YOUR_OWN_SECRET_KEY",
    "Issuer": "RecruitmentPlatform",
    "Audience": "RecruitmentPlatformClient"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1;Port=3306;Database=recruitmentplatformdb;User=root;Password=YOUR_DB_PASSWORD;GuidFormat=None;"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY"
  },
  "EmailSettings": {
    "SenderName": "AI Recruitment Platform",
    "SenderEmail": "yourgmail@gmail.com",
    "Username": "yourgmail@gmail.com",
    "Password": "YOUR_GMAIL_APP_PASSWORD",
    "SmtpServer": "smtp.gmail.com",
    "Port": "587"
  },
  "Twilio": {
    "AccountSid": "YOUR_TWILIO_ACCOUNT_SID",
    "AuthToken": "YOUR_TWILIO_AUTH_TOKEN",
    "PhoneNumber": "+1234567890"
  }
}
```

> ⚠️ Use `127.0.0.1` rather than `localhost` in the connection string to avoid IPv6 resolution issues on some Windows machines.

### 4. Run the backend

```bash
cd RecruitmentPlatform.API
dotnet run
```

The API starts at `http://localhost:5076`.

### 5. Run the frontend

```bash
cd RecruitmentPlatform.Client
npm install
npm start
```

The app starts at `http://localhost:4200`.

## Notes on Secrets

- **Never commit `appsettings.Development.json`.** It's excluded via `.gitignore`.
- If a secret is ever accidentally committed, rotate it immediately (regenerate the Gmail App Password, Twilio Auth Token, and/or JWT key) and remove it from the branch with `git rm --cached`.
- GitHub's push protection will block pushes containing recognizable secrets (API keys, tokens) — treat a blocked push as a signal to check `git status` before re-adding files.

## Communication Service

The platform sends automated notifications through:
- **Email** (`EmailService` via MailKit/SMTP) — application status updates, interview reminders
- **SMS** (`SmsService` via Twilio) — optional, requires a verified Twilio number (verified caller ID on trial accounts)
- **Interview Reminder Background Job** — runs periodically, checks for interviews within the next 24 hours, and sends a reminder if one hasn't been sent yet (`ReminderSent` flag)

All outgoing notifications are logged in the `Notifications` table with a `Status` of `Sent` or `Failed`.

