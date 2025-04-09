using iTextSharp.text.pdf;
using iTextSharp.text;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Image = iTextSharp.text.Image;

namespace Store
{
    public partial class AdminWindow : Window, INotifyPropertyChanged
    {
        private string dbCon = "Server=localhost;Port=5432;Database=Store;User Id=postgres;Password=12345";
        private ObservableCollection<SalesReportItem> _salesReport = new ObservableCollection<SalesReportItem>();
        private ObservableCollection<Employee> _employees = new ObservableCollection<Employee>();
        private SeriesCollection _salesSeries;
        private SeriesCollection _topProductsSeries;
        private string[] _salesLabels;
        private string[] _topProductsLabels;
        private decimal _totalSales;

        public ObservableCollection<SalesReportItem> SalesReport
        {
            get => _salesReport;
            set { _salesReport = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Employee> Employees
        {
            get => _employees;
            set { _employees = value; OnPropertyChanged(); }
        }

        public SeriesCollection SalesSeries
        {
            get => _salesSeries;
            set { _salesSeries = value; OnPropertyChanged(); }
        }

        public SeriesCollection TopProductsSeries
        {
            get => _topProductsSeries;
            set { _topProductsSeries = value; OnPropertyChanged(); }
        }

        public string[] SalesLabels
        {
            get => _salesLabels;
            set { _salesLabels = value; OnPropertyChanged(); }
        }

        public string[] TopProductsLabels
        {
            get => _topProductsLabels;
            set { _topProductsLabels = value; OnPropertyChanged(); }
        }

        public decimal TotalSales
        {
            get => _totalSales;
            set
            {
                _totalSales = value;
                OnPropertyChanged();
                TotalSalesText.Text = value.ToString("N2");
            }
        }

        public AdminWindow()
        {
            InitializeComponent();
            DataContext = this;

            EndDatePicker.SelectedDate = DateTime.Today;
            StartDatePicker.SelectedDate = DateTime.Today;

            LoadSalesReport();
            LoadAnalyticsData();

            if (App.CurrentUser == null)
            {
                MessageBox.Show("Доступ запрещен. Требуется авторизация.");
                ReturnToAuthorization();
                return;
            }

            LoadEmployeesData();
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

        private void LoadSalesReport()
        {
            try
            {
                string query = @"SELECT s.sale_date as SaleDate, p.name as ProductName, 
                si.quantity as Quantity, si.price as Price, 
                (si.price * si.quantity) - (si.price * si.quantity * COALESCE(si.discount, 0)) as Total
                FROM sales s
                JOIN sale_items si ON s.sale_id = si.sale_id
                JOIN products p ON si.product_id = p.product_id
                WHERE s.sale_date >= @startDate AND s.sale_date <= @endDate
                ORDER BY s.sale_date DESC";

                var sales = new List<SalesReportItem>();
                decimal total = 0;

                using (var conn = new NpgsqlConnection(dbCon))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@startDate",
                            StartDatePicker.SelectedDate?.Date ?? DateTime.Today.Date);
                        cmd.Parameters.AddWithValue("@endDate",
                            EndDatePicker.SelectedDate?.Date.AddDays(1).AddSeconds(-1) ?? DateTime.Today.Date.AddDays(1).AddSeconds(-1));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new SalesReportItem
                                {
                                    SaleDate = reader.GetDateTime(0),
                                    ProductName = reader.GetString(1),
                                    Quantity = reader.GetInt32(2),
                                    Price = reader.GetDecimal(3),
                                    Total = reader.GetDecimal(4)
                                };
                                sales.Add(item);
                                total += item.Total;
                            }
                        }
                    }
                }

