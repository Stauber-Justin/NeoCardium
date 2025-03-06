using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NeoCardium.Database
{
    public static class CsvImporter
    {
        // Pfad zur Datenbank im lokalen Anwendungsdatenverzeichnis
        private static readonly string _appFolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName)
            ?? throw new InvalidOperationException("Konnte den Speicherort des Programms nicht bestimmen.");

        private static readonly string _dataFolder = Path.Combine(_appFolder, "Data");
        private static readonly string _dbPath = Path.Combine(_dataFolder, "NeoCardium.db");

        public static void ImportCsv(string csvFilePath)
        {
            if (!File.Exists(csvFilePath))
            {
                Console.WriteLine($"[ERROR] CSV-Datei nicht gefunden: {csvFilePath}");
                return;
            }

            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            using var transaction = db.BeginTransaction();

            try
            {
                var lines = File.ReadAllLines(csvFilePath);
                if (lines.Length < 2)
                {
                    Console.WriteLine("[ERROR] CSV-Datei enthält keine Daten.");
                    return;
                }

                for (int i = 1; i < lines.Length; i++)
                {
                    var columns = lines[i].Split(',');
                    if (columns.Length < 10)
                    {
                        Console.WriteLine($"[WARNUNG] Ungültige Zeile (zu wenige Spalten): {lines[i]}");
                        continue;
                    }

                    string categoryName = columns[0].Trim();
                    string questionText = columns[1].Trim();
                    string[] answers = columns.Skip(2).Take(8).Select(a => a.Trim()).ToArray();
                    string[] correctAnswers = columns[9].Replace("\"", "").Split(',').Select(a => a.Trim()).ToArray();

                    // Kategorie-ID abrufen oder erstellen
                    int categoryId = GetOrCreateCategory(db, categoryName);

                    // Flashcard erstellen und ID abrufen
                    int flashcardId = InsertFlashcard(db, categoryId, questionText);

                    // Antworten einfügen
                    foreach (var answer in answers)
                    {
                        bool isCorrect = correctAnswers.Contains(answer);
                        InsertAnswer(db, flashcardId, answer, isCorrect);
                    }
                }

                transaction.Commit();
                Console.WriteLine("[SUCCESS] CSV-Import abgeschlossen.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"[ERROR] Fehler beim Import: {ex.Message}");
            }
        }

        private static int GetOrCreateCategory(SqliteConnection db, string categoryName)
        {
            string query = "SELECT Id FROM Categories WHERE CategoryName = @CategoryName";
            using var selectCmd = new SqliteCommand(query, db);
            selectCmd.Parameters.AddWithValue("@CategoryName", categoryName);

            var result = selectCmd.ExecuteScalar();
            if (result != null)
            {
                return Convert.ToInt32(result);
            }

            string insertQuery = "INSERT INTO Categories (CategoryName) VALUES (@CategoryName); SELECT last_insert_rowid();";
            using var insertCmd = new SqliteCommand(insertQuery, db);
            insertCmd.Parameters.AddWithValue("@CategoryName", categoryName);

            return Convert.ToInt32(insertCmd.ExecuteScalar());
        }

        private static int InsertFlashcard(SqliteConnection db, int categoryId, string questionText)
        {
            string query = "INSERT INTO Flashcards (CategoryId, Question) VALUES (@CategoryId, @Question); SELECT last_insert_rowid();";
            using var cmd = new SqliteCommand(query, db);
            cmd.Parameters.AddWithValue("@CategoryId", categoryId);
            cmd.Parameters.AddWithValue("@Question", questionText);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static void InsertAnswer(SqliteConnection db, int flashcardId, string answerText, bool isCorrect)
        {
            string query = "INSERT INTO FlashcardAnswers (FlashcardId, AnswerText, IsCorrect) VALUES (@FlashcardId, @AnswerText, @IsCorrect)";
            using var cmd = new SqliteCommand(query, db);
            cmd.Parameters.AddWithValue("@FlashcardId", flashcardId);
            cmd.Parameters.AddWithValue("@AnswerText", answerText);
            cmd.Parameters.AddWithValue("@IsCorrect", isCorrect ? 1 : 0);
            cmd.ExecuteNonQuery();
        }
    }
}