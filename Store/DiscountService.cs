using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;

namespace Store
{
    public class DiscountService
    {
        private readonly string _connectionString;

        public DiscountService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Discount> GetAllDiscounts()
        {
            var discounts = new List<Discount>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    @"SELECT pd.*, p.name as product_name 
                      FROM product_discounts pd
                      LEFT JOIN products p ON pd.product_id = p.product_id
                      ORDER BY pd.discount_id", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            discounts.Add(new Discount
                            {
                                DiscountId = reader.GetInt32(0),
                                ProductId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                                DiscountPercentage = reader.GetDecimal(2),
                                StartDate = reader.GetDateTime(3),
                                EndDate = reader.GetDateTime(4),
                                IsActive = reader.GetBoolean(5),
                                ProductName = reader.IsDBNull(6) ? null : reader.GetString(6)
                            });
                        }
                    }
                }
            }

            return discounts;
        }

        public bool AddDiscount(Discount discount)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    @"INSERT INTO product_discounts (product_id, discount_value, start_date, end_date, is_active)
                      VALUES (@product_id, @discount_value, @start_date, @end_date, @is_active)", conn))
                {
                    cmd.Parameters.AddWithValue("@product_id", (object)discount.ProductId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@discount_value", discount.DiscountPercentage);
                    cmd.Parameters.AddWithValue("@start_date", discount.StartDate);
                    cmd.Parameters.AddWithValue("@end_date", discount.EndDate);
                    cmd.Parameters.AddWithValue("@is_active", discount.IsActive);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool UpdateDiscount(Discount discount)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    @"UPDATE product_discounts SET 
                      product_id = @product_id, 
                      discount_value = @discount_value, 
                      start_date = @start_date, 
                      end_date = @end_date,
                      is_active = @is_active
                      WHERE discount_id = @discount_id", conn))
                {
                    cmd.Parameters.AddWithValue("@discount_id", discount.DiscountId);
                    cmd.Parameters.AddWithValue("@product_id", (object)discount.ProductId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@discount_value", discount.DiscountPercentage);
                    cmd.Parameters.AddWithValue("@start_date", discount.StartDate);
                    cmd.Parameters.AddWithValue("@end_date", discount.EndDate);
                    cmd.Parameters.AddWithValue("@is_active", discount.IsActive);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteDiscount(int discountId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("DELETE FROM product_discounts WHERE discount_id = @discount_id", conn))
                {
                    cmd.Parameters.AddWithValue("@discount_id", discountId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }

    public class Discount
    {
        public int DiscountId { get; set; }
        public int? ProductId { get; set; }
        public decimal DiscountPercentage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public string ProductName { get; set; }
    }
}