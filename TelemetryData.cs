using maorc287.RBRDataExtPlugin;
using System;
using System.Diagnostics;
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
      
        // ---------------- Helpers ---------------- //
        private static IntPtr EnsureProcess()
        {
            var hProcess = GetOrOpenProcessHandle(RBRProcessName);
            if (hProcess == IntPtr.Zero)
                pointerCache.ClearAllCache();
            return hProcess;
        }

        private static void EnsurePointers(IntPtr hProcess)
        {
            if (!pointerCache.IsGeameModeBaseValid())
                pointerCache.GameModeBasePtr = (IntPtr)ReadUInt(hProcess, (IntPtr)Pointers.GameMode);

            if (!pointerCache.IsCarInfoPointerValid())
                pointerCache.CarInfoBasePtr = (IntPtr)ReadUInt(hProcess, (IntPtr)Pointers.CarInfo);

            if (!pointerCache.IsCarMovPointerValid())
            {
                pointerCache.CarMovBasePtr = ReadPointer(hProcess, (IntPtr)Pointers.CarMov);
                pointerCache.FLWheelPtr = ReadPointer(hProcess, pointerCache.CarMovBasePtr + CarMov.FLWheel);
                pointerCache.FRWheelPtr = ReadPointer(hProcess, pointerCache.CarMovBasePtr + CarMov.FRWheel);
                pointerCache.RLWheelPtr = ReadPointer(hProcess, pointerCache.CarMovBasePtr + CarMov.RLWheel);
                pointerCache.RRWheelPtr = ReadPointer(hProcess, pointerCache.CarMovBasePtr + CarMov.RRWheel);
            }

            if (!pointerCache.IsTiresPhysicsPointerValid())
                pointerCache.TireModelBasePtr = (IntPtr)ReadUInt(hProcess, (IntPtr)Pointers.TireModel);

            if (!pointerCache.IsDamagePointerValid())
            {
                IntPtr damageStructPtr = pointerCache.CarMovBasePtr + CarMov.DamageStructurePointer;
                int damagePointer = ReadInt(hProcess, damageStructPtr);
                pointerCache.DamageBasePtr = (IntPtr)damagePointer;
            }
        }

        private static bool IsOnStage(IntPtr hProcess, RBRTelemetryData rbrData)
        {
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

        private static void ReadDamageData(IntPtr hProcess, IntPtr damageBasePtr, RBRTelemetryData rbrData)
        {
            rbrData.BatteryWearLevel = BatteryHealthLevel(ReadFloat(hProcess, damageBasePtr + Damage.BatteryWearPercent));
            rbrData.OilPumpDamage = OilPumpDamageLevel(ReadFloat(hProcess, damageBasePtr + Damage.OilPump));
            rbrData.WaterPumpDamage = PartWorkingStatus(ReadInt(hProcess, damageBasePtr + Damage.WaterPump));
            rbrData.ElectricSystemDamage = PartWorkingStatus(ReadInt(hProcess, damageBasePtr + Damage.ElectricSystem));
            rbrData.BrakeCircuitDamage = PartWorkingStatus(ReadInt(hProcess, damageBasePtr + Damage.BrakeCircuit));
            rbrData.GearboxActuatorDamage = PartWorkingStatus(ReadInt(hProcess, damageBasePtr + Damage.GearboxActuatorDamage));
            rbrData.RadiatorDamage = RadiatorDamageLevel(ReadFloat(hProcess, damageBasePtr + Damage.RadiatiorDamage));
            rbrData.IntercoolerDamage = IntercoolerDamageLevel(ReadFloat(hProcess, damageBasePtr + Damage.IntercoolerDamage));
            rbrData.StarterDamage = PartWorkingStatus(ReadInt(hProcess, damageBasePtr + Damage.StarterDamage));
            rbrData.HydraulicsDamage = PartWorkingStatus(ReadInt(hProcess, damageBasePtr + Damage.HydraulicsDamage));
            rbrData.OilCoolerDamage = InversePartWorkingStatus(ReadInt(hProcess, damageBasePtr + Damage.OilCoolerDamage));
        }

        private static void ReadEngineAndFluids(IntPtr hProcess, IntPtr carInfoBasePtr, IntPtr carMovBasePtr, RBRTelemetryData rbrData)
        {
            rbrData.IsEngineOn = ReadFloat(hProcess, carInfoBasePtr + CarInfo.EngineStatus) == 1.0f;

            rbrData.RadiatorCoolantTemperature = ReadFloat(hProcess, carMovBasePtr + CarMov.RadiatorCoolantTemperature);
            rbrData.OilTemperature = ReadFloat(hProcess, carMovBasePtr + CarMov.OilTempKelvin);
            rbrData.OilTemperatureWarning = rbrData.OilTemperature > 140.0f + kelvin_Celcius;

            float oilPRawBase = ReadFloat(hProcess, carMovBasePtr + CarMov.OilPressureRawBase);
            float oilPRaw = ReadFloat(hProcess, carMovBasePtr + CarMov.OilPressureRaw);
            rbrData.OilPressure = ComputeOilPressure(oilPRawBase, oilPRaw);

            rbrData.OilPressureWarning = !rbrData.IsEngineOn
                | rbrData.OilPressure < 0.2
                | rbrData.OilPumpDamage >= 2;

            float waterTemperature = ReadFloat(hProcess, carInfoBasePtr + CarInfo.WaterTemperatureCelsius);
            rbrData.WaterTemperatureWarning = waterTemperature > 120.0f;
        }

        private static void ReadBatteryData(IntPtr hProcess, IntPtr carInfoBasePtr, RBRTelemetryData rbrData)
        {
            rbrData.BatteryStatus = ReadFloat(hProcess, carInfoBasePtr + CarInfo.BatteryStatus);
            rbrData.BatteryVoltage = rbrData.IsEngineOn ? 14.5f
                : (rbrData.BatteryStatus * 0.2f) + 10.4f;
            rbrData.LowBatteryWarning = rbrData.BatteryStatus < 10.0f;
        }

        private static void ReadVelocityData(IntPtr hProcess, IntPtr carMovBasePtr, RBRTelemetryData rbrData)
        {
            float velX = ReadFloat(hProcess, carMovBasePtr + CarMov.VelocityX);
            float velY = ReadFloat(hProcess, carMovBasePtr + CarMov.VelocityY);
            float velZ = ReadFloat(hProcess, carMovBasePtr + CarMov.VelocityZ);
            float fwdX = ReadFloat(hProcess, carMovBasePtr + CarMov.ForwardX);
            float fwdY = ReadFloat(hProcess, carMovBasePtr + CarMov.ForwardY);
            float fwdZ = ReadFloat(hProcess, carMovBasePtr + CarMov.ForwardZ);

            float wheelSpeed = ReadFloat(hProcess, pointerCache.CarInfoBasePtr + CarInfo.WheelSpeed);

            rbrData.GroundSpeed = ComputeGroundSpeed(velX, velY, velZ, fwdX, fwdY, fwdZ);
            rbrData.WheelLock = ComputeWheelLockRatio(rbrData.GroundSpeed, wheelSpeed);
            rbrData.WheelSlip = ComputeWheelSpinRatio(rbrData.GroundSpeed, wheelSpeed);
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

            rbrData.FLWheelSteeringAngle = ReadFloat(hProcess, pointerCache.FLWheelPtr + Wheel.FrontWheelSteeringAngle);
            rbrData.FRWheelSteeringAngle = ReadFloat(hProcess, pointerCache.FRWheelPtr + Wheel.FrontWheelSteeringAngle);
        }

        private static void ReadSlipAndTireModel(IntPtr hProcess, RBRTelemetryData rbrData)
        {
            float flCornerStiffness = ReadFloat(hProcess, pointerCache.FLWheelPtr + Wheel.CornerStiffnes);
            float frCornerStiffness = ReadFloat(hProcess, pointerCache.FRWheelPtr + Wheel.CornerStiffnes);
            float rlCornerStiffness = ReadFloat(hProcess, pointerCache.RLWheelPtr + Wheel.CornerStiffnes);
            float rrCornerStiffness = ReadFloat(hProcess, pointerCache.RRWheelPtr + Wheel.CornerStiffnes);

            float[] slipCornerPk = ReadFloatArray(hProcess, pointerCache.TireModelBasePtr + TireModel.SlpPkCrn, 8);
            float[] slipTractionPk = ReadFloatArray(hProcess, pointerCache.TireModelBasePtr + TireModel.SlpPkTrct, 8);
            float[] cornerStiff = ReadFloatArray(hProcess, pointerCache.TireModelBasePtr + TireModel.CrnStf, 8);
            float[] tractionStiff = ReadFloatArray(hProcess, pointerCache.TireModelBasePtr + TireModel.TrctStf, 8);

            float lateralFL = ReadFloat(hProcess, pointerCache.FLWheelPtr + Wheel.LateralSpeedOffset);
            float longitudinalFL = ReadFloat(hProcess, pointerCache.FLWheelPtr + Wheel.LongitudinalSpeedOffset);
            float lateralFR = ReadFloat(hProcess, pointerCache.FRWheelPtr + Wheel.LateralSpeedOffset);
            float longitudinalFR = ReadFloat(hProcess, pointerCache.FRWheelPtr + Wheel.LongitudinalSpeedOffset);
            float lateralRL = ReadFloat(hProcess, pointerCache.RLWheelPtr + Wheel.LateralSpeedOffset);
            float longitudinalRL = ReadFloat(hProcess, pointerCache.RLWheelPtr + Wheel.LongitudinalSpeedOffset);
            float lateralRR = ReadFloat(hProcess, pointerCache.RRWheelPtr + Wheel.LateralSpeedOffset);
            float longitudinalRR = ReadFloat(hProcess, pointerCache.RRWheelPtr + Wheel.LongitudinalSpeedOffset);

            rbrData.FLWheelSlipAngle = GetSlipAngleRad(rbrData.GroundSpeed, rbrData.FLWheelSpeed,
                longitudinalFL, lateralFL, rbrData.FLWheelSteeringAngle);
            rbrData.FRWheelSlipAngle = GetSlipAngleRad(rbrData.GroundSpeed, rbrData.FRWheelSpeed,
                longitudinalFR, lateralFR, rbrData.FRWheelSteeringAngle);
            rbrData.RLWheelSlipAngle = GetSlipAngleRad(rbrData.GroundSpeed, rbrData.RLWheelSpeed,
                longitudinalRL, lateralRL);
            rbrData.RRWheelSlipAngle = GetSlipAngleRad(rbrData.GroundSpeed, rbrData.RRWheelSpeed,
                longitudinalRR, lateralRR);

            rbrData.FLWheelMaxSlipAngle = GetNormalizedSlip(rbrData.FLWheelSlipAngle, flCornerStiffness, cornerStiff, slipCornerPk);
            rbrData.FRWheelMaxSlipAngle = GetNormalizedSlip(rbrData.FRWheelSlipAngle, frCornerStiffness, cornerStiff, slipCornerPk);
            rbrData.RLWheelMaxSlipAngle = GetNormalizedSlip(rbrData.RLWheelSlipAngle, rlCornerStiffness, cornerStiff, slipCornerPk);
            rbrData.RRWheelMaxSlipAngle = GetNormalizedSlip(rbrData.RRWheelSlipAngle, rrCornerStiffness, cornerStiff, slipCornerPk);

            rbrData.FLWheelSlipRatio = ComputeWheelSlipRatio(rbrData.GroundSpeed, rbrData.FLWheelSpeed);
            rbrData.FRWheelSlipRatio = ComputeWheelSlipRatio(rbrData.GroundSpeed, rbrData.FRWheelSpeed);
            rbrData.RLWheelSlipRatio = ComputeWheelSlipRatio(rbrData.GroundSpeed, rbrData.RLWheelSpeed);
            rbrData.RRWheelSlipRatio = ComputeWheelSlipRatio(rbrData.GroundSpeed, rbrData.RRWheelSpeed);
        }

        private static void ReadExternalPluginData(RBRTelemetryData rbrData)
        {
            if (!MemoryReader.TryReadFromDll("GaugerPlugin.dll", 0x7ADFC, out float GaugerPluginLockSlip))
                GaugerPluginLockSlip = 0.0f;
            rbrData.GaugerLockSlip = GaugerPluginLockSlip;
        }


        /// Reads telemetry data from the Richard Burns Rally process.
        /// this method accesses the game's memory to retrieve various telemetry values.
        /// as a result, it requires the game to be running and the process to be accessible.
        /// without the game running and on stage, it will return default values.
        internal static RBRTelemetryData ReadTelemetryData()
        {
            var rbrData = new RBRTelemetryData();
            IntPtr hProcess = EnsureProcess();
            if (hProcess == IntPtr.Zero) return rbrData;

            try
            {
                EnsurePointers(hProcess);

                if (!IsOnStage(hProcess, rbrData))
                {
                    return LatestValidTelemetry;
                }

                ReadDamageData(hProcess, pointerCache.DamageBasePtr, rbrData);
                ReadEngineAndFluids(hProcess, pointerCache.CarInfoBasePtr, pointerCache.CarMovBasePtr, rbrData);
                ReadBatteryData(hProcess, pointerCache.CarInfoBasePtr, rbrData);
                ReadVelocityData(hProcess, pointerCache.CarMovBasePtr, rbrData);
                ReadWheelData(hProcess, rbrData);
                ReadSlipAndTireModel(hProcess, rbrData);
                ReadExternalPluginData(rbrData);
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

