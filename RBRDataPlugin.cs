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
            PluginManager.AddProperty("RBR.OilTemperatureC", GetType(), 0, "");
            PluginManager.AddProperty("RBR.EngineStatus", GetType(), 0, "");
            PluginManager.AddProperty("RBR.BatteryVoltage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.BatteryStatus", GetType(), 0, "");
            PluginManager.AddProperty("RBR.OilPressureWarning", GetType(), 0, "");
            PluginManager.AddProperty("RBR.LowBatteryWarning", GetType(), 0, "");
            PluginManager.AddProperty("RBR.OilPumpDamage", GetType(), 1, "");
            PluginManager.AddProperty("RBR.WaterPumpDamage", GetType(), 1, ""); 
            PluginManager.AddProperty("RBR.ElectricSystemDamage", GetType(), 1, "");
            PluginManager.AddProperty("RBR.BrakeCircuitDamage", GetType(), 1, "");
            PluginManager.AddProperty("RBR.WheelLock", GetType(), 0, "");
            PluginManager.AddProperty("RBR.WheelSlip", GetType(), 0, "");
        }

        public void End(PluginManager pluginManager) { }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) => null;

        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            var rbrData = ReadRBRData();

            PluginManager.SetPropertyValue("RBR.OnStage", GetType(), rbrData.IsOnStage);
            PluginManager.SetPropertyValue("RBR.TurboPressureBar", GetType(), rbrData.TurboPressureBar);
            PluginManager.SetPropertyValue("RBR.OilPressureBar", GetType(), rbrData.OilPressureBar);
            PluginManager.SetPropertyValue("RBR.OilTemperatureC", GetType(), rbrData.OilTemperatureC);
            PluginManager.SetPropertyValue("RBR.EngineStatus", GetType(), rbrData.IsEngineOn);
            PluginManager.SetPropertyValue("RBR.BatteryVoltage", GetType(), rbrData.BatteryVoltage);
            PluginManager.SetPropertyValue("RBR.BatteryStatus", GetType(), rbrData.BatteryStatus);
            PluginManager.SetPropertyValue("RBR.OilPressureWarning", GetType(), rbrData.OilPressureWarning);
            PluginManager.SetPropertyValue("RBR.LowBatteryWarning", GetType(), rbrData.LowBatteryWarning);
            PluginManager.SetPropertyValue("RBR.OilPumpDamage", GetType(), rbrData.OilPumpDamage);
            PluginManager.SetPropertyValue("RBR.WaterPumpDamage", GetType(), rbrData.WaterPumpDamage);
            PluginManager.SetPropertyValue("RBR.ElectricSystemDamage", GetType(), rbrData.ElectricSystemDamage);
            PluginManager.SetPropertyValue("RBR.BrakeCircuitDamage", GetType(), rbrData.BrakeCircuitDamage);
            PluginManager.SetPropertyValue("RBR.WheelLock", GetType(), rbrData.WheelLock);
            PluginManager.SetPropertyValue("RBR.WheelSlip", GetType(), rbrData.WheelSlip);
        }

        private uint GetProcessIdByName(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            return (uint)(processes.Length > 0 ? processes[0].Id : 0);
        }

        private static float CalculateOilPressure(float rawBase, float pressureRaw)
        {
            float adjustment = BitConverter.ToSingle(BitConverter.GetBytes(0x3f8460fe), 0);

            float pressureBase = (rawBase > 0.02f) ? adjustment : (rawBase * adjustment) / 0.02f;
            float pressureRawBar = pressureRaw * 1e-5f;
            return pressureBase + pressureRawBar;
        }

        float CalculateWheelLock(
    float velocityX,
    float velocityY,
    float velocityZ,
    float forwardX,
    float forwardY,
    float forwardZ,
    float wheelSpeed)
        {
            // Compute car speed along forward direction
            float carSpeed = (velocityX * forwardX +
                              velocityY * forwardY +
                              velocityZ * forwardZ) * -3.559f;

            if (carSpeed < 1.0f)
                return 0.0f;

            // Compute lock ratio
            float lockRatio = (carSpeed - wheelSpeed) / carSpeed;

            // Clamp between 0.0 and 1.0
            if (lockRatio < 0.0f) lockRatio = 0.0f;
            if (lockRatio > 1.0f) lockRatio = 1.0f;

            return lockRatio;
        }

        float CalculateWheelSlip(
    float velocityX,
    float velocityY,
    float velocityZ,
    float forwardX,
    float forwardY,
    float forwardZ,
    float wheelSpeed)
        {
            // Compute car speed along forward direction
            float carSpeed = (velocityX * forwardX +
                              velocityY * forwardY +
                              velocityZ * forwardZ) * -3.559f;

            if (carSpeed < 1.0f)
                return 0.0f;

            // Compute spin ratio
            float spinRatio = (wheelSpeed - carSpeed) / carSpeed;

            // Clamp between 0.0 and 1.0
            if (spinRatio < 0.0f) spinRatio = 0.0f;
            if (spinRatio > 1.0f) spinRatio = 1.0f;

            return spinRatio;
        }



        private RBRData ReadRBRData()
        {
            var rbrData = new RBRData();
            uint pid = GetProcessIdByName(ProcessName);
            if (pid == 0) return rbrData;

            IntPtr hProcess = OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, (int)pid);
            if (hProcess == IntPtr.Zero) return rbrData;

            try 
            {
                int carInfoBase = MemoryReader.ReadInt(hProcess, new IntPtr(Offsets.Pointers.CarInfo));
                int carMovBase = MemoryReader.ReadInt(hProcess, new IntPtr(Offsets.Pointers.CarMov));
                int gameModeBase = MemoryReader.ReadInt(hProcess, new IntPtr(Offsets.Pointers.GameMode));

                // Game Mode status 
                int gameMode = 
                    MemoryReader.ReadInt(hProcess, new IntPtr(gameModeBase + Offsets.Pointers.GameModeOffset));
                rbrData.IsOnStage = (gameMode == 1);

                // Early return if not on stage
                if (!rbrData.IsOnStage) return rbrData;

                // Engine status
                float engineStatus = 
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carInfoBase + Offsets.CarInfo.EngineStatus));
                rbrData.IsEngineOn = (engineStatus == 1.0f);

                // Turbo Pressure from Pascal to Bar
                float turboPressurePascal = 
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carInfoBase + Offsets.CarInfo.TurboPressurePascal));
                rbrData.TurboPressureBar = turboPressurePascal / 100000f;

                // Oil Temperature from Kelvin to Celsius
                float oilTempK = 
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.OilTempKelvin));
                rbrData.OilTemperatureC = oilTempK - 273.15f;

                // Oil Pressure Calculation
                float oilRawBase = 
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.OilPressureRawBase));
                float oilRaw = 
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.OilPressureRaw));
                rbrData.OilPressureBar = CalculateOilPressure(oilRawBase, oilRaw);

                rbrData.OilPressureWarning = oilRaw < 0.8f;

                // Battery status raw value
                rbrData.BatteryStatus = 
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carInfoBase + Offsets.CarInfo.BatteryStatus));

                // Battery Voltage Calculation
                rbrData.BatteryVoltage = rbrData.IsEngineOn
                    ? 14.5f
                    : (rbrData.BatteryStatus * 0.2f) + 10.4f;

                // Low Battery Warning 
                rbrData.LowBatteryWarning =
                   rbrData.BatteryStatus < 10.0f;

                if (rbrData.IsEngineOn)
                {
                    float velocityX = MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.VelocityX));
                    float velocityY = MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.VelocityY));
                    float velocityZ = MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.VelocityZ));
                    float forwardX = MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.ForwardX));
                    float forwardY = MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.ForwardY));
                    float forwardZ = MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.ForwardZ));

                    float wheelSpeed = MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarInfo.WheelSpeed));

                    rbrData.WheelLock = 
                        CalculateWheelLock(velocityX, velocityY, velocityZ, forwardX, forwardY, forwardZ, wheelSpeed);
                    rbrData.WheelSlip = 
                        CalculateWheelSlip(velocityX, velocityY, velocityZ, forwardX, forwardY, forwardZ, wheelSpeed);

                }

                // Read damage values
                int damagePointer =
                    MemoryReader.ReadInt(hProcess, new IntPtr(carMovBase + Offsets.CarMov.DamageStructurePointer));
                rbrData.OilPumpDamage = 
                    MemoryReader.ReadInt(hProcess, new IntPtr(damagePointer + Offsets.Damage.OilPump));
                rbrData.WaterPumpDamage = 
                    MemoryReader.ReadInt(hProcess, new IntPtr(damagePointer + Offsets.Damage.WaterPump));
                rbrData.ElectricSystemDamage = 
                    MemoryReader.ReadInt(hProcess, new IntPtr(damagePointer + Offsets.Damage.ElectricSystem));
                rbrData.BrakeCircuitDamage = 
                    MemoryReader.ReadInt(hProcess, new IntPtr(damagePointer + Offsets.Damage.BrakeCircuit));

            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Warn($"[RBRDataExt] Failed to read memory: {ex.Message}");
            }
            finally
            {
                CloseHandle(hProcess);
            }

            return rbrData;
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
        public float WheelLock { get; set; } = 0.0f;
        public float WheelSlip { get; set; } = 0.0f;

        // Damage values
        public int OilPumpDamage { get; set; } = 1;
        public int WaterPumpDamage { get; set; } = 1;
        public int ElectricSystemDamage { get; set; } = 1;
        public int BrakeCircuitDamage { get; set; } = 1;

    }

    internal class Offsets
    {
        public static class CarInfo
        {
            public const int WheelSpeed = 0xC;
            public const int TurboPressurePascal = 0x18;
            public const int WaterTemperatureCelsius = 0x14;
            public const int EngineStatus = 0x2B8;
            public const int BatteryStatus = 0x2B4;
            public const int CoolantTemperatureKelvin = 0x1170;
        }

        public static class CarMov
        {
            public const int OilPressureRawBase = 0x139C;
            public const int OilPressureRaw = 0x13AC;
            public const int OilTempKelvin = 0x138C;
            public const int DamageStructurePointer = 0x620;

            // Velocity vector components
            public const int VelocityX = 0x120;
            public const int VelocityY = 0x124;
            public const int VelocityZ = 0x11C;

            // Forward direction vector components
            public const int ForwardX = 0x1C4;
            public const int ForwardY = 0x1C8;
            public const int ForwardZ = 0x1C0;
        }

        public static class Damage
        {
            public const int BatteryStatusPercent = 0x8C;
            public const int OilPump = 0xE8;
            public const int WaterPump = 0xDC;
            public const int ElectricSystem = 0x1E8;
            public const int BrakeCircuit = 0x80;
        }

        public static class Pointers
        {
            public const int CarInfo = 0x0165FC68;
            public const int CarMov = 0x008EF660;
            public const int GameMode = 0x007EAC48;
            public const int GameModeOffset = 0x728;

        }
    }
}