                SalesReport = new ObservableCollection<SalesReportItem>(sales);
                TotalSales = total;
                TotalSalesText.Text = total.ToString("N2");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке отчета: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAnalyticsData()
        {
            try
            {
                DateTime startDate = GetAnalyticsStartDate();

                var salesData = new List<SalesAnalytics>();
                string salesQuery = @"SELECT date_trunc('day', sale_date) as day, 
                                    SUM(total_amount) as total
                                    FROM sales
                                    WHERE sale_date >= @startDate
                                    GROUP BY day
                                    ORDER BY day";

                using (var conn = new NpgsqlConnection(dbCon))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(salesQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@startDate", startDate);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                salesData.Add(new SalesAnalytics
                                {
                                    Date = reader.GetDateTime(0),
                                    TotalSales = reader.GetDecimal(1)
                                });
                            }
                        }
                    }

                    var topProducts = new List<TopProduct>();
                    string topQuery = @"SELECT p.name, SUM(si.quantity) as total_qty
                                      FROM sale_items si
                                      JOIN products p ON si.product_id = p.product_id
                                      JOIN sales s ON si.sale_id = s.sale_id
                                      WHERE s.sale_date >= @startDate
                                      GROUP BY p.name
                                      ORDER BY total_qty DESC
                                      LIMIT 5";

