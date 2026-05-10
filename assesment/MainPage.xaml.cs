using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using SQLite;
using Microsoft.Maui.Controls;
using assesment.Services;
using assesment.Models;

namespace assesment
{
    public partial class MainPage : ContentPage
    {
        // sets up base values and collections for the items
        double totalFood = 0;
        readonly ObservableCollection<WasteItem> wasteItems = new();

        // per-day collections and UI references
        readonly Dictionary<string, ObservableCollection<WasteItem>> dayCollections = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, CollectionView> dayCollectionViews = new(StringComparer.OrdinalIgnoreCase);
        readonly string[] days = new[]
        {
            "Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday"
        };

        // Inject the food waste store for persistence
        private readonly IFoodWasteStore _foodWasteStore;

        public MainPage()
        {
            InitializeComponent();

            // Get the store from DI container
            _foodWasteStore = IPlatformApplication.Current!.Services.GetService<IFoodWasteStore>()
                ?? throw new InvalidOperationException("IFoodWasteStore not registered in DI container");

            LoadDataAsync();

            // adds day picker
            pickerDay.ItemsSource = days;

            // bind the collection to the CollectionView
            cvWasteItems.ItemsSource = wasteItems;
            lblLetterGrade.Text = $"Total Wasted: ${totalFood:F2}";

            // create per day sections on the main page 
            foreach (var day in days)
            {
                var collection = new ObservableCollection<WasteItem>();
                dayCollections[day] = collection;

                var section = CreateDaySection(day, collection);
                slDaySections.Add(section);
            }
        }

