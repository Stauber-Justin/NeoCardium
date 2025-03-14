using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Diagnostics;
using NeoCardium.Helpers;

namespace NeoCardium.Database
{
    /// <summary>
    /// Encapsulates database connection details and initialization.
    /// </summary>
    public class Database
    {
        public string DbPath { get; }
        public string ConnectionString { get; }
        public bool IsDebuggerAttached { get; }

        public Database()
        {
            IsDebuggerAttached = Debugger.IsAttached;
            if (IsDebuggerAttached)
            {
                // When debugging, store the database in the working directory.
                DbPath = "NeoCardium.db";
            }
            else
            {
                string appFolder = AppContext.BaseDirectory
                    ?? throw new InvalidOperationException("Konnte den Speicherort des Programms nicht bestimmen.");
                string dataFolder = Path.Combine(appFolder, "Data");
                if (!Directory.Exists(dataFolder))
                {
                    Directory.CreateDirectory(dataFolder);
                    ExceptionHelper.LogError($"[INFO] Data-Verzeichnis erstellt: {dataFolder}");
                }
                DbPath = Path.Combine(dataFolder, "NeoCardium.db");
            }
            ConnectionString = $"Data Source={DbPath}";
        }

        public SqliteConnection GetConnection()
        {
            return new SqliteConnection(ConnectionString);
        }

        /// <summary>
        /// Initializes the database by creating required tables if the database file does not exist.
        /// </summary>
        public void Initialize()
        {
            try
            {
                bool exists = File.Exists(DbPath);
                using var db = GetConnection();
                db.Open();
                if (exists)
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

                using var cmd1 = new SqliteCommand(createCategoriesTable, db);
                cmd1.ExecuteNonQuery();
                using var cmd2 = new SqliteCommand(createFlashcardsTable, db);
                cmd2.ExecuteNonQuery();
                using var cmd3 = new SqliteCommand(createFlashcardAnswersTable, db);
                cmd3.ExecuteNonQuery();

                ExceptionHelper.LogError($"[SUCCESS] Datenbank erfolgreich initialisiert: {DbPath}");
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler bei der Initialisierung der Datenbank.", ex);
            }
        }
    }
}
