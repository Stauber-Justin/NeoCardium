namespace NeoCardium.Models
{
    public class Category
    {
        public int Id { get; set; } // Primärschlüssel für SQLite
        public string CategoryName { get; set; } = "";
    }
}