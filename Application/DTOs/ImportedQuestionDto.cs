using question_answer.Domain.Enums;

namespace question_answer.Application.DTOs;

public class ImportedQuestionDto
{
    public int? QuestionNumber { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<ImportedOptionDto> Options { get; set; } = new();
    public string RawAnswers { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string? Reference { get; set; }
    
    public List<string> Warnings { get; set; } = new();
    public bool IsValid { get; set; }
    public QuestionType Type { get; set; }
}

public class ImportedOptionDto
{
    public string Key { get; set; } = string.Empty; // e.g. A, B, C
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
