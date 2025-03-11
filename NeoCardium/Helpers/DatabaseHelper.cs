using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

using NeoCardium.Models;
using NeoCardium.Helpers;

namespace NeoCardium.Database
{
    public static class DatabaseHelper
    {
        // Prüft, ob ein Debugger angehängt ist
        private static readonly bool _isDebuggerAttached = Debugger.IsAttached;
        // Der Datenbankpfad wird je nach Debug-Status gesetzt
        private static readonly string _dbPath;
        // Wird nur im Release/ohne Debugger genutzt
        private static readonly string? _dataFolder;

        // Statischer Konstruktor: Initialisierung der Pfade je nach Debug-Status
        static DatabaseHelper()
        {
            if (_isDebuggerAttached)
            {
                // Kein definierter Pfad – die Datenbank wird im aktuellen Arbeitsverzeichnis erstellt (z. B. im bin/Debug-Ordner)
                _dbPath = "NeoCardium.db";
                _dataFolder = null;
                ExceptionHelper.LogError("[INFO] Debugger erkannt – verwende den Standard-Datenbankstandort.");
            }
            else
            {
                string appFolder = AppContext.BaseDirectory
                    ?? throw new InvalidOperationException("Konnte den Speicherort des Programms nicht bestimmen.");
                _dataFolder = Path.Combine(appFolder, "Data");
                if (!Directory.Exists(_dataFolder))
                {
                    Directory.CreateDirectory(_dataFolder);
                    ExceptionHelper.LogError($"[INFO] Data-Verzeichnis erstellt: {_dataFolder}");
                }
                _dbPath = Path.Combine(_dataFolder, "NeoCardium.db");
            }
        }

        private static SqliteConnection GetConnection()
        {
            return new SqliteConnection($"Data Source={_dbPath}");
        }

        public static void InitializeDatabase()
        {
            try
            {
                // Im Release-Modus sicherstellen, dass das Datenverzeichnis existiert
                if (!_isDebuggerAttached)
                {
                    EnsureDatabasePathExists();
                }

                bool databaseExists = File.Exists(_dbPath);
                using var db = GetConnection();
                db.Open();

                if (databaseExists)
                {
                    ExceptionHelper.LogError("[INFO] Datenbank existiert bereits. Keine Neuinitialisierung notwendig.");
                    return;
                }

                ExceptionHelper.LogError("[INFO] Erstelle neue Datenbank...");

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

                ExceptionHelper.LogError($"[SUCCESS] Datenbank erfolgreich initialisiert: {_dbPath}");
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler bei der Initialisierung der Datenbank.", ex);
            }
        }

