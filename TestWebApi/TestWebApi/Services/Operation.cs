using System;
using TestWebApi.Interfaces;

namespace TestWebApi
{
    internal class Operation : IOperationTransient, IOperationScoped, IOperationSingleton, IOperationSingletonInstance
    {
        public Operation()
        {
            this.OperationId = Guid.NewGuid(); 
        }

        public Operation(Guid guid)
        {
            this.OperationId = guid;
        }

        private Guid _operationId;
        public Guid OperationId {
            get {
                //if(_operationId == Guid.Empty)
                //{
                //    return Guid.NewGuid();
                //}
                return _operationId; 
            }
            set {
                _operationId = value;
            }
        }
    }
}