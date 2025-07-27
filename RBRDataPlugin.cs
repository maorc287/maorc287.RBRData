using SimHub.Plugins;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace maorc287.RBRDataExtPlugin
{
    [PluginDescription("Richard Burns Rally Additional Data Reader")]
    [PluginAuthor("maorc287")]
    [PluginName("RBRDataExt")]
    public class RBRDataPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "RBR Data Extension";

        private const string ProcessName = "RichardBurnsRally_SSE";

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            VirtualMemoryOperation = 0x00000008
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags access, bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("[RBRDataExt] Plugin initialized.");

            PluginManager = pluginManager;

            PluginManager.AddProperty("RBR.OnStage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.TurboPressureBar", GetType(), 0, "");
            PluginManager.AddProperty("RBR.OilPressureBar", GetType(), 0, "");
            PluginManager.AddProperty("RBR.OilTemperature", GetType(), 0, "");
            PluginManager.AddProperty("RBR.EngineStatus", GetType(), 0, "");
            PluginManager.AddProperty("RBR.BatteryVoltage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.BatteryStatus", GetType(), 0, "");
            PluginManager.AddProperty("RBR.LowBatteryWarning", GetType(), 0, "");
        }

        public void End(PluginManager pluginManager) { }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) => null;

        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            var snapshot = ReadRBRData();

            PluginManager.SetPropertyValue("RBR.OnStage", GetType(), snapshot.IsOnStage);
            PluginManager.SetPropertyValue("RBR.TurboPressureBar", GetType(), snapshot.TurboPressureBar);
            PluginManager.SetPropertyValue("RBR.OilPressureBar", GetType(), snapshot.OilPressureBar);
            PluginManager.SetPropertyValue("RBR.OilTemperature", GetType(), snapshot.OilTemperatureC);
            PluginManager.SetPropertyValue("RBR.EngineStatus", GetType(), snapshot.IsEngineOn);
            PluginManager.SetPropertyValue("RBR.BatteryVoltage", GetType(), snapshot.BatteryVoltage);
            PluginManager.SetPropertyValue("RBR.BatteryStatus", GetType(), snapshot.BatteryStatus);
            PluginManager.SetPropertyValue("RBR.LowBatteryWarning", GetType(), snapshot.LowBatteryWarning);
        }

        private uint GetProcessIdByName(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            return (uint)(processes.Length > 0 ? processes[0].Id : 0);
        }

        private static float CalculateOilPressure(float rawBase, float pressureRawPascal)
        {
            float adjustment = BitConverter.ToSingle(BitConverter.GetBytes(0x3f8460fe), 0);

            float pressureBase = (rawBase > 0.02f) ? adjustment : (rawBase * adjustment) / 0.02f;
            float pressureRawBar = pressureRawPascal * 1e-5f;
            return pressureBase + pressureRawBar;
        }

        private RBRData ReadRBRData()
        {
            var snapshot = new RBRData();
            uint pid = GetProcessIdByName(ProcessName);
            if (pid == 0) return snapshot;

            IntPtr hProcess = OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, (int)pid);
            if (hProcess == IntPtr.Zero) return snapshot;

            try
            {
                int carInfoBase = MemoryReader.ReadInt(hProcess, new IntPtr(Offsets.Pointers.CarInfo));
                int carMovBase = MemoryReader.ReadInt(hProcess, new IntPtr(Offsets.Pointers.CarMov));
                int gameModeBase = MemoryReader.ReadInt(hProcess, new IntPtr(Offsets.Pointers.GameMode));

                // Game Mode status 
                int gameMode = 
                    MemoryReader.ReadInt(hProcess, new IntPtr(gameModeBase + Offsets.Pointers.GameModeOffset));
                snapshot.IsOnStage = (gameMode == 1);

                // Early return if not on stage
                if (!snapshot.IsOnStage) return snapshot;

                // Engine status
                float engineStatus = 
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carInfoBase + Offsets.CarInfo.EngineStatus));
                snapshot.IsEngineOn = (engineStatus == 1.0f);

                // Turbo Pressure from Pascal to Bar
                float turboPressurePascal = 
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carInfoBase + Offsets.CarInfo.TurboPressurePascal));
                snapshot.TurboPressureBar = turboPressurePascal / 100000f;

                // Oil Temperature from Kelvin to Celsius
                float oilTempK = 
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.OilTempKelvin));
                snapshot.OilTemperatureC = oilTempK - 273.15f;

                // Oil Pressure Calculation
                float oilRawBase = 
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.OilPressureRawBase));
                float oilRawPascal = 
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.OilPressureRawPascal));
                snapshot.OilPressureBar = CalculateOilPressure(oilRawBase, oilRawPascal);

                // Battery status raw value
                snapshot.BatteryStatus = 
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carInfoBase + Offsets.CarInfo.BatteryWear));

                // Battery Voltage Calculation
                snapshot.BatteryVoltage = snapshot.IsEngineOn
                    ? 14.5f
                    : (snapshot.BatteryStatus / 12) * 2.4f + 10.4f;

                // Low Battery Warning 
                snapshot.LowBatteryWarning =
                   snapshot.BatteryStatus < 10.0f;

            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Warn($"[RBRDataExt] Failed to read memory: {ex.Message}");
            }
            finally
            {
                CloseHandle(hProcess);
            }

            return snapshot;
        }

    }

    internal class RBRData
    {
        public bool IsOnStage { get; set; } = false;
        public bool IsEngineOn { get; set; } = false;
        public float TurboPressureBar { get; set; } = 0.0f;
        public float OilPressureBar { get; set; } = 0.0f;
        public float OilTemperatureC { get; set; } = 0.0f;
        public bool OilPressureWarning { get; set; } = false;
        public float BatteryVoltage { get; set; } = 12.8f;
        public float BatteryStatus { get; set; } = 12.0f;
        public bool LowBatteryWarning { get; set; } = false;

        // Damage values
        public int OilPumpDamage { get; set; } = 1;
        public int WaterPumpDamage { get; set; } = 1;
        public int ElectricSystemDamage { get; set; } = 1;

    }

    internal class Offsets
    {
        public static class CarInfo
        {
            public const int EngineStatus = 0x2B8;
            public const int BatteryWear = 0x2B4;
            public const int TurboPressurePascal = 0x18;
        }

        public static class CarMov
        {
            public const int OilPressureRawBase = 0x139C;
            public const int OilPressureRawPascal = 0x13AC;
            public const int OilTempKelvin = 0x138C;
        }

        public static class Pointers
        {
            public const int CarInfo = 0x0165FC68;
            public const int CarMov = 0x008EF660;
            public const int GameMode = 0x007EAC48;
            public const int GameModeOffset = 0x728;

            public const int BatterySatusBasePointer = 0x0127EA70;
            public static readonly int[] BatteryStatusChain = { 0x9C, 0x430, 0x1D0, 0x8C };
        }
    }
}
