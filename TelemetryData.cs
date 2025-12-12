
using SimHub.Plugins;

using System;

using System.IO;
using static maorc287.RBRDataExtPlugin.DeltaCalc;
using static maorc287.RBRDataExtPlugin.MemoryReader;
using static maorc287.RBRDataExtPlugin.Offsets;
using static maorc287.RBRDataExtPlugin.TelemetryCalc;
using static maorc287.RBRDataExtPlugin.RBRDataExtPlugin;

using static SimHub.Logging;

namespace maorc287.RBRDataExtPlugin

{
    internal static class TelemetryData
    {
        internal static RBRTelemetryData LatestValidTelemetry { get; private set; } = new RBRTelemetryData();

        // Process name for Richard Burns Rally
        private const string RBRProcessName = "RichardBurnsRally_SSE";

        private const string RBRHUDPluginDllName = "RBRHUD.dll";
        private const string RSFPluginDllName = "Rallysimfans.hu.dll";

        private const string RaceTimeProperty = "DataCorePlugin.GameRawData.RaceTime";
        private const string TrackIdProperty = "DataCorePlugin.GameRawData.TrackId";
        private const string DistanceFromStartProperty = "DataCorePlugin.GameRawData.DistanceFromStart";
        private const string StageStartCountdownProperty = "DataCorePlugin.GameRawData.StageStartCountdown";
        private const string PressureUnitProperty = "DataCorePlugin.GameData.OilPressureUnit";
        private const string TemperatureUnitProperty = "DataCorePlugin.GameData.TemperatureUnit";
        private const string CurrentGame = "DataCorePlugin.CurrentGame";
        private const string GamePaused = "DataCorePlugin.GamePaused";
        private const string GameRunning = "DataCorePlugin.GameRunning";
        private const string IsRunning = "DataCorePlugin.GameRawData.IsRunning";

        private static int _tireType = -1;
        private static string _carSetupName = string.Empty;

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
            { 
                pointerCache.TireModelBasePtr = ReadPointer(hProcess, (IntPtr)Pointers.TireModel);
                _tireType = ReadInt(hProcess, pointerCache.TireModelBasePtr + TireModel.TireType);
            }

            if(!pointerCache.IsCarInfoSetupPointerValid())
            {
                pointerCache.CarInfoSetupBasePtr = ReadPointer(hProcess, (IntPtr)Pointers.CarSetup);
                _carSetupName = Path.GetFileNameWithoutExtension(ReadStringNulTerminated(hProcess, 
                                pointerCache.CarInfoSetupBasePtr + CarSetup.SetupName, 64));
            }

            if (!pointerCache.IsDamagePointerValid())
            {
                IntPtr damageStructPtr = pointerCache.CarMovBasePtr + CarMov.DamageStructurePointer;
                pointerCache.DamageBasePtr = ReadPointer(hProcess, damageStructPtr);
            }
        }

