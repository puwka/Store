using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Npgsql;

namespace Store
{
    /// <summary>
    /// Логика взаимодействия для PasswordReset.xaml
    /// </summary>
    public partial class PasswordReset : Window
    {
        private DatabaseConnection dbConnection;
        public PasswordReset()
        {
            InitializeComponent();
            dbConnection = new DatabaseConnection();
            NewPasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdatePasswordStrength(NewPasswordBox.Password);
        }

        private void UpdatePasswordStrength(string password)
        {
            int strength = 0;
            var indicators = new[] { StrengthIndicator1, StrengthIndicator2, StrengthIndicator3 };

            foreach (var indicator in indicators)
            {
                indicator.Fill = Brushes.LightGray;
            }

            if (string.IsNullOrEmpty(password))
            {
                StrengthText.Text = "";
                return;
            }

            if (password.Length >= 8) strength++;
            if (password.Length >= 12) strength++;

            if (Regex.IsMatch(password, @"[A-Z]")) strength++;
            if (Regex.IsMatch(password, @"[0-9]")) strength++;
            if (Regex.IsMatch(password, @"[^a-zA-Z0-9]")) strength++;

            for (int i = 0; i < strength && i < indicators.Length; i++)
            {
                if (i == 0)
                    indicators[i].Fill = Brushes.Red;
                else if (i == 1)
                    indicators[i].Fill = Brushes.Orange;
                else
                    indicators[i].Fill = Brushes.Green;
            }

            if (strength <= 1)
                StrengthText.Text = "Слабый";
            else if (strength <= 3)
                StrengthText.Text = "Средний";
            else
                StrengthText.Text = "Сильный";
        }

        private bool ValidatePassword()
        {
            ErrorText.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
            {
                ShowError("Введите новый пароль");
                return false;
            }

            if (NewPasswordBox.Password.Length < 8)
            {
                ShowError("Пароль должен содержать минимум 8 символов");
                return false;
            }

            if (!Regex.IsMatch(NewPasswordBox.Password, @"[A-Z]"))
            {
                ShowError("Пароль должен содержать хотя бы одну заглавную букву");
                return false;
            }

            if (!Regex.IsMatch(NewPasswordBox.Password, @"[0-9]"))
            {
                ShowError("Пароль должен содержать хотя бы одну цифру");
                return false;
            }

            if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                ShowError("Пароли не совпадают");
                return false;
            }

            return true;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidatePassword())
                return;

            string logins = loginBox.Text;
            string passwords = NewPasswordBox.Password;
            if (!string.IsNullOrEmpty(logins) && !string.IsNullOrEmpty(passwords))
            {
                AddUser(logins, passwords);
            }
            Close();
        }

        private void AddUser(string logins, string passwords)
        {
            using (var connection = dbConnection.GetConnection())
            {
                connection.Open();
                string query = @"UPDATE users SET password = crypt(@password, gen_salt('bf')) WHERE login = @login";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@login", logins);
                    command.Parameters.AddWithValue("@password", passwords);
                    command.ExecuteNonQuery();
                }

            }
            MessageBox.Show("Вы успешно сменили пароль!");
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
