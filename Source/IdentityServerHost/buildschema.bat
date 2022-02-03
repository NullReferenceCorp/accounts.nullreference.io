rmdir /S /Q Migrations

dotnet ef migrations add Grants -c PersistedGrantDbContext -o Data/Migrations/PersistedGrantDb
dotnet ef migrations add Configuration -c ConfigurationDbContext -o Data/Migrations/ConfigurationDb
dotnet ef migrations add Application -c ApplicationDbContext -o Data/Migrations/ApplicationDb
dotnet ef migrations add DataProtection -c DataProtectionKeysDbContext -o Data/Migrations/DataProtectionKeysDb

dotnet ef migrations script -c PersistedGrantDbContext -o Data/Migrations/PersistedGrantDb.sql
dotnet ef migrations script -c ConfigurationDbContext -o Data/Migrations/ConfigurationDb.sql
dotnet ef migrations script -c ApplicationDbContext -o Data/Migrations/ApplicationDb.sql
dotnet ef migrations script -c DataProtectionKeysDbContext -o Data/Migrations/DataProtectionKeysDb.sql
