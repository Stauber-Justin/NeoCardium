using Microsoft.Data.Sqlite;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using NeoCardium.Models;
using NeoCardium.Helpers;

namespace NeoCardium.Database
{
    /// <summary>
    /// Provides CRUD operations for Categories, Flashcards, and FlashcardAnswers.
    /// </summary>
    public class DatabaseHelper
    {
        private readonly Database _database;

        public DatabaseHelper(Database database)
        {
            _database = database;
        }

        // DRY up instance creation with a singleton.
        public static DatabaseHelper Instance { get; } = new DatabaseHelper(new Database());

        #region Category Operations

        public ObservableCollection<Category> GetCategories()
        {
            var categories = new ObservableCollection<Category>();
            try
            {
                using var db = _database.GetConnection();
                db.Open();
                string query = "SELECT * FROM Categories";
                using var command = new SqliteCommand(query, db);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    categories.Add(new Category { Id = reader.GetInt32(0), CategoryName = reader.GetString(1) });
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Abrufen der Kategorien.", ex);
            }
            return categories;
        }

        public bool AddCategory(string categoryName)
        {
            try
            {
                using var db = _database.GetConnection();
                db.Open();
                string insertQuery = "INSERT INTO Categories (CategoryName) VALUES (@CategoryName)";
                using var command = new SqliteCommand(insertQuery, db);
                command.Parameters.AddWithValue("@CategoryName", categoryName);
                int affectedRows = command.ExecuteNonQuery();
                return affectedRows > 0;
            }
            catch (SqliteException sqlEx) when (sqlEx.SqliteErrorCode == 19)
            {
                ExceptionHelper.LogError($"[WARNUNG] Kategorie existiert bereits: {categoryName}");
                return false;
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Erstellen der Kategorie.", ex);
                return false;
            }
        }

        public bool DeleteCategory(int categoryId)
        {
            try
            {
                using var db = _database.GetConnection();
                db.Open();
                string deleteQuery = "DELETE FROM Categories WHERE Id = @CategoryId";
                using var command = new SqliteCommand(deleteQuery, db);
                command.Parameters.AddWithValue("@CategoryId", categoryId);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Löschen der Kategorie.", ex);
                return false;
            }
        }

        public bool UpdateCategory(int categoryId, string newCategoryName)
        {
            try
            {
                using var db = _database.GetConnection();
                db.Open();
                string updateQuery = "UPDATE Categories SET CategoryName = @NewName WHERE Id = @CategoryId";
                using var command = new SqliteCommand(updateQuery, db);
                command.Parameters.AddWithValue("@NewName", newCategoryName);
                command.Parameters.AddWithValue("@CategoryId", categoryId);
                int affectedRows = command.ExecuteNonQuery();
                Debug.WriteLine($"UpdateCategory: Betroffene Zeilen = {affectedRows}");
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Aktualisieren der Kategorie.", ex);
                return false;
            }
        }

        public bool CategoryExists(string categoryName)
        {
            try
            {
                using var db = _database.GetConnection();
                db.Open();
                const string query = "SELECT 1 FROM Categories WHERE CategoryName = @CategoryName LIMIT 1";
                using var command = new SqliteCommand(query, db);
                command.Parameters.AddWithValue("@CategoryName", categoryName);
                return command.ExecuteScalar() != null;
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler in CategoryExists().", ex);
                return false;
            }
        }

        #endregion

        #region Flashcard Operations

        // Check if a flashcard with the same question (case-insensitive) exists in the given category.
        public bool FlashcardExists(int categoryId, string question)
        {
            try
            {
                using var db = _database.GetConnection();
                db.Open();
                const string query = "SELECT 1 FROM Flashcards WHERE CategoryId = @CategoryId AND UPPER(Question) = UPPER(@Question) LIMIT 1";
                using var command = new SqliteCommand(query, db);
                command.Parameters.AddWithValue("@CategoryId", categoryId);
                command.Parameters.AddWithValue("@Question", question);
                return command.ExecuteScalar() != null;
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler in FlashcardExists().", ex);
                return false;
            }
        }

