using System.Collections.Generic;
using System.ServiceModel;

using Model;

namespace RepositoryService
{
    [ServiceContract]
    public interface IRepositoryService
    {
        [OperationContract]
        IEnumerable<Customer> GetCustomers();

        [OperationContract]
        int SaveChanges(IEnumerable<Customer> customers, IEnumerable<Order> orders);
    }
}