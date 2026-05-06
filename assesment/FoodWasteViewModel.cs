using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using assesment.Models;
using assesment.Services;
using System.Text;

namespace assesment.ViewModels
{
    public class FoodWasteViewModel
    {
        //creates a foodwaste viewing model
        readonly IFoodWasteStore _store;
        public ObservableCollection<FoodWasteEntry> Entries { get; } = new();

        public FoodWasteViewModel(IFoodWasteStore store)
        {
            _store = store;
        }

        public async Task LoadAsync()
        {
            var items = await _store.GetEntriesAsync();
            Entries.Clear();
            foreach (var I in items) Entries.Add(I);
        }

        public async Task AddAsync(string name, double price, string day, string? notes = null)
        {
            var entry = new FoodWasteEntry { Name = name, Price = price, Day = day, Note = notes };
            await _store.AddAsync(entry);
            await _store.SaveAsync();
        }
    }
}
