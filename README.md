# Autistic Bank API
*Done by: IM-41 Boiko Danylo*

## Prerequisites
- .NET 9 SDK
- PostgreSQL 15+

## Installation

Clone the repository:
```bash
git clone https://github.com/YOUR_USERNAME/bank-architecture.git
cd bank-architecture
```

## Configuration

Open `MyBank.Api/appsettings.json` and set your PostgreSQL credentials:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=autistic_bank_lab1;Username=postgres;Password=YOUR_PASSWORD"
}
```

## Running

```bash
cd MyBank.Api
dotnet run
```

Database migration applies automatically on startup.

## Testing

```bash
cd MyBank.Tests
dotnet test
```

## Endpoints

| Method | URL | Auth | Description |
|--------|-----|------|-------------|
| POST | /api/auth/register | — | Register new user |
| POST | /api/auth/login | — | Login and get JWT token |
| POST | /api/accounts | JWT | Create bank account |
| GET | /api/accounts | JWT | Get my accounts |
| POST | /api/accounts/transfer | JWT | Transfer funds |
| POST | /api/accounts/{id}/deposit | JWT | Deposit funds |