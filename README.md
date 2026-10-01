# ASP.NET Core JWT Authentication API

An ASP.NET Core 9 Web API sample that uses ASP.NET Core Identity, SQL Server, JWT access tokens, and refresh tokens. Scalar API documentation is available in the Development environment.

## Requirements

- .NET 9 SDK
- SQL Server (local or remote instance)
- Entity Framework Core command-line tool (`dotnet-ef`)

## Getting started

1. Configure `ConnectionStrings:DefaultConnection` in `AspnetcoreJwtTest/appsettings.json` or `AspnetcoreJwtTest/appsettings.Development.json` for your SQL Server. Also set `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience`. The key must be at least 16 characters long.
2. Run these commands from the repository root:

   ```bash
   dotnet tool install --global dotnet-ef
   dotnet restore AspnetcoreJwtTest.sln
   dotnet ef database update --project AspnetcoreJwtTest/AspnetcoreJwtTest.csproj
   dotnet run --project AspnetcoreJwtTest/AspnetcoreJwtTest.csproj
   ```

   If `dotnet-ef` is already installed, skip the install command.
3. In Development, open the API documentation at `https://localhost:<port>/docs/scalar`. Find the port in the application output or `AspnetcoreJwtTest/Properties/launchSettings.json`.

## Configuration

You can also provide configuration through environment variables. Use `__` in place of `:` in .NET configuration keys.

```text
ConnectionStrings__DefaultConnection=<SQL Server connection string>
Jwt__Key=<secret key with at least 16 characters>
Jwt__Issuer=<token issuer>
Jwt__Audience=<token audience>
```

To send password reset emails, replace the SMTP server and sender credential placeholders in `AccountController`'s `SendResetEmail` method with your SMTP settings.

## API endpoints

Base path: `/api/Account`

| Method | Path | Description |
| --- | --- | --- |
| `POST` | `/register` | Create a user. Body: `{"userName":"user1","email":"user@example.com","password":"StrongPassword1!","role":"User"}`. The role defaults to `User` when omitted. |
| `POST` | `/login` | Sign in with an email and password to receive an access token and refresh token. Body: `{"email":"user@example.com","password":"StrongPassword1!"}` |
| `POST` | `/refresh` | Exchange an expired access token and refresh token for new tokens. Body: `{"accessToken":"<expired-access-token>","refreshToken":"<refresh-token>"}` |
| `POST` | `/forgot-password` | Request a password reset email. Body: `{"email":"user@example.com"}` |
| `POST` | `/reset-password` | Set a new password using a reset token. Body: `{"email":"user@example.com","token":"<reset-token>","newPassword":"NewStrongPassword1!"}` |
| `GET` | `/admin-only` | Requires a JWT belonging to a user with the `Admin` role. Header: `Authorization: Bearer <access-token>` |

Access tokens expire after 1 minute and refresh tokens expire after 3 minutes. Identity locks an account after 3 failed login attempts.

## Development admin account

On startup, the application creates the `Admin` and `User` roles and seeds a development admin account for `admin@admin.com`. Its current password, `Password@123`, is hard-coded in the source code. Use this account only for local development. Before using the application in a shared or production environment, remove or change the seed logic and credentials, and keep secrets out of source control.

## Project structure

- `AspnetcoreJwtTest/Controllers/` — API controllers and authentication endpoints
- `AspnetcoreJwtTest/Entities/` — Request models, Identity entities, and application entities
- `AspnetcoreJwtTest/Services/` — Token builder and email sender services
- `AspnetcoreJwtTest/Data/` — Entity Framework database context
- `AspnetcoreJwtTest/Migrations/` — Database schema migrations
