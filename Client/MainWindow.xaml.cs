using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Model;
using Repository;
using RepositoryProxy;

namespace Client
{
    public partial class MainWindow : Window
    {
        //entities
        IRepository proxy;
        ISet<Customer> dirtyCustomers = new HashSet<Customer>();
        ISet<Order> dirtyOrders = new HashSet<Order>();

        //binding sources
        CollectionViewSource customerViewSource;
        CollectionViewSource customerOrdersViewSource;

        MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(bool online) : this()
        {
            proxy = online ? (IRepository)new Online() : (IRepository)new Offline();
            this.online.Content = online ? "Online" : "Offline";
        }

        public bool Synchronising
        {
            set
            {
                Dispatcher.Invoke(new Action(delegate { synchronising.Visibility = value ? Visibility.Visible : Visibility.Hidden; }));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            customerViewSource = ((CollectionViewSource)(FindResource("customerViewSource")));
            customerOrdersViewSource = ((CollectionViewSource)(FindResource("customerOrdersViewSource")));

            var customers = new ObservableCollection<Customer>(proxy.GetCustomers());

            //track changes
            foreach (var customer in customers) {
                ((INotifyPropertyChanged)customer).PropertyChanged += (s, a) => PropertyChanged(dirtyCustomers, (Customer)s, a);
                customer.Orders.CollectionChanged += (s, a) => CollectionChanged(dirtyOrders, a);

                foreach (var order in customer.Orders)
                    ((INotifyPropertyChanged)order).PropertyChanged += (s, a) => PropertyChanged(dirtyOrders, (Order)s, a);
            }

            customers.CollectionChanged += (s, a) => CollectionChanged(dirtyCustomers, a);
            customerViewSource.Source = customers;
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            proxy.SaveChanges(dirtyCustomers, dirtyOrders);

            dirtyCustomers.Clear();
            dirtyOrders.Clear();
        }

        private void ordersDataGrid_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {
            ((Order)(e.NewItem)).Date = DateTime.Now;
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