namespace PepperDash.Essentials.DM
{
    /// <summary>
    /// Used to set debug levels for this EPI from one place.
    /// </summary>
    internal static class PepperDashEssentialsDmDebug
    {    
        public static uint Trace { get; private set; }
        public static uint Notice { get; private set; }
        public static uint Verbose { get; private set; }

        public static void ResetDebugLevels()
        {
            Trace = 0;
            Notice = 1;
            Verbose = 2;
        }

        public static void SetDebugLevels(uint level)
        {
            Trace = level;
            Notice = level;
            Verbose = level;
        }
    }
}
