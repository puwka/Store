using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Store
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static User CurrentUser { get; set; }

        public class User
        {
            public string Username { get; set; }
            public string Role { get; set; } // "Кассир" или "Бухгалтер"
        }
    }
}
