﻿using Microsoft.Data.Sqlite;
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

                string insertStat = "INSERT INTO FlashcardStats (FlashcardId, IsCorrect, AnswerDate) VALUES (@FlashcardId, @IsCorrect, @AnswerDate)";
                using var statCmd = new SqliteCommand(insertStat, db);
                statCmd.Parameters.AddWithValue("@FlashcardId", flashcardId);
                statCmd.Parameters.AddWithValue("@IsCorrect", isCorrect ? 1 : 0);
                statCmd.Parameters.AddWithValue("@AnswerDate", DateTime.UtcNow.ToString("o"));
                statCmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Aktualisieren der Statistik.", ex);
                return false;
            }
        }

        public bool ResetStatistics()
        {
            try
            {
                using var db = _database.GetConnection();
                db.Open();
                using var cmd = new SqliteCommand("DELETE FROM FlashcardStats", db);
                cmd.ExecuteNonQuery();
                using var resetCmd = new SqliteCommand("UPDATE Flashcards SET CorrectCount = 0, IncorrectCount = 0", db);
                resetCmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Zurücksetzen der Statistik.", ex);
                return false;
            }
        }

        public List<DailyStat> GetDailyStatistics()
        {
            var stats = new List<DailyStat>();
            try
            {
                using var db = _database.GetConnection();
                db.Open();
                string query = "SELECT date(AnswerDate) as Day, SUM(case when IsCorrect=1 then 1 else 0 end), SUM(case when IsCorrect=0 then 1 else 0 end) FROM FlashcardStats GROUP BY Day ORDER BY Day";
                using var command = new SqliteCommand(query, db);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    stats.Add(new DailyStat
                    {
                        Date = DateTime.Parse(reader.GetString(0)),
                        Correct = reader.GetInt32(1),
                        Incorrect = reader.GetInt32(2)
                    });
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Abrufen der Statistik.", ex);
            }
            return stats;
        }

        public string ExportStatisticsCsv()
        {
            try
            {
                var stats = GetDailyStatistics();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Date,Correct,Incorrect");
                foreach (var s in stats)
                {
                    sb.AppendLine($"{s.Date:yyyy-MM-dd},{s.Correct},{s.Incorrect}");
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Exportieren der Statistik.", ex);
                return string.Empty;
            }
        }

        #endregion

        #region Gamification Operations

        private void EnsureGamificationTable(SqliteConnection db)
        {
            const string createTable = @"CREATE TABLE IF NOT EXISTS GamificationStats (
                                        Id INTEGER PRIMARY KEY CHECK (Id = 1),
                                        Points INTEGER NOT NULL DEFAULT 0,
                                        Streak INTEGER NOT NULL DEFAULT 0,
                                        Badges TEXT)";
            using var cmd = new SqliteCommand(createTable, db);
            cmd.ExecuteNonQuery();
        }

        public GamificationStats GetGamificationStats()
        {
            try
            {
                using var db = _database.GetConnection();
                db.Open();
                EnsureGamificationTable(db);

                const string query = "SELECT Points, Streak, Badges FROM GamificationStats WHERE Id = 1";
                using var cmd = new SqliteCommand(query, db);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var stats = new GamificationStats
                    {
                        Points = reader.GetInt32(0),
                        Streak = reader.GetInt32(1),
                        Badges = new ObservableCollection<string>((reader.IsDBNull(2) ? string.Empty : reader.GetString(2)).Split(',', StringSplitOptions.RemoveEmptyEntries))
                    };
                    return stats;
                }
                else
                {
                    var stats = new GamificationStats();
                    SaveGamificationStats(stats);
                    return stats;
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Laden der Gamification-Daten.", ex);
                return new GamificationStats();
            }
        }

        private void SaveGamificationStats(GamificationStats stats)
        {
            using var db = _database.GetConnection();
            db.Open();
            EnsureGamificationTable(db);

            const string query = "INSERT OR REPLACE INTO GamificationStats (Id, Points, Streak, Badges) VALUES (1, @Points, @Streak, @Badges)";
            using var cmd = new SqliteCommand(query, db);
            cmd.Parameters.AddWithValue("@Points", stats.Points);
            cmd.Parameters.AddWithValue("@Streak", stats.Streak);
            cmd.Parameters.AddWithValue("@Badges", string.Join(',', stats.Badges));
            cmd.ExecuteNonQuery();
        }

        public List<string> AddSessionResult(int correctAnswers, bool perfectSession)
        {
            var stats = GetGamificationStats();
            stats.Points += correctAnswers;
            stats.Streak = perfectSession ? stats.Streak + 1 : 0;

            var newBadges = new List<string>();
            if (stats.Points >= 50 && !stats.Badges.Contains("Rookie"))
            {
                stats.Badges.Add("Rookie");
                newBadges.Add("Rookie");
            }
            if (stats.Points >= 200 && !stats.Badges.Contains("Pro"))
            {
                stats.Badges.Add("Pro");
                newBadges.Add("Pro");
            }

            SaveGamificationStats(stats);
            return newBadges;
        }

        #endregion
    }
}