                    using (var cmd = new NpgsqlCommand(topQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@startDate", startDate);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                topProducts.Add(new TopProduct
                                {
                                    ProductName = reader.GetString(0),
                                    SalesCount = reader.GetInt32(1)
                                });
                            }
                        }
                    }

                    UpdateCharts(salesData, topProducts);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке аналитики: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCharts(List<SalesAnalytics> salesData, List<TopProduct> topProducts)
        {
            SalesSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Продажи",
                    Values = new ChartValues<decimal>(salesData.Select(x => x.TotalSales)),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 10
                }
            };

            SalesLabels = salesData.Select(x => x.Date.ToString("dd.MM")).ToArray();

            TopProductsSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Продажи",
                    Values = new ChartValues<int>(topProducts.Select(x => x.SalesCount))
                }
            };

            TopProductsLabels = topProducts.Select(x => x.ProductName).ToArray();
        }

        private DateTime GetAnalyticsStartDate()
        {
            switch (AnalyticsPeriodCombo.SelectedIndex)
            {
                case 0: return DateTime.Today.AddDays(-7);
                case 1: return DateTime.Today.AddMonths(-1);
                case 2: return DateTime.Today.AddYears(-1);
                default: return DateTime.Today.AddDays(-7);
            }
        }

        private void LoadEmployeesData()
        {
            try
            {
                string query = @"SELECT 
                        u.user_id as ""Id"", 
                        u.full_name as ""FullName"", 
                        u.position as ""Position"", 
                        u.login as ""Login"", 
                        r.role_name as ""Role"",
                        CASE WHEN u.is_active THEN 'Активен' ELSE 'Неактивен' END as ""Status"",
                        NULL as ""LastLogin""
                    FROM users u
                    JOIN roles r ON u.role_id = r.role_id
                    WHERE u.role_id IN (1, 2, 3)
                    ORDER BY u.full_name";

                var employees = new List<Employee>();

                using (var conn = new NpgsqlConnection(dbCon))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                employees.Add(new Employee
                                {
                                    Id = reader.GetInt32(0),
                                    FullName = reader.GetString(1),
                                    Position = reader.GetString(2),
                                    Username = reader.GetString(3),
                                    Role = reader.GetString(4),
                                    Status = reader.GetString(5)
                                });
                            }
                        }
                    }
                }

                Employees = new ObservableCollection<Employee>(employees);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Отчет_продаж_{DateTime.Today:ddMMyyyy}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var fs = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    {
                        var document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
                        var writer = PdfWriter.GetInstance(document, fs);

                        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                        BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                        Font titleFont = new Font(baseFont, 16, Font.BOLD, BaseColor.DARK_GRAY);
                        Font headerFont = new Font(baseFont, 10, Font.BOLD, BaseColor.WHITE);
                        Font cellFont = new Font(baseFont, 10, Font.NORMAL, BaseColor.BLACK);

                        document.Open();

                        try
                        {
                            string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img", "C:\\Users\\csqqod\\source\\repos\\Store\\Store\\img\\logo1.png");
                            if (File.Exists(logoPath))
                            {
                                var logo = Image.GetInstance(logoPath);
                                logo.ScaleToFit(100, 100);
                                logo.Alignment = Element.ALIGN_CENTER;
                                document.Add(logo);
                            }
                            else
                            {
                                Console.WriteLine("Файл логотипа не найден: " + logoPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Ошибка при добавлении логотипа: " + ex.Message);
                        }

                        var title = new Paragraph("КНГК Store - Отчет о продажах", titleFont)
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 20
                        };
                        document.Add(title);

                        var periodText = $"Период: {StartDatePicker.SelectedDate?.ToString("dd.MM.yyyy") ?? "начало"} - " +
                                       $"{EndDatePicker.SelectedDate?.ToString("dd.MM.yyyy") ?? "конец"}";

                        var period = new Paragraph(periodText, cellFont)
                        {
                            Alignment = Element.ALIGN_RIGHT,
                            SpacingAfter = 10
                        };
                        document.Add(period);

                        var total = new Paragraph($"Итого: {SalesReport.Sum(x => x.Total).ToString("N2")} руб.", cellFont)
                        {
                            Alignment = Element.ALIGN_RIGHT,
                            SpacingAfter = 20
                        };
                        document.Add(total);

                        var table = new PdfPTable(5)
                        {
                            WidthPercentage = 100,
                            SpacingBefore = 10,
                            SpacingAfter = 30
                        };

                        float[] columnWidths = { 2f, 4f, 2f, 2f, 2f };
                        table.SetWidths(columnWidths);

                        AddHeaderCell(table, "Дата", headerFont, BaseColor.DARK_GRAY);
                        AddHeaderCell(table, "Товар", headerFont, BaseColor.DARK_GRAY);
                        AddHeaderCell(table, "Количество", headerFont, BaseColor.DARK_GRAY);
                        AddHeaderCell(table, "Цена", headerFont, BaseColor.DARK_GRAY);
                        AddHeaderCell(table, "Сумма", headerFont, BaseColor.DARK_GRAY);

                        foreach (var item in SalesReport)
                        {
                            AddCell(table, item.SaleDate.ToString("dd.MM.yyyy"), cellFont);
                            AddCell(table, item.ProductName, cellFont);
                            AddCell(table, item.Quantity.ToString(), cellFont);
                            AddCell(table, item.Price.ToString("N2"), cellFont);
                            AddCell(table, item.Total.ToString("N2"), cellFont);
                        }

                        document.Add(table);

                        var signText = new Paragraph($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}", cellFont)
                        {
                            Alignment = Element.ALIGN_RIGHT,
                            SpacingBefore = 50
                        };
                        document.Add(signText);

                        document.Close();
                    }

                    MessageBox.Show("Отчет успешно экспортирован в PDF!", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
                    Process.Start(saveFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в PDF: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddHeaderCell(PdfPTable table, string text, Font font, BaseColor bgColor)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                BackgroundColor = bgColor,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 5,
                BorderWidth = 1,
                BorderColor = BaseColor.LIGHT_GRAY
            };
            table.AddCell(cell);
        }

        private void AddCell(PdfPTable table, string text, Font font)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = Element.ALIGN_LEFT,
                Padding = 5,
                BorderWidth = 1,
                BorderColor = BaseColor.LIGHT_GRAY
            };
            table.AddCell(cell);
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EmployeeEditDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string query = @"INSERT INTO users (full_name, position, login, role_id, is_active)
                           VALUES (@fullName, @position, @login, @roleId, true)
                           RETURNING user_id";

                    using (var conn = new NpgsqlConnection(dbCon))
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@fullName", dialog.FullName);
                            cmd.Parameters.AddWithValue("@position", dialog.Position);
                            cmd.Parameters.AddWithValue("@login", dialog.Username);
                            cmd.Parameters.AddWithValue("@roleId", dialog.SelectedRoleId);

                            int newId = (int)cmd.ExecuteScalar();

                            string roleName = dialog.Roles.FirstOrDefault(r => r.Id == dialog.SelectedRoleId)?.Name ?? "Неизвестно";

                            Employees.Add(new Employee
                            {
                                Id = newId,
                                FullName = dialog.FullName,
                                Position = dialog.Position,
                                Username = dialog.Username,
                                Status = "Активен",
                                Role = roleName
                            });
                        }
                    }

                    MessageBox.Show("Сотрудник успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при добавлении сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeesDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите сотрудника для редактирования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedEmployee = (Employee)EmployeesDataGrid.SelectedItem;
            var dialog = new EmployeeEditDialog(selectedEmployee)
            {
                Title = "Редактирование сотрудника"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string query = @"UPDATE users 
                           SET full_name = @fullName, 
                               position = @position,
                               login = @login,
                               role_id = @roleId
                           WHERE user_id = @user_id";

                    using (var conn = new NpgsqlConnection(dbCon))
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@fullName", dialog.FullName);
                            cmd.Parameters.AddWithValue("@position", dialog.Position);
                            cmd.Parameters.AddWithValue("@login", dialog.Username);
                            cmd.Parameters.AddWithValue("@roleId", dialog.SelectedRoleId);
                            cmd.Parameters.AddWithValue("@user_id", selectedEmployee.Id);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    selectedEmployee.FullName = dialog.FullName;
                    selectedEmployee.Position = dialog.Position;
                    selectedEmployee.Username = dialog.Username;

                    var role = dialog.Roles.FirstOrDefault(r => r.Id == dialog.SelectedRoleId);
                    if (role != null)
                    {
                        selectedEmployee.Role = role.Name;
                    }

                    EmployeesDataGrid.Items.Refresh();

                    MessageBox.Show("Данные сотрудника обновлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeesDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите сотрудника для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedEmployee = (Employee)EmployeesDataGrid.SelectedItem;

            if (MessageBox.Show($"Вы уверены, что хотите удалить сотрудника {selectedEmployee.FullName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    string query = "UPDATE users SET is_active = false WHERE user_id = @user_id";

                    using (var conn = new NpgsqlConnection(dbCon))
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@user_id", selectedEmployee.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    selectedEmployee.Status = "Неактивен";
                    EmployeesDataGrid.Items.Refresh();

                    MessageBox.Show("Сотрудник деактивирован", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StartDatePicker.SelectedDate != null && EndDatePicker.SelectedDate != null)
            {
                if (StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
                {
                    MessageBox.Show("Начальная дата не может быть позже конечной", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                LoadSalesReport();
            }
        }

        private void RefreshAnalytics_Click(object sender, RoutedEventArgs e)
        {
            LoadAnalyticsData();
        }

        private void RefreshEmployees_Click(object sender, RoutedEventArgs e)
        {
            LoadEmployeesData();
        }

        private string HashPassword(string password)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password + "somesalt");
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class SalesReportItem : INotifyPropertyChanged
        {
            private DateTime _saleDate;
            private string _productName;
            private int _quantity;
            private decimal _price;
            private decimal _total;

            public DateTime SaleDate
            {
                get => _saleDate;
                set { _saleDate = value; OnPropertyChanged(); }
            }

            public string ProductName
            {
                get => _productName;
                set { _productName = value; OnPropertyChanged(); }
            }

            public int Quantity
            {
                get => _quantity;
                set { _quantity = value; OnPropertyChanged(); }
            }

            public decimal Price
            {
                get => _price;
                set { _price = value; OnPropertyChanged(); }
            }

            public decimal Total
            {
                get => _total;
                set { _total = value; OnPropertyChanged(); }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}