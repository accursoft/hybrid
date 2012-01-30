﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

using Model;
using Repository;
using RepositoryProxy;

namespace Client
{
    class ViewModel : INotifyPropertyChanged
    {
        IRepository proxy;
        ISet<Customer> dirtyCustomers = new HashSet<Customer>();
        ISet<Order> dirtyOrders = new HashSet<Order>();
        public ObservableCollection<Customer> Customers { get; private set; }

        bool online;
        public bool Online
        {
            set
            {
                proxy = (online = value) ? (IRepository)new Online() : (IRepository)new Offline();

                Customers = new ObservableCollection<Customer>(proxy.GetCustomers());

                //track changes
                foreach (var customer in Customers) {
                    ((INotifyPropertyChanged)customer).PropertyChanged += (s, a) => EntityPropertyChanged(dirtyCustomers, (Customer)s, a);
                    customer.Orders.CollectionChanged += (s, a) => CollectionChanged(dirtyOrders, a);

                    foreach (var order in customer.Orders)
                        ((INotifyPropertyChanged)order).PropertyChanged += (s, a) => EntityPropertyChanged(dirtyOrders, (Order)s, a);
                }

                Customers.CollectionChanged += (s, a) => CollectionChanged(dirtyCustomers, a);

                RaisePropertyChanged("Online");
                RaisePropertyChanged("Customers");
            }
            get { return online; }
        }

        bool synchronising;
        public bool Synchronising {
            get { return synchronising; }
            set { 
                synchronising = value;
                RaisePropertyChanged("Synchronising");
            }
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

        static void EntityPropertyChanged<T>(ISet<T> dirty, T item, PropertyChangedEventArgs e) where T : IObjectWithChangeTracker
        {
            if (item.ChangeTracker.State == ObjectState.Unchanged)
                dirty.Add(item.MarkAsModified());
        }

        public static void InitialiseOrder(Order order)
        {
            order.Date = DateTime.Now;
        }

        public void Save()
        {
            proxy.SaveChanges(dirtyCustomers, dirtyOrders);

            dirtyCustomers.Clear();
            dirtyOrders.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}