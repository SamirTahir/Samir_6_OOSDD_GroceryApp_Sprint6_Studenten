using System;
using System.ComponentModel;
using Grocery.App.ViewModels;
using Microsoft.Maui.Controls;

namespace Grocery.App.Behaviors
{
    public class AdminToolbarItemBehavior : Behavior<ContentPage>
    {
        private ContentPage? _page;
        private ToolbarItem? _adminItem;
        private GroceryListItemsViewModel? _vm;

        protected override void OnAttachedTo(ContentPage bindable)
        {
            base.OnAttachedTo(bindable);
            _page = bindable;
            bindable.BindingContextChanged += OnBindingContextChanged;
            WireVm();
            EnsureToolbarItem();
            UpdateToolbarItem();
        }

        protected override void OnDetachingFrom(ContentPage bindable)
        {
            UnwireVm();
            bindable.BindingContextChanged -= OnBindingContextChanged;
            RemoveToolbarItem();
            _page = null;
            base.OnDetachingFrom(bindable);
        }

        private void OnBindingContextChanged(object? sender, EventArgs e)
        {
            WireVm();
            if (_adminItem != null && _page != null)
                _adminItem.BindingContext = _page.BindingContext;
            UpdateToolbarItem();
        }

        private void WireVm()
        {
            if (_vm != null)
                _vm.PropertyChanged -= OnVmPropertyChanged;

            _vm = _page?.BindingContext as GroceryListItemsViewModel;

            if (_vm != null)
                _vm.PropertyChanged += OnVmPropertyChanged;
        }

        private void UnwireVm()
        {
            if (_vm != null)
                _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm = null;
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GroceryListItemsViewModel.IsAdmin))
                UpdateToolbarItem();
        }

        private void EnsureToolbarItem()
        {
            if (_adminItem != null || _page == null)
                return;

            _adminItem = new ToolbarItem { Text = "New product" };
            _adminItem.SetBinding(ToolbarItem.CommandProperty, nameof(GroceryListItemsViewModel.CreateNewProductCommand));
            _adminItem.BindingContext = _page.BindingContext;
        }

        private void UpdateToolbarItem()
        {
            if (_page == null || _adminItem == null)
                return;

            bool isAdmin = _vm?.IsAdmin ?? false;

            if (isAdmin && !_page.ToolbarItems.Contains(_adminItem))
                _page.ToolbarItems.Add(_adminItem);
            else if (!isAdmin && _page.ToolbarItems.Contains(_adminItem))
                _page.ToolbarItems.Remove(_adminItem);
        }

        private void RemoveToolbarItem()
        {
            if (_page != null && _adminItem != null)
                _page.ToolbarItems.Remove(_adminItem);
            _adminItem = null;
        }
    }
}