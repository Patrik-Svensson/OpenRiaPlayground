using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.DomainServices.Client.PortableWeb
{
    public class TaskBasedDomainClient : DomainClient
    {
        protected virtual Task<InvokeCompletedResult> InvokeCoreAsync(InvokeArgs invokeArgs, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("Either override InvokeCoreAsync or BeginInvokeCore, and EndInvokeCore");
        }

        protected virtual Task<QueryCompletedResult> QueryCoreAsync(EntityQuery query, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("Either override InvokeCoreAsync or BeginQueryCore, and EndQueryCore");
        }

        protected virtual Task<SubmitCompletedResult> SubmitCoreAsync(EntityChangeSet changeSet, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("Either override InvokeCoreAsync or BeginQueryCore, and EndQueryCore");
        }


        protected override IAsyncResult BeginInvokeCore(InvokeArgs invokeArgs, AsyncCallback callback, object userState)
        {
            return TaskExtensions.BeginApm(ct => InvokeCoreAsync(invokeArgs, ct), callback, userState);
        }

        protected override InvokeCompletedResult EndInvokeCore(IAsyncResult asyncResult)
        {
            return TaskExtensions.EndApm<InvokeCompletedResult>(asyncResult);
        }

        protected override void CancelInvokeCore(IAsyncResult asyncResult)
        {
            TaskExtensions.CancelApm<InvokeCompletedResult>(asyncResult);
        }


        protected override IAsyncResult BeginQueryCore(EntityQuery query, AsyncCallback callback, object userState)
        {
            return TaskExtensions.BeginApm(ct => QueryCoreAsync(query, ct), callback, userState);
        }
        protected override QueryCompletedResult EndQueryCore(IAsyncResult asyncResult)
        {
            return TaskExtensions.EndApm<QueryCompletedResult>(asyncResult);
        }
        protected override void CancelQueryCore(IAsyncResult asyncResult)
        {
            TaskExtensions.CancelApm<QueryCompletedResult>(asyncResult);
        }

        protected override IAsyncResult BeginSubmitCore(EntityChangeSet changeSet, AsyncCallback callback, object userState)
        {
            return TaskExtensions.BeginApm(ct => SubmitCoreAsync(changeSet, ct), callback, userState);
        }
        protected override SubmitCompletedResult EndSubmitCore(IAsyncResult asyncResult)
        {
            return TaskExtensions.EndApm<SubmitCompletedResult>(asyncResult);
        }
        protected override void CancelSubmitCore(IAsyncResult asyncResult)
        {
            TaskExtensions.CancelApm<SubmitCompletedResult>(asyncResult);
        }
    }
}
