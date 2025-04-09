using System;
using System.Collections.Generic;
using System.Windows;

namespace Store
{
    public partial class DiscountEditDialog : Window
    {
        private readonly ProductService _productService;
        private string dbCon = "Server=localhost;Port=5432;Database=Store;User Id=postgres;Password=12345";
        public Discount Discount { get; private set; }

        public DiscountEditDialog()
        {
            InitializeComponent();

            _productService = new ProductService(dbCon);

            Discount = new Discount
            {
                IsActive = true,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7)
            };

            LoadProducts();
            DataContext = Discount;
        }

        public DiscountEditDialog(Discount discount) : this()
        {
            Discount = new Discount
            {
                DiscountId = discount.DiscountId,
                ProductId = discount.ProductId,
                DiscountPercentage = discount.DiscountPercentage,
                StartDate = discount.StartDate,
                EndDate = discount.EndDate,
                IsActive = discount.IsActive
            };

            DataContext = Discount;
        }

        private void LoadProducts()
        {
            try
            {
                List<Product> products = _productService.GetAllProducts();
                cmbProduct.ItemsSource = products;

                products.Insert(0, new Product { ProductId = -1, Name = "Не выбрано" });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке продуктов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (Discount.DiscountPercentage <= 0 || Discount.DiscountPercentage >= 100)
            {
                MessageBox.Show("Процент скидки должен быть между 0 и 100", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Discount.EndDate < Discount.StartDate)
            {
                MessageBox.Show("Дата окончания не может быть раньше даты начала", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Discount.ProductId == -1) Discount.ProductId = null;

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}