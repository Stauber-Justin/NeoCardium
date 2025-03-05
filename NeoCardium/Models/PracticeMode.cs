using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeoCardium.Models
{
    public enum PracticeMode
    {
        MultipleChoice,
        Flashcard
    }
    public class PracticeModeOption
    {
        public PracticeMode Mode { get; set; }
        public string ModeName { get; set; } = "";
    }

}
