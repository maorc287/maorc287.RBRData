using SimHub;
using SimHub.Plugins;
using System;
using static maorc287.RBRDataExtPlugin.MemoryReader;
using static maorc287.RBRDataExtPlugin.Offsets;
using static maorc287.RBRDataExtPlugin.TelemetryCalc;

namespace maorc287.RBRDataExtPlugin

{
    internal static class TelemetryData
    {
        internal static RBRTelemetryData LatestValidTelemetry { get; private set; } = new RBRTelemetryData();

        // Process name for Richard Burns Rally
        private const string RBRProcessName = "RichardBurnsRally_SSE";

        private const string GaugerPluginDllName = "GaugerPlugin.dll";
        private const string RBRHUDPluginDllName = "RBRHUD.dll";
        private const string RSFPluginDllName = "Rallysimfans.hu.dll";

        private const string RaceTimeProperty = "DataCorePlugin.GameRawData.RaceTime";
        private const string TrackIdProperty = "DataCorePlugin.GameRawData.TrackId";
        private const string DistanceFromStartProperty = "DataCorePlugin.GameRawData.DistanceFromStart";
        private const string StageStartCountdownProperty = "DataCorePlugin.GameRawData.StageStartCountdown";
        private const string PressureUnitProperty = "DataCorePlugin.GameData.OilPressureUnit";
        private const string TemperatureUnitProperty = "DataCorePlugin.GameData.TemperatureUnit";

        // Cache for pointers to avoid repeated memory reads
        internal static readonly PointerCache pointerCache = new PointerCache();

        // ---------------- Helpers ---------------- //
        private static IntPtr GetProcess()
        {
            var hProcess = GetOrOpenProcessHandle(RBRProcessName);
            if (hProcess == IntPtr.Zero)
                pointerCache.ClearAllCache();
            return hProcess;
        }

        private static void InitializePointers(IntPtr hProcess)
        {
            if (!pointerCache.IsGeameModeBaseValid())
                pointerCache.GameModeBasePtr = ReadPointer(hProcess, (IntPtr)Pointers.GameMode);

            if (!pointerCache.IsCarInfoPointerValid())
                pointerCache.CarInfoBasePtr = ReadPointer(hProcess, (IntPtr)Pointers.CarInfo);

            if (!pointerCache.IsCarMovPointerValid())
            {
                pointerCache.CarMovBasePtr = ReadPointer(hProcess, (IntPtr)Pointers.CarMov);

                pointerCache.FLWheelPtr = ReadPointer(hProcess, pointerCache.CarMovBasePtr + CarMov.FLWheel);
                pointerCache.FRWheelPtr = ReadPointer(hProcess, pointerCache.CarMovBasePtr + CarMov.FRWheel);
                pointerCache.RLWheelPtr = ReadPointer(hProcess, pointerCache.CarMovBasePtr + CarMov.RLWheel);
                pointerCache.RRWheelPtr = ReadPointer(hProcess, pointerCache.CarMovBasePtr + CarMov.RRWheel);
            }

            if (!pointerCache.IsTireModelPointerValid())
                pointerCache.TireModelBasePtr = ReadPointer(hProcess, (IntPtr)Pointers.TireModel);

            if (!pointerCache.IsDamagePointerValid())
            {
                IntPtr damageStructPtr = pointerCache.CarMovBasePtr + CarMov.DamageStructurePointer;
                pointerCache.DamageBasePtr = ReadPointer(hProcess, damageStructPtr);
            }
        }

        private static bool IsOnStage(IntPtr hProcess, RBRTelemetryData rbrData)
        {
            if (!pointerCache.IsGeameModeBaseValid() || pointerCache.GameModeBasePtr == IntPtr.Zero)
            {
                Logging.Current.Debug("[RBRDataExt] GameMode pointer invalid");
                return false;
            }

            int gameMode = ReadInt(hProcess, pointerCache.GameModeBasePtr + Pointers.GameModeOffset);
            rbrData.IsOnStage = (gameMode == 1);

            if (!rbrData.IsOnStage)
            {
                LatestValidTelemetry.IsOnStage = false;
                pointerCache.ClearAllCache();
                return false;
            }
            return true;
        }

