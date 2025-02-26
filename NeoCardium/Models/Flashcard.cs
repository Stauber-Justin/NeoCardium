namespace NeoCardium.Models
{
    public class Flashcard
    {
        public int Id { get; set; } // Primärschlüssel
        public int CategoryId { get; set; } // Fremdschlüssel zur Kategorie
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
        public int CorrectCount { get; set; } = 0;
        public int IncorrectCount { get; set; } = 0;
    }
}
