using System;
using BudgetTracker.Console.Net8.Presentation;

namespace BudgetTracker.Console.Net8
{
    /// <summary>
    /// This is the entry point for your console application.
    /// 
    /// Responsibilities:
    /// - Start the application
    /// - Create the MainMenu
    /// - Hand off control to the menu loop
    /// 
    /// After this, MainMenu takes over and drives the whole app.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main is the first method that runs when your app starts.
        /// </summary>
        static void Main(string[] args)
        {
            // Create a MainMenu instance.
            // MainMenu wires up:
            // - Repositories (data)
            // - Services (business logic)
            // - Presentation flow (menu system)
            var menu = new MainMenu();

            // Start the menu loop.
            // This will keep running until the user chooses the Exit option.
            menu.Run();

            // When Run() returns, the user has chosen to exit the application.
            // You could add clean-up or logging here if needed in the future.
        }
    }
}
