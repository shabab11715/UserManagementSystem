# User Management System

ASP.NET Core MVC user management app using PostgreSQL and SendGrid Web API for email verification and password resets.

## Features

- Register with email and password
- Email verification required before login
- Resend verification email (rate-limited)
- Forgot password and reset password by email (rate-limited)
- Session-based login and logout
- Home page behavior:
  - Verified users see the user management table
  - Unverified users see a verification required message and a resend option
  - Blocked users see a blocked message
- User management table (for verified users):
  - Sorted by last login time (latest first)
  - Filtering and selection
  - Bulk actions: block, unblock, delete
  - Confirmation prompts and action feedback messages
- Database-level unique index on email

## Tech Stack

- ASP.NET Core MVC
- Entity Framework Core
- PostgreSQL (Npgsql)
- SendGrid Web API (HTTPS)

## Configuration

Use environment variables for deployment.

### Database

Set:

- `ConnectionStrings__Default`

Example:

```bash
ConnectionStrings__Default=Host=localhost;Port=5433;Database=task4_database;Username=postgres;Password=postgre_123
```

### SendGrid Web API

Set:

- `SendGrid__ApiKey`
- `SendGrid__FromEmail`
- `SendGrid__FromName`

Example:

```bash
SendGrid__ApiKey=SG_xxx
SendGrid__FromEmail=your_verified_sender@example.com
SendGrid__FromName=Task4
```

Important:
- `SendGrid__FromEmail` should be a verified sender in SendGrid (Single Sender Verification) or from an authenticated domain.

## Run locally

```bash
dotnet restore
dotnet run
```

Open the printed URL in your browser.

## Migrations

If the app applies migrations on startup, you do not need to do anything.

If you run migrations manually:

```bash
dotnet ef database update
```

## How to use

### Register and verify
1. Register a new account
2. Check email and click the verification link
3. Login after verification

### Login behavior
- Verified: access the main page with the user table
- Unverified: see verification required message and resend option
- Blocked: see blocked message

### Resend verification
- Use the Resend Verification page to request another verification email
- Requests are rate-limited

### Forgot password
- Use the Forgot Password page
- You will receive a reset link by email
- Reset links expire

### Manage users
- Filter users
- Select one or more users
- Use action buttons to block, unblock, or delete
- Buttons show tooltips, and disabled buttons indicate you must select at least one user

## Notes

- Email uniqueness is enforced at the database level by a unique index.
- Passwords are stored as hashes.