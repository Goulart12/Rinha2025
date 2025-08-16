FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["RinhaDeBackend2025.csproj", "."]
RUN dotnet restore "./RinhaDeBackend2025.csproj"
COPY . .
RUN dotnet publish "RinhaDeBackend2025.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "RinhaDeBackend2025.dll"]