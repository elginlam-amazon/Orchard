using System.Data;
using Microsoft.AspNetCore.Mvc.Filters;
using NHibernate;
using Orchard.Mvc.Filters;

namespace Orchard.Data
{
    public interface ITransactionManager : IDependency
    {
        void Demand();
        void RequireNew();
        void RequireNew(IsolationLevel level);
        void Cancel();

        ISession GetSession();
    }

    public class TransactionFilter : FilterProvider, IExceptionFilter
    {
        private readonly ITransactionManager _transactionManager;

        public TransactionFilter(ITransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        public void OnException(ExceptionContext filterContext)
        {
            _transactionManager.Cancel();
        }
    }

    public class WebApiTransactionFilter : WebApi.Filters.IApiFilterProvider, IExceptionFilter
    {
        private readonly ITransactionManager _transactionManager;

        public WebApiTransactionFilter(ITransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        public void OnException(ExceptionContext actionExecutedContext)
        {
            _transactionManager.Cancel();
        }
    }
}