        // Load saved data from the store on app startup
        private async void LoadDataAsync()
        {
            try
            {
                var entries = await _foodWasteStore.GetAllAsync();
                if (entries != null && entries.Count > 0)
                {
                    foreach (var entry in entries)
                    {
                        var item = new WasteItem 
                        { 
                            Id = entry.Id,
                            Name = entry.Name, 
                            Price = entry.Price, 
                            Day = entry.Day 
                        };

                        if (!dayCollections.TryGetValue(entry.Day, out var collection))
                        {
                            collection = new ObservableCollection<WasteItem>();
                            dayCollections[entry.Day] = collection;
                        }

                        collection.Add(item);
                        totalFood += item.Price;
                    }

                    lblLetterGrade.Text = $"Total Wasted for the week: ${totalFood:F2}";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        // Creates a collapsible section for a given day
        View CreateDaySection(string day, ObservableCollection<WasteItem> items)
        {
            // header label
            var headerLabel = new Label
            {
                Text = $"{day} (0)",
                HorizontalOptions = LayoutOptions.Fill,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                TextColor = Colors.White,
                Padding = new Thickness(8)
            };

            // accessibility / test id
            headerLabel.AutomationId = $"DayHeaderLabel_{day}";
            AutomationProperties.SetName(headerLabel, day);

            // Use Frame instead of Border so CornerRadius is available
            var headerFrame = new Frame
            {
                BorderColor = Colors.LightGray,
                CornerRadius = 8,
                BackgroundColor = Colors.Transparent,
                Padding = 0,
                Content = headerLabel,
                HasShadow = false
            };
            headerFrame.AutomationId = $"DayHeader_{day}";

            var cv = new CollectionView
            {
                ItemsSource = items,
                SelectionMode = SelectionMode.None,
                IsVisible = false, // start collapsed
                ItemTemplate = new DataTemplate(() =>
                {
                    var swipeView = new SwipeView();

                    var deleteSwipe = new SwipeItem
                    {
                        Text = "Delete",
                        BackgroundColor = Colors.IndianRed
                    };
                    // use a lambda subscription to avoid CS8622 nullability mismatch
                    deleteSwipe.Invoked += (s, e) => OnDeleteItem(s, e);

                    var swipeItems = new SwipeItems { Mode = SwipeMode.Execute };
                    swipeItems.Add(deleteSwipe);
                    swipeView.RightItems = swipeItems;

                    var grid = new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = GridLength.Auto },
                            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                        },
                        Padding = 8
                    };

                    var nameLabel = new Label { VerticalOptions = LayoutOptions.Center };
                    nameLabel.SetBinding(Label.TextProperty, "Name");

                    var priceLabel = new Label { VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.End };
                    priceLabel.SetBinding(Label.TextProperty, new Binding("Price", stringFormat: "${0:F2}"));

                    grid.Children.Add(nameLabel);
                    Grid.SetColumn(nameLabel, 0);
                    Grid.SetRow(nameLabel, 0);

                    grid.Children.Add(priceLabel);
                    Grid.SetColumn(priceLabel, 1);
                    Grid.SetRow(priceLabel, 0);

                    // keep Border for the row container (no CornerRadius used here)
                    swipeView.Content = new Border { Content = grid, Padding = 0, Margin = new Thickness(6) };

                    return swipeView;
                }),
                HeightRequest = 160
            };

            // remember the CollectionView for updates/removal
            dayCollectionViews[day] = cv;

            // tap toggles visibility 
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => { cv.IsVisible = !cv.IsVisible; };
            headerFrame.GestureRecognizers.Add(tap);

            // update header text when collection changes
            items.CollectionChanged += (s, e) =>
            {
                headerLabel.Text = $"{day} ({items.Count})";
            };

            return new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    headerFrame,
                    cv
                }
            };
        }

        // Calculates the total amount wasted and updates the display, providing error handling for invalid input
        private void OnCalculate(object sender, EventArgs e)
        {
            string foodName = txtFoodName.Text?.Trim() ?? string.Empty;
            string priceText = txtWasteAmount.Text?.Trim() ?? string.Empty;
            string day = pickerDay.SelectedItem as string ?? string.Empty;

            // checks if the food name is empty or whitespace
            if (string.IsNullOrWhiteSpace(foodName))
            {
                lblWarning.Text = "Please enter the food name.";
                return;
            }
            // require a day selection
            if (string.IsNullOrWhiteSpace(day))
            {
                lblWarning.Text = "Please select a day.";
                return;
            }

            //checks if price is a valid number or if it's negative
            if (!double.TryParse(priceText, NumberStyles.Number, CultureInfo.CurrentCulture, out double wasteAmount))
            {
                lblWarning.Text = "Please enter a numeric amount.";
                return;
            }

            if (wasteAmount < 0)
            {
                lblWarning.Text = "Please enter a non-negative amount.";
                return;
            }

            // Check if price has more than 2 decimal places
            if (HasMoreThanTwoDecimalPlaces(priceText))
            {
                lblWarning.Text = "Price can only have up to 2 decimal places (e.g., 10.50).";
                return;
            }

            // valid: add item to collection, update total, clear inputs and warnings
            var item = new WasteItem { Name = foodName, Price = wasteAmount, Day = day };
            //wasteItems.Insert(0, item);

            if (!dayCollections.TryGetValue(day, out var collection))
            {
                collection = new ObservableCollection<WasteItem>();
                dayCollections[day] = collection;
            }
            collection.Insert(0, item);

            totalFood += wasteAmount;
            lblLetterGrade.Text = $"Total Wasted for the week: ${totalFood:F2}";

            lblWarning.Text = string.Empty;
            txtFoodName.Text = string.Empty;
            txtWasteAmount.Text = string.Empty;
            pickerDay.SelectedIndex = -1;
            txtFoodName.Focus();

            // Save to persistent storage
            SaveItemAsync(item);
        }

        // Save a single item to the store
        private async void SaveItemAsync(WasteItem item)
        {
            try
            {
                var entry = new FoodWasteEntry
                {
                    Id = item.Id,
                    Name = item.Name,
                    Price = item.Price,
                    Day = item.Day
                };
                await _foodWasteStore.AddAsync(entry);
                await _foodWasteStore.SaveAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving item: {ex.Message}");
            }
        }

        // Loop for deleting items from the collection and updating the total, handling potential issues with item removal
        private void OnDeleteItem(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipeItem && swipeItem.BindingContext is WasteItem item)
            {
                // Try to find the collection by the stored Day first
                if (!string.IsNullOrWhiteSpace(item.Day) && dayCollections.TryGetValue(item.Day, out var targetCollection) && targetCollection.Remove(item))
                {
                    // removed from day collection
                }
                else
                {
                    // fallback: search all day collections for the item
                    ObservableCollection<WasteItem>? found = null;
                    foreach (var coll in dayCollections.Values)
                    {
                        if (coll.Contains(item))
                        {
                            found = coll;
                            break;
                        }
                    }

                    if (found != null)
                    {
                        found.Remove(item);
                    }
                    else
                    {
                        // also attempt to remove from the general wasteItems collection (if used)
                        if (!wasteItems.Remove(item))
                        {
                            lblWarning.Text = "Could not remove item.";
                            return;
                        }
                    }
                }

                // Update total and UI
                totalFood -= item.Price;
                if (totalFood < 0) totalFood = 0;
                lblLetterGrade.Text = $"Total Wasted for the week: ${totalFood:F2}";
                lblWarning.Text = string.Empty;

                // Reload from store to ensure sync
                ReloadFromStoreAsync();

            }
        }

        // Reload data from store to ensure persistence
        private async void ReloadFromStoreAsync()
        {
            try
            {
                // First, rebuild the store from current UI state
                var allItems = new List<WasteItem>();
                foreach (var collection in dayCollections.Values)
                {
                    allItems.AddRange(collection);
                }

                // Clear the store and reload with current items
                await _foodWasteStore.ClearAsync();
                foreach (var item in allItems)
                {
                    var entry = new FoodWasteEntry
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Price = item.Price,
                        Day = item.Day
                    };
                    await _foodWasteStore.AddAsync(entry);
                }
                await _foodWasteStore.SaveAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reloading data: {ex.Message}");
            }
        }

        // Helper method to check if a price string has more than 2 decimal places
        private bool HasMoreThanTwoDecimalPlaces(string priceText)
        {
            if (string.IsNullOrWhiteSpace(priceText))
                return false;

            // Find the decimal point
            int decimalIndex = priceText.IndexOf('.');
            if (decimalIndex == -1)
                return false; // No decimal point, so 0 decimal places

            // Check how many digits are after the decimal point
            int decimalPlaces = priceText.Length - decimalIndex - 1;
            return decimalPlaces > 2;
        }

        // Reset all items and clear the total
        private async void OnReset(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Confirm Reset",
                "Are you sure you want to clear all items and reset the total wasted?",
                "Yes",
                "Cancel");
            
            if (!confirm) return;

            try
            {
                // Clear all day collections
                foreach (var collection in dayCollections.Values)
                {
                    collection.Clear();
                }
                wasteItems.Clear();
                // Reset total
                totalFood = 0;
                lblLetterGrade.Text = $"Total Wasted for the week: ${totalFood:F2}";
                lblWarning.Text = string.Empty;
                // Clear persistent storage
                await _foodWasteStore.ClearAsync();

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error resetting data: {ex.Message}");
                lblWarning.Text = "Error clearing data. Please try again.";
            }
        }


        // simple model for display
        [Table("WasteItem")]
        public class WasteItem
        {
            [PrimaryKey]
            public string Id { get; set; } = Guid.NewGuid().ToString();

            public string Name { get; set; } = string.Empty;
            public double Price { get; set; }

            // day for the item
            public string Day { get; set; } = string.Empty;
        }
    }
}
