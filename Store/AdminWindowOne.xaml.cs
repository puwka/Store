using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Imaging;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows.Navigation;
using System.Linq;
using System.Net.Http.Headers;
using JsonException = System.Text.Json.JsonException;
using System.Diagnostics;
using System.Net;
using Npgsql;

namespace Store
{
    public partial class AdminWindowOne : Window
    {
        private readonly ProductService _productService;
        private readonly DiscountService _discountService;
        private readonly OrderService _orderService;
        private string dbCon = "Server=localhost;Port=5432;Database=Store;User Id=postgres;Password=12345";

        public AdminWindowOne()
        {
            InitializeComponent();

            _productService = new ProductService(dbCon);
            _discountService = new DiscountService(dbCon);
            _orderService = new OrderService(dbCon);

            LoadProducts();
            LoadDiscounts();
            LoadOrders();
        }

        private void LoadProducts()
        {
            try
            {
                List<Product> products = _productService.GetAllProducts();

                if (!string.IsNullOrWhiteSpace(productSearchBox.Text))
                {
                    var searchTerm = productSearchBox.Text.ToLower();
                    products = products.Where(p =>
                        p.Name.ToLower().Contains(searchTerm) ||
                        p.CategoryName.ToLower().Contains(searchTerm) ||
                        p.SupplierName.ToLower().Contains(searchTerm) ||
                        p.Price.ToString().Contains(searchTerm) ||
                        p.ProductId.ToString().Contains(searchTerm)).ToList();
                }

                productsGrid.ItemsSource = products;
                statusText.Text = $"Загружено {products.Count} продуктов";
            }
            catch (Exception ex)
            {
                statusText.Text = "Ошибка загрузки продуктов";
                MessageBox.Show($"Ошибка при загрузке продуктов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProductEditDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    if (_productService.AddProduct(dialog.Product))
                    {
                        statusText.Text = "Продукт успешно добавлен";
                        LoadProducts();
                    }
                    else
                    {
                        statusText.Text = "Не удалось добавить продукт";
                    }
                }
                catch (Exception ex)
                {
                    statusText.Text = "Ошибка добавления продукта";
                    MessageBox.Show($"Ошибка при добавлении продукта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnEditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (productsGrid.SelectedItem is Product selectedProduct)
            {
                var dialog = new ProductEditDialog(selectedProduct);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        if (_productService.UpdateProduct(dialog.Product))
                        {
                            statusText.Text = "Продукт успешно обновлен";
                            LoadProducts();
                        }
                        else
                        {
                            statusText.Text = "Не удалось обновить продукт";
                        }
                    }
                    catch (Exception ex)
                    {
                        statusText.Text = "Ошибка обновления продукта";
                        MessageBox.Show($"Ошибка при обновлении продукта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                statusText.Text = "Выберите продукт для редактирования";
                MessageBox.Show("Пожалуйста, выберите продукт для редактирования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnDeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (productsGrid.SelectedItem is Product selectedProduct)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить продукт '{selectedProduct.Name}'?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (_productService.DeleteProduct(selectedProduct.ProductId))
                        {
                            statusText.Text = "Продукт успешно удален";
                            LoadProducts();
                        }
                        else
                        {
                            statusText.Text = "Не удалось удалить продукт";
                        }
                    }
                    catch (Exception ex)
                    {
                        statusText.Text = "Ошибка удаления продукта";
                        MessageBox.Show($"Ошибка при удалении продукта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                statusText.Text = "Выберите продукт для удаления";
                MessageBox.Show("Пожалуйста, выберите продукт для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadProducts();
            statusText.Text = "Данные обновлены";
        }

        private void LoadDiscounts()
        {
            try
            {
                List<Discount> discounts = _discountService.GetAllDiscounts();
                discountsGrid.ItemsSource = discounts;
                discountStatusText.Text = $"Загружено {discounts.Count} скидок";
            }
            catch (Exception ex)
            {
                discountStatusText.Text = "Ошибка загрузки скидок";
                MessageBox.Show($"Ошибка при загрузке скидок: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddDiscount_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new DiscountEditDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    if (_discountService.AddDiscount(dialog.Discount))
                    {
                        discountStatusText.Text = "Скидка успешно добавлена";
                        LoadDiscounts();
                    }
                    else
                    {
                        discountStatusText.Text = "Не удалось добавить скидку";
                    }
                }
                catch (Exception ex)
                {
                    discountStatusText.Text = "Ошибка добавления скидки";
                    MessageBox.Show($"Ошибка при добавлении скидки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnEditDiscount_Click(object sender, RoutedEventArgs e)
        {
            if (discountsGrid.SelectedItem is Discount selectedDiscount)
            {
                var dialog = new DiscountEditDialog(selectedDiscount);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        if (_discountService.UpdateDiscount(dialog.Discount))
                        {
                            discountStatusText.Text = "Скидка успешно обновлена";
                            LoadDiscounts();
                        }
                        else
                        {
                            discountStatusText.Text = "Не удалось обновить скидку";
                        }
                    }
                    catch (Exception ex)
                    {
                        discountStatusText.Text = "Ошибка обновления скидки";
                        MessageBox.Show($"Ошибка при обновлении скидки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                discountStatusText.Text = "Выберите скидку для редактирования";
                MessageBox.Show("Пожалуйста, выберите скидку для редактирования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnDeleteDiscount_Click(object sender, RoutedEventArgs e)
        {
            if (discountsGrid.SelectedItem is Discount selectedDiscount)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить скидку для продукта '{selectedDiscount.ProductName}'?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (_discountService.DeleteDiscount(selectedDiscount.DiscountId))
                        {
                            discountStatusText.Text = "Скидка успешно удалена";
                            LoadDiscounts();
                        }
                        else
                        {
                            discountStatusText.Text = "Не удалось удалить скидку";
                        }
                    }
                    catch (Exception ex)
                    {
                        discountStatusText.Text = "Ошибка удаления скидки";
                        MessageBox.Show($"Ошибка при удалении скидки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                discountStatusText.Text = "Выберите скидку для удаления";
                MessageBox.Show("Пожалуйста, выберите скидку для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnRefreshDiscounts_Click(object sender, RoutedEventArgs e)
        {
            LoadDiscounts();
            discountStatusText.Text = "Данные обновлены";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = new ButtonOneWindow();
            btn.Show();
            this.Close();
        }

        private void LoadOrders()
        {
            try
            {
                List<Order> orders = _orderService.GetAllOrders();
                ordersGrid.ItemsSource = orders;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProductSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTerm = productSearchBox.Text.ToLower();
            var allProducts = _productService.GetAllProducts();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                productsGrid.ItemsSource = allProducts;
                statusText.Text = $"Загружено {allProducts.Count} продуктов";
            }
            else
            {
                var filteredProducts = allProducts.Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.CategoryName.ToLower().Contains(searchTerm) ||
                    p.SupplierName.ToLower().Contains(searchTerm) ||
                    p.Price.ToString().Contains(searchTerm) ||
                    p.ProductId.ToString().Contains(searchTerm)).ToList();

                productsGrid.ItemsSource = filteredProducts;
                statusText.Text = $"Найдено {filteredProducts.Count} продуктов";
            }
        }

        private void DiscountSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTerm = discountSearchBox.Text.ToLower();
            var allDiscounts = _discountService.GetAllDiscounts();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                discountsGrid.ItemsSource = allDiscounts;
                discountStatusText.Text = $"Загружено {allDiscounts.Count} скидок";
            }
            else
            {
                var filteredDiscounts = allDiscounts.Where(d =>
                    d.ProductName.ToLower().Contains(searchTerm) ||
                    d.DiscountPercentage.ToString().Contains(searchTerm) ||
                    d.DiscountId.ToString().Contains(searchTerm) ||
                    d.StartDate.ToString("dd.MM.yyyy").Contains(searchTerm) ||
                    d.EndDate.ToString("dd.MM.yyyy").Contains(searchTerm)).ToList();

                discountsGrid.ItemsSource = filteredDiscounts;
                discountStatusText.Text = $"Найдено {filteredDiscounts.Count} скидок";
            }
        }

        private void OrderSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTerm = orderSearchBox.Text.ToLower();
            var allOrders = _orderService.GetAllOrders();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                ordersGrid.ItemsSource = allOrders;
            }
            else
            {
                var filteredOrders = allOrders.Where(o =>
                    o.SaleId.ToString().Contains(searchTerm) ||
                    o.SaleDate.ToString("dd.MM.yyyy HH:mm").Contains(searchTerm) ||
                    o.TotalAmount.ToString().Contains(searchTerm) ||
                    o.FinalAmount.ToString().Contains(searchTerm)).ToList();

                ordersGrid.ItemsSource = filteredOrders;
            }
        }

        private void BtnRefreshOrders_Click(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        private void ordersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ordersGrid.SelectedItem is Order selectedOrder)
            {
                try
                {
                    List<OrderItem> orderItems = _orderService.GetOrderItems(selectedOrder.SaleId);
                    orderItemsGrid.ItemsSource = orderItems;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки деталей заказа: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите заказ для просмотра деталей", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (ordersGrid.SelectedItem is Order selectedOrder)
            {
                if (ordersGrid.SelectedItem != null)
                {
                    try
                    {
                        if (_productService.OrderProduct(selectedOrder.SaleId))
                        {
                            statusText.Text = "Заказ успешно удален";
                            LoadProducts();
                        }
                        else
                        {
                            statusText.Text = "Не удалось удалить заказ";
                        }
                    }
                    catch (Exception ex)
                    {
                        statusText.Text = "Ошибка удаления заказа";
                        MessageBox.Show($"Ошибка при удалении заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Пожалуйста, выберите заказ для удаления", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }

    public class OrderService
    {
        private readonly string _connectionString;

        public OrderService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Order> GetAllOrders()
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand(
                    "SELECT s.sale_id, s.sale_date, s.total_amount, " +
                    "s.discount_amount, s.final_amount " +
                    "FROM sales s ORDER BY s.sale_date DESC", conn);

                var orders = new List<Order>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        orders.Add(new Order
                        {
                            SaleId = reader.GetInt32(0),
                            SaleDate = reader.GetDateTime(1),
                            TotalAmount = reader.GetDecimal(2),
                            DiscountAmount = reader.GetDecimal(3),
                            FinalAmount = reader.GetDecimal(4)
                        });
                    }
                }
                return orders;
            }
        }

        public List<OrderItem> GetOrderItems(int saleId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand(
                    "SELECT si.item_id, si.product_id, p.name as product_name, " +
                    "si.quantity, si.price, si.discount " +
                    "FROM sale_items si " +
                    "JOIN products p ON si.product_id = p.product_id " +
                    "WHERE si.sale_id = @saleId", conn);
                cmd.Parameters.AddWithValue("@saleId", saleId);

                var items = new List<OrderItem>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new OrderItem
                        {
                            ItemId = reader.GetInt32(0),
                            ProductId = reader.GetInt32(1),
                            ProductName = reader.GetString(2),
                            Quantity = reader.GetInt32(3),
                            Price = reader.GetDecimal(4),
                            Discount = reader.GetDecimal(5),
                            ItemTotal = reader.GetDecimal(4) * reader.GetInt32(3) * (1 - reader.GetDecimal(5) / 100)
                        });
                    }
                }
                return items;
            }
        }
    }

    public class Order
    {
        public int SaleId { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
    }

    public class OrderItem
    {
        public int ItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal ItemTotal { get; set; }
    }
}