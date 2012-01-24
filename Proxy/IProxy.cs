using System.Collections.Generic;

using Model;

namespace Proxy
{
    public interface IProxy
    {
        IEnumerable<Customer> GetCustomers();

        int SaveChanges(IEnumerable<Customer> customers, IEnumerable<Order> orders);
    }
}