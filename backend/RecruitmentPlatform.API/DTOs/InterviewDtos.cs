using System;

namespace RecruitmentPlatform.API.DTOs
{
    public class CreateInterviewDto
    {
        public string CandidateId { get; set; } = string.Empty;
        public string JobPostingId { get; set; } = string.Empty;
        public DateTime InterviewDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string MeetingLink { get; set; } = string.Empty;
    }

    public class InterviewResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string CandidateId { get; set; } = string.Empty;
        public string CandidateName { get; set; } = string.Empty;
        public string CandidateEmail { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public DateTime InterviewDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string MeetingLink { get; set; } = string.Empty;
        public string Status { get; set; } = "Scheduled";
    }

    public class UpdateInterviewStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
