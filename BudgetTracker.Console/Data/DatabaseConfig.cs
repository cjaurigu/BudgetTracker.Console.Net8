using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BudgetTracker.Console.Data
{
    public static class DatabaseConfig
    {
        // Adjust server if you're not using LocalDB
        public static string ConnectionString { get; } =
            "Server=(localdb)\\MSSQLLocalDB;Database=BudgetTrackerDB;Trusted_Connection=True;TrustServerCertificate=True;";
    }
}
