using System.Collections.Generic;
using System.Linq;

using Model;

namespace Proxy
{
    public class Online : IProxy
    {
        public IEnumerable<Customer> GetCustomers()
        {
            using (var repositoryService = new RepositoryService.RepositoryServiceClient())
                return repositoryService.GetCustomers();
        }

        public int SaveChanges(IEnumerable<Customer> customers, IEnumerable<Order> orders)
        {
            using (var repositoryService = new RepositoryService.RepositoryServiceClient())
                return repositoryService.SaveChanges(customers.ToArray(), orders.ToArray());
        }
    }
}