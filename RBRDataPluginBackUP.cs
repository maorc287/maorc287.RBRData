
// RBRAdditioinalData - SimHub addon plugin for Richard Burns Rally racing game

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
        private const int OffsetOilPressureRaw = 0x117C;
        private const int OffsetOilPressureRaw1 = 0x139C;
        private const int OffsetOilPressureRaw2 = 0x13AC;

        private const int OffsetRBRHUD_OilPressureBar = 0x8CB5F8;
        private const int OffsetRBRHUD_OilPressurePsi = 0x8CB668;



        private const int OffsetEngineStatus = 0x2B8;

        private const float OilPressureBarConversionFactor = 0.003636088f; //this value should depend on the car maybe.
        private const float BatteryWearConversionFactor = 0.20318f; // don't know about this but seems to correspond to the RBRHUD data
        private const float BatteryVoltBaseValue = 10.37f;

        public uint GetProcessIdByName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                return (uint)processes[0].Id;
            }
            else
            {
                return 0;
            }
        }

        //Thanks Mika-N for this one and his RBRAPI
        public bool IsRaceOn()
        {
            // Get the process ID of the game
            uint processId = GetProcessIdByName(ProcessName);

            // If the process ID is 0, the game is not running
            if (processId == 0)
            {
                return false;
            }

            // Open the process for reading
            IntPtr hProcess = OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, (int)processId);

            // If the process handle is zero, we couldn't open the process
            if (hProcess == IntPtr.Zero)
            {
                return false;
            }

            // Read the memory at the game mode pointer address
            byte[] bufferA = new byte[4];
            ReadProcessMemory(hProcess, new IntPtr(PointerAddressGameMode), bufferA, (uint)bufferA.Length, out _);
            long valueAddressGameMode = BitConverter.ToInt32(bufferA, 0);

            // Calculate the actual game mode address
            IntPtr addressGameMode = new IntPtr(valueAddressGameMode + OffsetGameMode);

            // Read the game status from memory
            byte[] buffer = new byte[Marshal.SizeOf(typeof(float))];
            ReadProcessMemory(hProcess, addressGameMode, buffer, (uint)buffer.Length, out _);
            int GameStatus = BitConverter.ToInt32(buffer, 0);

            CloseHandle(hProcess);
            // Return true if the game status is 1 (race is on), false otherwise
            return GameStatus == 0x01;
        }

        public bool IsEngineOn()
        {
            uint processId = GetProcessIdByName(ProcessName);
            if (processId == 0)
            {
                return false;
            }

            IntPtr hProcess = OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, (int)processId);
            if (hProcess == IntPtr.Zero)
            {
                return false;
            }

            byte[] bufferA = new byte[4];
            bool isReading = ReadProcessMemory(hProcess, new IntPtr(PointerAddressCarInfo), bufferA, (uint)bufferA.Length, out _);
            if (!isReading)
            {
                CloseHandle(hProcess);
                return false;
            }

            long valueAddressCarInfo = BitConverter.ToInt32(bufferA, 0);
            IntPtr addressEngineStatus = new IntPtr(valueAddressCarInfo + OffsetEngineStatus);

            byte[] buffer = new byte[Marshal.SizeOf(typeof(float))];
            ReadProcessMemory(hProcess, addressEngineStatus, buffer, (uint)buffer.Length, out _);
            float engineStatus = BitConverter.ToSingle(buffer, 0);

            CloseHandle(hProcess);
            return engineStatus == 1;
        }

        public float GetRBROilPBarData()
        {
            // Step 1: Get game process ID
            uint processId = GetProcessIdByName(ProcessName);
            if (processId == 0 || !IsEngineOn())
                return 0.0f;

            // Step 2: Open handle to process memory
            IntPtr hProcess = OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, (int)processId);
            if (hProcess == IntPtr.Zero)
                return 0.0f;

            try
            {
                // Step 3: Read base pointer value from the address that stores it
                byte[] pointerBuffer = new byte[4];
                bool pointerRead = ReadProcessMemory(hProcess, new IntPtr(PointerAddressCarMov), pointerBuffer, 4, out _);
                if (!pointerRead)
                    return 0.0f;

                int basePointer = BitConverter.ToInt32(pointerBuffer, 0);

                // Step 4: Compute absolute addresses using your known offsets
                IntPtr addressFloatA = new IntPtr(basePointer + OffsetOilPressureRaw1); 
                IntPtr addressFloatB = new IntPtr(basePointer + OffsetOilPressureRaw2); 

                // Step 5: Read floatA (offset 0x139C)
                byte[] floatBufferA = new byte[4];
                if (!ReadProcessMemory(hProcess, addressFloatA, floatBufferA, 4, out _))
                    return 0.0f;
                float floatA = BitConverter.ToSingle(floatBufferA, 0);

                // Step 6: Read floatB (offset 0x13AC)
                byte[] floatBufferB = new byte[4];
                if (!ReadProcessMemory(hProcess, addressFloatB, floatBufferB, 4, out _))
                    return 0.0f;
                float floatB = BitConverter.ToSingle(floatBufferB, 0);

                uint uVar1 = (floatA > 0.02f) ? 0xFFFFFFFFu : 0u;

                uint part1 = 0x3f8460fe & uVar1;
                uint part2 = (~uVar1) & (uint)((floatA * 1.03421f) / 0.02f);

                float computedValue = floatB * 1e-5f + BitConverter.ToSingle(BitConverter.GetBytes(part1 | part2), 0);

                return computedValue; // Or: return computedValue + 1.0f; if needed
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        public float GetRBROilPBarDataOLD()
        {
            uint processId = GetProcessIdByName(ProcessName);
            if (processId == 0 || !IsEngineOn())
            {
                return 0.0f;
            }

            IntPtr hProcess = OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, (int)processId);
            if (hProcess == IntPtr.Zero)
            {
                return 0;
            }

            byte[] bufferA = new byte[4];
            bool isReading = ReadProcessMemory(hProcess, new IntPtr(PointerAddressCarMov), bufferA, (uint)bufferA.Length, out _);
            if (!isReading)
            {
                CloseHandle(hProcess);
                return 0.0f;
            }

            long valueAddressCarMov = BitConverter.ToInt32(bufferA, 0);
            IntPtr addressOilPressureRaw = new IntPtr(valueAddressCarMov + OffsetOilPressureRaw);

            byte[] buffer = new byte[Marshal.SizeOf(typeof(float))];
            ReadProcessMemory(hProcess, addressOilPressureRaw, buffer, (uint)buffer.Length, out _);
            float oilPressureRaw = BitConverter.ToSingle(buffer, 0);

            CloseHandle(hProcess);
            return oilPressureRaw * OilPressureBarConversionFactor + 1.0f;
        }

        // I Tried to get the voltage with some questionable calculations same with Oil Pressure
        public float GetRBRBatteryVoltage()
        {
            uint processId = GetProcessIdByName(ProcessName);
            if (processId == 0)
                return 12.8f;

            IntPtr hProcess = OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, (int)processId);
            if (hProcess == IntPtr.Zero)
                return 12.8f;

            // Read base pointer for CarInfo
            byte[] bufferA = new byte[4];
            ReadProcessMemory(hProcess, new IntPtr(PointerAddressCarInfo), bufferA, (uint)bufferA.Length, out _);
            int carInfoBase = BitConverter.ToInt32(bufferA, 0);

            float voltage;

            if (!IsEngineOn())
            {
                // When engine off, read raw battery status and convert
                IntPtr batteryStatusAddress = new IntPtr(carInfoBase + OffsetBatteryStatus);
                byte[] batteryBuffer = new byte[4];
                ReadProcessMemory(hProcess, batteryStatusAddress, batteryBuffer, (uint)batteryBuffer.Length, out _);
                float batteryFactor = BitConverter.ToSingle(batteryBuffer, 0);

                voltage = batteryFactor * 2.4f + 10.4f;
            }
            else
            {
                // Engine on: return fixed voltage value
                voltage = 14.5f;
            }

            CloseHandle(hProcess);
            return voltage;
        }

        //The battery data or battery wear value starts at 12 (I don't think it is volts) and diminishes when starting the engine.
        //When it goes under 10, the battery light in the game dash turns on, the light starts blinking when it goes under 8. Under 6 Co-Driver Call
        public float GetRBRBatteryData()
        {
            uint processId = GetProcessIdByName(ProcessName);
            if (processId == 0)
            {
                return 12f;
            }

            IntPtr hProcess = OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, (int)processId);
            if (hProcess == IntPtr.Zero)
            {
                return 12f;
            }

            byte[] bufferA = new byte[4];
            ReadProcessMemory(hProcess, new IntPtr(PointerAddressCarInfo), bufferA, (uint)bufferA.Length, out _);
            long valueAddressCarInfo = BitConverter.ToInt32(bufferA, 0);

            IntPtr addressBatteryWear = new IntPtr(valueAddressCarInfo + OffsetBatteryWear);
            byte[] buffer = new byte[Marshal.SizeOf(typeof(float))];
            ReadProcessMemory(hProcess, addressBatteryWear, buffer, (uint)buffer.Length, out _);
            float batteryWear = BitConverter.ToSingle(buffer, 0);

            CloseHandle(hProcess);
            return batteryWear;
        }


        public PluginManager PluginManager { get; set; }

        /// <summary>
        /// Instance of the current plugin manager
        /// </summary>

        /// <summary>
        /// Gets the left menu icon. Icon must be 24x24 and compatible with black and white display.
        /// </summary>
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);

        /// <summary>
        /// Gets a short plugin title to show in left menu. Return null if you want to use the title as defined in PluginName attribute.
        /// </summary>
        public string LeftMenuTitle => "RBR Additional Data Reader";

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return null;
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
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

        /// <summary>
        /// Called one time per game data update, contains all normalized game data,
        /// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        ///
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        ///
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data">Current game data, including current and previous data frame.</param>
        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            PluginManager.SetPropertyValue("RBR.OnStage", GetType(), IsRaceOn());
            if (IsRaceOn())
            {
                PluginManager.SetPropertyValue("RBR.OilPressureBar", GetType(), GetRBROilPBarData());
                PluginManager.SetPropertyValue("RBR.EngineStatus", GetType(), IsEngineOn());
                PluginManager.SetPropertyValue("RBR.BatteryVoltage", GetType(), GetRBRBatteryVoltage());
                PluginManager.SetPropertyValue("RBR.BatteryWear", GetType(), GetRBRBatteryData());

                // NEW: Read from RBRHUD.dll directly
                PluginManager.SetPropertyValue("RBRHUD.OilPressureBar", GetType(), RBRHUDReader.ReadFloat(OffsetRBRHUD_OilPressureBar));
                PluginManager.SetPropertyValue("RBRHUD.OilPressurePsi", GetType(), RBRHUDReader.ReadFloat(OffsetRBRHUD_OilPressurePsi));

            }
        }
    }
}
