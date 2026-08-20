# Library Management System API

A simple CRUD **ASP.NET Core Web API** for managing a library's books, members, and loans (borrow/return records).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) (or later)

Check with:

```bash
dotnet --version
```

## Running it locally

```bash
# 1. Clone the repo
git clone https://github.com/<your-username>/<your-repo-name>.git
cd <your-repo-name>

# 2. Restore & run
dotnet restore
dotnet run --project src/LibraryManagementSystem.Api
```

The first run automatically creates `library.db` and seeds it with a few sample books and members. Then open:

- **Frontend:** http://localhost:5119/ — manage books, members, and loans from the browser
