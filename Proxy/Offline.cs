using System.Collections.Generic;

using Repository;
using Model;

namespace Proxy
{
    public class Offline : IRepositoryService
    {
        Repository.Repository repository = new Repository.Repository("name=LocalData");

        public IEnumerable<Customer> GetCustomers()
        {
            return repository.GetCustomers();
        }

        public int SaveChanges(IEnumerable<Customer> customers, IEnumerable<Order> orders)
        {
            return repository.SaveChanges(customers, orders, false);
        }
    }
}