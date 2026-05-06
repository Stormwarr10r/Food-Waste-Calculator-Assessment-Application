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
        Task<List<FoodWasteEntry>> GetAllAsync();
        Task AddAsync(FoodWasteEntry entry);
        Task RemoveAsync(string id);
        Task SaveAsync();
        Task ClearAsync();
    }
}

