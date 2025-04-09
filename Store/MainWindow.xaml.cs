using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Npgsql;

namespace Store
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string dbCon = "Server=localhost;Port=5432;Database=Store;User Id=postgres;Password=12345";
        private Dictionary<string, Func<Window>> roleWindows;

        public MainWindow()
        {
            InitializeComponent();

            roleWindows = new Dictionary<string, Func<Window>>
            {
                { "Бухгалтер", () => new ButtonWindow() },
                { "Кассир", () => new CashierWindow() },
                { "Администратор", () => new ButtonOneWindow() }
            };
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = loginBox.Text;
            string password = passwordBox.Password;

            try
            {
                using (var conn = new NpgsqlConnection(dbCon))
                {
                    conn.Open();
                    string query = @"SELECT u.login, r.role_name 
                          FROM users u
                          JOIN roles r ON u.role_id = r.role_id
                          WHERE u.login = @login AND u.password = crypt(@password, u.password)";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", username);
                        cmd.Parameters.AddWithValue("@password", password);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                App.CurrentUser = new App.User
                                {
                                    Username = reader["login"].ToString(),
                                    Role = reader["role_name"].ToString()
                                };

                                if (App.CurrentUser.Role == "Кассир")
                                {
                                    var cashierWindow = new CashierWindow();
                                    cashierWindow.Show();
                                }
                                else if  (App.CurrentUser.Role == "Бухгалтер")
                                {
                                    var mainMenu = new ButtonWindow();
                                    mainMenu.Show();
                                }
                                else
                                {
                                    var adm = new ButtonOneWindow();
                                    adm.Show();
                                }

                                this.Close();
                                return;
                            }
                        }
                    }
                }

                ShowError("Неверный логин или пароль");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void ForgotPasswordLink_Click(object sender, RoutedEventArgs e)
        {
            var resetWindow = new PasswordReset();
            resetWindow.Owner = this;
            resetWindow.ShowDialog();
        }
    }
}
