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

        //If the entities have been deserialised, automatically deleting a customer with orders will confuse the EF
        public int SaveChanges(IEnumerable<Customer> customers, IEnumerable<Order> orders, bool deserialised)
        {
            using (var entities = new Entities(connection)) {

                foreach (var customer in customers)
                    if (deserialised && customer.ChangeTracker.State == ObjectState.Deleted) {
                        entities.Customers.Attach(customer);
                        entities.Customers.DeleteObject(customer);
                    }
                    else
                        entities.Customers.ApplyChanges(customer);

                foreach (var order in orders)
                    entities.Orders.ApplyChanges(order);

                return entities.SaveChanges();
            }
        }
    }
}