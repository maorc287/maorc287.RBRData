using maorc287.RBRDataExtPlugin;
using System;


namespace maorc287.RBRDataPluginExt
{

    internal static class TelemetryData
    {
        internal static RBRTelemetryData LatestValidTelemetry { get; private set; } = new RBRTelemetryData();

        // Process name for Richard Burns Rally
        private const string RBRProcessName = "RichardBurnsRally_SSE";

        // Base adjustment for oil pressure calculation 
        // BitConverter.ToSingle(BitConverter.GetBytes(0x3f8460fe), 0);
        private const float OilPressureBaseAdjustment = 1.03421f;


        /// Computes the oil pressure from raw values using RBRHUD logic.
        private static float ComputeOilPressure(float rawBase, float pressureRaw)
        {
            float pressureBase = (rawBase > 0.02f) ? OilPressureBaseAdjustment : 
                (rawBase * OilPressureBaseAdjustment) / 0.02f;
            float pressureRawBar = pressureRaw * 1e-5f; 
            return pressureBase + pressureRawBar;
        }

        /// Formats the pressure intercoolerCondition based on the specified unit.
        internal static float FormatPressure(float pressure, string unit)
        {
            if (string.IsNullOrEmpty(unit)) return pressure;

            unit = unit.Trim().ToLowerInvariant();
            switch (unit)
            {
                case "psi":
                    return pressure * 14.5038f;
                case "kpa":
                    return pressure * 100f;
                case "bar":
                default:
                    return pressure; // default is Bar
            }
        }

