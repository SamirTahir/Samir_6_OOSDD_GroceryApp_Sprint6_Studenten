using Grocery.App.ViewModels;

namespace Grocery.App.Views
{
    public partial class AddProductView : ContentPage
    {
        public AddProductView(AddProductViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}