# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.sln .
COPY AIDA.Server/*.csproj ./AIDA.Server/
RUN dotnet restore

# Copy everything else and build
COPY . .
WORKDIR /src/AIDA.Server
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AIDA.Server.dll"]