        private static void ReadDamageData(IntPtr hProcess, RBRTelemetryData rbrData)
        {
            rbrData.BatteryWearLevel = BatteryHealthLevel(ReadFloat(hProcess,
                pointerCache.DamageBasePtr + Damage.BatteryWearPercent));
            rbrData.OilPumpDamage = OilPumpDamageLevel(ReadFloat(hProcess,
                pointerCache.DamageBasePtr + Damage.OilPump));
            rbrData.WaterPumpDamage = PartWorkingStatus(ReadInt(hProcess,
                pointerCache.DamageBasePtr + Damage.WaterPump));
            rbrData.ElectricSystemDamage = PartWorkingStatus(ReadInt(hProcess,
                pointerCache.DamageBasePtr + Damage.ElectricSystem));
            rbrData.BrakeCircuitDamage = PartWorkingStatus(ReadInt(hProcess,
                pointerCache.DamageBasePtr + Damage.BrakeCircuit));
            rbrData.GearboxActuatorDamage = PartWorkingStatus(ReadInt(hProcess,
                pointerCache.DamageBasePtr + Damage.GearboxActuator));
            rbrData.RadiatorDamage = RadiatorDamageLevel(ReadFloat(hProcess,
                pointerCache.DamageBasePtr + Damage.Radiator));
            rbrData.IntercoolerDamage = IntercoolerDamageLevel(ReadFloat(hProcess,
                pointerCache.DamageBasePtr + Damage.Intercooler));
            rbrData.StarterDamage = PartWorkingStatus(ReadInt(hProcess,
                pointerCache.DamageBasePtr + Damage.Starter));
            rbrData.HydraulicsDamage = PartWorkingStatus(ReadInt(hProcess,
                pointerCache.DamageBasePtr + Damage.Hydraulics));
            rbrData.OilCoolerDamage = InversePartWorkingStatus(ReadInt(hProcess,
                pointerCache.DamageBasePtr + Damage.OilCooler));
        }

        private static void ReadEngineAndFluids(IntPtr hProcess, RBRTelemetryData rbrData, PluginManager pluginManager)
        {
            string pressureUnit = (string)pluginManager.GetPropertyValue(PressureUnitProperty);
            string temperatureUnit = (string)pluginManager.GetPropertyValue(TemperatureUnitProperty);

            rbrData.IsEngineOn = ReadFloat(hProcess, pointerCache.CarInfoBasePtr + CarInfo.EngineStatus) == 1.0f;

            float radiatorCoolantTemperature = ReadFloat(hProcess,
                pointerCache.CarMovBasePtr + CarMov.RadiatorCoolantTemperature);

            float oilTemperature = ReadFloat(hProcess, pointerCache.CarMovBasePtr + CarMov.OilTempKelvin);

            rbrData.RadiatorCoolantTemperature = FormatTemperature(radiatorCoolantTemperature, temperatureUnit);
            rbrData.OilTemperature = FormatTemperature(oilTemperature, temperatureUnit);

            rbrData.OilTemperatureWarning = oilTemperature > 140.0f + kelvin_Celcius;

            //Pressure calculation
            float oilPRawBase = ReadFloat(hProcess, pointerCache.CarMovBasePtr + CarMov.OilPressureRawBase);
            float oilPRaw = ReadFloat(hProcess, pointerCache.CarMovBasePtr + CarMov.OilPressureRaw);
            float oilPressure = ComputeOilPressure(oilPRawBase, oilPRaw);
            rbrData.OilPressure = FormatPressure(oilPressure, pressureUnit);

            rbrData.OilPressureWarning = !rbrData.IsEngineOn
                || rbrData.OilPressure < 0.2
                || rbrData.OilPumpDamage >= 2;

            float waterTemperature = ReadFloat(hProcess, pointerCache.CarInfoBasePtr + CarInfo.WaterTemperatureCelsius);
            rbrData.WaterTemperatureWarning = waterTemperature > 120.0f;
        }

        private static void ReadBatteryData(IntPtr hProcess, RBRTelemetryData rbrData)
        {
            rbrData.BatteryStatus = ReadFloat(hProcess, pointerCache.CarInfoBasePtr + CarInfo.BatteryStatus);
            rbrData.BatteryVoltage = rbrData.IsEngineOn ? 14.5f
                : (rbrData.BatteryStatus * 0.2f) + 10.4f;
            rbrData.LowBatteryWarning = rbrData.BatteryStatus < 10.0f;
        }

