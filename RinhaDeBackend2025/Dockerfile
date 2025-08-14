FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RinhaDeBackend2025/RinhaDeBackend2025.csproj", "RinhaDeBackend2025/"]
RUN dotnet restore "RinhaDeBackend2025/RinhaDeBackend2025.csproj"
COPY . .
WORKDIR "/src/RinhaDeBackend2025"
RUN dotnet build "./RinhaDeBackend2025.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RinhaDeBackend2025.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RinhaDeBackend2025.dll"]
