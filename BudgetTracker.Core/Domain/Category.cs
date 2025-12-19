namespace BudgetTracker.Console.Net8.Domain
{
    /// <summary>
    /// Represents a single category in your budget system.
    /// 
    /// Categories are used to group transactions logically:
    /// - "Food"
    /// - "Gas"
    /// - "Rent"
    /// - "Salary"
    /// 
    /// They live in the Categories table, and Transactions point to them
    /// via CategoryId (in the database) while still storing the name as text.
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Primary key for the category (IdENTITY column in SQL).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The display name of the category.
        /// Must be unique at the database level and non-empty.
        /// Example: "Food", "Gas", "Rent", "Utilities".
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
