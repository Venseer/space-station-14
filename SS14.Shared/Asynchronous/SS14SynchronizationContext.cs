using System;
using System.Collections.Concurrent;
using System.Threading;
using SS14.Shared.Log;

namespace SS14.Shared.Asynchronous
{
    internal class SS14SynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<(SendOrPostCallback d, object state)> _pending = new BlockingCollection<(SendOrPostCallback, object)>();

        public override void Send(SendOrPostCallback d, object state)
        {
            if (Current != this)
            {
                // Being invoked from another thread?
                // If this not implemented exception starts being a problem I'll fix it but right now I'd rather err on the side of caution,
                // so that if cross thread usage is required I have a test case, instead of a data race.
                throw new NotImplementedException();
            }

            d(state);
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            _pending.Add((d, state));
        }

        public void ProcessPendingTasks()
        {
            while (_pending.TryTake(out var task))
            {
                try
                {
                    task.d(task.state);
                }
                catch (Exception e)
                {
                    Logger.ErrorS("async", "Caught exception in queued callback: {0}", e);
                }
            }
        }
    }
}