        /// Formats the temperature intercoolerCondition based on the specified unit.
        internal static float FormatTemperature(float temperature, string unit)
        {
            float tempC = temperature - 273.15f;
            if (tempC < 0f) tempC = 0f;

            if (string.IsNullOrEmpty(unit)) return tempC;

            unit = unit.Trim().ToLowerInvariant();
            switch (unit)
            {
                case "celcius":
                    return tempC;
                case "fahrenheit":
                    return (tempC * 9f / 5f) + 32f;
                case "kelvin":
                    return temperature;
                default:
                    return tempC; // default is Celsius
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

        /// Clamps a intercoolerCondition between 0 and 1.
        private static float Clampers(float val) => val < 0f ? 0f : (val > 1f ? 1f : val);


        /// <summary>
        /// Determines the damage level of the oil pump based on its current health oilPumpCondition.
        /// </summary>
        /// <param name="oilPumpCondition">
        /// The oil pump status as a float:
        /// - 1.0 means the oil pump is fully functional (no damage).
        /// - Values decrease as damage increases.
        /// - 0.0 or below means the oil pump has completely failed.
        /// </param>
        /// <returns>
        /// An integer damage level code:
        /// 1 = Fine (near perfect condition),
        /// 2 = Light damage,
        /// 3 = Medium damage,
        /// 4 = Severe damage (significant loss of performance),
        /// 5 = Lost, Oil pump failure (no longer working).
        /// </returns>
        private static uint OilPumpDamageLevel(float oilPumpCondition)
        {
            if (oilPumpCondition > 0.9f)
                return 1u;

            if (oilPumpCondition > 0.6f)
                return 2u;

            if (oilPumpCondition > 0.2f)
                return 3u;

            if (oilPumpCondition > 0.0f)
                return 4u;

            return 5u;
        }


        /// <summary>
        /// Determines the battery wear level based on the raw battery status batteryCondition normalized to 0.0–1.0 scale.
        /// </summary>
        /// <param name="batteryCondition">
        /// Battery status normalized as a float between 0.0 and 1.0, where 1.0 represents full health.
        /// 
        /// Raw thresholds for in-game warnings (out of 12 in BatterySatauts 0x2B4):
        /// - Below 0.833 (≈ 10.0) turns battery warning light on.
        /// - Below 0.667  (≈ 8.0) starts blinking battery warning light.
        /// - Below 0.5  (≈  6.0) triggers Co-Driver call about battery issues.
        /// </param>
        /// <returns>
        /// An integer wear level code:
        /// 1 = Fine (above ~0.9),
        /// 2 = Light (above ~0.8),
        /// 3 = Medium (above ~0.65),
        /// 4 = Severe (above ~0.5),
        /// 5 = Battery failure or very poor condition (0.5 or below). Not able to start the car.
        /// </returns>
        private static uint BatteryHealthLevel(float batteryCondition)
        {
            if (batteryCondition > 0.9f)
                return 1u;  // Fine

            if (batteryCondition > 0.8f)
                return 2u;  // Light

            if (batteryCondition > 0.65f)
                return 3u;  // Medium

            if (batteryCondition > 0.5f)
                return 4u;  // Severe

            return 5u;      // Lost
        }


        /// <summary>
        /// Categorizes the intercooler damage based on the damage float intercoolerCondition.
        /// Lower values indicate better condition. Higher values indicate more damage.
        /// </summary>
        /// <param name="intercoolerCondition">
        /// The intercooler damage intercoolerCondition (0.0 = no damage, 0.4 = fully damaged).</param>
        /// <returns>
        /// An integer representing damage severity:
        /// 1 = Fine, 2 = Light, 3 = Medium, 4 = Severe, 5 = Lost.
        /// </returns>
        private static uint IntercoolerDamageLevel(float intercoolerCondition)
        {
            if (intercoolerCondition < 0.01f)
                return 1u; // Fine

            if (intercoolerCondition < 0.05f)
                return 2u; // Light

            if (intercoolerCondition < 0.1f)
                return 3u; // Medium

            if (intercoolerCondition < 0.4f)
                return 4u; // Severe

            return 5u; // Lost
        }

        /// <summary>
        /// Categorizes radiator damage based on a damage float radiatorCondition.
        /// Lower values mean healthier radiator. Higher values indicate more severe damage.
        /// </summary>
        /// <param name="radiatorCondition">
        /// Radiator damage radiatorCondition (0.0 = perfect, 0.2 = lost).</param>
        /// <returns>
        /// Damage severity level:
        /// 1 = Fine, 2 = Light, 3 = Medium, 5 = Lost.
        /// </returns>
        private static uint RadiatorDamageLevel(float radiatorCondition)
        {
            if (radiatorCondition < 0.005f)
                return 1u; // Fine

            if (radiatorCondition < 0.03f)
                return 2u; // Light

            if (radiatorCondition < 0.2f)
                return 3u; // Medium

            return 5u; // Lost
        }


        /// Determines if the part is lost or working no intermediate values.
        private static uint PartWorkingStatus(int value)
        {
            return value == 0 ? 5u : 1u;
        }

        /// Determines if the part is lost or working no intermediate values.
        private static uint InversePartWorkingStatus(int value)
        {
            return value == 0 ? 1u : 5u;
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
                if (!rbrData.IsOnStage) 
                { 
                    LatestValidTelemetry.IsOnStage = false;
                    return LatestValidTelemetry; 
                }

                // Read damage values
                int damagePointer =
                    MemoryReader.ReadInt(hProcess, new IntPtr(carMovBase + Offsets.CarMov.DamageStructurePointer));
                if (damagePointer == 0)
                {
                    SimHub.Logging.Current.Warn("[RBRDataExt] Damage structure pointer is null, cannot read damage values.");
                    return LatestValidTelemetry; // Return early if damage structure pointer is null
                }
                rbrData.BatteryWearLevel =
                    BatteryHealthLevel(MemoryReader.ReadFloat(hProcess, new IntPtr(damagePointer + Offsets.Damage.BatteryWearPercent)));
                rbrData.OilPumpDamage =
                    OilPumpDamageLevel(MemoryReader.ReadFloat(hProcess, new IntPtr(damagePointer + Offsets.Damage.OilPump)));
                rbrData.WaterPumpDamage =
                    PartWorkingStatus(MemoryReader.ReadInt(hProcess, new IntPtr(damagePointer + Offsets.Damage.WaterPump)));
                rbrData.ElectricSystemDamage =
                    PartWorkingStatus(MemoryReader.ReadInt(hProcess, new IntPtr(damagePointer + Offsets.Damage.ElectricSystem)));
                rbrData.BrakeCircuitDamage =
                    PartWorkingStatus(MemoryReader.ReadInt(hProcess, new IntPtr(damagePointer + Offsets.Damage.BrakeCircuit)));
                rbrData.GearboxActuatorDamage =
                    PartWorkingStatus(MemoryReader.ReadInt(hProcess, new IntPtr(damagePointer + Offsets.Damage.GearboxActuatorDamage)));
                rbrData.RadiatorDamage =
                    RadiatorDamageLevel(MemoryReader.ReadFloat(hProcess, new IntPtr(damagePointer + Offsets.Damage.RadiatiorDamage)));
                rbrData.IntercoolerDamage = 
                    IntercoolerDamageLevel(MemoryReader.ReadFloat(hProcess, new IntPtr(damagePointer + Offsets.Damage.IntercoolerDamage)));
                rbrData.StarterDamage = 
                    PartWorkingStatus(MemoryReader.ReadInt(hProcess, new IntPtr(damagePointer + Offsets.Damage.StarterDamage)));
                rbrData.HydraulicsDamage = 
                    PartWorkingStatus(MemoryReader.ReadInt(hProcess, new IntPtr(damagePointer + Offsets.Damage.HydraulicsDamage))); 
                rbrData.StarterDamage = 
                    PartWorkingStatus(MemoryReader.ReadInt(hProcess, new IntPtr(damagePointer + Offsets.Damage.StarterDamage)));
                
                rbrData.OilCoolerDamage = 
                    InversePartWorkingStatus(MemoryReader.ReadInt(hProcess, new IntPtr(damagePointer + Offsets.Damage.OilCoolerDamage)));


                // Engine status
                float engineStatus =
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carInfoBase + Offsets.CarInfo.EngineStatus));
                rbrData.IsEngineOn = (engineStatus == 1.0f);

                // Radiator Coolant Temperature is in Kelvin, it will be formatted later 
                rbrData.RadiatorCoolantTemperature = 
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.RadiatorCoolantTemperature));

