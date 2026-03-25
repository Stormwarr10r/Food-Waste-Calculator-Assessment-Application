using System;
using System.Collections.ObjectModel;
using System.Globalization;
using SQLite;

namespace assesment
{
    public partial class MainPage : ContentPage
    {
        // sets up base values and collections for the items
        double totalFood = 0;
        readonly ObservableCollection<WasteItem> wasteItems = new();

        public MainPage()
        {
            InitializeComponent();

            // bind the collection to the CollectionView
            cvWasteItems.ItemsSource = wasteItems;
            lblLetterGrade.Text = $"Total Wasted: ${totalFood:F2}";
        }

        // Calculates the total amount wasted and updates the display, providing error handling for invalid input
        private void OnCalculate(object sender, EventArgs e)
        {
            string foodName = txtFoodName.Text?.Trim() ?? string.Empty;
            string priceText = txtWasteAmount.Text?.Trim() ?? string.Empty;

            // checks if the food name is empty or whitespace
            if (string.IsNullOrWhiteSpace(foodName))
            {
                lblWarning.Text = "Please enter the food name.";
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

            // valid: add item, update total, clear inputs and warnings
            wasteItems.Insert(0, new WasteItem { Name = foodName, Price = wasteAmount });
            totalFood += wasteAmount;
            lblLetterGrade.Text = $"Total Wasted: ${totalFood:F2}";

            lblWarning.Text = string.Empty;
            txtFoodName.Text = string.Empty;
            txtWasteAmount.Text = string.Empty;
            txtFoodName.Focus();
        }

        // Loop for deleting items from the colelction and updating the total, handing potential issues with item removal
        private void OnDeleteItem(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipeItem && swipeItem.BindingContext is WasteItem item)
            {
                // remove from collection and update total
                if (wasteItems.Remove(item))
                {
                    totalFood -= item.Price;
                    if (totalFood < 0) totalFood = 0; // safety
                    lblLetterGrade.Text = $"Total Wasted: ${totalFood:F2}";
                    lblWarning.Text = string.Empty;
                }
                else
                {
                    lblWarning.Text = "Could not remove item.";
                }
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
    }
}
