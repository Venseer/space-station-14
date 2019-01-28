using System;

namespace SS14.Client.GodotGlue
{
    public class GodotSignalSubscriber0 : BaseGodotSignalSubscriber
    {
        public event Action Signal;

        public void SignalInvoker()
        {
            try
            {
                Signal?.Invoke();
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                Signal = null;
            }
        }
    }
}
