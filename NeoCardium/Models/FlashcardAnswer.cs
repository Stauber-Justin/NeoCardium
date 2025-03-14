﻿namespace NeoCardium.Models
{
    public class FlashcardAnswer
    {
        public int AnswerId { get; set; } // Primärschlüssel für jede Antwort
        public int Index { get; set; } // Diese Eigenschaft wird für die Grid-Position genutzt
        public int FlashcardId { get; set; } // Fremdschlüssel zur Karteikarte
        public string AnswerText { get; set; } = ""; // Der Antwort-Text
        public bool IsCorrect { get; set; } // Gibt an, ob die Antwort richtig ist
    }
}