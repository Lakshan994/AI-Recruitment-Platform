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
