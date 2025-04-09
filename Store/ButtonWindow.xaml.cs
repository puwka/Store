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
using System.Windows.Shapes;

namespace Store
{
    /// <summary>
    /// Логика взаимодействия для ButtonWindow.xaml
    /// </summary>
    public partial class ButtonWindow : Window
    {
        public ButtonWindow()
        {
            InitializeComponent();
        }

        private void CashierButton_Click(object sender, RoutedEventArgs e)
        {
            var cashierWindow = new CashierWindow();
            cashierWindow.Show();
            this.Close();
        }

        private void AccountingButton_Click(object sender, RoutedEventArgs e)
        {
            var accountingWindow = new AdminWindow();
            accountingWindow.Show();
            this.Close();
        }

        private void ForgotPasswordLink_Click(object sender, RoutedEventArgs e)
        {
            var resetWindow = new MainWindow();
            resetWindow.Show();
            this.Close();
        }
    }
}
