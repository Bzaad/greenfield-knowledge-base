FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src

# Copy project file first so restore is a cached layer (only re-runs when .csproj changes)
COPY KnowledgeBase.Api/KnowledgeBase.Api.csproj KnowledgeBase.Api/
RUN dotnet restore KnowledgeBase.Api/KnowledgeBase.Api.csproj

COPY KnowledgeBase.Api/ KnowledgeBase.Api/
RUN dotnet publish KnowledgeBase.Api/KnowledgeBase.Api.csproj \
    --configuration Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KnowledgeBase.Api.dll"]
