FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Quizapp/Quizapp.Api/Quizapp.Api.csproj", "Quizapp/Quizapp.Api/"]
COPY ["Quizapp/Quizapp.Application/Quizapp.Application.csproj", "Quizapp/Quizapp.Application/"]
COPY ["Quizapp/Quizapp.Domain/Quizapp.Domain.csproj", "Quizapp/Quizapp.Domain/"]
COPY ["Quizapp/Quizapp.Infrastructure/Quizapp.Infrastructure.csproj", "Quizapp/Quizapp.Infrastructure/"]
RUN dotnet restore "Quizapp/Quizapp.Api/Quizapp.Api.csproj"
COPY . .
WORKDIR "/src/Quizapp/Quizapp.Api"
RUN dotnet build "./Quizapp.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Quizapp.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Quizapp.Api.dll"]