        private static void EnsureDatabasePathExists()
        {
            // Falls im Debug-Modus kein spezifisches Verzeichnis verwendet wird, hier abbrechen
            if (_dataFolder == null)
            {
                return;
            }
            try
            {
                if (!Directory.Exists(_dataFolder))
                {
                    Directory.CreateDirectory(_dataFolder);
                    ExceptionHelper.LogError($"[INFO] Data-Verzeichnis erstellt: {_dataFolder}");
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Erstellen des Datenbankverzeichnisses.", ex);
                throw new InvalidOperationException("Fehler beim Erstellen des Datenbankverzeichnisses.", ex);
            }
        }

        public static ObservableCollection<Category> GetCategories()
        {
            ObservableCollection<Category> categories = new();
            try
            {
                using var db = GetConnection();
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

        public static ObservableCollection<Flashcard> GetFlashcardsByCategory(int categoryId)
        {
            ObservableCollection<Flashcard> flashcards = new();
            try
            {
                using var db = GetConnection();
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
            return flashcards; // Immer eine leere Liste zurückgeben, niemals `null`
        }

        public static string GetCorrectAnswerForFlashcard(int flashcardId)
        {
            try
            {
                using var db = GetConnection();
                db.Open();

                const string query = "SELECT AnswerText FROM FlashcardAnswers WHERE FlashcardId = @FlashcardId AND IsCorrect = 1 LIMIT 1";
                using var command = new SqliteCommand(query, db);
                command.Parameters.AddWithValue("@FlashcardId", flashcardId);
                using var reader = command.ExecuteReader();

                return reader.Read() ? reader.GetString(0) : "Keine Antwort gefunden"; // Kein `null`, sondern verständliche Ausgabe
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Abrufen der richtigen Antwort.", ex);
                return "Fehler beim Abrufen der Antwort";
            }
        }

        public static List<FlashcardAnswer> GetAnswersByFlashcard(int flashcardId)
        {
            List<FlashcardAnswer> answers = new();
            try
            {
                using var db = GetConnection();
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
            return answers; // Immer eine leere Liste zurückgeben, niemals `null`
        }

        public static List<FlashcardAnswer> GetRandomAnswersForFlashcard(int flashcardId)
        {
            try
            {
                List<FlashcardAnswer> allAnswers = GetAnswersByFlashcard(flashcardId);
                if (allAnswers == null || allAnswers.Count == 0)
                {
                    throw new Exception("Keine Antworten für diese Karteikarte gefunden.");
                }

                List<FlashcardAnswer> correctAnswers = allAnswers.Where(a => a.IsCorrect).ToList();
                List<FlashcardAnswer> incorrectAnswers = allAnswers.Where(a => !a.IsCorrect).ToList();

                Random rnd = new();
                List<FlashcardAnswer> selectedAnswers = new();

                // Mindestens eine richtige Antwort muss enthalten sein
                if (correctAnswers.Any())
                {
                    selectedAnswers.Add(correctAnswers[rnd.Next(correctAnswers.Count)]);
                }
                else
                {
                    throw new Exception("Keine richtige Antwort für diese Karteikarte vorhanden.");
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
            catch (Exception ex)
            {
                _ = ExceptionHelper.ShowErrorDialogAsync("Fehler beim Generieren der Antworten.", ex).ConfigureAwait(false);
                return new List<FlashcardAnswer>(); // Gibt eine leere Liste zurück, um Crashes zu vermeiden
            }
        }

        public static bool AddCategory(string categoryName)
        {
            try
            {
                using var db = GetConnection();
                db.Open();

                string insertQuery = "INSERT INTO Categories (CategoryName) VALUES (@CategoryName)";
                using var command = new SqliteCommand(insertQuery, db);
                command.Parameters.AddWithValue("@CategoryName", categoryName);

                int affectedRows = command.ExecuteNonQuery();
                return affectedRows > 0;
            }
            catch (SqliteException sqlEx) when (sqlEx.SqliteErrorCode == 19) // UNIQUE Constraint
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

        public static bool DeleteCategory(int categoryId)
        {
            try
            {
                using var db = GetConnection();
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

        public static bool UpdateCategory(int categoryId, string newCategoryName)
        {
            try
            {
                using var db = GetConnection();
                db.Open();

                string updateQuery = "UPDATE Categories SET CategoryName = @NewName WHERE Id = @CategoryId";
                using var command = new SqliteCommand(updateQuery, db);
                command.Parameters.AddWithValue("@NewName", newCategoryName);
                command.Parameters.AddWithValue("@CategoryId", categoryId);

                int affectedRows = command.ExecuteNonQuery();
                Debug.WriteLine($"UpdateCategory: Betroffene Zeilen = {affectedRows}");

                return affectedRows > 0; // Falls Zeilen aktualisiert wurden, true zurückgeben
            }
            catch (Exception ex)
            {
                _ = ExceptionHelper.ShowErrorDialogAsync("Fehler beim Aktualisieren der Kategorie.", ex).ConfigureAwait(false);
                return false;
            }
        }

        public static bool CategoryExists(string categoryName)
        {
            try
            {
                using var db = GetConnection();
                db.Open();

                const string query = "SELECT 1 FROM Categories WHERE CategoryName = @CategoryName LIMIT 1";
                using var command = new SqliteCommand(query, db);
                command.Parameters.AddWithValue("@CategoryName", categoryName);

                return command.ExecuteScalar() != null; // Direkt prüfen, ob eine Zeile existiert
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler in CategoryExists().", ex);
                return false; // Standardwert statt `null`
            }
        }

        public static bool AddFlashcard(int categoryId, string question, List<FlashcardAnswer> answers)
        {
            try
            {
                using var db = GetConnection();
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
                _ = ExceptionHelper.ShowErrorDialogAsync("Fehler beim Erstellen der Karteikarte.", ex).ConfigureAwait(false);
                return false;
            }
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

        public static bool DeleteFlashcard(int flashcardId)
        {
            try
            {
                using var db = GetConnection();
                db.Open();

                string deleteQuery = "DELETE FROM Flashcards WHERE Id = @FlashcardId";
                using var command = new SqliteCommand(deleteQuery, db);
                command.Parameters.AddWithValue("@FlashcardId", flashcardId);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                _ = ExceptionHelper.ShowErrorDialogAsync("Fehler beim Löschen der Karteikarte.", ex).ConfigureAwait(false);
                return false;
            }
        }

        public static bool UpdateFlashcard(int flashcardId, string newQuestion, List<FlashcardAnswer> answers)
        {
            try
            {
                using var db = GetConnection();
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
                _ = ExceptionHelper.ShowErrorDialogAsync("Fehler beim Aktualisieren der Karteikarte.", ex).ConfigureAwait(false);
                return false;
            }
        }

        public static bool UpdateFlashcardStats(int flashcardId, bool isCorrect)
        {
            try
            {
                using var db = GetConnection();
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
                _ = ExceptionHelper.ShowErrorDialogAsync("Fehler beim Aktualisieren der Statistik.", ex).ConfigureAwait(false);
                return false;
            }
        }
    }
}
