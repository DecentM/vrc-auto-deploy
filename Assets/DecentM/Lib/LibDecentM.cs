using UdonSharp;
using UnityEngine;

using DecentM.Permissions;
using DecentM.Tools;

namespace DecentM
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LibDecentM : UdonSharpBehaviour
    {
        [Header("References")]
        public PermissionUtils permissions;
        public Debugging debugging;
        public Scheduling scheduling;

        // deprecated: use DecentM.Collections instead
        public ArrayTools arrayTools;
        public PerformanceGovernor performanceGovernor;
    }
}