        private static void ReadTimingData(RBRTelemetryData rbrData, PluginManager pluginManager)
        {
            // Delta Time Calculation
            if (rbrData.IsOnStage)
            {
                // Data Needed from SimHub Core Plugin For Delta calculation:
                float countdownTime = (float)pluginManager.GetPropertyValue(StageStartCountdownProperty);
                int trackId = (int)pluginManager.GetPropertyValue(TrackIdProperty);
                float travelledDistance = (float)pluginManager.GetPropertyValue(DistanceFromStartProperty);
                float raceTime = (float)pluginManager.GetPropertyValue(RaceTimeProperty);

                DeltaCalc.LoadDeltaData(trackId, rbrData.CarId, countdownTime);

                if (DeltaCalc.IsReady)
                {
                    float travelledM = travelledDistance - rbrData.StartLine;
                    if (travelledM < 0f) travelledM = 0f;

                    rbrData.DeltaTime = DeltaCalc.CalculateDelta(travelledM, raceTime);
                    float bestTime = DeltaCalc.BestTimeSeconds;
                    rbrData.BestTime = (string)FormatTime(bestTime);
                }
            }
        }

        private static void ReadWheelData(IntPtr hProcess, RBRTelemetryData rbrData)
        {
            rbrData.FLWheelSpeed = ComputeWheelSpeed(
                ReadFloat(hProcess, pointerCache.FLWheelPtr + Wheel.WheelRadiusOffset),
                ReadFloat(hProcess, pointerCache.FLWheelPtr + Wheel.WheelRotationOffset));

            rbrData.FRWheelSpeed = ComputeWheelSpeed(
                ReadFloat(hProcess, pointerCache.FRWheelPtr + Wheel.WheelRadiusOffset),
                ReadFloat(hProcess, pointerCache.FRWheelPtr + Wheel.WheelRotationOffset));

            rbrData.RLWheelSpeed = ComputeWheelSpeed(
                ReadFloat(hProcess, pointerCache.RLWheelPtr + Wheel.WheelRadiusOffset),
                ReadFloat(hProcess, pointerCache.RLWheelPtr + Wheel.WheelRotationOffset));

            rbrData.RRWheelSpeed = ComputeWheelSpeed(
                ReadFloat(hProcess, pointerCache.RRWheelPtr + Wheel.WheelRadiusOffset),
                ReadFloat(hProcess, pointerCache.RRWheelPtr + Wheel.WheelRotationOffset));

            rbrData.FLWheelSteeringAngle = ReadFloat(hProcess, pointerCache.FLWheelPtr + Wheel.SteeringAngle);
            rbrData.FRWheelSteeringAngle = ReadFloat(hProcess, pointerCache.FRWheelPtr + Wheel.SteeringAngle);

            int tireType = ReadInt(hProcess, pointerCache.TireModelBasePtr + TireModel.TireType);

            rbrData.CurrentTireType = GetTireType(tireType);

        }

