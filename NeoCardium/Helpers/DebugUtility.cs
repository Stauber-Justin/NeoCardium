using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NeoCardium.Models;
using NeoCardium.Database;

namespace NeoCardium.Helpers
{
    public static class DebugUtility
    {
        public static void InitializeDebugData()
        {
            if (!Debugger.IsAttached)
                return; // Ensure this only runs when debugging

            var db = DatabaseHelper.Instance; // Use the singleton instance

            try
            {
                Console.WriteLine("[DEBUG] Initializing Debug Categories...");

                // Insert AnswerButtonTest Category
                int answerButtonTestId = EnsureCategoryExists(db, "AnswerButtonTest");
                InsertAnswerButtonTestData(db, answerButtonTestId);

                // Insert MassFlashcardTest Category
                int massFlashcardTestId = EnsureCategoryExists(db, "MassFlashcardTest");
                InsertMassFlashcardTestData(db, massFlashcardTestId);

                Console.WriteLine("[DEBUG] Debug data initialized successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] DebugUtility failed: {ex.Message}");
            }
        }

        private static int EnsureCategoryExists(DatabaseHelper db, string categoryName)
        {
            var categories = db.GetCategories();
            var existingCategory = categories.FirstOrDefault(c => c.CategoryName == categoryName);

            if (existingCategory != null)
                return existingCategory.Id; // Return existing category ID to avoid duplicates

            // Create the category if it doesn't exist
            db.AddCategory(categoryName);
            categories = db.GetCategories();
            return categories.First(c => c.CategoryName == categoryName).Id;
        }

        private static void InsertAnswerButtonTestData(DatabaseHelper db, int categoryId)
        {
            for (int i = 1; i <= 8; i++)
            {
                string questionText = $"Question {i}";
                if (db.FlashcardExists(categoryId, questionText))
                    continue; // Skip if already exists

                List<FlashcardAnswer> answers = new();
                for (int j = 1; j <= i; j++)
                {
                    answers.Add(new FlashcardAnswer
                    {
                        AnswerText = j.ToString(),
                        IsCorrect = j == i // Only the last answer is correct
                    });
                }

                string errorMessage=" "; // Placeholder variable for error message
                db.AddFlashcard(categoryId, questionText, answers, out errorMessage);
            }
        }

        private static void InsertMassFlashcardTestData(DatabaseHelper db, int categoryId)
        {
            for (int i = 1; i <= 255; i++)
            {
                string questionText = $"Mass Question {i}";
                if (db.FlashcardExists(categoryId, questionText))
                    continue; // Skip if already exists

                List<FlashcardAnswer> answers = new();

                if (i == 1)
                {
                    // First question has 255 answers
                    for (int j = 1; j <= 255; j++)
                    {
                        answers.Add(new FlashcardAnswer
                        {
                            AnswerText = $"Option {j}",
                            IsCorrect = j % 50 == 0 // Some correct answers every 50
                        });
                    }
                }
                else
                {
                    // Other questions have only one correct answer
                    answers.Add(new FlashcardAnswer
                    {
                        AnswerText = "Correct Answer",
                        IsCorrect = true
                    });
                }

                string errorMessage=" "; // Placeholder variable for error message
                db.AddFlashcard(categoryId, questionText, answers, out errorMessage);
            }
        }
    }
}
