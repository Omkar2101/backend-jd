# Backend JD API

## Project Overview
This project is a .NET 8.0 Web API backend for job description analysis. It provides RESTful endpoints to manage and analyze job descriptions. The backend uses MongoDB as its primary database and integrates with an external Python API service. It supports CORS for frontend applications running on common development ports.

## Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) (for containerized deployment)
- [MongoDB](https://www.mongodb.com/) (if running locally without Docker)
- Python API service running (if not using Docker Compose)

## Running Locally

1. Clone the repository and navigate to the `backend-jd/backend-jd-api` folder.
2. Configure your environment variables in a `.env` file at the root of `backend-jd` (see Environment Variables section).
3. Restore dependencies and build the project:
   ```bash
   dotnet restore
   dotnet build
   ```
4. Run the API:
   ```bash
   dotnet run
   ```
5. The API will be available at `https://localhost:5268` or `http://localhost:5268`.

## Running with Docker

This project includes a Dockerfile and a docker-compose.yml for easy containerized setup.

### Build and Run with Docker Compose

1. Create a `.env` file in the `backend-jd` root directory with the required environment variables (see Environment Variables section).
2. Run the following command to build and start the containers:
   ```bash
   docker-compose up --build
   ```
3. The API will be accessible at `http://localhost:5268`.

4. ensure that the appsettings.json will look like this for running the project locally using `dotnet run` command
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "backend_jd_api.Services": "Debug"
    }
  },
  "AllowedHosts": "*",
  "Database": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "JobAnalyzerDB",
    "CollectionName": "JobDescriptions"
  },
  "PythonApi": {
    "BaseUrl": "http://localhost:8000",
    "TimeoutSeconds": 300
  },
  "Files": {
    "MaxSizeMB": 10,
    "AllowedTypes": [".txt", ".pdf", ".doc", ".docx", ".png", ".jpg"]
  }
}
```

5. If you are using the docker and running the setup using the network make sure the file has correct variables like below
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "backend_jd_api.Services": "Debug"
    }
  },
  "AllowedHosts": "*",
  "Database": {
    "ConnectionString": "mongodb://mongodb:27017",
    "DatabaseName": "JobAnalyzerDB",
    "CollectionName": "JobDescriptions"
  },
  "PythonApi": {
    "BaseUrl": "http://python-llm:8000",
    "TimeoutSeconds": 300
  },
  "Files": {
    "MaxSizeMB": 10,
    "AllowedTypes": [".txt", ".pdf", ".doc", ".docx", ".png", ".jpg"]
  }
}
```


### Running Tests with Docker

To run tests inside a Docker container, you can create a test service or run the tests manually inside the container by executing:

```bash
docker exec -it <container_name> dotnet test backend-jd-api.Tests/backend-jd-api.Tests.csproj
```

Replace `<container_name>` with the running container's name or ID.

## Environment Variables

The application uses the following environment variables, which can be set in a `.env` file at the root of the `backend-jd` folder:

| Variable                 | Description                                  | Example                      |
|--------------------------|----------------------------------------------|------------------------------|
| `API_PORT`               | Port to expose the API service on localhost | `5268`                       |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment (Development/Production) | `Development`            |
| `MONGO_CONNECTION_STRING`| MongoDB connection string                    | `mongodb://localhost:27017`  |
| `MONGO_DATABASE_NAME`    | MongoDB database name                        | `JobAnalyzerDB`              |
| `MONGO_COLLECTION_NAME`  | MongoDB collection name                      | `JobDescriptions`            |
| `MONGO_PORT`             | MongoDB port exposed by Docker               | `27017`                      |
| `PYTHON_API_BASE_URL`    | Base URL for the external Python API service | `http://localhost:8000`      |
| `PYTHON_API_TIMEOUT`     | Timeout in seconds for Python API calls      | `30`                         |

## API Endpoints (JobController)

The API exposes the following endpoints under the route `/api/jobs`:

- `POST /api/jobs/upload`  
  Upload a file (txt, doc, docx, pdf, jpg, jpeg, png) and analyze it for bias. Requires `UserEmail` and file in form data.

- `POST /api/jobs/analyze`  
  Analyze raw text for bias. Requires JSON body with `Text`, `UserEmail`, and optional `JobTitle`.

- `GET /api/jobs/{id}`  
  Get a specific job analysis by its ID.

- `GET /api/jobs`  
  Get all job analyses with pagination support via query parameters `skip` and `limit`.

- `GET /api/jobs/user/{email}`  
  Get all job analyses submitted by a specific user email.

- `DELETE /api/jobs/{id}`  
  Delete a specific job analysis by its ID.

### Validation and Error Handling

The API includes enhanced validation rules for job description text, such as minimum length, repetitive characters, special character ratio, and presence of job-related keywords. Detailed error responses are provided for validation failures, service unavailability, timeouts, and unexpected errors.

## API Endpoints (FileController)

The API exposes the following endpoints under the route `/api/files`:

- `GET /api/files/{storedFileName}`  
  Download a file by its stored filename.

- `GET /api/files/{storedFileName}/view`  
  View a file inline in the browser (useful for images and PDFs).

## Project Structure

```
backend-jd/
├── backend-jd-api/
│   ├── Controllers/
│   │   ├── JobController.cs          # Handles job description analysis endpoints
│   │   ├── FileController.cs         # Handles file download and viewing endpoints
│   ├── Services/
│   │   ├── JobService.cs             # Business logic for job analysis and storage
│   │   ├── PythonService.cs          # Integration with external Python API for analysis
│   │   ├── IFileStorageService.cs   # Interface for file storage operations
│   │   ├── FileStorageService.cs    # Implementation of file storage (if applicable)
│   ├── Models/
│   │   ├── JobDescription.cs         # Data model for job descriptions
│   ├── Data/
│   │   ├── MongoDbContext.cs         # MongoDB context and data access
│   ├── Performance/
│   │   ├── load.js                   # Performance testing script
│   ├── uploads/                      # Directory for uploaded files
│   ├── Program.cs                   # Application entry point
│   ├── appsettings.json             # Configuration file
│   └── backend-jd-api.csproj        # Project file
├── backend-jd-api.Tests/
│   ├── Controllers/
│   │   ├── JobControllerTests.cs    # Unit tests for JobController
│   │   ├── FileControllerTests.cs   # Unit tests for FileController
│   ├── Services/
│   │   ├── JobServiceTests.cs       # Unit tests for JobService
│   │   ├── PythonServiceTests.cs    # Unit tests for PythonService
│   │   ├── FileStorageServiceTests.cs # Unit tests for FileStorageService
│   └── backend-jd-api.Tests.csproj  # Test project file
```

## File Upload Restrictions

- Supported file types for upload: `.txt`, `.doc`, `.docx`, `.pdf`, `.jpg`, `.jpeg`, `.png`.
- Maximum file size: 10MB.

## Running Tests

Unit and integration tests are located in the `backend-jd-api.Tests` project. Tests cover controllers and services, including validation, error handling, and integration with the external Python API.

To run tests locally:

```bash
cd backend-jd/backend-jd-api.Tests
dotnet test
```

## Contributing

Contributions are welcome! Please open issues or submit pull requests for improvements or bug fixes.
