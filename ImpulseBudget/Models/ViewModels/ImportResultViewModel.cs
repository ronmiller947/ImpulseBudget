using System.Collections.Generic;

namespace ImpulseBudget.Models
{
    public class ImportResultViewModel
    {
        public List<string> Errors { get; set; } = new();
        public string? SuccessMessage { get; set; }

        public List<RecurringSuggestion>? RecurringSuggestions { get; set; }
    }
}
