# MGT : Customer Requirement

Internal web application for managing ECC6 customer requirement data.

## Stack

- Frontend: React + Vite + TypeScript
- Backend: .NET 8 Minimal API
- Database: SQL Server, database `MGT_Datawarehouse`, table `dbo.Ms_CustomerRequirement`

## Features

- `Display Customer Requirement` main screen: display all requirement records, select a record, manage it in a popup, inactive/reactivate, delete, and print the selected document.
- Search by `CustomerCode_ECC6`, `CustomerName_ECC6`, or `CustomerRequirement`
- Display list uses mapped `CustomerCode` and `CustomerName` from `Setting_MappingData`, `Ms_BusinessPartner`, and `Setting_Mapping_Customer`
- Filter all, active, and inactive records
- Add customer requirement
- View and edit customer requirement in a popup dialog
- Mark record inactive or active
- Delete record
- Print selected customer requirement document
- API loading, empty, and error states

## Required SQL Table

Run `database/Ms_CustomerRequirement.sql` if the table or new `CustomerName_ECC6` column does not exist.

## Backend Setup

.NET 8 SDK is required. This machine was validated with .NET SDK `8.0.422`.

Set the connection string before running:

```powershell
$env:ConnectionStrings__MgtDatawarehouse = "Server=203.188.231.177;Database=MGT_Datawarehouse;User Id=sa;Password=<password>;TrustServerCertificate=True;Encrypt=False"
dotnet run --project .\backend\Mgt.CustomerRequirement.Api.csproj
```

The API runs at:

```text
http://localhost:5168
```

## Frontend Setup

```powershell
cd .\frontend
npm install
copy .env.example .env
npm run dev
```

The frontend runs at:

```text
http://127.0.0.1:5173
```

## API Endpoints

- `GET /api/health`
- `GET /api/customer-requirements?search=&status=all&page=1&pageSize=50`
- `GET /api/customer-requirements/{customerCode}`
- `POST /api/customer-requirements`
- `PUT /api/customer-requirements/{customerCode}`
- `PATCH /api/customer-requirements/{customerCode}/inactive`
- `PATCH /api/customer-requirements/{customerCode}/active`
- `DELETE /api/customer-requirements/{customerCode}`

## Notes

- Do not commit real database passwords into `appsettings.json`.
- `CrateedBy` keeps the current table spelling for compatibility with the existing database.
- `CustomerCode_ECC6` is handled as a trimmed code in the API because the current table uses `nchar(10)`.
- The list endpoint keeps `CustomerCode_ECC6` as the edit/delete key and returns `CustomerCode` / `CustomerName` for display.
