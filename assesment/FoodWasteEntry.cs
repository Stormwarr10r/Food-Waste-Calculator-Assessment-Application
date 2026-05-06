using System;

namespace assesment.Models
{
    public class FoodWasteEntry
    {
        //preliminaries for foodwastestoring and entries
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; } // Price/cost of wasted food
        public string Day { get; set; } = string.Empty; // Day of the week
        public DateTimeOffset Date { get; set; } = DateTimeOffset.Now;
        public string? Note { get; set; }
    }
}

