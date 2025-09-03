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
            rbrData.BatteryWearLevel = BatteryHealthLevel(ReadFloat(hProcess, pointerCache.DamageBasePtr + Damage.BatteryWearPercent));
            rbrData.OilPumpDamage = OilPumpDamageLevel(ReadFloat(hProcess, pointerCache.DamageBasePtr + Damage.OilPump));
            rbrData.WaterPumpDamage = PartWorkingStatus(ReadInt(hProcess, pointerCache.DamageBasePtr + Damage.WaterPump));
            rbrData.ElectricSystemDamage = PartWorkingStatus(ReadInt(hProcess, pointerCache.DamageBasePtr + Damage.ElectricSystem));
            rbrData.BrakeCircuitDamage = PartWorkingStatus(ReadInt(hProcess, pointerCache.DamageBasePtr + Damage.BrakeCircuit));
            rbrData.GearboxActuatorDamage = PartWorkingStatus(ReadInt(hProcess, pointerCache.DamageBasePtr + Damage.GearboxActuator));
            rbrData.RadiatorDamage = RadiatorDamageLevel(ReadFloat(hProcess, pointerCache.DamageBasePtr + Damage.Radiator));
            rbrData.IntercoolerDamage = IntercoolerDamageLevel(ReadFloat(hProcess, pointerCache.DamageBasePtr + Damage.Intercooler));
            rbrData.StarterDamage = PartWorkingStatus(ReadInt(hProcess, pointerCache.DamageBasePtr + Damage.Starter));
            rbrData.HydraulicsDamage = PartWorkingStatus(ReadInt(hProcess, pointerCache.DamageBasePtr + Damage.Hydraulics));
            rbrData.OilCoolerDamage = InversePartWorkingStatus(ReadInt(hProcess, pointerCache.DamageBasePtr + Damage.OilCooler));
        }

        private static void ReadEngineAndFluids(IntPtr hProcess, RBRTelemetryData rbrData)
        {
            rbrData.IsEngineOn = ReadFloat(hProcess, pointerCache.CarInfoBasePtr + CarInfo.EngineStatus) == 1.0f;

            rbrData.RadiatorCoolantTemperature = ReadFloat(hProcess, pointerCache.CarMovBasePtr + CarMov.RadiatorCoolantTemperature);
            rbrData.OilTemperature = ReadFloat(hProcess, pointerCache.CarMovBasePtr + CarMov.OilTempKelvin);
            rbrData.OilTemperatureWarning = rbrData.OilTemperature > 140.0f + kelvin_Celcius;

            float oilPRawBase = ReadFloat(hProcess, pointerCache.CarMovBasePtr + CarMov.OilPressureRawBase);
            float oilPRaw = ReadFloat(hProcess, pointerCache.CarMovBasePtr + CarMov.OilPressureRaw);
            rbrData.OilPressure = ComputeOilPressure(oilPRawBase, oilPRaw);

            rbrData.OilPressureWarning = !rbrData.IsEngineOn
                | rbrData.OilPressure < 0.2
                | rbrData.OilPumpDamage >= 2;

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

        private static void ReadVelocityData(IntPtr hProcess, RBRTelemetryData rbrData)
        {
            float velX = ReadFloat(hProcess, pointerCache.CarMovBasePtr + CarMov.VelocityX);
            float velY = ReadFloat(hProcess, pointerCache.CarMovBasePtr + CarMov.VelocityY);
            float velZ = ReadFloat(hProcess, pointerCache.CarMovBasePtr + CarMov.VelocityZ);
            float fwdX = ReadFloat(hProcess, pointerCache.CarMovBasePtr + CarMov.ForwardX);
            float fwdY = ReadFloat(hProcess, pointerCache.CarMovBasePtr + CarMov.ForwardY);
            float fwdZ = ReadFloat(hProcess, pointerCache.CarMovBasePtr + CarMov.ForwardZ);

            rbrData.GroundSpeed = ComputeGroundSpeed(velX, velY, velZ, fwdX, fwdY, fwdZ);
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
            float flCornerStiffness = ReadFloat(hProcess, pointerCache.FLWheelPtr + Wheel.CorneringStiffness);
            float frCornerStiffness = ReadFloat(hProcess, pointerCache.FRWheelPtr + Wheel.CorneringStiffness);
            float rlCornerStiffness = ReadFloat(hProcess, pointerCache.RLWheelPtr + Wheel.CorneringStiffness);
            float rrCornerStiffness = ReadFloat(hProcess, pointerCache.RRWheelPtr + Wheel.CorneringStiffness);

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
                longitudinalFL, lateralFL);
            rbrData.FRWheelSlipAngle = GetSlipAngleRad(rbrData.GroundSpeed, rbrData.FRWheelSpeed,
                longitudinalFR, lateralFR);
            rbrData.RLWheelSlipAngle = GetSlipAngleRad(rbrData.GroundSpeed, rbrData.RLWheelSpeed,
                longitudinalRL, lateralRL);
            rbrData.RRWheelSlipAngle = GetSlipAngleRad(rbrData.GroundSpeed, rbrData.RRWheelSpeed,
                longitudinalRR, lateralRR);

            //float gripValueFL = ReadFloat(hProcess, pointerCache.FLWheelPtr + Wheel.GripValue);

            float vLoadFL = ReadFloat(hProcess, pointerCache.FLWheelPtr + Wheel.VerticalLoad);
            float vLoadFR = ReadFloat(hProcess, pointerCache.FRWheelPtr + Wheel.VerticalLoad);
            float vLoadRL = ReadFloat(hProcess, pointerCache.RLWheelPtr + Wheel.VerticalLoad);
            float vLoadRR = ReadFloat(hProcess, pointerCache.RRWheelPtr + Wheel.VerticalLoad);
            float frictionScalingFL = ReadFloat(hProcess, pointerCache.FLWheelPtr + Wheel.SurfaceFrictionScaling);
            float frictionScalingFR = ReadFloat(hProcess, pointerCache.FRWheelPtr + Wheel.SurfaceFrictionScaling);
            float frictionScalingRL = ReadFloat(hProcess, pointerCache.RLWheelPtr + Wheel.SurfaceFrictionScaling);
            float frictionScalingRR = ReadFloat(hProcess, pointerCache.RRWheelPtr + Wheel.SurfaceFrictionScaling);

            int activeSlipPk1 = ReadInt(hProcess, pointerCache.FLWheelPtr + Wheel.ActiveSlpPkCrn1);
            int activeSlipPk2 = ReadInt(hProcess, pointerCache.FLWheelPtr + Wheel.ActiveSlpPkCrn2);
            float slipWeight = ReadFloat(hProcess, pointerCache.FLWheelPtr + Wheel.SlpPkCrnWeight);

            float maxSlipAngleFL = GetSaturationSlipFromArray(slipCornerPk, activeSlipPk1, activeSlipPk2, slipWeight);
            float maxSlipAngleFR = GetSaturationSlipFromArray(slipCornerPk, activeSlipPk1, activeSlipPk2, slipWeight);
            //ComputeMaxSlipAngleRad(frictionScalingFL ,vLoadFL, flCornerStiffness);

            rbrData.FLWheelSlipAngleOver = 
                GetSlipAngleExcessNormalized(rbrData.FLWheelSlipAngle, flCornerStiffness, cornerStiff, slipCornerPk,
                frictionScalingFL, out float slipMaxFL, out float percentSlipAngleFL);
            rbrData.FRWheelSlipAngleOver = 
                GetSlipAngleExcessNormalized(rbrData.FRWheelSlipAngle, frCornerStiffness, cornerStiff, slipCornerPk,
                frictionScalingFR, out float slipMaxFR, out float percentSlipAngleFR);
            rbrData.RLWheelSlipAngleOver = 
                GetSlipAngleExcessNormalized(rbrData.RLWheelSlipAngle, rlCornerStiffness, cornerStiff, slipCornerPk,
                frictionScalingRL, out float slipMaxRL, out float percentSlipAngleRL);
            rbrData.RRWheelSlipAngleOver = 
                GetSlipAngleExcessNormalized(rbrData.RRWheelSlipAngle, rrCornerStiffness, cornerStiff, slipCornerPk,
                frictionScalingRR, out float slipMaxRR, out float percentSlipAngleRR);

            rbrData.FLWheelMaxSlipAngle = percentSlipAngleFL;
            rbrData.FRWheelMaxSlipAngle = percentSlipAngleFR;
            rbrData.RLWheelMaxSlipAngle = percentSlipAngleRL;
            rbrData.RRWheelMaxSlipAngle = percentSlipAngleRR;

            rbrData.FLWheelLimitSlipAngleRad = maxSlipAngleFL;//slipMaxFL;
            rbrData.FRWheelLimitSlipAngleRad = slipMaxFR;
            rbrData.RLWheelLimitSlipAngleRad = slipMaxRL;
            rbrData.RRWheelLimitSlipAngleRad = slipMaxRR;

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
            IntPtr hProcess = GetProcess();
            if (hProcess == IntPtr.Zero) return rbrData;

            try
            {
                InitializePointers(hProcess);

                if (!IsOnStage(hProcess, rbrData))
                {
                    return LatestValidTelemetry;
                }

                ReadDamageData(hProcess, rbrData);
                ReadEngineAndFluids(hProcess, rbrData);
                ReadBatteryData(hProcess, rbrData);
                ReadVelocityData(hProcess, rbrData);
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

            public float FLWheelSlipAngleOver { get; set; } = 0.0f;
            public float FRWheelSlipAngleOver { get; set; } = 0.0f;
            public float RLWheelSlipAngleOver { get; set; } = 0.0f;
            public float RRWheelSlipAngleOver { get; set; } = 0.0f;

            public float FLWheelLimitSlipAngleRad { get; set; } = 0.0f;
            public float FRWheelLimitSlipAngleRad { get; set; } = 0.0f;
            public float RLWheelLimitSlipAngleRad { get; set; } = 0.0f;
            public float RRWheelLimitSlipAngleRad { get; set; } = 0.0f;

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

