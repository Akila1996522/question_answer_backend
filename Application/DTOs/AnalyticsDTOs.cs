namespace question_answer.Application.DTOs;

public class SuperAdminAnalyticsDto
{
    public int TotalUsers { get; set; }
    public int PendingApprovalsCount { get; set; }
    public int TotalExams { get; set; }
    public int TotalAttempts { get; set; }
    public decimal OverallPassRate { get; set; }
    
    public List<QuestionPerformanceDto> TopQuestions { get; set; } = new();
    public List<RecentActivityDto> RecentActivity { get; set; } = new();
}

public class CreatorAnalyticsDto
{
    public int MyQuestionsCount { get; set; }
    public int MyExamsCount { get; set; }
    public int TotalAttemptsOnMyExams { get; set; }
    
    public List<ExamPerformanceDto> ExamPerformances { get; set; } = new();
}

public class ExamPerformanceDto
{
    public string ExamTitle { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public decimal AverageScore { get; set; }
}

public class QuestionPerformanceDto
{
    public string QuestionExcerpt { get; set; } = string.Empty;
    public int IncludedInExamsCount { get; set; }
    public decimal CorrectnessRate { get; set; }
}

public class RecentActivityDto
{
    public DateTime Timestamp { get; set; }
    public string Description { get; set; } = string.Empty;
}
