# Post-itter Backend

The Post-itter backend is a RESTful API developed in C# (.NET 8) that serves as the bridge between the frontend application and a MySQL database. It handles data retrieval, storage, modification, real-time communication via SignalR, and access control.

> **Personal project archive (2024).** Backend for [PostItter](https://github.com/anItalianGeek/PostItter),
> built for a three-month personal challenge after my internship in Vienna. Kept for
> reference, without active development or maintenance. It isn't production software.
>
> Authentication uses a signed bearer token carrying session data rather than a standard JWT.
> See the postmortem in the frontend README for the CORS issue and the temporary authorization bypass it caused.

## Project Structure

The repository contains a solution file at the root and the main application project in a subdirectory:

- `PostItter_RESTfulAPI.sln`: Solution file.
- `PostItter_RESTfulAPI/PostItter_RESTfulAPI.csproj`: Main ASP.NET Core project (`net8.0`).
- **Controllers**: Contains API controllers handling endpoints for users, posts, messages, notifications, and auth (`api/users`, `api/posts`, `api/authguard`, etc.).
- **Entity and Database Models**:
  - `Entity/`: Domain entity models and custom token representations (`JwtWebToken`).
  - `Entity/DatabaseModels/`: EF Core models mapped to database tables (e.g., `UserDto`, `PostDto`, `ActiveUsersDto`).
- **DatabaseContext**: `ApplicationDbContext` defining EF Core database sets and relational mappings.
- **Migrations**: EF Core code-first migrations (`20240830165210_Initial.cs`).
- **Hubs / WebSockets**: SignalR hub (`ChatHub.cs`) and WebSocket handling for real-time messaging.

## Prerequisites

- **.NET SDK**: Version 8.0 (`net8.0`)
- **MySQL / MariaDB**: Server instance running on localhost (default port 3306)

## Setup and Commands

1. **Clone the repository:**
   ```bash
   git clone https://github.com/anItalianGeek/PostItter_RESTfulAPI.git
   cd PostItter_RESTfulAPI
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore PostItter_RESTfulAPI/PostItter_RESTfulAPI.csproj
   ```

3. **Build the project:**
   ```bash
   dotnet build PostItter_RESTfulAPI/PostItter_RESTfulAPI.csproj
   ```

4. **Run the API:**
   ```bash
   dotnet run --project PostItter_RESTfulAPI/PostItter_RESTfulAPI.csproj
   ```
   *(Alternatively, navigate directly to `cd PostItter_RESTfulAPI` and run `dotnet run`.)*

### Runtime & Network Configuration

- **Kestrel Port**: The application configures Kestrel in `Program.cs` to listen on all network interfaces (`IPAddress.Any`) on port **5265** (`http://localhost:5265`).
- **Swagger Documentation**: In the `Development` environment, Swagger UI is available at `http://localhost:5265/swagger`.
- **CORS**: The default CORS policy is explicitly configured to allow origins from `http://localhost:4200` (the Angular frontend for PostItter) with credentials, any method, and any header.

## Database Configuration & Schema Limitations

- **Configuration File**: Connection string settings are located in `PostItter_RESTfulAPI/appsettings.json` under `ConnectionStrings:DefaultConnection`.
- **Requirements**: Requires a reachable MySQL server with an initialized database. Set your own `ConnectionStrings:DefaultConnection` in local ASP.NET Core configuration; the application reads it via `builder.Configuration.GetConnectionString("DefaultConnection")`. Do not reuse the credentials committed with the prototype.
- **Schema & Migrations**: An initial migration (`20240830165210_Initial`) is present in `PostItter_RESTfulAPI/Migrations/`, but the application does not automatically apply migrations or bootstrap the database at startup. Applying migrations via `dotnet ef database update` requires the external EF Core CLI tool, targeting the project file (`--project PostItter_RESTfulAPI`), and an active, accessible database. Because the default connection string specifies local credentials and comma-based port notation, executing migrations without a configured local MySQL instance is not supported out of the box.

## Authentication & Known Implementation Notes

- **Custom Bearer Token**: The application uses a custom session token (`JwtWebToken`) composed of Base64Url-encoded fields (`sub`, `displayname`, `username`, `iat`, `exp`, `server_signature`) stored in the `activeUsers` table. It does not follow the standard RFC 7519 JSON Web Token specification.
- **Temporary Authorization Bypass**: As documented in the frontend postmortem, authorization validation blocks in several controllers were temporarily bypassed during the prototype sprint due to CORS preflight and bearer header processing challenges.
- **Password Hashing**: Passwords are hashed using unsalted SHA-256 in `Hasher.cs`.
- **Storage Dependencies**: `BucketsController` and `FilesController` declare dependencies on `IAmazonS3`, but AWS service registration is not configured in `Program.cs`.

## License

The original README declares this project under the MIT License. A standalone `LICENSE` file is not currently tracked in the repository.
