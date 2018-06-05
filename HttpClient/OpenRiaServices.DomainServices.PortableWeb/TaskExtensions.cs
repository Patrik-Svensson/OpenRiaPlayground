using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.DomainServices.Client.PortableWeb
{
    public static class TaskExtensions
    {
        public abstract class AsyncResultWrapper : IAsyncResult
        {
            public CancellationTokenSource CancellationTokenSource { get; set; }
            public AsyncCallback Callback { get; set; }
            public object State { get; set; }

            protected abstract Task InnerTask { get; }

            #region IAsyncResult implementation forwarded to Task implementation
            object IAsyncResult.AsyncState => State;
            WaitHandle IAsyncResult.AsyncWaitHandle => ((IAsyncResult)InnerTask).AsyncWaitHandle;
            bool IAsyncResult.CompletedSynchronously => ((IAsyncResult)InnerTask).CompletedSynchronously;
            bool IAsyncResult.IsCompleted => InnerTask.IsCompleted;
            #endregion
        }

        public class AsyncResultWrapper<T> : AsyncResultWrapper
        {
            public Task<T> Task { get; set; }
            protected override Task InnerTask => this.Task;

            public T GetResult()
            {
                var task = Task;

                // If opertaion is cancelled 
                if (CancellationTokenSource.IsCancellationRequested)
                    throw new InvalidOperationException(Resources.OperationCancelled);

                return task.GetAwaiter().GetResult();
            }
        }

        public static IAsyncResult BeginApm<T>(Func<CancellationToken, Task<T>> func,
                                    AsyncCallback callback,
                                    object state)
        {
            var cts = new CancellationTokenSource();
            var task = func(cts.Token);

            var asyncResult = new AsyncResultWrapper<T>()
            {
                Callback = callback,
                CancellationTokenSource = cts,
                Task = task,
                State = state,
            };

            if (callback != null)
            {
                task.ContinueWith(t =>
                {
                    callback?.Invoke(asyncResult);
                }, TaskScheduler.Default);
            }

            return asyncResult;
        }

        public static IAsyncResult BeginApm<T>(Func<Task<T>> func,
                            AsyncCallback callback,
                            object state)
        {
            CancellationTokenSource cts = null;
            var task = func();

            var asyncResult = new AsyncResultWrapper<T>()
            {
                Callback = callback,
                CancellationTokenSource = cts,
                Task = task,
                State = state,
            };

            if (callback != null)
            {
                task.ContinueWith(t =>
                {
                    callback?.Invoke(asyncResult);
                }, TaskScheduler.Default);
            }

            return asyncResult;
        }

        internal static void CancelApm<T>(IAsyncResult asyncResult)
        {
            var wrapper = GetAsyncResultWrapper<T>(asyncResult);
            wrapper.CancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Help with providing "EndXXX" method for APM based methods where <see cref="BeginApm{T}(Task{T}, AsyncCallback, object)"/> is used in BeginXXX method
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asyncResult"></param>
        /// <returns></returns>
        public static T EndApm<T>(IAsyncResult asyncResult)
        {
            var wrapper = GetAsyncResultWrapper<T>(asyncResult);
            return wrapper.GetResult();
        }

        private static AsyncResultWrapper<T> GetAsyncResultWrapper<T>(IAsyncResult asyncResult)
        {
            var task = asyncResult as AsyncResultWrapper<T>;
            if (task == null)
                throw new ArgumentException(Resources.WrongAsyncResult, "asyncResult");
            return task;
        }
    }
}
