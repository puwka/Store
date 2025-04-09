using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Store
{
    public class SalesReportItem
    {
        public DateTime SaleDate { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price;
    }

    public class Employee : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Position { get; set; }
        public string Username { get; set; }
        public string Status { get; set; }
        public DateTime? LastLogin { get; set; }
        private string _role;
        public string Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SalesAnalytics
    {
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
    }

    public class TopProduct
    {
        public string ProductName { get; set; }
        public int SalesCount { get; set; }
    }
}