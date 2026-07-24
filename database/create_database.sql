-- ============================================================
-- RecruitmentPlatform - MySQL schema
-- Run this whole script in MySQL Workbench (File > Open SQL Script,
-- then the lightning-bolt "Execute" button, or select-all + Ctrl+Shift+Enter)
-- ============================================================

CREATE DATABASE IF NOT EXISTS RecruitmentPlatformDb
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE RecruitmentPlatformDb;

-- ------------------------------------------------------------
-- Users
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Users (
    Id            CHAR(36)      NOT NULL PRIMARY KEY,
    Email         VARCHAR(255)  NOT NULL,
    PasswordHash  VARCHAR(255)  NOT NULL,
    Role          VARCHAR(50)   NOT NULL DEFAULT 'Candidate',
    FirstName     VARCHAR(100)  NOT NULL DEFAULT '',
    LastName      VARCHAR(100)  NOT NULL DEFAULT '',
    Bio           TEXT          NULL,
    Skills        TEXT          NULL,
    Experience    TEXT          NULL,
    Education     TEXT          NULL,
    ResumeUrl     VARCHAR(500)  NULL,
    CreatedAt     DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------------
-- JobPostings
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS JobPostings (
    Id              CHAR(36)      NOT NULL PRIMARY KEY,
    Title           VARCHAR(255)  NOT NULL,
    Description     TEXT          NULL,
    RequiredSkills  TEXT          NULL,
    RecruiterId     CHAR(36)      NOT NULL,
    CreatedAt       DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    IsActive        TINYINT(1)    NOT NULL DEFAULT 1,
    CONSTRAINT FK_JobPostings_Recruiter FOREIGN KEY (RecruiterId)
        REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX IX_JobPostings_RecruiterId (RecruiterId),
    INDEX IX_JobPostings_IsActive (IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------------
-- Applications
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Applications (
    Id             CHAR(36)       NOT NULL PRIMARY KEY,
    JobPostingId   CHAR(36)       NOT NULL,
    CandidateId    CHAR(36)       NOT NULL,
    ResumeUrl      VARCHAR(500)   NULL,
    Status         VARCHAR(50)    NOT NULL DEFAULT 'Applied',
    AiMatchScore   DECIMAL(5,2)   NOT NULL DEFAULT 0,
    AppliedAt      DATETIME(6)    NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT FK_Applications_JobPosting FOREIGN KEY (JobPostingId)
        REFERENCES JobPostings(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Applications_Candidate FOREIGN KEY (CandidateId)
        REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Applications_Job_Candidate UNIQUE (JobPostingId, CandidateId),
    INDEX IX_Applications_CandidateId (CandidateId),
    INDEX IX_Applications_Status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------------
-- UserDocuments
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS UserDocuments (
    Id             CHAR(36)       NOT NULL PRIMARY KEY,
    UserId         CHAR(36)       NOT NULL,
    DocumentType   VARCHAR(50)    NOT NULL DEFAULT 'Resume',
    FileName       VARCHAR(255)   NOT NULL,
    FileUrl        VARCHAR(500)   NOT NULL,
    UploadedAt     DATETIME(6)    NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT FK_UserDocuments_User FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX IX_UserDocuments_UserId (UserId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------------
-- Notifications
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Notifications (
    Id             CHAR(36)       NOT NULL PRIMARY KEY,
    CandidateId    CHAR(36)       NOT NULL,
    Type           VARCHAR(50)    NOT NULL,
    Subject        VARCHAR(255)   NULL,
    Message        TEXT           NOT NULL,
    Status         VARCHAR(50)    NOT NULL DEFAULT 'Pending',
    SentAt         DATETIME(6)    NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT FK_Notifications_Candidate FOREIGN KEY (CandidateId)
        REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX IX_Notifications_CandidateId (CandidateId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------------
-- Interviews
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Interviews (
    Id             CHAR(36)       NOT NULL PRIMARY KEY,
    CandidateId    CHAR(36)       NOT NULL,
    JobPostingId   CHAR(36)       NOT NULL,
    InterviewDate  DATETIME(6)    NOT NULL,
    Location       VARCHAR(255)   NULL,
    MeetingLink    VARCHAR(500)   NULL,
    Status         VARCHAR(50)    NOT NULL DEFAULT 'Scheduled',
    ReminderSent   TINYINT(1)     NOT NULL DEFAULT 0,
    CreatedAt      DATETIME(6)    NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT FK_Interviews_Candidate FOREIGN KEY (CandidateId)
        REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Interviews_JobPosting FOREIGN KEY (JobPostingId)
        REFERENCES JobPostings(Id) ON DELETE CASCADE,
    INDEX IX_Interviews_CandidateId (CandidateId),
    INDEX IX_Interviews_JobPostingId (JobPostingId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------------
-- Organizations
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Organizations (
    Id             CHAR(36)       NOT NULL PRIMARY KEY,
    Name           VARCHAR(255)   NOT NULL,
    HQLocation     VARCHAR(255)   NULL,
    Headcount      INT            NOT NULL DEFAULT 0,
    Website        VARCHAR(255)   NULL,
    CreatedAt      DATETIME(6)    NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------------
-- Departments
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Departments (
    Id             CHAR(36)       NOT NULL PRIMARY KEY,
    Name           VARCHAR(255)   NOT NULL,
    Code           VARCHAR(50)    NULL,
    Manager        VARCHAR(255)   NULL,
    StaffCount     INT            NOT NULL DEFAULT 0,
    Vacancies      INT            NOT NULL DEFAULT 0,
    CreatedAt      DATETIME(6)    NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------------
-- SkillAssessments
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS SkillAssessments (
    Id             CHAR(36)       NOT NULL PRIMARY KEY,
    CandidateId    CHAR(36)       NOT NULL,
    SkillName      VARCHAR(255)   NOT NULL,
    Score          DECIMAL(5,2)   NOT NULL,
    Status         VARCHAR(50)    NOT NULL DEFAULT 'Completed',
    CompletedAt    DATETIME(6)    NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT FK_SkillAssessments_Candidate FOREIGN KEY (CandidateId)
        REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX IX_SkillAssessments_CandidateId (CandidateId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------------
-- RecruitmentAnalytics
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS RecruitmentAnalytics (
    Id             CHAR(36)       NOT NULL PRIMARY KEY,
    EventType      VARCHAR(100)   NOT NULL,
    Description    TEXT           NULL,
    RecordedAt     DATETIME(6)    NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
