FROM mcr.microsoft.com/dotnet/aspnet:8.0

RUN useradd -m appuser

WORKDIR /app

COPY publish/ .

RUN chown -R appuser:appuser /app

USER appuser

ENTRYPOINT ["dotnet", "question_answer.dll"]