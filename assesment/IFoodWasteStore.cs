using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using assesment.Models;

namespace assesment.Services
{
    public interface IFoodWasteStore
    {
        //preliminaries for foodwastestore.cs
        Task<List<FoodWasteEntry>> GetEntriesAsync();
        Task AddAsync(FoodWasteEntry entry);
        Task SaveAsync();
        Task ClearAsync();
    }
}