                // Oil Temperature Value is in kelvin, it will be formatted later 
                rbrData.OilTemperature =
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.OilTempKelvin));

                // Oil Temperature Warning when oil temperature is above 140 Celsius
                rbrData.OilTemperatureWarning = rbrData.OilTemperature > 140.0f + 273.15f; 

                // Oil Pressure Calculation
                float oilRawBase =
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.OilPressureRawBase));
                float oilRaw =
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carMovBase + Offsets.CarMov.OilPressureRaw));
                rbrData.OilPressure = ComputeOilPressure(oilRawBase, oilRaw);

                //Warning for low oil pressure under 0.5 raw Value or if the oil pump is damaged at level 2 or higher
                rbrData.OilPressureWarning = oilRaw < 0.5f | rbrData.OilPumpDamage >= 2;

                //Water Temperature in Celsius
                float waterTemperature =
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carInfoBase + Offsets.CarInfo.WaterTemperatureCelsius));
                // Water Temperature Warning when water temperature is above 120 Celsius
                rbrData.WaterTemperatureWarning = waterTemperature > 120.0f;

                // Battery status raw Value
                //When it goes under 10.0f, the battery light in the game dash turns on,
                //the light starts blinking when it goes under 8.0f. Under 6.0f Co-Driver Call
                rbrData.BatteryStatus =
                    MemoryReader.ReadFloat(hProcess, new IntPtr(carInfoBase + Offsets.CarInfo.BatteryStatus));

                // Battery Voltage Calculation if the engine is on, it will be 14.5V,
                // otherwise it will be calculated based on battery status
                rbrData.BatteryVoltage = rbrData.IsEngineOn
                    ? 14.5f
                    : (rbrData.BatteryStatus * 0.2f) + 10.4f;

                // Low Battery Warning when battery status is below 10 (max Value is 12)
                // or if we use BatteryWearPercent from damage offset it will be below 0.833f (healthy battery is 1.0f)
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

            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Warn($"[RBRDataExt] Failed to read memory: {ex.Message}");
            }
            finally
            {
                MemoryReader.CloseHandle(hProcess);
            }
            // Update the latest valid telemetry data
            LatestValidTelemetry = rbrData; 
            return rbrData;
        }

    }

    /// Class to hold telemetry data read from the game
    internal class RBRTelemetryData
    {
        public bool IsOnStage { get; set; } = false;
        public bool IsEngineOn { get; set; } = false;

        public bool OilPressureWarning { get; set; } = false;
        public bool LowBatteryWarning { get; set; } = false;
        public bool WaterTemperatureWarning { get; set; } = false;
        public bool OilTemperatureWarning { get; set; } = false;

        public float RadiatorCoolantTemperature { get; set; } = 0.0f;
        public float OilPressure { get; set; } = 0.0f;
        public float OilTemperature { get; set; } = 0.0f;
        public float BatteryVoltage { get; set; } = 12.8f;
        public float BatteryStatus { get; set; } = 12.0f;
        public float GroundSpeed { get; set; } = 0.0f;
        public float WheelLock { get; set; } = 0.0f;
        public float WheelSpin { get; set; } = 0.0f;

       // Damage Value, when Value is 5 means part is lost, 1 means part is Fine
        public uint OilPumpDamage { get; set; } = 1;
        public uint BatteryWearLevel { get; set; } = 1;
        public uint WaterPumpDamage { get; set; } = 1;
        public uint ElectricSystemDamage { get; set; } = 1;
        public uint BrakeCircuitDamage { get; set; } = 1;
        public uint IntercoolerDamage { get; set; } = 1;
        public uint RadiatorDamage { get; set; } = 1;
        public uint GearboxActuatorDamage { get; set; } = 1;
        public uint StarterDamage { get; set; } = 1;
        public uint HydraulicsDamage { get; set; } = 1;
        public uint GearboxDamage { get; set; } = 1;
        public uint OilCoolerDamage { get; set; } = 1;
    }

    internal class Offsets
    {
        public static class CarInfo
        {
            //This data is already available in SimHub
            public const int WheelSpeed = 0xC;
            public const int TurboPressure = 0x18;
            public const int WaterTemperatureCelsius = 0x14;
            public const int EngineStatus = 0x2B8;
            public const int BatteryStatus = 0x2B4;
        }

        public static class CarMov
        {
            // New offsets to calculate oil pressure like in RBRHUD 
            public const int OilPressureRawBase = 0x139C;
            public const int OilPressureRaw = 0x13AC;

            // NGP telemetry provides these values already but they can also be used in the Original RBR
            public const int OilTempKelvin = 0x138C;
            public const int RadiatorCoolantTemperature = 0x1170;

            // Offset for the damage structure pointer
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

        //Incomplete offsets for damage structure (still need to be woked on)
        //These offsets are used to read the damage structure from the game memory

        public static class Damage
        {
            // Battery wear level, 1.0f is the best condition gradually decrease to 0.0f when starting the car
            public const int BatteryWearPercent = 0x8C;
            // Oil pump status starts at 1.0f, negative float Value means not working
            public const int OilPump = 0xF0;
            public const int OilCoolerDamage = 0xF4;
            // These Parts start all at Value 1, when Value is 0 means not working and lost
            public const int WaterPump = 0xDC;
            public const int ElectricSystem = 0x1E8;
            public const int BrakeCircuit = 0x80;
            public const int GearboxActuatorDamage = 0x78;

            // 10 Parameters for Gearbox Damage all float values, 1.0f is the best condition,
            //0x48 is the first parameter, 0x6C is the last (4bytes interval)
            //I will write only the first offset, the rest can be calculated
            //Need to create a method to read all 10 parameters and calculate the GearboxDamage
            public const int GearboxDamage = 0x48;

            public const int RadiatiorDamage = 0xE8;
            public const int StarterDamage = 0x7C;
            public const int HydraulicsDamage = 0x90;
            public const int IntercoolerDamage = 0xF8;


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

