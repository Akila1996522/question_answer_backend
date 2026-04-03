using question_answer.Application.DTOs;

namespace question_answer.Application.Services;

public interface IDocxParserService
{
    List<ImportedQuestionDto> ParseDocxQuestions(Stream stream);
}
