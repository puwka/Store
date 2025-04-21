using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Store.CashierWindow;

namespace Store
{
    public partial class CashierWindow : Window, INotifyPropertyChanged
    {
        private string dbCon = "Server=localhost;Port=5432;Database=Store;User Id=postgres;Password=12345";

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<CartItem> _cartItems = new ObservableCollection<CartItem>();

        public ObservableCollection<CartItem> CartItems
        {
            get => _cartItems;
            set { _cartItems = value; OnPropertyChanged(nameof(CartItems)); }
        }

        private decimal _totalPrice;
        public decimal TotalPrice
        {
            get => _totalPrice;
            set { _totalPrice = value; OnPropertyChanged(nameof(TotalPrice)); }
        }

        private decimal _totalDiscount;
        public decimal TotalDiscount
        {
            get => _totalDiscount;
            set { _totalDiscount = value; OnPropertyChanged(nameof(TotalDiscount)); }
        }

        private decimal _finalPrice;
        public decimal FinalPrice
        {
            get => _finalPrice;
            set { _finalPrice = value; OnPropertyChanged(nameof(FinalPrice)); }
        }

        public CashierWindow()
        {
            InitializeComponent();
            DataContext = this;

            if (App.CurrentUser == null)
            {
                MessageBox.Show("Доступ запрещен. Требуется авторизация.");
                ReturnToAuthorization();
                return;
            }

            LoadProducts();
        }

