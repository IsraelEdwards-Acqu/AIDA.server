# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY AIDA.server.sln .
COPY AIDA.Server/AIDA.Server.csproj AIDA.Server/

RUN dotnet restore AIDA.server.sln

# Copy everything else
COPY . .
WORKDIR /src/AIDA.Server
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AIDA.Server.dll"]
