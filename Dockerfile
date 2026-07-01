# Use official .NET 8 SDK image to build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY AIDA.Server.csproj .
RUN dotnet restore AIDA.Server.csproj

# Copy everything else and build
COPY . .
RUN dotnet publish AIDA.Server.csproj -c Release -o /app/publish

# Use official .NET 8 runtime image to run
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AIDA.Server.dll"]
