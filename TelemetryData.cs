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
        private const float OilPressureBaseLimit = 0.02f;

        // Conversion constants
        private const float pascal_Bar = 1e-5f;
        private const float bar_Psi = 14.5038f;
        private const float bar_Kpa = 100f;
        private const float kelvin_Celcius = 273.15f;

        // Cache for pointers to avoid repeated memory reads
        internal static readonly PointerCache pointerCache = new PointerCache();


        /// Computes the oil pressure from raw values using RBRHUD logic.
        private static float ComputeOilPressure(float rawBase, float pressureRaw)
        {
            float pressureBase = (rawBase > OilPressureBaseLimit) ? OilPressureBaseAdjustment :
                (rawBase * OilPressureBaseAdjustment) / OilPressureBaseLimit;
            float pressureRawBar = pressureRaw * pascal_Bar;
            return pressureBase + pressureRawBar;
        }

        /// Formats the pressure value based on the specified unit.
        internal static float FormatPressure(float pressure, string unit)
        {
            if (string.IsNullOrEmpty(unit)) return pressure;

            unit = unit.Trim().ToLowerInvariant();
            switch (unit)
            {
                case "psi":
                    return pressure * bar_Psi;
                case "kpa":
                    return pressure * bar_Kpa;
                case "bar":
                default:
                    return pressure; // default is Bar
            }
        }

        /// Formats the temperature value based on the specified unit.
        internal static float FormatTemperature(float temperature, string unit)
        {
            float tempC = temperature - kelvin_Celcius;
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

        /// Function to compute single whheel rotatinon speed in km/h.
        private static float ComputeWheelSpeed(float wheelRadius, float wheelOmega)
        {
            float wheelSpeed = Math.Abs(wheelRadius * wheelOmega * 3.6f);
            if (wheelSpeed < 1.0f)
                return 0.0f; // Avoid very small values

            return wheelSpeed;
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
        private static float ComputeWheelSlipRatio(
            float groundSpeed,
            float wheelSpeed)
        {
            if (groundSpeed < 1.0f)
                return 0.0f;

            float slipRatio = (wheelSpeed - groundSpeed) / groundSpeed;

            return Clampers(slipRatio);
        }

        /// Clamps a value between 0 and 1.
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
        /// 
        internal static RBRTelemetryData ReadTelemetryData()
        {
            var rbrData = new RBRTelemetryData();

            IntPtr hProcess = MemoryReader.GetOrOpenProcessHandle(RBRProcessName);
            if (hProcess == IntPtr.Zero)
            {
                pointerCache.ClearAll(); // Clear cached pointers if process is not found
                return rbrData;
            }

            try
            {
                if (!pointerCache.IsGeameModeBaseValid())
                    pointerCache.GameModeBasePtr =
                        (IntPtr)MemoryReader.ReadUInt(hProcess, (IntPtr)Offsets.Pointers.GameMode);

                IntPtr gameModeBasePtr = pointerCache.GameModeBasePtr;

                IntPtr gameModePtr = gameModeBasePtr + Offsets.Pointers.GameModeOffset;
                int gameMode = MemoryReader.ReadInt(hProcess, gameModePtr);
                rbrData.IsOnStage = (gameMode == 1);

                if (!rbrData.IsOnStage)
                {
                    pointerCache.ClearAll(); // Clear cached pointers if not on stage
                    LatestValidTelemetry.IsOnStage = false;
                    return LatestValidTelemetry;
                }

                if (!pointerCache.IsCarInfoPointerValid())
                    pointerCache.CarInfoBasePtr = 
                        (IntPtr)MemoryReader.ReadUInt(hProcess, (IntPtr)Offsets.Pointers.CarInfo);

                IntPtr carInfoBasePtr = pointerCache.CarInfoBasePtr;

                if (!pointerCache.IsCarMovPointerValid())
                    pointerCache.CarMovBasePtr = 
                        (IntPtr)MemoryReader.ReadUInt(hProcess, (IntPtr)Offsets.Pointers.CarMov);

                IntPtr carMovBasePtr = pointerCache.CarMovBasePtr;

                if (!pointerCache.IsDamagePointerValid())
                {
                    IntPtr damageStructPtr = carMovBasePtr + Offsets.CarMov.DamageStructurePointer;
                    int damagePointer = MemoryReader.ReadInt(hProcess, damageStructPtr);
                    pointerCache.DamageBasePtr = (IntPtr)damagePointer;
                }

                IntPtr damageBasePtr = pointerCache.DamageBasePtr;

                rbrData.BatteryWearLevel =
                    BatteryHealthLevel(MemoryReader.ReadFloat(hProcess, damageBasePtr 
                    + Offsets.Damage.BatteryWearPercent));
                rbrData.OilPumpDamage =
                    OilPumpDamageLevel(MemoryReader.ReadFloat(hProcess, damageBasePtr 
                    + Offsets.Damage.OilPump));
                rbrData.WaterPumpDamage =
                    PartWorkingStatus(MemoryReader.ReadInt(hProcess, damageBasePtr 
                    + Offsets.Damage.WaterPump));
                rbrData.ElectricSystemDamage =
                    PartWorkingStatus(MemoryReader.ReadInt(hProcess, damageBasePtr 
                    + Offsets.Damage.ElectricSystem));
                rbrData.BrakeCircuitDamage =
                    PartWorkingStatus(MemoryReader.ReadInt(hProcess, damageBasePtr 
                    + Offsets.Damage.BrakeCircuit));
                rbrData.GearboxActuatorDamage =
                    PartWorkingStatus(MemoryReader.ReadInt(hProcess, damageBasePtr 
                    + Offsets.Damage.GearboxActuatorDamage));
                rbrData.RadiatorDamage =
                    RadiatorDamageLevel(MemoryReader.ReadFloat(hProcess, damageBasePtr 
                    + Offsets.Damage.RadiatiorDamage));
                rbrData.IntercoolerDamage =
                    IntercoolerDamageLevel(MemoryReader.ReadFloat(hProcess, damageBasePtr 
                    + Offsets.Damage.IntercoolerDamage));
                rbrData.StarterDamage =
                    PartWorkingStatus(MemoryReader.ReadInt(hProcess, damageBasePtr 
                    + Offsets.Damage.StarterDamage));
                rbrData.HydraulicsDamage =
                    PartWorkingStatus(MemoryReader.ReadInt(hProcess, damageBasePtr 
                    + Offsets.Damage.HydraulicsDamage));
                rbrData.OilCoolerDamage =
                    InversePartWorkingStatus(MemoryReader.ReadInt(hProcess, damageBasePtr 
                    + Offsets.Damage.OilCoolerDamage));

                rbrData.IsEngineOn =
                    MemoryReader.ReadFloat(hProcess, carInfoBasePtr + Offsets.CarInfo.EngineStatus) == 1.0f;

                rbrData.RadiatorCoolantTemperature =
                    MemoryReader.ReadFloat(hProcess, carMovBasePtr + Offsets.CarMov.RadiatorCoolantTemperature);

                rbrData.OilTemperature =
                    MemoryReader.ReadFloat(hProcess, carMovBasePtr + Offsets.CarMov.OilTempKelvin);

                rbrData.OilTemperatureWarning = rbrData.OilTemperature > 140.0f + kelvin_Celcius;

                float oilPRawBase =
                    MemoryReader.ReadFloat(hProcess, carMovBasePtr + Offsets.CarMov.OilPressureRawBase);
                float oilPRaw =
                    MemoryReader.ReadFloat(hProcess, carMovBasePtr + Offsets.CarMov.OilPressureRaw);
                rbrData.OilPressure = ComputeOilPressure(oilPRawBase, oilPRaw);

                rbrData.OilPressureWarning = !rbrData.IsEngineOn
                    | rbrData.OilPressure < 0.2
                    | rbrData.OilPumpDamage >= 2;

                float waterTemperature =
                    MemoryReader.ReadFloat(hProcess, carInfoBasePtr + Offsets.CarInfo.WaterTemperatureCelsius);
                rbrData.WaterTemperatureWarning = waterTemperature > 120.0f;

                rbrData.BatteryStatus =
                    MemoryReader.ReadFloat(hProcess, carInfoBasePtr + Offsets.CarInfo.BatteryStatus);
                rbrData.BatteryVoltage = rbrData.IsEngineOn ? 14.5f //simulate Alternator output when engine is on
                    : (rbrData.BatteryStatus * 0.2f) + 10.4f;
                rbrData.LowBatteryWarning = rbrData.BatteryStatus < 10.0f;

                float velocityX = MemoryReader.ReadFloat(hProcess, carMovBasePtr + Offsets.CarMov.VelocityX);
                float velocityY = MemoryReader.ReadFloat(hProcess, carMovBasePtr + Offsets.CarMov.VelocityY);
                float velocityZ = MemoryReader.ReadFloat(hProcess, carMovBasePtr + Offsets.CarMov.VelocityZ);
                float fwdX = MemoryReader.ReadFloat(hProcess, carMovBasePtr + Offsets.CarMov.ForwardX);
                float fwdY = MemoryReader.ReadFloat(hProcess, carMovBasePtr + Offsets.CarMov.ForwardY);
                float fwdZ = MemoryReader.ReadFloat(hProcess, carMovBasePtr + Offsets.CarMov.ForwardZ);

                float wheelSpeed = MemoryReader.ReadFloat(hProcess, carInfoBasePtr + Offsets.CarInfo.WheelSpeed);
                rbrData.GroundSpeed = ComputeGroundSpeed(velocityX, velocityY, velocityZ, fwdX, fwdY, fwdZ);
                rbrData.WheelLock = ComputeWheelLockRatio(rbrData.GroundSpeed, wheelSpeed);
                rbrData.WheelSlip = ComputeWheelSlipRatio(rbrData.GroundSpeed, wheelSpeed);

                IntPtr FLWheelPointer = MemoryReader.ReadPointer(hProcess, carMovBasePtr + Offsets.CarMov.FLWheel);
                IntPtr FRWheelPointer = MemoryReader.ReadPointer(hProcess, carMovBasePtr + Offsets.CarMov.FRWheel);
                IntPtr RLWheelPointer = MemoryReader.ReadPointer(hProcess, carMovBasePtr + Offsets.CarMov.RLWheel);
                IntPtr RRWheelPointer = MemoryReader.ReadPointer(hProcess, carMovBasePtr + Offsets.CarMov.RRWheel);

                rbrData.FrontLeftWheelSpeed = ComputeWheelSpeed(
                    MemoryReader.ReadFloat(hProcess, FLWheelPointer + Offsets.CarMov.WheelRadiusOffset),
                    MemoryReader.ReadFloat(hProcess, FLWheelPointer + Offsets.CarMov.WheelRotationOffset));
                rbrData.FrontRightWheelSpeed = ComputeWheelSpeed(
                    MemoryReader.ReadFloat(hProcess, FRWheelPointer + Offsets.CarMov.WheelRadiusOffset),
                    MemoryReader.ReadFloat(hProcess, FRWheelPointer + Offsets.CarMov.WheelRotationOffset));
                rbrData.RearLeftWheelSpeed = ComputeWheelSpeed(
                    MemoryReader.ReadFloat(hProcess, RLWheelPointer + Offsets.CarMov.WheelRadiusOffset),
                    MemoryReader.ReadFloat(hProcess, RLWheelPointer + Offsets.CarMov.WheelRotationOffset));
                rbrData.RearRightWheelSpeed = ComputeWheelSpeed(
                    MemoryReader.ReadFloat(hProcess, RRWheelPointer + Offsets.CarMov.WheelRadiusOffset),
                    MemoryReader.ReadFloat(hProcess, RRWheelPointer + Offsets.CarMov.WheelRotationOffset));

            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Warn($"[RBRDataExt] Failed to read memory: {ex.Message}");
            }

            LatestValidTelemetry = rbrData;
            return rbrData;
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
            public float WheelSlip { get; set; } = 0.0f;
            public float FrontLeftWheelSpeed { get; set; } = 0.0f;
            public float FrontRightWheelSpeed { get; set; } = 0.0f;
            public float RearLeftWheelSpeed { get; set; } = 0.0f;
            public float RearRightWheelSpeed { get; set; } = 0.0f;

            // Steering angles for front wheels. This values are probably wrong.
            public float FLWheelSteeringAngle { get; set; } = 0.0f;
            public float FRWheelSteeringAngle { get; set; } = 0.0f;

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
    }
}

