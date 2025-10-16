using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;

namespace Grocery.App.ViewModels
{
    public partial class AddProductViewModel : BaseViewModel
    {
        private readonly IProductService _productService;
        private readonly GlobalViewModel _global;

        [ObservableProperty] private string name;
        [ObservableProperty] private string stock = "0";
        [ObservableProperty] private DateTime shelfLife = DateTime.Today;
        [ObservableProperty] private string price = "0.00";
        [ObservableProperty] private string message;

        public AddProductViewModel(IProductService productService, GlobalViewModel global)
        {
            _productService = productService;
            _global = global;
        }

        [RelayCommand]
        private async Task Save()
        {
            // Only admins
            if (_global.Client?.Role != Role.Admin)
            {
                Message = "Only admins can create products.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                Message = "Name is required.";
                return;
            }
            if (!int.TryParse(stock, out int parsedStock) || parsedStock < 0)
            {
                Message = "Stock must be a non-negative integer.";
                return;
            }
            if (!decimal.TryParse(price, out decimal parsedPrice) || parsedPrice < 0 || parsedPrice > 999.99m)
            {
                Message = "Price must be between 0.00 and 999.99.";
                return;
            }

            var p = new Product(0, Name.Trim(), parsedStock, DateOnly.FromDateTime(ShelfLife), parsedPrice);
            _productService.Add(p);

            await Shell.Current.GoToAsync("..", true);
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await Shell.Current.GoToAsync("..", true);
        }
    }
}