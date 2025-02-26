using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;
using NeoCardium.Models;
using System.IO;

namespace NeoCardium.Database
{
    public static class DatabaseHelper
    {
        private static string _dbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "NeoCardium.db");

        public static void InitializeDatabase()
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            string tableCommand = @"CREATE TABLE IF NOT EXISTS Categories (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    CategoryName TEXT NOT NULL UNIQUE)";

            using var command = new SqliteCommand(tableCommand, db);
            command.ExecuteNonQuery();
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
    }
}
