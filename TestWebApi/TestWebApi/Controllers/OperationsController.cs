
namespace TestWebApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System.Collections.Generic;
    using TestWebApi.Interfaces;
    using TestWebApi.Services;

    [Route("api/[controller]")]
    public class OperationsController : Controller
    {
        private readonly OperationService _operationService;
        private readonly IOperationTransient _transientOperation;
        private readonly IOperationScoped _scopedOperation;
        private readonly IOperationSingleton _singletonOperation;
        private readonly IOperationSingletonInstance _singletonInstanceOperation;

        public OperationsController(OperationService operationService,
            IOperationTransient transientOperation,
            IOperationScoped scopedOperation,
            IOperationSingleton singletonOperation,
            IOperationSingletonInstance singletonInstanceOperation)
        {
            _operationService = operationService;
            _transientOperation = transientOperation;
            _scopedOperation = scopedOperation;
            _singletonOperation = singletonOperation;
            _singletonInstanceOperation = singletonInstanceOperation;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Access list of realized services from this object.
            // But preferably use by constructor injection.
            var services = this.HttpContext.RequestServices;

            IList<IOperation> list = new List<IOperation>();

            // viewbag contains controller-requested services
            list.Add(_transientOperation);
            list.Add(_scopedOperation);
            list.Add(_singletonOperation);
            list.Add(_singletonInstanceOperation);

            list.Add(_operationService.TransientOperation);
            list.Add(_operationService.ScopedOperation);
            list.Add(_operationService.SingletonOperation);
            list.Add(_operationService.SingletonInstanceOperation);

            return Ok(list);
        }
    }
}
