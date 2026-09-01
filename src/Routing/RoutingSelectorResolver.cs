using System.Collections.Generic;
using System.Linq;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.DM.Routing
{
    /// <summary>
    /// Translates a routing selector that arrived as a named slot key back into the selector object
    /// the device's <c>ExecuteSwitch</c> expects.
    ///
    /// Slots published through <see cref="RoutingPortNamedSlots"/> are keyed by the routing port's
    /// own key, so mobile control's matrix routing sends that key back as the selector - a string -
    /// rather than the port's <see cref="RoutingPort.Selector"/>. Devices whose ports carry Crestron
    /// selector objects have to map the key back to its port before switching, or the switch is
    /// silently dropped.
    ///
    /// The chassis-specific slots (<see cref="DmMatrixInput"/>/<see cref="DmMatrixOutput"/>) are
    /// keyed "matrixInput-N"/"matrixOutput-N" instead of by port key, so DmChassisController resolves
    /// through its own slot dictionaries rather than this helper.
    /// </summary>
    public static class RoutingSelectorResolver
    {
        /// <summary>
        /// Maps a selector that arrived as a port key to that port's Selector, and passes anything
        /// else through unchanged. Use this for devices whose selectors are value types (a slot
        /// number, say) where <see cref="Resolve{T}"/>'s reference-type constraint does not apply.
        /// An unmatched key is returned as-is, so the caller reports it the same way it always has.
        /// </summary>
        public static object ResolveSelector(object selector, IEnumerable<RoutingPort> ports)
        {
            if (!(selector is string key) || ports == null)
                return selector;

            var port = ports.FirstOrDefault(p => p != null && p.Key == key);

            return port != null ? port.Selector : selector;
        }

        /// <summary>
        /// Returns <paramref name="selector"/> when it is already a <typeparamref name="T"/>,
        /// otherwise treats it as a port key and returns that port's Selector. Null when the
        /// selector is null (callers read a null input selector as "clear this output"), matches no
        /// port, or the matched port's Selector is not a <typeparamref name="T"/>.
        /// </summary>
        public static T Resolve<T>(object selector, IEnumerable<RoutingPort> ports) where T : class
        {
            return ResolveSelector(selector, ports) as T;
        }
    }
}
