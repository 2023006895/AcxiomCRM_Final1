# AcxiomCRM

Role-based CRM built with ASP.NET Core 8 MVC, Identity, EF Core (SQL Server) and Chart.js.

## Run
1. Install the .NET 8 SDK and SQL Server LocalDB (comes with Visual Studio).
2. `dotnet restore`
3. `dotnet run` and open the URL printed in the console.
   The database is created and seeded automatically on first run.

## Test logins
| Role | Email | Password |
|---|---|---|
| Admin | admin@acxiomcrm.com | Admin@123 |
| Manager | manager@acxiomcrm.com | Manager@123 |
| SalesExecutive | sales@acxiomcrm.com | Sales@123 |

## Implemented so far
Identity login/register/logout, password policy, lockout, role seeding, anti-forgery, audit log service,
Customer CRUD with client + server validation, search/filter, role-scoped data, dashboard with Chart.js.

## Next modules
Leads, Opportunities, Follow-Ups, Activities, user/role admin, audit log viewer, REST API, reports.
