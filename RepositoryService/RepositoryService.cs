using System.Collections.Generic;

using Model;
using Repository;

namespace RepositoryService
{
    public class RepositoryService : IRepositoryService
    {
        Repository.Repository repository = new Repository.Repository("name=Server");

        public IEnumerable<Customer> GetCustomers()
        {
            return repository.GetCustomers();
        }

        public int SaveChanges(IEnumerable<Customer> customers, IEnumerable<Order> orders)
        {
            return repository.SaveChanges(customers, orders, true);
        }
    }
}