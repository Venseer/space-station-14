using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using SS14.Client.ViewVariables.Instances;

namespace SS14.Client.ViewVariables
{
    /// <summary>
    ///     Traits define what behavior an object can have that VV cares about.
    ///     So like, is it enumerable, does it have VV accessible members. That kinda deal.
    ///     These are the "modular" way of extending VV.
    /// </summary>
    internal abstract class ViewVariablesTrait
    {
        protected ViewVariablesInstanceObject Instance { get; private set; }

        public virtual void Initialize(ViewVariablesInstanceObject instance)
        {
            Instance = instance;
        }

        public virtual void Refresh()
        {
        }
    }
}
