using Extended.Patterns;
using System;
using System.Runtime.InteropServices;

namespace Hex.Auxiliary
{
    public static class Extern
    {
        #region Public Methods

        // This probably makes more sense as an enum with three values: WindowsDesktop, WindowsLaptop, Unknown
        /// <summary> Tries to determine if the application is running on a windows laptop. </summary>
        /// <returns> If the application is running on windows, an option wrapping a boolean indicating if it is a laptop.
        /// If the application is not running on windows, an empty option. </returns>
        public static Option<bool> IsWindowsLaptop()
        {
            try
            {
                return Option.Some(Extern.GetPwrCapabilities(out var power) && power.LidPresent);
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                return Option.None;
            }
        }

        #endregion

        #region External Methods

        [DllImport("powrprof.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool GetPwrCapabilities(out SYSTEM_POWER_CAPABILITIES systemPowerCapabilites);

        private struct SYSTEM_POWER_CAPABILITIES
        {
            [MarshalAs(UnmanagedType.U1)]
            public bool PowerButtonPresent;
            [MarshalAs(UnmanagedType.U1)]
            public bool SleepButtonPresent;
            [MarshalAs(UnmanagedType.U1)]
            public bool LidPresent;
            [MarshalAs(UnmanagedType.U1)]
            public bool SystemS1;
            [MarshalAs(UnmanagedType.U1)]
            public bool SystemS2;
            [MarshalAs(UnmanagedType.U1)]
            public bool SystemS3;
            [MarshalAs(UnmanagedType.U1)]
            public bool SystemS4;
            [MarshalAs(UnmanagedType.U1)]
            public bool SystemS5;
            [MarshalAs(UnmanagedType.U1)]
            public bool HiberFilePresent;
            [MarshalAs(UnmanagedType.U1)]
            public bool FullWake;
            [MarshalAs(UnmanagedType.U1)]
            public bool VideoDimPresent;
            [MarshalAs(UnmanagedType.U1)]
            public bool ApmPresent;
            [MarshalAs(UnmanagedType.U1)]
            public bool UpsPresent;
            [MarshalAs(UnmanagedType.U1)]
            public bool ThermalControl;
            [MarshalAs(UnmanagedType.U1)]
            public bool ProcessorThrottle;
            public byte ProcessorMinThrottle;
            public byte ProcessorMaxThrottle;    // Also known as ProcessorThrottleScale before Windows XP
            [MarshalAs(UnmanagedType.U1)]
            public bool FastSystemS4;   // Ignore if earlier than Windows XP
            [MarshalAs(UnmanagedType.U1)]
            public bool Hiberboot;  // Ignore if earlier than Windows XP
            [MarshalAs(UnmanagedType.U1)]
            public bool WakeAlarmPresent;   // Ignore if earlier than Windows XP
            [MarshalAs(UnmanagedType.U1)]
            public bool AoAc;   // Ignore if earlier than Windows XP
            [MarshalAs(UnmanagedType.U1)]
            public bool DiskSpinDown;
            public byte HiberFileType;  // Ignore if earlier than Windows 10 (10.0.10240.0)
            [MarshalAs(UnmanagedType.U1)]
            public bool AoAcConnectivitySupported;  // Ignore if earlier than Windows 10 (10.0.10240.0)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            private readonly byte[] spare3;
            [MarshalAs(UnmanagedType.U1)]
            public bool SystemBatteriesPresent;
            [MarshalAs(UnmanagedType.U1)]
            public bool BatteriesAreShortTerm;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public BATTERY_REPORTING_SCALE[] BatteryScale;
            public SYSTEM_POWER_STATE AcOnLineWake;
            public SYSTEM_POWER_STATE SoftLidWake;
            public SYSTEM_POWER_STATE RtcWake;
            public SYSTEM_POWER_STATE MinDeviceWakeState;
            public SYSTEM_POWER_STATE DefaultLowLatencyWake;
        }
        private struct BATTERY_REPORTING_SCALE
        {
            public uint Granularity;
            public uint Capacity;
        }
        private enum SYSTEM_POWER_STATE
        {
            PowerSystemUnspecified = 0,
            PowerSystemWorking = 1,
            PowerSystemSleeping1 = 2,
            PowerSystemSleeping2 = 3,
            PowerSystemSleeping3 = 4,
            PowerSystemHibernate = 5,
            PowerSystemShutdown = 6,
            PowerSystemMaximum = 7
        }

        #endregion
    }
}