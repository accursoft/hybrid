using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LocalData;

namespace Client
{
    public partial class MainWindow : Window
    {
        Entities entities;
        CollectionViewSource customerViewSource;
        CollectionViewSource customerOrdersViewSource;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Sync.IsProvisioned()) Sync.Provision();
            customerViewSource = ((CollectionViewSource) (FindResource("customerViewSource")));
            customerOrdersViewSource = ((CollectionViewSource) (FindResource("customerOrdersViewSource")));

            Refresh();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            Save();
        }

        private void Synchronise(object sender, RoutedEventArgs e)
        {
            Save();
            Sync.Synchronize();
            Refresh();
        }

        private void ordersDataGrid_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {
            ((Order) (e.NewItem)).Date = DateTime.Now;
        }

        void Save()
        {
            foreach (var order in entities.Orders.ToList())
                if (order.Customer == null)
                    entities.Orders.Remove(order);

            entities.SaveChanges();
            entities.Dispose();
        }

        void Refresh()
        {
            entities = new Entities();
            entities.Customers.Load(); 
            customerViewSource.Source = entities.Customers.Local;
        }
    }
}