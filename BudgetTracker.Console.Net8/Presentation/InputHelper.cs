using System;
using BudgetTracker.Console.Net8.Domain.Enums;
using SystemConsole = System.Console;

namespace BudgetTracker.Console.Net8.Presentation
{
    /// <summary>
    /// InputHelper centralizes all the logic for reading
    /// and validating user input from the console.
    /// 
    /// Benefits:
    /// - UI code (MainMenu) stays clean
    /// - Validation logic is reusable and testable
    /// - Easier to handle bad input in one place
    /// </summary>
    public static class InputHelper
    {
        /// <summary>
        /// Reads a non-empty string from the user.
        /// This is used for things like descriptions and category names.
        /// </summary>
        /// <param name="prompt">Prompt text to show before reading input.</param>
        /// <returns>A trimmed, non-empty string.</returns>
        public static string ReadString(string prompt)
        {
            while (true)
            {
                SystemConsole.Write(prompt);
                var input = SystemConsole.ReadLine() ?? string.Empty;

                // Trim whitespace so "   rent   " becomes "rent".
                input = input.Trim();

                if (!string.IsNullOrWhiteSpace(input))
                    return input;

                SystemConsole.WriteLine("Value is required. Please try again.");
            }
        }

        /// <summary>
        /// Reads a decimal value (e.g., transaction amount)
        /// and keeps prompting until the user enters a valid number.
        /// </summary>
        public static decimal ReadDecimal(string prompt)
        {
            while (true)
            {
                SystemConsole.Write(prompt);
                var input = SystemConsole.ReadLine();

                if (decimal.TryParse(input, out var value))
                    return value;

                SystemConsole.WriteLine("Invalid number. Please try again.");
            }
        }

        /// <summary>
        /// Reads the transaction type (Income or Expense) in a safe way,
        /// using a custom prompt supplied by the caller.
        /// 
        /// Accepts:
        ///   - "1" or "Income"  -> TransactionType.Income
        ///   - "2" or "Expense" -> TransactionType.Expense
        /// 
        /// Keeps asking until the user enters a valid value.
        /// </summary>
        /// <param name="prompt">Prompt text, for example:
        /// "Type (1 = Income, 2 = Expense): "</param>
        public static TransactionType ReadTransactionType(string prompt)
        {
            while (true)
            {
                SystemConsole.Write(prompt);
                var input = (SystemConsole.ReadLine() ?? string.Empty).Trim();

                // Allow both numeric and word-based input for convenience.
                if (input.Equals("1") ||
                    input.Equals("Income", StringComparison.OrdinalIgnoreCase))
                {
                    return TransactionType.Income;
                }

                if (input.Equals("2") ||
                    input.Equals("Expense", StringComparison.OrdinalIgnoreCase))
                {
                    return TransactionType.Expense;
                }

                SystemConsole.WriteLine(
                    "Invalid type. Please enter 1 for Income, 2 for Expense, " +
                    "or type 'Income' / 'Expense'.");
            }
        }

        /// <summary>
        /// Original overload kept for backwards compatibility.
        /// Uses a default, generic prompt.
        /// </summary>
        public static TransactionType ReadTransactionType()
        {
            // Delegate to the new overload with a default prompt.
            return ReadTransactionType("Type (Income/Expense): ");
        }

        /// <summary>
        /// Reads a date from the user in a flexible format.
        /// If the user presses Enter, this will default to today's date.
        /// </summary>
        public static DateTime ReadDate(string prompt)
        {
            while (true)
            {
                SystemConsole.Write(prompt);
                var input = SystemConsole.ReadLine();

                // Blank -> use today's date.
                if (string.IsNullOrWhiteSpace(input))
                    return DateTime.Today;

                if (DateTime.TryParse(input, out var date))
                    return date;

                SystemConsole.WriteLine("Invalid date. Please try again.");
            }
        }

        /// <summary>
        /// Reads an integer within a specified inclusive range.
        /// For example: menu choices (1-10), year (2000-2100), etc.
        /// </summary>
        public static int ReadInt(string prompt, int min, int max)
        {
            while (true)
            {
                SystemConsole.Write(prompt);
                var input = SystemConsole.ReadLine();

                if (int.TryParse(input, out var value) && value >= min && value <= max)
                    return value;

                SystemConsole.WriteLine($"Please enter a number between {min} and {max}.");
            }
        }
    }
}