        private static bool OnStage(IntPtr hProcess, RBRTelemetryData rbrData)
        {
            if (!pointerCache.IsGeameModeBaseValid())
            {
                Current.Debug("[RBRDataExt] GameMode pointer invalid");
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

        private static void ReadEngineData(IntPtr hProcess, RBRTelemetryData rbrData, PluginManager pluginManager)
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

        private static void ReadTimingData( RBRTelemetryData rbrData, PluginManager pluginManager)
        {
            // Delta Time Calculation
            if (rbrData.IsOnStage)
            {
                // Data Needed from SimHub Core Plugin For Delta calculation:
                int trackId = (int)pluginManager.GetPropertyValue(TrackIdProperty);
                float travelledDistance = (float)pluginManager.GetPropertyValue(DistanceFromStartProperty);
                float raceTime = (float)pluginManager.GetPropertyValue(RaceTimeProperty);

                if(_lastCarId != rbrData.CarId || _lastStageId != trackId)
                    LoadDeltaData(trackId, rbrData.CarId);

                if (IsReady)
                {
                    float travelledM = travelledDistance - rbrData.StartLine;
                    if (travelledM < 0f) travelledM = 0f;

                    rbrData.DeltaTime = CalculateDelta(travelledM, raceTime);

                    float bestTime = BestTimeSeconds;
                    rbrData.BestTime = (string)FormatTime(bestTime);
                    rbrData.TravelledDistance = travelledM;
                }
            }
        }

        private static void ReadWheelData(IntPtr hProcess, RBRTelemetryData rbrData)
        {

            rbrData.FLWheelSteeringAngle = ReadFloat(hProcess, pointerCache.FLWheelPtr + Wheel.SteeringAngle);
            rbrData.FRWheelSteeringAngle = ReadFloat(hProcess, pointerCache.FRWheelPtr + Wheel.SteeringAngle);

        }

        private static void ReadTiresData(IntPtr hProcess, RBRTelemetryData rbrData)
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

            rbrData.TireType = GetTireType(_tireType);
            rbrData.CarSetup = _carSetupName;
        }

        private static void ReadRSF(RBRTelemetryData rbrData)
        {
            // Only RSF data needed for delta calc
            if (!TryReadFromDll(RSFPluginDllName, Pointers.RSFCarId, out int rsfCarId))
                rsfCarId = 0;
            rbrData.CarId = rsfCarId;

            if (!TryReadFromDll(RSFPluginDllName, Pointers.RSFStartLineDistance, out float rsfStartLine))
                rsfStartLine = 0f;
            rbrData.StartLine = rsfStartLine;
        }

        private static void ReadRBRHUD(RBRTelemetryData rbrData)
        {

            // RBRHUD built-in delta time - optional
            if (!TryReadFromDll(RBRHUDPluginDllName, Pointers.RBRHUDTimeDelta, out float deltaTime))
                deltaTime = 0.0f;
            rbrData.RBRHUDDeltaTime = deltaTime;
        }


        /*private static void ReadOtherData(RBRTelemetryData rbrData)
        {
            /* Gauger Plugin Slip Value - Deprecated
            if (!MemoryReader.TryReadFromDll(GaugerPluginDllName, Pointers.GaugerSlip, out float GaugerPluginSlip))
                GaugerPluginSlip = 0.0f;
            rbrData.GaugerSlip = GaugerPluginSlip;
            

            if (!TryReadFromDll(RBRHUDPluginDllName, Pointers.RBRHUDTimeDelta, out float DeltaTime))
                DeltaTime = 0.0f;
            rbrData.RBRHUDDeltaTime = DeltaTime;

            if (!TryReadFromDll(RSFPluginDllName, Pointers.RSFCarId, out int rsfCarId))
                rsfCarId = 0;
            rbrData.CarId = rsfCarId;

            if (!TryReadFromDll(RSFPluginDllName, Pointers.RSFStartLineDistance, out float rsfStartLine))
                rsfStartLine = 0;
            rbrData.StartLine = rsfStartLine;

        }*/


        private static bool _sessionInitialized = false;
        private static int _notRunningFrames = 0;
        private const int NotRunningThreshold = 10; // number of plugin ticks to treat as not running

        /// Reads telemetry data from the Richard Burns Rally process.
        /// this method accesses the game's memory to retrieve various telemetry values.
        /// as a result, it requires the game to be running and the process to be accessible.
        /// without the game running and on stage, it will return default values.
        internal static RBRTelemetryData ReadTelemetryData(PluginManager pluginManager)
        {
            bool isRBR = (string)pluginManager.GetPropertyValue(CurrentGame) == "RBR";
            bool isGameActive = (bool)pluginManager.GetPropertyValue(IsRunning);

            // Skip when no RBR or RBR is not the Current Game in simhub
            if (!isGameActive || !isRBR)
            {
                _sessionInitialized = false;
                pointerCache.ClearAllCache();
                return new RBRTelemetryData();
            }
            
            IntPtr hProcess = GetProcess();

            if (hProcess == IntPtr.Zero)
            {
                return new RBRTelemetryData();
            }

            var rbrData = new RBRTelemetryData();

            int isGamePaused = (int)pluginManager.GetPropertyValue(GamePaused);
            int isGameRunningRaw = (int)pluginManager.GetPropertyValue(GameRunning); 

            // Debounce GameRunning
            if (isGameRunningRaw == 0)
                _notRunningFrames++;
            else
                _notRunningFrames = 0;

            bool isGameRunning = _notRunningFrames < NotRunningThreshold;

            try
            {
                // If not running (after debounce), end session and return latest
                if (!isGameRunning)
                {
                    _sessionInitialized = false;
                    pointerCache.ClearAllCache();
                    LatestValidTelemetry.IsOnStage = false;
                    return LatestValidTelemetry;
                }

                // Do not update while paused
                if (isGamePaused == 1)
                    return LatestValidTelemetry;

                // Only initialize pointers once per session (while running)
                if (!_sessionInitialized)
                {
                    if (!pointerCache.IsGeameModeBaseValid())
                        pointerCache.GameModeBasePtr = ReadPointer(hProcess, (IntPtr)Pointers.GameMode);

                    if (!pointerCache.IsGeameModeBaseValid())
                        return LatestValidTelemetry;

                    // Use OnStage to decide whether to init the rest
                    if (!OnStage(hProcess, rbrData))
                    {
                        _sessionInitialized = false;
                        return LatestValidTelemetry;
                    }

                    // Now we know we are actually on stage: init the rest once
                    InitializePointers(hProcess);
                    _sessionInitialized = true;
                }

                // From here on, use cached pointers.
                if (!OnStage(hProcess, rbrData))
                {
                    _sessionInitialized = false;
                    return LatestValidTelemetry;
                }

                ReadDamageData(hProcess, rbrData);

                ReadEngineData(hProcess, rbrData, pluginManager);
                ReadBatteryData(hProcess, rbrData);

                ReadTiresData(hProcess, rbrData);

                ReadRBRHUD(rbrData);

                ReadRSF(rbrData);
                ReadTimingData(rbrData, pluginManager);
               

            }
            catch (Exception ex)
            {
                Current.Debug($"[RBRDataExt] Failed to read memory: {ex.Message}");
                return LatestValidTelemetry;
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


            public string TireType { get; set; } = "Unknown";
            public string CarSetup { get; set; } = "Unknown";

            // Steering angles for front wheels.
            public float FLWheelSteeringAngle { get; set; } = 0.0f;
            public float FRWheelSteeringAngle { get; set; } = 0.0f;

            public float FLWheelPercentLateral { get; set; } = 0.0f;
            public float FRWheelPercentLateral { get; set; } = 0.0f;
            public float RLWheelPercentLateral { get; set; } = 0.0f;
            public float RRWheelPercentLateral { get; set; } = 0.0f;

            public float FLWheelExcessLateral { get; set; } = 0.0f;
            public float FRWheelExcessLateral { get; set; } = 0.0f;
            public float RLWheelExcessLateral { get; set; } = 0.0f;
            public float RRWheelExcessLateral { get; set; } = 0.0f;


            public float FLWheelPercentLongitudinal { get; set; } = 0.0f;
            public float FRWheelPercentLongitudinal { get; set; } = 0.0f;
            public float RLWheelPercentLongitudinal { get; set; } = 0.0f;
            public float RRWheelPercentLongitudinal { get; set; } = 0.0f;

            public float FLWheelExcessLongitudinal { get; set; } = 0.0f;
            public float FRWheelExcessLongitudinal { get; set; } = 0.0f;
            public float RLWheelExcessLongitudinal { get; set; } = 0.0f;
            public float RRWheelExcessLongitudinal { get; set; } = 0.0f;


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
            public uint OilCoolerDamage { get; set; } = 1;


            //External data not in Vanilla RBR Memory
            public float RBRHUDDeltaTime { get; set; } = 0.0f;

            public float DeltaTime { get; set; } = 0.0f;
            public string BestTime { get; set; } = "0:00.000";

            public float TravelledDistance { get; set; } = 0.0f;
            public float StartLine { get; set; } = 0.0f;
            public int CarId { get; set; } = 0;

        }
    }
}

