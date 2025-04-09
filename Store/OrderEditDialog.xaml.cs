using System;
using System.Collections.Generic;
using System.Windows;

namespace Store
{
    public partial class OrderEditDialog : Window
    {
        public Order Order { get; set; }
        public List<OrderItem> OrderItems { get; set; }

        public OrderEditDialog()
        {
            InitializeComponent();

            Order = new Order
            {
                SaleDate = DateTime.Now,
            };

            OrderItems = new List<OrderItem>();

            DataContext = this;
        }

        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            OrderItems.Add(new OrderItem
            {
                ProductId = 1,
                ProductName = "Тестовый товар",
                Price = 100,
                Quantity = 1,
                Discount = 0
            });
        }

        private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (OrderItems.Count > 0)
                OrderItems.RemoveAt(OrderItems.Count - 1);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (OrderItems.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один товар", "Ошибка");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}