        private static void ReadSlipAndTireModel(IntPtr hProcess, RBRTelemetryData rbrData)
        {

            float lateralGripFL = ReadFloat(hProcess, pointerCache.FLWheelPtr + Wheel.LateralGripValue);
            float lateralGripFR = ReadFloat(hProcess, pointerCache.FRWheelPtr + Wheel.LateralGripValue);
            float lateralGripRL = ReadFloat(hProcess, pointerCache.RLWheelPtr + Wheel.LateralGripValue);
            float lateralGripRR = ReadFloat(hProcess, pointerCache.RRWheelPtr + Wheel.LateralGripValue);

            float longitudinalGripFL = ReadFloat(hProcess, pointerCache.FLWheelPtr + Wheel.LongitudinalGripValue);
            float longitudinalGripFR = ReadFloat(hProcess, pointerCache.FRWheelPtr + Wheel.LongitudinalGripValue);
            float longitudinalGripRL = ReadFloat(hProcess, pointerCache.RLWheelPtr + Wheel.LongitudinalGripValue);
            float longitudinalGripRR = ReadFloat(hProcess, pointerCache.RRWheelPtr + Wheel.LongitudinalGripValue);


            GetGripLevel(lateralGripFL, longitudinalGripFL,
                out float excessAlphaFL, out float excessKappaFL, out float percentAlphaFL, out float percentKappaFL);
            GetGripLevel(lateralGripFR, longitudinalGripFR,
                out float excessAlphaFR, out float excessKappaFR, out float percentAlphaFR, out float percentKappaFR);
            GetGripLevel(lateralGripRL, longitudinalGripRL,
                out float excessAlphaRL, out float excessKappaRL, out float percentAlphaRL, out float percentKappaRL);
            GetGripLevel(lateralGripRR, longitudinalGripRR,
                out float excessAlphaRR, out float excessKappaRR, out float percentAlphaRR, out float percentKappaRR);

            rbrData.FLWheelExcessLateral = excessAlphaFL;
            rbrData.FRWheelExcessLateral = excessAlphaFR;
            rbrData.RLWheelExcessLateral = excessAlphaRL;
            rbrData.RRWheelExcessLateral = excessAlphaRR;

            rbrData.FLWheelPercentLateral = percentAlphaFL;
            rbrData.FRWheelPercentLateral = percentAlphaFR;
            rbrData.RLWheelPercentLateral = percentAlphaRL;
            rbrData.RRWheelPercentLateral = percentAlphaRR;

            rbrData.FLWheelExcessLongitudinal = excessKappaFL;
            rbrData.FRWheelExcessLongitudinal = excessKappaFR;
            rbrData.RLWheelExcessLongitudinal = excessKappaRL;
            rbrData.RRWheelExcessLongitudinal = excessKappaRR;

            rbrData.FLWheelPercentLongitudinal = percentKappaFL;
            rbrData.FRWheelPercentLongitudinal = percentKappaFR;
            rbrData.RLWheelPercentLongitudinal = percentKappaRL;
            rbrData.RRWheelPercentLongitudinal = percentKappaRR;
        }

        private static void ReadOtherData(RBRTelemetryData rbrData)
        {
            if (!MemoryReader.TryReadFromDll(GaugerPluginDllName, Pointers.GaugerSlip, out float GaugerPluginSlip))
                GaugerPluginSlip = 0.0f;
            rbrData.GaugerSlip = GaugerPluginSlip;

            if (!MemoryReader.TryReadFromDll(RBRHUDPluginDllName, Pointers.RBRHUDTimeDelta, out float DeltaTime))
                DeltaTime = 0.0f;
            rbrData.RBRHUDDeltaTime = DeltaTime;

            if (!MemoryReader.TryReadFromDll(RSFPluginDllName, Pointers.RSFCarId, out int rsfCarId))
                rsfCarId = 0;
            rbrData.CarId = rsfCarId;

            if (!MemoryReader.TryReadFromDll(RSFPluginDllName, Pointers.RSFStartLineDistance, out float rsfStartLine))
                rsfStartLine = 0;
            rbrData.StartLine = rsfStartLine;

        }


        private static DateTime _lastTelemetryRead = DateTime.MinValue;
        private static readonly TimeSpan NoProcessInterval = TimeSpan.FromSeconds(5);   // Only when no RBR
        private static bool _rbrRunning = false;

        /// Reads telemetry data from the Richard Burns Rally process.
        /// this method accesses the game's memory to retrieve various telemetry values.
        /// as a result, it requires the game to be running and the process to be accessible.
        /// without the game running and on stage, it will return default values.
        internal static RBRTelemetryData ReadTelemetryData(PluginManager pluginManager)
        {

            // Skip only when no RBR (non-blocking rate limit)
            if (!_rbrRunning && DateTime.Now - _lastTelemetryRead < NoProcessInterval)
                return LatestValidTelemetry;

            _lastTelemetryRead = DateTime.Now;

            var rbrData = new RBRTelemetryData();
            IntPtr hProcess = GetProcess();
            if (hProcess == IntPtr.Zero)
            {
                _rbrRunning = false; // Extend interval only when no RBR
                return rbrData;
            }

            _rbrRunning = true; // Full speed when RBR running - no interval

            try
            {
                InitializePointers(hProcess);

                if (!IsOnStage(hProcess, rbrData))
                {
                    return LatestValidTelemetry;
                }

                ReadDamageData(hProcess, rbrData);
                ReadEngineAndFluids(hProcess, rbrData, pluginManager);
                ReadBatteryData(hProcess, rbrData);
                ReadSlipAndTireModel(hProcess, rbrData);
                ReadOtherData(rbrData);
                ReadTimingData(rbrData, pluginManager);

            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Debug($"[RBRDataExt] Failed to read memory: {ex.Message}");
            }

            LatestValidTelemetry = rbrData;
            return rbrData;
        }

