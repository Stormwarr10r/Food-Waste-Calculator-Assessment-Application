using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using assesment.Models;

namespace assesment.Services
{
    public class FoodWasteStore : IFoodWasteStore
    {
        //creates file for storing and stores data under the json file
        const string FileName = "foodwaste.json";
        readonly string _filePath;
        readonly SemaphoreSlim _lock = new(1, 1);
        private List<FoodWasteEntry> _entries = new();

        readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public FoodWasteStore()
        {
            _filePath = Path.Combine(FileSystem.AppDataDirectory, FileName);
        }

        public async Task<List<FoodWasteEntry>> GetEntriesAsync()
        {
            await EnsureLoadedAsync().ConfigureAwait(false);
            return new List<FoodWasteEntry>(_entries);
        }

        public async Task<List<FoodWasteEntry>> GetAllAsync()
        {
            return await GetEntriesAsync();
        }

        public async Task AddAsync(FoodWasteEntry entry)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                await EnsureLoadedAsync().ConfigureAwait(false);
                _entries.Add(entry);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task SaveAsync()
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                await WriteFileAsync().ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task ClearAsync()
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                _entries.Clear();
                await WriteFileAsync().ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task EnsureLoadedAsync()
        {
            if (_entries.Count > 0) return;

            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath).ConfigureAwait(false);
                _entries = JsonSerializer.Deserialize<List<FoodWasteEntry>>(json, _options) ?? new();
            }
        }

        private async Task WriteFileAsync()
        {
            var json = JsonSerializer.Serialize(_entries, _options);
            await File.WriteAllTextAsync(_filePath, json).ConfigureAwait(false);
        }
    }
}