        public ObservableCollection<Flashcard> GetFlashcardsByCategory(int categoryId)
        {
            var flashcards = new ObservableCollection<Flashcard>();
            try
            {
                using var db = _database.GetConnection();
                db.Open();
                const string query = "SELECT Id, CategoryId, Question, CorrectCount, IncorrectCount FROM Flashcards WHERE CategoryId = @CategoryId";
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
                        CorrectCount = reader.GetInt32(3),
                        IncorrectCount = reader.GetInt32(4)
                    });
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Laden der Karteikarten.", ex);
            }
            return flashcards;
        }

        public string GetCorrectAnswerForFlashcard(int flashcardId)
        {
            try
            {
                using var db = _database.GetConnection();
                db.Open();
                const string query = "SELECT AnswerText FROM FlashcardAnswers WHERE FlashcardId = @FlashcardId AND IsCorrect = 1 LIMIT 1";
                using var command = new SqliteCommand(query, db);
                command.Parameters.AddWithValue("@FlashcardId", flashcardId);
                using var reader = command.ExecuteReader();
                return reader.Read() ? reader.GetString(0) : "Keine Antwort gefunden";
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Abrufen der richtigen Antwort.", ex);
                return "Fehler beim Abrufen der Antwort";
            }
        }

        public List<FlashcardAnswer> GetAnswersByFlashcard(int flashcardId)
        {
            var answers = new List<FlashcardAnswer>();
            try
            {
                using var db = _database.GetConnection();
                db.Open();
                const string query = "SELECT AnswerId, FlashcardId, AnswerText, IsCorrect FROM FlashcardAnswers WHERE FlashcardId = @FlashcardId";
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
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Abrufen der Antworten.", ex);
            }
            return answers;
        }

        public List<FlashcardAnswer> GetRandomAnswersForFlashcard(int flashcardId)
        {
            try
            {
                var allAnswers = GetAnswersByFlashcard(flashcardId);
                if (allAnswers == null || allAnswers.Count == 0)
                    throw new Exception("Keine Antworten für diese Karteikarte gefunden.");

                var correctAnswers = allAnswers.Where(a => a.IsCorrect).ToList();
                var incorrectAnswers = allAnswers.Where(a => !a.IsCorrect).ToList();
                Random rnd = new();
                var selectedAnswers = new List<FlashcardAnswer>();
                if (correctAnswers.Any())
                    selectedAnswers.Add(correctAnswers[rnd.Next(correctAnswers.Count)]);
                else
                    throw new Exception("Keine richtige Antwort für diese Karteikarte vorhanden.");
                while (selectedAnswers.Count < 4 && incorrectAnswers.Any())
                {
                    FlashcardAnswer randomIncorrect = incorrectAnswers[rnd.Next(incorrectAnswers.Count)];
                    incorrectAnswers.Remove(randomIncorrect);
                    selectedAnswers.Add(randomIncorrect);
                }
                return selectedAnswers.OrderBy(_ => rnd.Next()).ToList();
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Generieren der Antworten.", ex);
                return new List<FlashcardAnswer>();
            }
        }

        public bool AddFlashcard(int categoryId, string question, List<FlashcardAnswer> answers, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                if (FlashcardExists(categoryId, question))
                {
                    errorMessage = "duplicate";
                    return false;
                }
                using var db = _database.GetConnection();
                db.Open();
                string insertFlashcardQuery = "INSERT INTO Flashcards (CategoryId, Question) VALUES (@CategoryId, @Question); SELECT last_insert_rowid();";
                using var flashcardCommand = new SqliteCommand(insertFlashcardQuery, db);
                flashcardCommand.Parameters.AddWithValue("@CategoryId", categoryId);
                flashcardCommand.Parameters.AddWithValue("@Question", question);
                int flashcardId = Convert.ToInt32(flashcardCommand.ExecuteScalar());
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
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "unknown";
                ExceptionHelper.LogError("Fehler beim Erstellen der Karteikarte.", ex);
                return false;
            }
        }

        public bool UpdateFlashcard(int flashcardId, string newQuestion, List<FlashcardAnswer> answers, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using var db = _database.GetConnection();
                db.Open();
                string updateQuery = "UPDATE Flashcards SET Question = @Question WHERE Id = @FlashcardId";
                using var updateCommand = new SqliteCommand(updateQuery, db);
                updateCommand.Parameters.AddWithValue("@Question", newQuestion);
                updateCommand.Parameters.AddWithValue("@FlashcardId", flashcardId);
                updateCommand.ExecuteNonQuery();
                string deleteAnswersQuery = "DELETE FROM FlashcardAnswers WHERE FlashcardId = @FlashcardId";
                using var deleteCommand = new SqliteCommand(deleteAnswersQuery, db);
                deleteCommand.Parameters.AddWithValue("@FlashcardId", flashcardId);
                deleteCommand.ExecuteNonQuery();
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
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "unknown";
                ExceptionHelper.LogError("Fehler beim Aktualisieren der Karteikarte.", ex);
                return false;
            }
        }

        public bool AddFlashcardAnswer(int flashcardId, string answerText, bool isCorrect)
        {
            using var db = _database.GetConnection();
            db.Open();
            string insertQuery = "INSERT INTO FlashcardAnswers (FlashcardId, AnswerText, IsCorrect) VALUES (@FlashcardId, @AnswerText, @IsCorrect)";
            using var command = new SqliteCommand(insertQuery, db);
            command.Parameters.AddWithValue("@FlashcardId", flashcardId);
            command.Parameters.AddWithValue("@AnswerText", answerText);
            command.Parameters.AddWithValue("@IsCorrect", isCorrect ? 1 : 0);
            command.ExecuteNonQuery();
            return true;
        }

        public bool DeleteFlashcard(int flashcardId)
        {
            try
            {
                using var db = _database.GetConnection();
                db.Open();
                string deleteQuery = "DELETE FROM Flashcards WHERE Id = @FlashcardId";
                using var command = new SqliteCommand(deleteQuery, db);
                command.Parameters.AddWithValue("@FlashcardId", flashcardId);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Löschen der Karteikarte.", ex);
                return false;
            }
        }

        public bool UpdateFlashcardStats(int flashcardId, bool isCorrect)
        {
            try
            {
                using var db = _database.GetConnection();
                db.Open();
                string columnToUpdate = isCorrect ? "CorrectCount" : "IncorrectCount";
                string query = $"UPDATE Flashcards SET {columnToUpdate} = {columnToUpdate} + 1 WHERE Id = @FlashcardId";
                using var command = new SqliteCommand(query, db);
                command.Parameters.AddWithValue("@FlashcardId", flashcardId);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Aktualisieren der Statistik.", ex);
                return false;
            }
        }

        #endregion
    }
}
