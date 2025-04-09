using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;

namespace Store
{
    public class ProductService
    {
        private readonly string _connectionString;

        public ProductService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Product> GetAllProducts()
        {
            var products = new List<Product>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    @"SELECT p.*, c.category_name as category_name, s.supplier_name as supplier_name 
              FROM products p
              LEFT JOIN categories c ON p.category_id = c.category_id
              LEFT JOIN suppliers s ON p.supplier_id = s.supplier_id
              ORDER BY p.product_id", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product
                            {
                                ProductId = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                CategoryId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                                SupplierId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                                Price = reader.GetDecimal(5),
                                QuantityInStock = reader.GetInt32(6),
                                Barcode = reader.IsDBNull(7) ? null : reader.GetString(7),
                                ImageUrl = reader.IsDBNull(8) ? null : reader.GetString(8),
                                ImageData = reader.IsDBNull(9) ? null : (byte[])reader[9],
                                IsActive = reader.GetBoolean(10),
                                CreatedAt = reader.GetDateTime(11),
                                UpdatedAt = reader.GetDateTime(12),
                                ExpiryDate = reader.IsDBNull(13) ? (DateTime?)null : reader.GetDateTime(13),
                                Weight = reader.IsDBNull(14) ? (decimal?)null : reader.GetDecimal(14),
                                Volume = reader.IsDBNull(15) ? (decimal?)null : reader.GetDecimal(15),
                                CategoryName = reader.IsDBNull(16) ? null : reader.GetString(16),
                                SupplierName = reader.IsDBNull(17) ? null : reader.GetString(17)
                            });
                        }
                    }
                }
            }

            return products;
        }

        public bool AddProduct(Product product)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    @"INSERT INTO products (name, description, category_id, supplier_id, price, quantity_in_stock, 
                      barcode, image_url, image_data, is_active, created_at, updated_at, expiry_date, weight, volume)
                      VALUES (@name, @description, @category_id, @supplier_id, @price, @quantity_in_stock, 
                      @barcode, @image_url, @image_data, @is_active, @created_at, @updated_at, @expiry_date, @weight, @volume)", conn))
                {
                    cmd.Parameters.AddWithValue("@name", product.Name);
                    cmd.Parameters.AddWithValue("@description", (object)product.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@category_id", (object)product.CategoryId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@supplier_id", (object)product.SupplierId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@price", product.Price);
                    cmd.Parameters.AddWithValue("@quantity_in_stock", product.QuantityInStock);
                    cmd.Parameters.AddWithValue("@barcode", (object)product.Barcode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@image_url", (object)product.ImageUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@image_data", (object)product.ImageData ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@is_active", product.IsActive);
                    cmd.Parameters.AddWithValue("@created_at", DateTime.Now);
                    cmd.Parameters.AddWithValue("@updated_at", DateTime.Now);
                    cmd.Parameters.AddWithValue("@expiry_date", (object)product.ExpiryDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@weight", (object)product.Weight ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@volume", (object)product.Volume ?? DBNull.Value);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool UpdateProduct(Product product)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    @"UPDATE products SET name = @name, description = @description, category_id = @category_id, 
                      supplier_id = @supplier_id, price = @price, quantity_in_stock = @quantity_in_stock, 
                      barcode = @barcode, image_url = @image_url, image_data = @image_data, is_active = @is_active, 
                      updated_at = @updated_at, expiry_date = @expiry_date, weight = @weight, volume = @volume
                      WHERE product_id = @product_id", conn))
                {
                    cmd.Parameters.AddWithValue("@product_id", product.ProductId);
                    cmd.Parameters.AddWithValue("@name", product.Name);
                    cmd.Parameters.AddWithValue("@description", (object)product.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@category_id", (object)product.CategoryId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@supplier_id", (object)product.SupplierId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@price", product.Price);
                    cmd.Parameters.AddWithValue("@quantity_in_stock", product.QuantityInStock);
                    cmd.Parameters.AddWithValue("@barcode", (object)product.Barcode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@image_url", (object)product.ImageUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@image_data", (object)product.ImageData ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@is_active", product.IsActive);
                    cmd.Parameters.AddWithValue("@updated_at", DateTime.Now);
                    cmd.Parameters.AddWithValue("@expiry_date", (object)product.ExpiryDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@weight", (object)product.Weight ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@volume", (object)product.Volume ?? DBNull.Value);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteProduct(int productId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("DELETE FROM products WHERE product_id = @product_id", conn))
                {
                    cmd.Parameters.AddWithValue("@product_id", productId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
        public bool OrderProduct(int saleId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("DELETE FROM sales WHERE sale_id = @sale_id", conn))
                {
                    cmd.Parameters.AddWithValue("@sale_id", saleId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public List<Category> GetAllCategories()
        {
            var categories = new List<Category>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT category_id, category_name FROM categories ORDER BY category_name", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new Category
                            {
                                CategoryId = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            return categories;
        }

        public List<Supplier> GetAllSuppliers()
        {
            var suppliers = new List<Supplier>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT supplier_id, supplier_name FROM suppliers ORDER BY supplier_name", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            suppliers.Add(new Supplier
                            {
                                SupplierId = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            return suppliers;
        }

        public class Category
        {
            public int CategoryId { get; set; }
            public string Name { get; set; }
        }

        public class Supplier
        {
            public int SupplierId { get; set; }
            public string Name { get; set; }
        }
    }

    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public decimal Price { get; set; }
        public int QuantityInStock { get; set; }
        public string Barcode { get; set; }
        public string ImageUrl { get; set; }
        public byte[] ImageData { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Volume { get; set; }
        public string CategoryName { get; set; }
        public string SupplierName { get; set; }
    }
}