---
applyTo: "**/Data/**,**/Entities/**"
---

# EF Core Instructions

- All entities go in `src/ITSupportDesk.Core/Entities/`
- DbContext: `AppDbContext` in `src/ITSupportDesk.Core/Data/`
- Use SQLite (connection string in `appsettings.json`)
- Always create a migration after model changes:
  `dotnet ef migrations add <Name> --project src/ITSupportDesk.Core --startup-project src/ITSupportDesk.Web`
- Database auto-migrates at startup (`db.Database.Migrate()` in Program.cs)
- Use nullable reference types on entity properties
- Use file-scoped namespaces
- Seed data via `SeedData.InitializeAsync` pattern
