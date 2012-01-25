using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Model;
using SyncClient;
using Proxy;

namespace Client
{
    public partial class MainWindow : Window
    {
        //entities
        IProxy proxy = new Online();
        ISet<Customer> dirtyCustomers = new HashSet<Customer>();
        ISet<Order> dirtyOrders = new HashSet<Order>();

        //binding sources
        CollectionViewSource customerViewSource;
        CollectionViewSource customerOrdersViewSource;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //set up binding sources
            customerViewSource = ((CollectionViewSource)(FindResource("customerViewSource")));
            customerOrdersViewSource = ((CollectionViewSource)(FindResource("customerOrdersViewSource")));

            //bind to data
            if (!Sync.IsProvisioned()) Sync.Provision();
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
            ((Order)(e.NewItem)).Date = DateTime.Now;
        }

        void Save()
        {
            proxy.SaveChanges(dirtyCustomers, dirtyOrders);

            dirtyCustomers.Clear();
            dirtyOrders.Clear();
        }

        void Refresh()
        {
            var customers = new ObservableCollection<Customer>(proxy.GetCustomers());

            //track changes
            foreach (var customer in customers) {
                ((INotifyPropertyChanged)customer).PropertyChanged += (sender, e) => PropertyChanged(dirtyCustomers, (Customer)sender, e);
                customer.Orders.CollectionChanged += (sender, e) => CollectionChanged(dirtyOrders, e);

                foreach (var order in customer.Orders)
                    ((INotifyPropertyChanged)order).PropertyChanged += (sender, e) => PropertyChanged(dirtyOrders, (Order)sender, e);
            }

            customers.CollectionChanged += (sender, e) => CollectionChanged(dirtyCustomers, e);
            customerViewSource.Source = customers;
        }

        static void CollectionChanged<T>(ISet<T> dirty, NotifyCollectionChangedEventArgs e) where T : IObjectWithChangeTracker
        {
            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace)
                foreach (var item in e.OldItems) {
                    T entity = (T)item;
                    if (entity.ChangeTracker.State == ObjectState.Added)
                        dirty.Remove(entity);
                    else
                        dirty.Add(entity.MarkAsDeleted());
                }

            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
                foreach (var item in e.NewItems)
                    dirty.Add(((T)item).MarkAsAdded());
        }

        static void PropertyChanged<T>(ISet<T> dirty, T item, PropertyChangedEventArgs e) where T : IObjectWithChangeTracker
        {
            if (item.ChangeTracker.State == ObjectState.Unchanged)
                dirty.Add(item.MarkAsModified());
        }
    }
}