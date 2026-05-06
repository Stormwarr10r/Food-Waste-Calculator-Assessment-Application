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

        //creates or finds a path
        public FoodWasteStore()
        {
            _filePath = Path.Combine(FileSystem.AppDataDirectory, FileName);
        }

        public async Task<List<FoodWasteEntry>> GetEntriesAsync()
        {
            await EnsureLoadedAsync().ConfigureAwait(false);
            return new List<FoodWasteEntry>(_entries);
        }

        //syncs path and returns entries
        public async Task<List<FoodWasteEntry>> GetAllAsync()
        {
            return await GetEntriesAsync();
        }

        //adds entry to the list and saves it to the file
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


        //removes entry from the list and saves it to the file
        public async Task RemoveAsync(string id)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                await EnsureLoadedAsync().ConfigureAwait(false);
                _entries.RemoveAll(e => e.Id == id);
            }
            finally
            {
                _lock.Release();
            }
        }

        //saves the current state of the entries to the file
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

        //clears all entries and saves the empty list to the file
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

        //loads entries from the file if they haven't been loaded yet
        private async Task EnsureLoadedAsync()
        {
            if (_entries.Count > 0) return;

            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath).ConfigureAwait(false);
                _entries = JsonSerializer.Deserialize<List<FoodWasteEntry>>(json, _options) ?? new();
            }
        }

        //writes the current entries to the file in JSON format
        private async Task WriteFileAsync()
        {
            var json = JsonSerializer.Serialize(_entries, _options);
            await File.WriteAllTextAsync(_filePath, json).ConfigureAwait(false);
        }
    }
}
