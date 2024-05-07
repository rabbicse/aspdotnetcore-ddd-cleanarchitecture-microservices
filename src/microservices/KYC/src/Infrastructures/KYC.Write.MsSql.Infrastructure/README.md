# RDBMS Write Infrastructure build instructions

## Install EntityFrameworkCore Cli Tools
```
dotnet tool install --global dotnet-ef
```

## Add migrations
```
dotnet ef migrations add InitialCreate --startup-project ..\..\Presentation\KYC.API\KYC.API.csproj
```

## Update schema to database
```
dotnet ef database update --startup-project ..\..\Presentation\KYC.API\KYC.API.csproj
```
