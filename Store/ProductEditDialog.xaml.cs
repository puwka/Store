using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static Store.ProductService;

namespace Store
{
    public partial class ProductEditDialog : Window
    {
        private readonly ProductService _productService;
        private string dbCon = "Server=localhost;Port=5432;Database=Store;User Id=postgres;Password=12345";
        public Product Product { get; private set; }

        public ProductEditDialog()
        {
            InitializeComponent();

            _productService = new ProductService(dbCon);

            Product = new Product
            {
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            LoadCategoriesAndSuppliers();
            DataContext = Product;
        }

        public ProductEditDialog(Product product) : this()
        {
            Product = new Product
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                CategoryId = product.CategoryId,
                SupplierId = product.SupplierId,
                Price = product.Price,
                QuantityInStock = product.QuantityInStock,
                Barcode = product.Barcode,
                ImageUrl = product.ImageUrl,
                ImageData = product.ImageData,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                ExpiryDate = product.ExpiryDate,
                Weight = product.Weight,
                Volume = product.Volume
            };

            DataContext = Product;
        }

        private void LoadCategoriesAndSuppliers()
        {
            try
            {
                List<Category> categories = _productService.GetAllCategories();
                cmbCategory.ItemsSource = categories;

                categories.Insert(0, new Category { CategoryId = -1, Name = "Не выбрано" });

                List<Supplier> suppliers = _productService.GetAllSuppliers();
                cmbSupplier.ItemsSource = suppliers;

                suppliers.Insert(0, new Supplier { SupplierId = -1, Name = "Не выбрано" });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Product.Name))
            {
                MessageBox.Show("Введите название продукта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Product.Price <= 0)
            {
                MessageBox.Show("Цена должна быть больше нуля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Product.QuantityInStock < 0)
            {
                MessageBox.Show("Количество не может быть отрицательным", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Product.CategoryId == -1) Product.CategoryId = null;
            if (Product.SupplierId == -1) Product.SupplierId = null;

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}