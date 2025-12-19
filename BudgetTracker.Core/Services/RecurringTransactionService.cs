// File: Services/RecurringTransactionService.cs
// Namespace: BudgetTracker.Console.Net8.Services
//
// Purpose:
// Business rules for recurring templates + generating normal Transactions.

using System;
using System.Collections.Generic;
using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Domain;
using BudgetTracker.Console.Net8.Domain.Enums;

namespace BudgetTracker.Console.Net8.Services
{
    /// <summary>
    /// Creates and runs recurring transaction templates.
    /// </summary>
    public class RecurringTransactionService
    {
        private readonly RecurringTransactionRepository _recurringRepo;
        private readonly BudgetService _budgetService;

        public RecurringTransactionService(RecurringTransactionRepository recurringRepo, BudgetService budgetService)
        {
            _recurringRepo = recurringRepo ?? throw new ArgumentNullException(nameof(recurringRepo));
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
        }

        public int CreateTemplate(
            string description,
            decimal amount,
            string type,
            string category,
            int? categoryId,
            DateTime startDate,
            RecurringFrequency frequency,
            int? dayOfMonth)
        {
            description = (description ?? string.Empty).Trim();
            category = (category ?? string.Empty).Trim();
            type = (type ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description is required.", nameof(description));

            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

            if (!type.Equals("Income", StringComparison.OrdinalIgnoreCase) &&
                !type.Equals("Expense", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Type must be 'Income' or 'Expense'.", nameof(type));

            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Category is required.", nameof(category));

            if (frequency == RecurringFrequency.Monthly)
            {
                // Keeping this constraint avoids month-length edge cases (29/30/31).
                if (!dayOfMonth.HasValue || dayOfMonth.Value < 1 || dayOfMonth.Value > 28)
                    throw new ArgumentException("Monthly templates require DayOfMonth between 1 and 28.", nameof(dayOfMonth));
            }
            else
            {
                dayOfMonth = null;
            }

            var nextRun = CalculateInitialNextRunDate(startDate.Date, frequency, dayOfMonth, DateTime.Today);

            var template = new RecurringTransaction
            {
                Description = description,
                Amount = amount,
                Type = NormalizeType(type),
                Category = category,
                CategoryId = categoryId,
                StartDate = startDate.Date,
                Frequency = frequency,
                DayOfMonth = dayOfMonth,
                NextRunDate = nextRun.Date,
                IsActive = true
            };

            return _recurringRepo.Add(template);
        }

        public List<RecurringTransaction> GetAllTemplates() => _recurringRepo.GetAll();

        public void DeactivateTemplate(int id) => _recurringRepo.Deactivate(id);

        /// <summary>
        /// Runs all templates due on or before 'asOfDate', generating normal Transactions.
        /// Returns the number of transactions created.
        /// </summary>
        public int RunDue(DateTime asOfDate)
        {
            var due = _recurringRepo.GetDue(asOfDate.Date);
            if (due.Count == 0) return 0;

            int created = 0;

            foreach (var t in due)
            {
                // Important: We generate the transaction using the template's NextRunDate
                // so back-to-back runs keep correct history.
                var transaction = new Transaction
                {
                    Description = t.Description,
                    Amount = t.Amount,
                    Type = t.Type,            // "Income" / "Expense"
                    Category = t.Category,
                    Date = t.NextRunDate.Date,
                    CategoryId = t.CategoryId
                };

                _budgetService.AddTransaction(transaction);
                created++;

                var nextRun = CalculateNextRunDate(t.NextRunDate.Date, t.Frequency, t.DayOfMonth);
                _recurringRepo.UpdateNextRunDate(t.Id, nextRun);
            }

            return created;
        }

        private static string NormalizeType(string type)
        {
            if (type.Equals("Income", StringComparison.OrdinalIgnoreCase)) return "Income";
            return "Expense";
        }

        private static DateTime CalculateInitialNextRunDate(
            DateTime startDate,
            RecurringFrequency frequency,
            int? dayOfMonth,
            DateTime today)
        {
            // If start is in the future, that's the next run date.
            if (startDate.Date >= today.Date)
                return startDate.Date;

            // Otherwise, advance until the next run is today or later.
            var next = startDate.Date;
            while (next.Date < today.Date)
            {
                next = CalculateNextRunDate(next, frequency, dayOfMonth);
            }

            return next.Date;
        }

        private static DateTime CalculateNextRunDate(DateTime fromDate, RecurringFrequency frequency, int? dayOfMonth)
        {
            return frequency switch
            {
                RecurringFrequency.Weekly => fromDate.AddDays(7),
                RecurringFrequency.BiWeekly => fromDate.AddDays(14),
                RecurringFrequency.Monthly => NextMonthly(fromDate, dayOfMonth),
                _ => fromDate.AddMonths(1)
            };
        }

        private static DateTime NextMonthly(DateTime fromDate, int? dayOfMonth)
        {
            var day = dayOfMonth ?? Math.Min(fromDate.Day, 28);
            day = Math.Clamp(day, 1, 28);

            var nextMonth = new DateTime(fromDate.Year, fromDate.Month, 1).AddMonths(1);
            return new DateTime(nextMonth.Year, nextMonth.Month, day);
        }
    }
}
