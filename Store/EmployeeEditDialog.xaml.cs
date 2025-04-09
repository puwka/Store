using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Npgsql;

namespace Store
{
    public partial class EmployeeEditDialog : Window, INotifyPropertyChanged
    {
        private string dbCon = "Server=localhost;Port=5432;Database=Store;User Id=postgres;Password=12345";

        public string DialogTitle => IsEditMode ? "Редактирование сотрудника" : "Новый сотрудник";
        public string FullName { get; set; }
        public string Position { get; set; }
        public string Username { get; set; }
        public bool IsEditMode { get; set; }
        public ObservableCollection<Role> Roles { get; set; }
        public int SelectedRoleId { get; set; }

        public EmployeeEditDialog(Employee employee = null)
        {
            InitializeComponent();
            DataContext = this;
            LoadRoles();

            if (employee != null)
            {
                IsEditMode = true;
                FullName = employee.FullName;
                Position = employee.Position;
                Username = employee.Username;

                var role = Roles.FirstOrDefault(r => r.Name == employee.Role);
                if (role != null)
                {
                    SelectedRoleId = role.Id;
                }
            }
        }

        private void LoadRoles()
        {
            Roles = new ObservableCollection<Role>();

            try
            {
                using (var conn = new NpgsqlConnection(dbCon))
                {
                    conn.Open();
                    string query = "SELECT role_id, role_name FROM roles";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Roles.Add(new Role
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1)
                                });
                            }
                        }
                    }
                }

                var defaultRole = Roles.FirstOrDefault(r => r.Name == "Кассир");
                if (defaultRole != null)
                {
                    SelectedRoleId = defaultRole.Id;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}");
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Position) || string.IsNullOrWhiteSpace(Username))
            {
                MessageBox.Show("Заполните все обязательные поля");
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}