namespace NeoCardium.Models
{
    public class FlashcardStat
    {
        public int Id { get; set; }
        public int FlashcardId { get; set; }
        public bool IsCorrect { get; set; }
        public DateTime AnswerDate { get; set; }
    }
}
