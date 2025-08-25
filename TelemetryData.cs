using maorc287.RBRDataExtPlugin;
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


        // Cache for pointers to avoid repeated memory reads
        internal static readonly PointerCache pointerCache = new PointerCache();

   

        /// Reads telemetry data from the Richard Burns Rally process.
        /// this method accesses the game's memory to retrieve various telemetry values.
        /// as a result, it requires the game to be running and the process to be accessible.
        /// without the game running and on stage, it will return default values.
        /// 
        internal static RBRTelemetryData ReadTelemetryData()
        {
            var rbrData = new RBRTelemetryData();

            IntPtr hProcess = GetOrOpenProcessHandle(RBRProcessName);
            if (hProcess == IntPtr.Zero)
            {
                pointerCache.ClearAllCache(); // Clear cached pointers if process is not found
                return rbrData;
            }

            try
            {
                if (!pointerCache.IsGeameModeBaseValid())
                    pointerCache.GameModeBasePtr =
                        (IntPtr)ReadUInt(hProcess, (IntPtr)Pointers.GameMode);

                IntPtr gameModeBasePtr = pointerCache.GameModeBasePtr;

                IntPtr gameModePtr = gameModeBasePtr + Pointers.GameModeOffset;
                int gameMode = ReadInt(hProcess, gameModePtr);
                rbrData.IsOnStage = (gameMode == 1);

                if (!rbrData.IsOnStage)
                {
                    pointerCache.ClearAllCache(); // Clear cached pointers if not on stage
                    LatestValidTelemetry.IsOnStage = false;
                    return LatestValidTelemetry;
                }

                if (!pointerCache.IsCarInfoPointerValid())
                    pointerCache.CarInfoBasePtr =
                        (IntPtr)ReadUInt(hProcess, (IntPtr)Pointers.CarInfo);

                IntPtr carInfoBasePtr = pointerCache.CarInfoBasePtr;

                if (!pointerCache.IsCarMovPointerValid())
                {

                    pointerCache.CarMovBasePtr =
                        ReadPointer(hProcess, (IntPtr)Pointers.CarMov);

                    pointerCache.FLWheelPtr = 
                        ReadPointer(hProcess, pointerCache.CarMovBasePtr + CarMov.FLWheel);
                    pointerCache.FRWheelPtr = 
                        ReadPointer(hProcess, pointerCache.CarMovBasePtr + CarMov.FRWheel);
                    pointerCache.RLWheelPtr = 
                        ReadPointer(hProcess, pointerCache.CarMovBasePtr + CarMov.RLWheel);
                    pointerCache.RRWheelPtr = 
                        ReadPointer(hProcess, pointerCache.CarMovBasePtr + CarMov.RRWheel);

                }

                if (!pointerCache.IsTiresPhysicsPointerValid())
                {
                    pointerCache.TiresPhysicsBasePtr =
                        (IntPtr)ReadUInt(hProcess, (IntPtr) Pointers.TiresPhysics);
                }

                IntPtr carMovBasePtr = pointerCache.CarMovBasePtr;

                IntPtr FLWheelPointer = pointerCache.FLWheelPtr;
                IntPtr FRWheelPointer = pointerCache.FRWheelPtr;
                IntPtr RLWheelPointer = pointerCache.RLWheelPtr;
                IntPtr RRWheelPointer = pointerCache.RRWheelPtr;

                IntPtr tiresPhysicsBasePtr = pointerCache.TiresPhysicsBasePtr;

                if (!pointerCache.IsDamagePointerValid())
                {
                    IntPtr damageStructPtr = carMovBasePtr + CarMov.DamageStructurePointer;
                    int damagePointer = ReadInt(hProcess, damageStructPtr);
                    pointerCache.DamageBasePtr = (IntPtr)damagePointer;
                }

                IntPtr damageBasePtr = pointerCache.DamageBasePtr;

                rbrData.BatteryWearLevel =
                    BatteryHealthLevel(ReadFloat(hProcess, damageBasePtr
                    + Damage.BatteryWearPercent));
                rbrData.OilPumpDamage =
                    OilPumpDamageLevel(ReadFloat(hProcess, damageBasePtr
                    + Damage.OilPump));
                rbrData.WaterPumpDamage =
                    PartWorkingStatus(ReadInt(hProcess, damageBasePtr
                    + Damage.WaterPump));
                rbrData.ElectricSystemDamage =
                    PartWorkingStatus(ReadInt(hProcess, damageBasePtr
                    + Damage.ElectricSystem));
                rbrData.BrakeCircuitDamage =
                    PartWorkingStatus(ReadInt(hProcess, damageBasePtr
                    + Damage.BrakeCircuit));
                rbrData.GearboxActuatorDamage =
                    PartWorkingStatus(ReadInt(hProcess, damageBasePtr
                    + Damage.GearboxActuatorDamage));
                rbrData.RadiatorDamage =
                    RadiatorDamageLevel(ReadFloat(hProcess, damageBasePtr
                    + Damage.RadiatiorDamage));
                rbrData.IntercoolerDamage =
                    IntercoolerDamageLevel(ReadFloat(hProcess, damageBasePtr
                    + Damage.IntercoolerDamage));
                rbrData.StarterDamage =
                    PartWorkingStatus(ReadInt(hProcess, damageBasePtr
                    + Damage.StarterDamage));
                rbrData.HydraulicsDamage =
                    PartWorkingStatus(ReadInt(hProcess, damageBasePtr
                    + Damage.HydraulicsDamage));
                rbrData.OilCoolerDamage =
                    InversePartWorkingStatus(ReadInt(hProcess, damageBasePtr
                    + Damage.OilCoolerDamage));

                rbrData.IsEngineOn =
                    ReadFloat(hProcess, carInfoBasePtr + CarInfo.EngineStatus) == 1.0f;

                rbrData.RadiatorCoolantTemperature =
                    ReadFloat(hProcess, carMovBasePtr + CarMov.RadiatorCoolantTemperature);

                rbrData.OilTemperature =
                    ReadFloat(hProcess, carMovBasePtr + CarMov.OilTempKelvin);

                rbrData.OilTemperatureWarning = rbrData.OilTemperature > 140.0f + kelvin_Celcius;

                float oilPRawBase =
                    ReadFloat(hProcess, carMovBasePtr + CarMov.OilPressureRawBase);
                float oilPRaw =
                    ReadFloat(hProcess, carMovBasePtr + CarMov.OilPressureRaw);
                rbrData.OilPressure = ComputeOilPressure(oilPRawBase, oilPRaw);

                rbrData.OilPressureWarning = !rbrData.IsEngineOn
                    | rbrData.OilPressure < 0.2
                    | rbrData.OilPumpDamage >= 2;

                float waterTemperature =
                    ReadFloat(hProcess, carInfoBasePtr + CarInfo.WaterTemperatureCelsius);
                rbrData.WaterTemperatureWarning = waterTemperature > 120.0f;

                rbrData.BatteryStatus =
                    ReadFloat(hProcess, carInfoBasePtr + CarInfo.BatteryStatus);
                rbrData.BatteryVoltage = rbrData.IsEngineOn ? 14.5f //simulate Alternator output when engine is on
                    : (rbrData.BatteryStatus * 0.2f) + 10.4f;
                rbrData.LowBatteryWarning = rbrData.BatteryStatus < 10.0f;

                float velX = ReadFloat(hProcess, carMovBasePtr + CarMov.VelocityX);
                float velY = ReadFloat(hProcess, carMovBasePtr + CarMov.VelocityY);
                float velZ = ReadFloat(hProcess, carMovBasePtr + CarMov.VelocityZ);
                float fwdX = ReadFloat(hProcess, carMovBasePtr + CarMov.ForwardX);
                float fwdY = ReadFloat(hProcess, carMovBasePtr + CarMov.ForwardY);
                float fwdZ = ReadFloat(hProcess, carMovBasePtr + CarMov.ForwardZ);
                float accX = ReadFloat(hProcess, carMovBasePtr + CarMov.AccelerationX);
                float accY = ReadFloat(hProcess, carMovBasePtr + CarMov.AccelerationY);
                float accZ = ReadFloat(hProcess, carMovBasePtr + CarMov.AccelerationZ);

                float flLoad = ReadFloat(hProcess, FLWheelPointer + Wheel.Load);
                float frLoad = ReadFloat(hProcess, FRWheelPointer + Wheel.Load);
                float rlLoad = ReadFloat(hProcess, RLWheelPointer + Wheel.Load);
                float rrLoad = ReadFloat(hProcess, RRWheelPointer + Wheel.Load);

                float[] slipCornerPk = 
                    ReadFloatArray(hProcess,tiresPhysicsBasePtr + TiresPhysics.SlpPkCrn, 8);
                float[] slipTractionPk =
                    ReadFloatArray(hProcess, tiresPhysicsBasePtr + TiresPhysics.SlpPkTrct, 8);
                float[] cornerStiff = 
                    ReadFloatArray(hProcess, tiresPhysicsBasePtr + TiresPhysics.CrnStf, 8);
                float[] tractionStiff = 
                    ReadFloatArray(hProcess, tiresPhysicsBasePtr + TiresPhysics.TrctStf, 8);

                float wheelSpeed = ReadFloat(hProcess, carInfoBasePtr + CarInfo.WheelSpeed);
                rbrData.GroundSpeed = ComputeGroundSpeed(velX, velY, velZ, fwdX, fwdY, fwdZ);
                rbrData.WheelLock = ComputeWheelLockRatio(rbrData.GroundSpeed, wheelSpeed);
                rbrData.WheelSlip = ComputeWheelSpinRatio(rbrData.GroundSpeed, wheelSpeed);


                rbrData.FLWheelSpeed = ComputeWheelSpeed(
                    ReadFloat(hProcess, FLWheelPointer + Wheel.WheelRadiusOffset),
                    ReadFloat(hProcess, FLWheelPointer + Wheel.WheelRotationOffset));
                rbrData.FRWheelSpeed = ComputeWheelSpeed(
                    ReadFloat(hProcess, FRWheelPointer + Wheel.WheelRadiusOffset),
                    ReadFloat(hProcess, FRWheelPointer + Wheel.WheelRotationOffset));
                rbrData.RLWheelSpeed = ComputeWheelSpeed(
                    ReadFloat(hProcess, RLWheelPointer + Wheel.WheelRadiusOffset),
                    ReadFloat(hProcess, RLWheelPointer + Wheel.WheelRotationOffset));
                rbrData.RRWheelSpeed = ComputeWheelSpeed(
                    ReadFloat(hProcess, RRWheelPointer + Wheel.WheelRadiusOffset),
                    ReadFloat(hProcess, RRWheelPointer + Wheel.WheelRotationOffset));

                rbrData.FLWheelSteeringAngle =
                    ReadFloat(hProcess, FLWheelPointer + Wheel.FrontWheelSteeringAngle);
                rbrData.FRWheelSteeringAngle =
                    ReadFloat(hProcess, FRWheelPointer + Wheel.FrontWheelSteeringAngle);

                //timing calculations not used currently
                /*
                float currentTimestamp = MemoryReader.ReadFloat(hProcess, carInfoBasePtr + Offsets.CarInfo.Timer);
                
                if (currentTimestamp < 0.016f || currentTimestamp < prevTimestamp)
                {
                    prevTimestamp = currentTimestamp;
                }

                //float dt = currentTimestamp + 0.001f - prevTimestamp;
                */

                float lateralFL = ReadFloat(hProcess, FLWheelPointer + Wheel.LateralSpeedOffset);
                float longitudinalFL = ReadFloat(hProcess, FLWheelPointer + Wheel.LongitudinalSpeedOffset);
                float lateralFR = ReadFloat(hProcess, FRWheelPointer + Wheel.LateralSpeedOffset);
                float longitudinalFR = ReadFloat(hProcess, FRWheelPointer + Wheel.LongitudinalSpeedOffset);
                float lateralRL = ReadFloat(hProcess, RLWheelPointer + Wheel.LateralSpeedOffset);
                float longitudinalRL = ReadFloat(hProcess, RLWheelPointer + Wheel.LongitudinalSpeedOffset);
                float lateralRR = ReadFloat(hProcess, RRWheelPointer + Wheel.LateralSpeedOffset);


                rbrData.FLWheelSlipAngle = GetSlipAngleRad(longitudinalFL, lateralFL, rbrData.FLWheelSteeringAngle);
                rbrData.FRWheelSlipAngle = GetSlipAngleRad(longitudinalRL, lateralFR, rbrData.FRWheelSteeringAngle);

                rbrData.RLWheelSlipAngle = GetSlipAngleRad(longitudinalRL, lateralRL);
                rbrData.RRWheelSlipAngle = GetSlipAngleRad(longitudinalRL, lateralRR);

                rbrData.FLWheelMaxSlipAngle =
                GetNormalizedSlip(rbrData.FLWheelSlipAngle, flLoad, cornerStiff, slipCornerPk);
                rbrData.FRWheelMaxSlipAngle =
                GetNormalizedSlip(rbrData.FRWheelSlipAngle, frLoad, cornerStiff, slipCornerPk);
                rbrData.RLWheelMaxSlipAngle =
                GetNormalizedSlip(rbrData.RLWheelSlipAngle, rlLoad, cornerStiff, slipCornerPk);
                rbrData.RRWheelMaxSlipAngle =
                GetNormalizedSlip(rbrData.RRWheelSlipAngle, rrLoad, cornerStiff, slipCornerPk);

                rbrData.FLWheelSlipRatio = ComputeWheelSlipRatio(rbrData.GroundSpeed, rbrData.FLWheelSpeed);
                rbrData.FRWheelSlipRatio = ComputeWheelSlipRatio(rbrData.GroundSpeed, rbrData.FRWheelSpeed);
                rbrData.RLWheelSlipRatio = ComputeWheelSlipRatio(rbrData.GroundSpeed, rbrData.RLWheelSpeed);
                rbrData.RRWheelSlipRatio = ComputeWheelSlipRatio(rbrData.GroundSpeed, rbrData.RRWheelSpeed);

                // Read GaugerPlugin.dll memory for lock slip value
                if (!MemoryReader.TryReadFromDll("GaugerPlugin.dll", 0x7ADFC, out float GaugerPluginLockSlip))
                {
                    GaugerPluginLockSlip = 0.0f; // default
                }
                rbrData.GaugerLockSlip = GaugerPluginLockSlip;

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
            public float GaugerLockSlip { get; set; } = 0.0f;
            public float FLWheelSpeed { get; set; } = 0.0f;
            public float FRWheelSpeed { get; set; } = 0.0f;
            public float RLWheelSpeed { get; set; } = 0.0f;
            public float RRWheelSpeed { get; set; } = 0.0f;

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

            public float FLWheelMaxSlipAngle { get; set; } = 0.0f;
            public float FRWheelMaxSlipAngle { get; set; } = 0.0f;
            public float RLWheelMaxSlipAngle { get; set; } = 0.0f;
            public float RRWheelMaxSlipAngle { get; set; } = 0.0f;

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

