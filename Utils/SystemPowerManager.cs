using System.Runtime.InteropServices;

namespace GenDash.Utils {
    /// <summary>
    /// Manages system power settings to prevent sleep during long-running operations.
    /// </summary>
    class SystemPowerManager {
        // Windows API to prevent sleep
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);
        
        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;

        /// <summary>
        /// Prevents the system from going to sleep.
        /// </summary>
        public static void PreventSystemSleep() {
            SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
        }

        /// <summary>
        /// Allows the system to sleep normally.
        /// </summary>
        public static void AllowSystemSleep() {
            SetThreadExecutionState(ES_CONTINUOUS);
        }
    }
}
