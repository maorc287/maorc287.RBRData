using maorc287.RBRDataExtPlugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace maorc287.RBRDataPluginExt
{

    internal static class TelemetryData
    {
        // Process name for Richard Burns Rally
        private const string RBRProcessName = "RichardBurnsRally_SSE";

        // Base adjustment for oil pressure calculation 
        // BitConverter.ToSingle(BitConverter.GetBytes(0x3f8460fe), 0);
        private const float OilPressureBaseAdjustment = 1.03421f;

        /// Computes the oil pressure based on raw base and pressure values.
        private static float ComputeOilPressure(float rawBase, float pressureRaw)
        {

            float pressureBase = (rawBase > 0.02f) ? OilPressureBaseAdjustment : (rawBase * OilPressureBaseAdjustment) / 0.02f;
            float pressureRawBar = pressureRaw * 1e-5f;
            return pressureBase + pressureRawBar;
        }

        /// Formats the pressure value based on the specified unit.
        internal static float FormatPressure(float pressure, string unit)
        {
            switch (unit)
            {
                case "Psi":
                    return pressure * 14.5038f;
                case "KPa":
                    return pressure * 100f;
                case "Bar":
                    return pressure;
                default:
                    return pressure; // default is Bar
            }
        }

        /// Formats the temperature value based on the specified unit.
        internal static float FormatTemperature(float temperature, string unit)
        {
            switch (unit)
            {
                case "Celcius":
                    return temperature - 273.15f < 0 ? 0 : temperature - 273.15f;
                case "Fahrenheit":
                    return (temperature * 9 / 5) + 32;
                case "Kelvin":
                    return temperature; // already Kelvin
                default:
                    return temperature - 273.15f < 0 ? 0 : temperature - 273.15f; // default is Celsius
            }
        }


        /// Computes the ground speed based on velocity and forward direction vectors.
        private static float ComputeGroundSpeed(
            float velocityX,
            float velocityY,
            float velocityZ,
            float forwardX,
            float forwardY,
            float forwardZ)
        {
            return (velocityX * forwardX +
                    velocityY * forwardY +
                    velocityZ * forwardZ) * -3.559f;
        }

        /// Computes the wheel lock ratio based on ground speed and wheel speed.
        private static float ComputeWheelLockRatio(
            float groundSpeed,
            float wheelSpeed)
        {
            if (groundSpeed < 1.0f)
                return 0.0f;

            float lockRatio = (groundSpeed - wheelSpeed) / groundSpeed;

            return Clampers(lockRatio);
        }

        /// Computes the wheel spin ratio based on ground speed and wheel speed.
        private static float ComputeWheelSpinRatio(
            float groundSpeed,
            float wheelSpeed)
        {
            if (groundSpeed < 1.0f)
                return 0.0f;

            float spinRatio = (wheelSpeed - groundSpeed) / groundSpeed;

            return Clampers(spinRatio);
        }

        /// Clamps a value between 0 and 1.
        private static float Clampers(float val) => val < 0f ? 0f : (val > 1f ? 1f : val);

        private static uint OilPumpDamage(float value)
        {
            return value <= 0.0f ? 5u : 1u;
        }

        private static uint BatteryWearLevel(float value)
        {
            if (value > 0.9f) return 1u;
            if (value > 0.8f) return 2u;
            if (value > 0.65f) return 3u;
            if (value > 0.5f) return 4u;
            return 5u;
        }

        private static uint PartLost(int value)
        {
            return value == 0 ? 5u : 1u;
        }

        /// Reads telemetry data from the Richard Burns Rally process.
        /// this method accesses the game's memory to retrieve various telemetry values.
        /// as a result, it requires the game to be running and the process to be accessible.
        /// without the game running and on stage, it will return default values.
        internal static RBRTelemetryData ReadTelemetryData()
        {
            var rbrData = new RBRTelemetryData();
            uint pid = MemoryReader.GetProcessIdByName(RBRProcessName);
            if (pid == 0) return rbrData;

            IntPtr hProcess = MemoryReader.OpenProcess(MemoryReader.ProcessAccessFlags.VirtualMemoryRead, false, (int)pid);
            if (hProcess == IntPtr.Zero) return rbrData;

            try
            {
                uint carInfoBase = MemoryReader.ReadUInt(hProcess, new IntPtr(Offsets.Pointers.CarInfo));
                uint carMovBase = MemoryReader.ReadUInt(hProcess, new IntPtr(Offsets.Pointers.CarMov));
                uint gameModeBase = MemoryReader.ReadUInt(hProcess, new IntPtr(Offsets.Pointers.GameMode));

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
                rbrData.TurboPressure =
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carInfoBase + Offsets.CarInfo.TurboPressure)) / 100000f;

                // Radiator Coolant Temperature is in Kelvin, it will be formatted later by SimHub
                rbrData.RadiatorCoolantTemperature = 
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.RadiatorCoolantTemperature));

                // Oil Temperature value is in kelvin, it will be formatted letter by SimHub
                rbrData.OilTemperature =
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.OilTempKelvin));

                // Oil Pressure Calculation
                float oilRawBase =
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.OilPressureRawBase));
                float oilRaw =
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.OilPressureRaw));
                rbrData.OilPressure = ComputeOilPressure(oilRawBase, oilRaw);

                //Warning for low oil pressure under 0.5 raw value
                rbrData.OilPressureWarning = oilRaw < 0.5f;

                // Battery status raw value
                rbrData.BatteryStatus =
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carInfoBase + Offsets.CarInfo.BatteryStatus));

                // Battery Voltage Calculation
                rbrData.BatteryVoltage = rbrData.IsEngineOn
                    ? 14.5f
                    : (rbrData.BatteryStatus * 0.2f) + 10.4f;

                // Low Battery Warning when battery status is below 10 (max value is 12)
                rbrData.LowBatteryWarning =
                   rbrData.BatteryStatus < 10.0f;

                //Velocity and Forward Direction Vectors
                float velocityX = MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.VelocityX));
                float velocityY = MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.VelocityY));
                float velocityZ = MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.VelocityZ));
                float fwdX = MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.ForwardX));
                float fwdY = MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.ForwardY));
                float fwdZ = MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.ForwardZ));

                // Calculate ground speed and wheel lock/spin ratios
                float wheelSpeed = 
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carInfoBase + Offsets.CarInfo.WheelSpeed));
                rbrData.GroundSpeed = ComputeGroundSpeed(velocityX, velocityY, velocityZ, fwdX, fwdY, fwdZ);
                rbrData.WheelLock = ComputeWheelLockRatio(rbrData.GroundSpeed, wheelSpeed);
                rbrData.WheelSpin = ComputeWheelSpinRatio(rbrData.GroundSpeed, wheelSpeed);


                // Read damage values
                int damagePointer =
                    MemoryReader.ReadInt(hProcess, new IntPtr(carMovBase + Offsets.CarMov.DamageStructurePointer));

                rbrData.BatteryWearLevel =
                    BatteryWearLevel(MemoryReader.ReadFloat(hProcess, new IntPtr(damagePointer + Offsets.Damage.BatteryWearPercent)));
                rbrData.OilPumpDamage =
                    OilPumpDamage(MemoryReader.ReadFloat(hProcess, new IntPtr(damagePointer + Offsets.Damage.OilPump)));
                rbrData.WaterPumpDamage =
                    PartLost(MemoryReader.ReadInt(hProcess, new IntPtr(damagePointer + Offsets.Damage.WaterPump)));
                rbrData.ElectricSystemDamage =
                    PartLost(MemoryReader.ReadInt(hProcess, new IntPtr(damagePointer + Offsets.Damage.ElectricSystem)));
                rbrData.BrakeCircuitDamage =
                    PartLost(MemoryReader.ReadInt(hProcess, new IntPtr(damagePointer + Offsets.Damage.BrakeCircuit)));

            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Warn($"[RBRDataExt] Failed to read memory: {ex.Message}");
            }
            finally
            {
                MemoryReader.CloseHandle(hProcess);
            }

            return rbrData;
        }

    }

    /// Class to hold telemetry data read from the game
    internal class RBRTelemetryData
    {
        public bool IsOnStage { get; set; } = false;
        public bool IsEngineOn { get; set; } = false;
        public float TurboPressure { get; set; } = 0.0f;
        public float RadiatorCoolantTemperature { get; set; } = 0.0f;
        public float OilPressure { get; set; } = 0.0f;
        public float OilTemperature { get; set; } = 0.0f;
        public float BatteryVoltage { get; set; } = 12.8f;
        public float BatteryStatus { get; set; } = 12.0f;
        public bool OilPressureWarning { get; set; } = false;
        public bool LowBatteryWarning { get; set; } = false;
        public float GroundSpeed { get; set; } = 0.0f;
        public float WheelLock { get; set; } = 0.0f;
        public float WheelSpin { get; set; } = 0.0f;

        // Damage values
        public uint OilPumpDamage { get; set; } = 1;
        public uint BatteryWearLevel { get; set; } = 1;
        public uint WaterPumpDamage { get; set; } = 1;
        public uint ElectricSystemDamage { get; set; } = 1;
        public uint BrakeCircuitDamage { get; set; } = 1;

    }

    internal class Offsets
    {
        public static class CarInfo
        {
            public const int WheelSpeed = 0xC;
            public const int TurboPressure = 0x18;
            public const int WaterTemperatureCelsius = 0x14;
            public const int EngineStatus = 0x2B8;
            public const int BatteryStatus = 0x2B4;
        }

        public static class CarMov
        {
            public const int OilPressureRawBase = 0x139C;
            public const int OilPressureRaw = 0x13AC;
            public const int OilTempKelvin = 0x138C;
            public const int RadiatorCoolantTemperature = 0x1170;
            public const int DamageStructurePointer = 0x620;

            // Velocity vector components
            public const int VelocityX = 0x1C0;
            public const int VelocityY = 0x1C4;
            public const int VelocityZ = 0x1C8;

            // Forward direction vector components
            public const int ForwardX = 0x11C;
            public const int ForwardY = 0x120;
            public const int ForwardZ = 0x124;
        }

        public static class Damage
        {
            public const int BatteryWearPercent = 0x8C;
            public const int OilPump = 0xF0;
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

