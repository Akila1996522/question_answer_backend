using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using question_answer.Application.DTOs;
using question_answer.Domain.Enums;
using System.Text.RegularExpressions;

namespace question_answer.Application.Services;

public class DocxParserService : IDocxParserService
{
    public List<ImportedQuestionDto> ParseDocxQuestions(Stream stream)
    {
        var questions = new List<ImportedQuestionDto>();
        ImportedQuestionDto? currentQuestion = null;
        string currentState = "SEARCHING"; // SEARCHING, QUESTION, OPTIONS, ANSWER, EXPLANATION, REFERENCE

        using (var document = WordprocessingDocument.Open(stream, false))
        {
            var body = document?.MainDocumentPart?.Document?.Body;
            if (body == null) return questions;

            var paragraphs = body.Elements<Paragraph>();

            foreach (var paragraph in paragraphs)
            {
                var text = paragraph.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(text)) continue;

                // Match "1. " or "Q1." or "1)" 
                var questionMatch = Regex.Match(text, @"^Q?(\d+)[\.\)]\s*(.+)");
                if (questionMatch.Success)
                {
                    if (currentQuestion != null)
                    {
                        FinalizeQuestion(currentQuestion);
                        questions.Add(currentQuestion);
                    }

                    currentQuestion = new ImportedQuestionDto
                    {
                        QuestionNumber = int.Parse(questionMatch.Groups[1].Value),
                        Text = questionMatch.Groups[2].Value,
                        IsValid = true
                    };
                    currentState = "QUESTION";
                    continue;
                }

                if (currentQuestion == null) continue;

                // Check for Option like "A." or "A)" or "A-"
                var optionMatch = Regex.Match(text, @"^([A-E])[\.\)-]\s+(.+)");
                if (optionMatch.Success)
                {
                    currentState = "OPTIONS";
                    currentQuestion.Options.Add(new ImportedOptionDto
                    {
                        Key = optionMatch.Groups[1].Value.ToUpper(),
                        Text = optionMatch.Groups[2].Value
                    });
                    continue;
                }

                if (text.StartsWith("Answer:", StringComparison.OrdinalIgnoreCase) || text.StartsWith("Answers:", StringComparison.OrdinalIgnoreCase))
                {
                    currentState = "ANSWER";
                    currentQuestion.RawAnswers = text.Substring(text.IndexOf(":") + 1).Trim();
                    continue;
                }

                if (text.StartsWith("Explanation:", StringComparison.OrdinalIgnoreCase))
                {
                    currentState = "EXPLANATION";
                    currentQuestion.Explanation = text.Substring(text.IndexOf(":") + 1).Trim();
                    continue;
                }

                if (text.StartsWith("Reference:", StringComparison.OrdinalIgnoreCase) || text.StartsWith("References:", StringComparison.OrdinalIgnoreCase))
                {
                    currentState = "REFERENCE";
                    currentQuestion.Reference = text.Substring(text.IndexOf(":") + 1).Trim();
                    continue;
                }

                // If it doesn't match a new prefix, append to existing buffer based on State
                switch (currentState)
                {
                    case "QUESTION":
                        currentQuestion.Text += "\n" + text;
                        break;
                    case "OPTIONS":
                        if (currentQuestion.Options.Any())
                        {
                            currentQuestion.Options.Last().Text += "\n" + text;
                        }
                        break;
                    case "ANSWER":
                        currentQuestion.RawAnswers += " " + text;
                        break;
                    case "EXPLANATION":
                        currentQuestion.Explanation += "\n" + text;
                        break;
                    case "REFERENCE":
                        currentQuestion.Reference += "\n" + text;
                        break;
                }
            }

            if (currentQuestion != null)
            {
                FinalizeQuestion(currentQuestion);
                questions.Add(currentQuestion);
            }
        }

        return questions;
    }

    private void FinalizeQuestion(ImportedQuestionDto q)
    {
        // Trim spaces
        q.Text = q.Text?.Trim() ?? "";
        q.RawAnswers = q.RawAnswers?.Trim().ToUpper() ?? "";
        q.Explanation = q.Explanation?.Trim();
        q.Reference = q.Reference?.Trim();

        // Validate basic rules
        if (string.IsNullOrEmpty(q.Text))
        {
            q.Warnings.Add("Question text is empty.");
            q.IsValid = false;
        }

        if (q.Options.Count < 2)
        {
            q.Warnings.Add($"Question contains only {q.Options.Count} options. At least 2 are required.");
            q.IsValid = false;
        }

        if (string.IsNullOrEmpty(q.RawAnswers))
        {
            q.Warnings.Add("No answer was provided.");
            q.IsValid = false;
        }

        // Map Answers
        var answerParts = Regex.Matches(q.RawAnswers, @"[A-E]");
        int mappedAnswers = 0;
        foreach (Match match in answerParts)
        {
            var charKey = match.Value;
            var opt = q.Options.FirstOrDefault(o => o.Key == charKey);
            if (opt != null)
            {
                opt.IsCorrect = true;
                mappedAnswers++;
            }
        }

        if (mappedAnswers == 0)
        {
            q.Warnings.Add($"Raw answer '{q.RawAnswers}' didn't map to any of the parsed options.");
            q.IsValid = false;
        }

        q.Type = mappedAnswers > 1 ? QuestionType.MultipleChoice : QuestionType.SingleChoice;
    }
}