        /// Class to hold telemetry data read from the game
        internal class RBRTelemetryData
        {
            public bool IsOnStage { get; set; } = false;
            public bool IsEngineOn { get; set; } = false;

            public float DeltaTime { get; set; } = 0.0f;
            public string BestTime { get; set; } = "0:00.000";

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
            public float GaugerSlip { get; set; } = 0.0f;
            public float FLWheelSpeed { get; set; } = 0.0f;
            public float FRWheelSpeed { get; set; } = 0.0f;
            public float RLWheelSpeed { get; set; } = 0.0f;
            public float RRWheelSpeed { get; set; } = 0.0f;
            public string CurrentTireType { get; set; } = "Unknown";

            // Steering angles for front wheels.
            public float FLWheelSteeringAngle { get; set; } = 0.0f;
            public float FRWheelSteeringAngle { get; set; } = 0.0f;

            public float FLWheelSlipAngle { get; set; } = 0.0f;
            public float FRWheelSlipAngle { get; set; } = 0.0f;
            public float RLWheelSlipAngle { get; set; } = 0.0f;
            public float RRWheelSlipAngle { get; set; } = 0.0f;

            public float FLWheelSlipRatio { get; set; } = 0.0f;
            public float FRWheelSlipRatio { get; set; } = 0.0f;
            public float RLWheelSlipRatio { get; set; } = 0.0f;
            public float RRWheelSlipRatio { get; set; } = 0.0f;

            public float FLWheelPercentLateral { get; set; } = 0.0f;
            public float FRWheelPercentLateral { get; set; } = 0.0f;
            public float RLWheelPercentLateral { get; set; } = 0.0f;
            public float RRWheelPercentLateral { get; set; } = 0.0f;

            public float FLWheelExcessLateral { get; set; } = 0.0f;
            public float FRWheelExcessLateral { get; set; } = 0.0f;
            public float RLWheelExcessLateral { get; set; } = 0.0f;
            public float RRWheelExcessLateral { get; set; } = 0.0f;

            public float FLWheelLimitSlipAngleRad { get; set; } = 0.0f;
            public float FRWheelLimitSlipAngleRad { get; set; } = 0.0f;
            public float RLWheelLimitSlipAngleRad { get; set; } = 0.0f;
            public float RRWheelLimitSlipAngleRad { get; set; } = 0.0f;

            public float FLWheelPercentLongitudinal { get; set; } = 0.0f;
            public float FRWheelPercentLongitudinal { get; set; } = 0.0f;
            public float RLWheelPercentLongitudinal { get; set; } = 0.0f;
            public float RRWheelPercentLongitudinal { get; set; } = 0.0f;

            public float FLWheelExcessLongitudinal { get; set; } = 0.0f;
            public float FRWheelExcessLongitudinal { get; set; } = 0.0f;
            public float RLWheelExcessLongitudinal { get; set; } = 0.0f;
            public float RRWheelExcessLongitudinal { get; set; } = 0.0f;

            public float FLWheelLimitSlipRatio { get; set; } = 0.0f;
            public float FRWheelLimitSlipRatio { get; set; } = 0.0f;
            public float RLWheelLimitSlipRatio { get; set; } = 0.0f;
            public float RRWheelLimitSlipRatio { get; set; } = 0.0f;

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

            public float RBRHUDDeltaTime { get; set; } = 0.0f;

            public float TravelledDistance { get; set; } = 0.0f;
            public float StartLine { get; set; } = 0.0f;
            public int CarId { get; set; } = 0;
        }
    }
}

