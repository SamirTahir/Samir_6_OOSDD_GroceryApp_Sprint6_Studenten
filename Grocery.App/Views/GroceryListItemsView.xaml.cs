using Grocery.App.ViewModels;

namespace Grocery.App.Views
{
    public partial class GroceryListItemsView : ContentPage
    {
        public GroceryListItemsView(GroceryListItemsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as GroceryListItemsViewModel)?.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            (BindingContext as GroceryListItemsViewModel)?.OnDisappearing();
        }
    }
}