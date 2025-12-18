namespace BudgetTracker.Console.Net8.Data
{
    public static class DatabaseConfig
    {
        public static string ConnectionString { get; } =
            "Server=(localdb)\\MSSQLLocalDB;Database=BudgetTrackerDB;Trusted_Connection=True;TrustServerCertificate=True;";
    }
}
