// RBRAdditionalData - SimHub addon plugin for Richard Burns Rally

using SimHub.Plugins;
using System;
using System.Windows.Media;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace maorc287.RBRDataPlugin
{
    [PluginDescription("RBR Additional Data Reader")]
    [PluginAuthor("mbp187")]
    [PluginName("RBR Additional Data")]

    public class RBRDataPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public Process p;
        public IntPtr hProcess;

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            VirtualMemoryOperation = 0x00000008
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, uint dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        private const string ProcessName = "RichardBurnsRally_SSE";

        private const int PointerAddressCarInfo = 0x0165FC68;
        private const int PointerAddressCarMov = 0x008EF660;
        private const int PointerAddressGameMode = 0x007EAC48;

        private const int OffsetGameMode = 0x728;
        private const int OffsetBatteryStatus = 0x204DCF4;
        private const int OffsetBatteryWear = 0x2B4;

        private const int OffsetOilPressureRaw1 = 0x139C;
        private const int OffsetOilPressureRaw2 = 0x13AC;

        private const int OffsetRBRHUD_OilPressureBar = 0x8CB5F8;
        private const int OffsetRBRHUD_OilPressurePsi = 0x8CB668;

        private const int OffsetEngineStatus = 0x2B8;


        public static T ReadMemory<T>(IntPtr hProcess, IntPtr address) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            byte[] buffer = new byte[size];
            if (!ReadProcessMemory(hProcess, address, buffer, (uint)size, out _))
            {
                throw new InvalidOperationException($"Failed to read memory at {address:X}");
            }

            if (typeof(T) == typeof(float)) return (T)(object)BitConverter.ToSingle(buffer, 0);
            if (typeof(T) == typeof(int)) return (T)(object)BitConverter.ToInt32(buffer, 0);
            if (typeof(T) == typeof(uint)) return (T)(object)BitConverter.ToUInt32(buffer, 0);

            throw new NotSupportedException($"Unsupported type {typeof(T)}");
        }

        public uint GetProcessIdByName(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            return (uint)(processes.Length > 0 ? processes[0].Id : 0);
        }

        public bool IsRaceOn()
        {
            uint pid = GetProcessIdByName(ProcessName);
            if (pid == 0) return false;

            IntPtr h = OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, (int)pid);
            if (h == IntPtr.Zero) return false;

            try
            {
                int baseAddr = ReadMemory<int>(h, new IntPtr(PointerAddressGameMode));
                int gameMode = ReadMemory<int>(h, new IntPtr(baseAddr + OffsetGameMode));
                return gameMode == 1;
            }
            finally { CloseHandle(h); }
        }

        public bool IsEngineOn()
        {
            uint pid = GetProcessIdByName(ProcessName);
            if (pid == 0) return false;

            IntPtr h = OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, (int)pid);
            if (h == IntPtr.Zero) return false;

            try
            {
                int baseAddr = ReadMemory<int>(h, new IntPtr(PointerAddressCarInfo));
                float status = ReadMemory<float>(h, new IntPtr(baseAddr + OffsetEngineStatus));
                return status == 1.0f;
            }
            finally { CloseHandle(h); }
        }

        public float GetRBROilPBarData()
        {
            uint pid = GetProcessIdByName(ProcessName);
            if (pid == 0 || !IsEngineOn()) return 0f;

            IntPtr h = OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, (int)pid);
            if (h == IntPtr.Zero) return 0f;

            try
            {
                int basePtr = ReadMemory<int>(h, new IntPtr(PointerAddressCarMov));
                float A = ReadMemory<float>(h, new IntPtr(basePtr + OffsetOilPressureRaw1));
                float B = ReadMemory<float>(h, new IntPtr(basePtr + OffsetOilPressureRaw2));

                uint uVar1 = (A > 0.02f) ? 0xFFFFFFFFu : 0u;
                uint part1 = 0x3f8460fe & uVar1;
                uint part2 = (~uVar1) & (uint)((A * 1.03421f) / 0.02f);

                float result = B * 1e-5f + BitConverter.ToSingle(BitConverter.GetBytes(part1 | part2), 0);
                return result;
            }
            finally { CloseHandle(h); }
        }

        public float GetRBRBatteryVoltage()
        {
            uint pid = GetProcessIdByName(ProcessName);
            if (pid == 0) return 12.8f;

            IntPtr h = OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, (int)pid);
            if (h == IntPtr.Zero) return 12.8f;

            try
            {
                int baseAddr = ReadMemory<int>(h, new IntPtr(PointerAddressCarInfo));
                if (!IsEngineOn())
                {
                    float raw = ReadMemory<float>(h, new IntPtr(baseAddr + OffsetBatteryStatus));
                    return raw * 2.4f + 10.4f;
                }
                else
                {
                    return 14.5f;
                }
            }
            finally { CloseHandle(h); }
        }

        public float GetRBRBatteryData()
        {
            uint pid = GetProcessIdByName(ProcessName);
            if (pid == 0) return 12f;

            IntPtr h = OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, (int)pid);
            if (h == IntPtr.Zero) return 12f;

            try
            {
                int baseAddr = ReadMemory<int>(h, new IntPtr(PointerAddressCarInfo));
                return ReadMemory<float>(h, new IntPtr(baseAddr + OffsetBatteryWear));
            }
            finally { CloseHandle(h); }
        }

        public PluginManager PluginManager { get; set; }

        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "RBR Additional Data Reader";

        public void End(PluginManager pluginManager) { }
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) => null;

        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("[RBRAdditioinalData] Starting the plugin");

            PluginManager.AddProperty("RBR.OnStage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.OilPressureBar", GetType(), 0, "");
            PluginManager.AddProperty("RBR.BatteryVoltage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.EngineStatus", GetType(), 0, "");
            PluginManager.AddProperty("RBR.BatteryWear", GetType(), 0, "");
            PluginManager.AddProperty("RBRHUD.OilPressureBar", GetType(), 0, "");
            PluginManager.AddProperty("RBRHUD.OilPressurePsi", GetType(), 0, "");
        }

        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            PluginManager.SetPropertyValue("RBR.OnStage", GetType(), IsRaceOn());
            if (IsRaceOn())
            {
                PluginManager.SetPropertyValue("RBR.OilPressureBar", GetType(), GetRBROilPBarData());
                PluginManager.SetPropertyValue("RBR.EngineStatus", GetType(), IsEngineOn());
                PluginManager.SetPropertyValue("RBR.BatteryVoltage", GetType(), GetRBRBatteryVoltage());
                PluginManager.SetPropertyValue("RBR.BatteryWear", GetType(), GetRBRBatteryData());
                PluginManager.SetPropertyValue("RBRHUD.OilPressureBar", GetType(), RBRHUDReader.ReadFloat(OffsetRBRHUD_OilPressureBar));
                PluginManager.SetPropertyValue("RBRHUD.OilPressurePsi", GetType(), RBRHUDReader.ReadFloat(OffsetRBRHUD_OilPressurePsi));
            }
        }
    }
}
