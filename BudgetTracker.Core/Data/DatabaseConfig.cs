// File: Data/DatabaseConfig.cs
// Namespace: BudgetTracker.Console.Net8.Data
//
// Purpose:
// Central place for the SQL Server connection string.

namespace BudgetTracker.Console.Net8.Data
{
    public static class DatabaseConfig
    {
        public static string ConnectionString { get; } =
            "Server=(localdb)\\MSSQLLocalDB;Database=BudgetTrackerDB;Trusted_Connection=True;TrustServerCertificate=True;";
    }
}
