using System;

namespace assesment.Models
{
    public class FoodWasteEntry
    {
        //preliminaries for foodwastestoring and entries
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public double Quantity { get; set; } // Quantity in kilograms
        public DateTimeOffset Date { get; set; } = DateTimeOffset.Now;
        public string? Note { get; set; }
    }
}

