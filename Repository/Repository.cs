using System.Collections.Generic;
using System.Linq;

using Model;

namespace Repository
{
    public class Repository
    {
        readonly string connection;

        public Repository(string connection)
        {
            this.connection = connection;
        }

        public IEnumerable<Customer> GetCustomers()
        {
            using (var entities = new Entities(connection))
                return entities.Customers.Include("Orders").ToList();
        }

        public int SaveChanges(IEnumerable<Customer> customers, IEnumerable<Order> orders)
        {
            using (var entities = new Entities(connection)) {
                
                foreach (var customer in customers)
                    entities.Customers.ApplyChanges(customer);

                foreach (var order in orders)
                    entities.Orders.ApplyChanges(order);
                
                return entities.SaveChanges();
            }
        }
    }
}