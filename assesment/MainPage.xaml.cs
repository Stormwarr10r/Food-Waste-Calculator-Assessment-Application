using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using SQLite;
using Microsoft.Maui.Controls;

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

        public MainPage()
        {
            InitializeComponent();

            // populate day picker
            pickerDay.ItemsSource = days;

            // bind the collection to the CollectionView
            cvWasteItems.ItemsSource = wasteItems;
            lblLetterGrade.Text = $"Total Wasted: ${totalFood:F2}";

            // create per-day sections on the main page (collapsed by default)
            foreach (var day in days)
            {
                var collection = new ObservableCollection<WasteItem>();
                dayCollections[day] = collection;

                var section = CreateDaySection(day, collection);
                slDaySections.Add(section);
            }
        }

        // Creates a collapsible section for a given day
        View CreateDaySection(string day, ObservableCollection<WasteItem> items)
        {
            // header label (wrapped in a Frame so it looks like the screenshot)
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

            // tap toggles visibility (use a tap gesture so header looks like a label)
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
            }
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
