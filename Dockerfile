# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution and project files first to take advantage of layer caching
COPY ["backend-jd-api/backend-jd-api.csproj", "backend-jd-api/"]
COPY ["backend-jd-api.Tests/backend-jd-api.Tests.csproj", "backend-jd-api.Tests/"]

# Restore dependencies
RUN dotnet restore "backend-jd-api/backend-jd-api.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build "backend-jd-api/backend-jd-api.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "backend-jd-api/backend-jd-api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Set environment variables if run the image without docker compose
ENV ASPNETCORE_URLS=http://+:80
ENV Database__ConnectionString=mongodb://mongodb:27017
ENV Database__DatabaseName=JobAnalyzerDB
ENV Database__CollectionName=JobDescriptions

# Expose the port your application runs on
EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "backend-jd-api.dll"]