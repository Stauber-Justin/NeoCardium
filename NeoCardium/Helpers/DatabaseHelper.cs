using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;
using NeoCardium.Models;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

namespace NeoCardium.Database
{
    public static class DatabaseHelper
    {
        private static string _dbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "NeoCardium.db");

        public static void InitializeDatabase()
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            string createCategoriesTable = @"CREATE TABLE IF NOT EXISTS Categories (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    CategoryName TEXT NOT NULL UNIQUE)";

            string createFlashcardsTable = @"CREATE TABLE IF NOT EXISTS Flashcards (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    CategoryId INTEGER NOT NULL,
                                    Question TEXT NOT NULL,
                                    CorrectCount INTEGER DEFAULT 0,
                                    IncorrectCount INTEGER DEFAULT 0,
                                    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE CASCADE)";

            string createFlashcardAnswersTable = @"CREATE TABLE IF NOT EXISTS FlashcardAnswers (
                                    AnswerId INTEGER PRIMARY KEY AUTOINCREMENT,
                                    FlashcardId INTEGER NOT NULL,
                                    AnswerText TEXT NOT NULL,
                                    IsCorrect INTEGER NOT NULL CHECK (IsCorrect IN (0,1)),
                                    FOREIGN KEY (FlashcardId) REFERENCES Flashcards(Id) ON DELETE CASCADE)";

            using var categoryCommand = new SqliteCommand(createCategoriesTable, db);
            categoryCommand.ExecuteNonQuery();
            using var cardCommand = new SqliteCommand(createFlashcardsTable, db);
            cardCommand.ExecuteNonQuery();
            using var cardAnswerTable = new SqliteCommand(createFlashcardAnswersTable, db);
            cardAnswerTable.ExecuteNonQuery();
        }


        public static ObservableCollection<Category> GetCategories()
        {
            ObservableCollection<Category> categories = new();
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            string query = "SELECT * FROM Categories";
            using var command = new SqliteCommand(query, db);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                categories.Add(new Category { Id = reader.GetInt32(0), CategoryName = reader.GetString(1) });
            }

            return categories;
        }

        public static void AddCategory(string categoryName)
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            string insertQuery = "INSERT INTO Categories (CategoryName) VALUES (@CategoryName)";
            using var command = new SqliteCommand(insertQuery, db);
            command.Parameters.AddWithValue("@CategoryName", categoryName);
            command.ExecuteNonQuery();
        }

        public static void DeleteCategory(int categoryId)
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            string deleteQuery = "DELETE FROM Categories WHERE Id = @CategoryId";
            using var command = new SqliteCommand(deleteQuery, db);
            command.Parameters.AddWithValue("@CategoryId", categoryId);
            command.ExecuteNonQuery();
        }

        public static void UpdateCategory(int categoryId, string newCategoryName)
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            string updateQuery = "UPDATE Categories SET CategoryName = @NewName WHERE Id = @CategoryId";
            using var command = new SqliteCommand(updateQuery, db);
            command.Parameters.AddWithValue("@NewName", newCategoryName);
            command.Parameters.AddWithValue("@CategoryId", categoryId);
            command.ExecuteNonQuery();
        }

        public static int AddFlashcard(int categoryId, string question, List<FlashcardAnswer> answers)
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            // 1️⃣ Karteikarte in die Flashcards-Tabelle einfügen
            string insertFlashcardQuery = "INSERT INTO Flashcards (CategoryId, Question) VALUES (@CategoryId, @Question); SELECT last_insert_rowid();";
            using var flashcardCommand = new SqliteCommand(insertFlashcardQuery, db);
            flashcardCommand.Parameters.AddWithValue("@CategoryId", categoryId);
            flashcardCommand.Parameters.AddWithValue("@Question", question);
            int flashcardId = Convert.ToInt32(flashcardCommand.ExecuteScalar());

            // 2️⃣ Antworten zur Karteikarte speichern
            string insertAnswerQuery = "INSERT INTO FlashcardAnswers (FlashcardId, AnswerText, IsCorrect) VALUES (@FlashcardId, @AnswerText, @IsCorrect)";
            using var answerCommand = new SqliteCommand(insertAnswerQuery, db);

            foreach (var answer in answers)
            {
                answerCommand.Parameters.Clear();
                answerCommand.Parameters.AddWithValue("@FlashcardId", flashcardId);
                answerCommand.Parameters.AddWithValue("@AnswerText", answer.AnswerText);
                answerCommand.Parameters.AddWithValue("@IsCorrect", answer.IsCorrect ? 1 : 0);
                answerCommand.ExecuteNonQuery();
            }

            return flashcardId;
        }

        public static List<Flashcard> GetFlashcardsByCategory(int categoryId)
        {
            List<Flashcard> flashcards = new();
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            string query = "SELECT * FROM Flashcards WHERE CategoryId = @CategoryId";
            using var command = new SqliteCommand(query, db);
            command.Parameters.AddWithValue("@CategoryId", categoryId);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                flashcards.Add(new Flashcard
                {
                    Id = reader.GetInt32(0),
                    CategoryId = reader.GetInt32(1),
                    Question = reader.GetString(2),
                    Answer = reader.GetString(3),
                    CorrectCount = reader.GetInt32(4),
                    IncorrectCount = reader.GetInt32(5)
                });
            }

            return flashcards;
        }
        public static void AddFlashcardAnswer(int flashcardId, string answerText, bool isCorrect)
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            string insertQuery = "INSERT INTO FlashcardAnswers (FlashcardId, AnswerText, IsCorrect) VALUES (@FlashcardId, @AnswerText, @IsCorrect)";
            using var command = new SqliteCommand(insertQuery, db);
            command.Parameters.AddWithValue("@FlashcardId", flashcardId);
            command.Parameters.AddWithValue("@AnswerText", answerText);
            command.Parameters.AddWithValue("@IsCorrect", isCorrect ? 1 : 0);
            command.ExecuteNonQuery();
        }

        public static List<FlashcardAnswer> GetAnswersByFlashcard(int flashcardId)
        {
            List<FlashcardAnswer> answers = new();
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            string query = "SELECT * FROM FlashcardAnswers WHERE FlashcardId = @FlashcardId";
            using var command = new SqliteCommand(query, db);
            command.Parameters.AddWithValue("@FlashcardId", flashcardId);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                answers.Add(new FlashcardAnswer
                {
                    AnswerId = reader.GetInt32(0),
                    FlashcardId = reader.GetInt32(1),
                    AnswerText = reader.GetString(2),
                    IsCorrect = reader.GetInt32(3) == 1
                });
            }

            return answers;
        }
        public static List<FlashcardAnswer> GetRandomAnswersForFlashcard(int flashcardId)
        {
            List<FlashcardAnswer> allAnswers = GetAnswersByFlashcard(flashcardId);
            List<FlashcardAnswer> correctAnswers = allAnswers.Where(a => a.IsCorrect).ToList();
            List<FlashcardAnswer> incorrectAnswers = allAnswers.Where(a => !a.IsCorrect).ToList();

            Random rnd = new();
            List<FlashcardAnswer> selectedAnswers = new();

            // Mindestens eine richtige Antwort muss enthalten sein
            if (correctAnswers.Any())
            {
                selectedAnswers.Add(correctAnswers[rnd.Next(correctAnswers.Count)]);
            }

            // Restliche Plätze mit zufälligen Antworten auffüllen (bis 4 insgesamt)
            while (selectedAnswers.Count < 4 && incorrectAnswers.Any())
            {
                FlashcardAnswer randomIncorrect = incorrectAnswers[rnd.Next(incorrectAnswers.Count)];
                incorrectAnswers.Remove(randomIncorrect); // Verhindert Dopplungen
                selectedAnswers.Add(randomIncorrect);
            }

            return selectedAnswers.OrderBy(_ => rnd.Next()).ToList(); // Durchmischen
        }

        public static void DeleteFlashcard(int flashcardId)
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            string deleteQuery = "DELETE FROM Flashcards WHERE Id = @FlashcardId";
            using var command = new SqliteCommand(deleteQuery, db);
            command.Parameters.AddWithValue("@FlashcardId", flashcardId);
            command.ExecuteNonQuery();
        }

        public static void UpdateFlashcard(int flashcardId, string newQuestion, List<FlashcardAnswer> answers)
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            // Frage aktualisieren
            string updateQuery = "UPDATE Flashcards SET Question = @Question WHERE Id = @FlashcardId";
            using var updateCommand = new SqliteCommand(updateQuery, db);
            updateCommand.Parameters.AddWithValue("@Question", newQuestion);
            updateCommand.Parameters.AddWithValue("@FlashcardId", flashcardId);
            updateCommand.ExecuteNonQuery();

            // Alte Antworten löschen
            string deleteAnswersQuery = "DELETE FROM FlashcardAnswers WHERE FlashcardId = @FlashcardId";
            using var deleteCommand = new SqliteCommand(deleteAnswersQuery, db);
            deleteCommand.Parameters.AddWithValue("@FlashcardId", flashcardId);
            deleteCommand.ExecuteNonQuery();

            // Neue Antworten hinzufügen
            string insertAnswerQuery = "INSERT INTO FlashcardAnswers (FlashcardId, AnswerText, IsCorrect) VALUES (@FlashcardId, @AnswerText, @IsCorrect)";
            using var insertCommand = new SqliteCommand(insertAnswerQuery, db);

            foreach (var answer in answers)
            {
                insertCommand.Parameters.Clear();
                insertCommand.Parameters.AddWithValue("@FlashcardId", flashcardId);
                insertCommand.Parameters.AddWithValue("@AnswerText", answer.AnswerText);
                insertCommand.Parameters.AddWithValue("@IsCorrect", answer.IsCorrect ? 1 : 0);
                insertCommand.ExecuteNonQuery();
            }
        }
    }
}