        private void LoadProducts(string searchTerm = "")
        {
            try
            {
                using (var connection = new NpgsqlConnection(dbCon))
                {
                    connection.Open();
                    string query = @"SELECT 
                p.product_id, 
                p.name as product_name, 
                p.price, 
                p.quantity_in_stock,
                p.barcode,
                COALESCE(d.discount_value, 0) as discount
                FROM products p
                LEFT JOIN product_discounts d ON p.product_id = d.product_id 
                    AND CURRENT_TIMESTAMP BETWEEN d.start_date AND d.end_date
                    AND d.is_active = true
                WHERE p.is_active = true AND p.quantity_in_stock > 0";

                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        query += " AND (p.name ILIKE @searchTerm OR p.barcode = @barcode)";
                    }

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(searchTerm))
                        {
                            command.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
                            command.Parameters.AddWithValue("@barcode", searchTerm);
                        }

                        var adapter = new NpgsqlDataAdapter(command);
                        var dt = new DataTable();
                        adapter.Fill(dt);

                        ProductsGrid.ItemsSource = dt.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}");
            }
        }

        private void AddToCart(DataRowView selectedRow)
        {
            if (selectedRow == null) return;

            try
            {
                int productId = Convert.ToInt32(selectedRow["product_id"]);
                var existingItem = CartItems.FirstOrDefault(item => item.ProductId == productId);

                if (existingItem != null)
                {
                    existingItem.Quantity++;
                    return;
                }

                var newItem = new CartItem
                {
                    ProductId = productId,
                    ProductName = selectedRow["product_name"].ToString(),
                    Price = Convert.ToDecimal(selectedRow["price"]),
                    Quantity = 1,
                    Discount = selectedRow["discount"] != DBNull.Value ?
                              Convert.ToDecimal(selectedRow["discount"]) / 100m : 0m,
                    MaxQuantity = Convert.ToInt32(selectedRow["quantity_in_stock"])
                };

                CartItems.Add(newItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
            finally
            {
                CalculateTotals();
            }
        }

        private void CalculateTotals()
        {
            TotalPrice = Math.Round(quantity * price, 2);
            TotalDiscount = CartItems.Sum(item => item.DiscountAmount);
            FinalPrice = TotalPrice - TotalDiscount;

            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(TotalDiscount));
            OnPropertyChanged(nameof(FinalPrice));
        }

        private void RemoveItemFromCart(int productId)
        {
            var item = CartItems.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                CartItems.Remove(item);
                CalculateTotals();
            }
        }

        private void ProcessCheckout()
        {
            if (CartItems.Count == 0)
            {
                MessageBox.Show("Добавьте товары в корзину");
                return;
            }

            try
            {
                using (var connection = new NpgsqlConnection(dbCon))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            string saleQuery = @"INSERT INTO sales (sale_date, total_amount, discount_amount, final_amount)
                                               VALUES (CURRENT_TIMESTAMP, @total, @discount, @final) RETURNING sale_id";

                            int saleId;
                            using (var saleCommand = new NpgsqlCommand(saleQuery, connection, transaction))
                            {
                                saleCommand.Parameters.AddWithValue("@total", TotalPrice);
                                saleCommand.Parameters.AddWithValue("@discount", TotalDiscount);
                                saleCommand.Parameters.AddWithValue("@final", FinalPrice);
                                saleId = Convert.ToInt32(saleCommand.ExecuteScalar());
                            }

                            string itemsQuery = @"INSERT INTO sale_items (sale_id, product_id, quantity, price, discount)
                                               VALUES (@saleId, @productId, @quantity, @price, @discount)";

                            string updateStockQuery = @"UPDATE products 
                                                      SET quantity_in_stock = quantity_in_stock - @quantity
                                                      WHERE product_id = @productId";

                            foreach (var item in CartItems)
                            {
                                using (var itemCommand = new NpgsqlCommand(itemsQuery, connection, transaction))
                                {
                                    itemCommand.Parameters.AddWithValue("@saleId", saleId);
                                    itemCommand.Parameters.AddWithValue("@productId", item.ProductId);
                                    itemCommand.Parameters.AddWithValue("@quantity", item.Quantity);
                                    itemCommand.Parameters.AddWithValue("@price", item.Price);
                                    itemCommand.Parameters.AddWithValue("@discount", item.Discount);
                                    itemCommand.ExecuteNonQuery();
                                }

                                using (var updateCommand = new NpgsqlCommand(updateStockQuery, connection, transaction))
                                {
                                    updateCommand.Parameters.AddWithValue("@productId", item.ProductId);
                                    updateCommand.Parameters.AddWithValue("@quantity", item.Quantity);
                                    updateCommand.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            MessageBox.Show($"Продажа оформлена. Номер чека: {saleId}");
                            CartItems.Clear();
                            CalculateTotals();
                            LoadProducts();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка оформления продажи: {ex.Message}");
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoadProducts(SearchTextBox.Text);
            }
        }

        private void BarcodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(BarcodeTextBox.Text))
            {
                LoadProducts(BarcodeTextBox.Text);
                BarcodeTextBox.Clear();
            }
        }

        private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Console.WriteLine("Selection changed event triggered");

            if (ProductsGrid.SelectedItem is DataRowView selectedRow)
            {
                Console.WriteLine($"Selected row: {selectedRow.Row.ItemArray[0]}");
                AddToCart(selectedRow);
                ProductsGrid.SelectedItem = null;
            }
            else
            {
                Console.WriteLine("Selected item is not DataRowView");
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is CartItem item)
            {
                RemoveItemFromCart(item.ProductId);
            }
        }

        private void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckout();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadProducts(SearchTextBox.Text);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (IsUserCashier())
            {
                ReturnToAuthorization();
            }
            else if (IsUserCashier())
            {
                ReturnToMainMenu();
            }
            else
            {
                ReturnToButton();
            }
        }

        private bool IsUserCashier()
        {
            return App.CurrentUser?.Role == "Кассир";
        }

        private void ReturnToAuthorization()
        {
            App.CurrentUser = null;

            var authWindow = new MainWindow();
            authWindow.Show();
            this.Close();
        }

        private void ReturnToMainMenu()
        {
            var mainMenu = new ButtonWindow();
            mainMenu.Show();
            this.Close();
        }
        private void ReturnToButton()
        {
            var btn = new ButtonOneWindow();
            btn.Show();
            this.Close();
        }

        public void PlaceOrder(int productId, int quantity, decimal price)
        {
            if (quantity <= 0)
            {
                MessageBox.Show("Количество товара должно быть больше нуля.", 
                               "Ошибка", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Warning);
                return;
            }
            decimal total = quantity * price;
            SaveOrder(productId, quantity, total);
        }


    }

    public class CartItem : INotifyPropertyChanged
    {
        private int _quantity;
        private decimal _discount;

        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(DiscountAmount));
                OnPropertyChanged(nameof(TotalDisplay));
            }
        }

        public decimal Discount
        {
            get => _discount;
            set
            {
                _discount = value;
                OnPropertyChanged(nameof(Discount));
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(DiscountAmount));
                OnPropertyChanged(nameof(TotalDisplay));
            }
        }

        public decimal DiscountAmount => Price * Quantity * Discount;
        public decimal Total => Price * Quantity - DiscountAmount;
        public string DiscountDisplay => (Discount * 100).ToString("0") + "%";
        public string PriceDisplay => Price.ToString("N2") + " ₽";
        public string TotalDisplay => Total.ToString("N2") + " ₽";

        public int MaxQuantity { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
