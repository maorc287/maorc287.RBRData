using System;
using System.Diagnostics.Contracts;

namespace maorc287.RBRDataExtPlugin
{
    internal class PointerCache
    {
        public IntPtr GameModeBasePtr { get; set; } = IntPtr.Zero;

        // pointer only valid in stage
        public IntPtr CarInfoBasePtr { get; set; } = IntPtr.Zero;
        public IntPtr CarMovBasePtr { get; set; } = IntPtr.Zero;
        public IntPtr DamageBasePtr { get; set; } = IntPtr.Zero;
        public IntPtr FLWheelPtr { get; set; } = IntPtr.Zero;
        public IntPtr FRWheelPtr { get; set; } = IntPtr.Zero;
        public IntPtr RLWheelPtr { get; set; } = IntPtr.Zero;
        public IntPtr RRWheelPtr { get; set; } = IntPtr.Zero;

        public IntPtr TireModelBasePtr { get; set; } = IntPtr.Zero;

        internal void ClearAllCache()
        {
            CarInfoBasePtr = IntPtr.Zero;
            CarMovBasePtr = IntPtr.Zero;
            GameModeBasePtr = IntPtr.Zero;
            DamageBasePtr = IntPtr.Zero;
            TireModelBasePtr = IntPtr.Zero;
            FLWheelPtr = IntPtr.Zero;
            FRWheelPtr = IntPtr.Zero;
            RLWheelPtr = IntPtr.Zero;
            RRWheelPtr = IntPtr.Zero;
        }

        internal bool IsTireModelPointerValid()
        {
            return TireModelBasePtr != IntPtr.Zero;
        }

        internal bool IsCarInfoPointerValid()
        {
            return CarInfoBasePtr != IntPtr.Zero;
        }

        internal bool IsCarMovPointerValid()
        {
            return CarMovBasePtr != IntPtr.Zero;
        }

        internal bool IsDamagePointerValid()
        {
            return DamageBasePtr != IntPtr.Zero;
        }

        internal bool IsWheelPointerValid(int wheelOffset)
        {
            return CarMovBasePtr != IntPtr.Zero && CarMovBasePtr + wheelOffset != IntPtr.Zero;
        }

        internal bool IsGeameModeBaseValid()
        {
            return GameModeBasePtr != IntPtr.Zero;
        }

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
            public const int Timer = 0x140; // Timer in seconds
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

            public const int AccelerationX = 0x250; // ?Acceleration in X direction?
            public const int AccelerationY = 0x254; // ?Acceleration in Y direction?
            public const int AccelerationZ = 0x258; // ?Acceleration in Z direction?

            // Wheel pointers for the CarMov structure
            public const int FLWheel = 0x470; // Front Left Wheel Pointer
            public const int FRWheel = 0x474; // Front Right Wheel Pointer
            public const int RLWheel = 0x478; // Rear Left Wheel Pointer
            public const int RRWheel = 0x47C; // Rear Right Wheel Pointer

        }

        public static class Wheel
        {
            //Wheel structure offsets (same for all wheels)
            public const int WheelRadiusOffset = 0xAA0;
            public const int WheelRotationOffset = 0xA50;

            public const int LongSpeedNegativeOffset = 0x126C; // ?? Offset to negative longitudinal speed (m/s)
            public const int LongitudinalSpeedOffset = 0x1260; // Offset to longitudinal speed (m/s)
            public const int LateralSpeedOffset = 0x1268; // Offset to lateral speed (m/s)
            public const int CorrectionOffset = 0x12AC; // Offset to correction value?

            public const int LockSlipMagnitude = 0x1330; // ?? Offset to wheel lock slip magnitude? (0.5 to 2.0 float value)
            public const int LateralGripValue = 0x12E0; // ?? Offset to wheel lateral grip value? (0.0 to 2.0 float value)
            public const int LongitudinalGripValue = 0x12E4; // ?? Offset to wheel longitudinal grip value? (0.0 to 2.0 float value)

            public const int CorneringStiffness = 0x128C; // Cornering stiffness? Don't know what unit Newtons/rad?
            public const int VerticalLoad = 0x1334; // Offset to vertical load in Newtons?

            //Not sure about this, maybe it is related to steering angle in Radians (Offset for the Front Wheels Only)
            public const int SteeringAngle = 0x9E4;

            public const int SurfaceEffectScaling = 0xB9C;
            public const int SurfaceFrictionScaling = 0xAEC;

            // Active Slip Peak Cornering value integer (0 to 7) based on load,
            // index for the array in the TireModel structure.
            // Weight of the 2 active load value in float (0.0 to 1.0)
            public const int ActiveLoad1 = 0x74C;
            public const int ActiveLoad2 = 0x750;
            public const int LoadWeight = 0x754;
        }

        //Incomplete offsets for damage structure (still need to be worked on)
        //These offsets are used to read the damage structure from the game memory

        public static class Damage
        {
            // Battery wear level, 1.0f is the best condition gradually decrease to 0.0f when starting the car
            public const int BatteryWearPercent = 0x8C;

            //Value is 1.0f when fine, 0.0 float Value means part is lost
            public const int OilPump = 0xF0;
            public const int Intercooler = 0xF8;
            public const int Radiator = 0xE8;

            //Oil Cooler valus is 0 when it is working, 1 when it is damaged
            public const int OilCooler = 0xF4;

            // These Parts start all at Value 1, when Value is 0 means not working and lost
            public const int WaterPump = 0xDC;
            public const int ElectricSystem = 0x1E8;
            public const int BrakeCircuit = 0x80;
            public const int GearboxActuator = 0x78;
            public const int Starter = 0x7C;
            public const int Hydraulics = 0x90;

            // 10 Parameters for Gearbox Damage all float values, 1.0f is the best condition,
            //0x48 is the first parameter, 0x6C is the last (4bytes interval)
            //I will write only the first offset, the rest can be calculated
            //Need to create a method to read all 10 parameters and calculate the GearboxDamage
            public const int GearboxDamage = 0x48;
        }

        public static class TireModel
        {
            // Array of 8 float values from the Tires structure from File tyres.lsp
            public const int SlpPkCrn = 0x7F0;  // Slip Peak Cornering value in the Tires structure from File tyres.lsp
            public const int SlpPkTrct = 0x810;  // Slip Peak Traction value in the Tires structure from File tyres.lsp
            public const int CrnStf = 0x770;  // Cornering Stiffness value in the Tires structure from File tyres.lsp
            public const int TrctStf = 0x790;  // Traction Stiffness value in the Tires structure from File tyres.lsp
            //Array of 5 float value for friction muliplier based on surface type?
            public const int SFric = 0x830;

            public const int TireType = 0x578; // Actual tire Type value in the Tires structure from File tyres.lsp
        }


        private static IntPtr GetRBRModuleBase()
        {
            return MemoryReader.TryGetOrCacheDllBaseAddress("RichardBurnsRally_SSE.exe");
        }

        public static class Pointers
        {
            public const int CarMov = 0x8EF660;  // Dynamic!
            public const int GameMode = 0x7EAC48;
            public const int CarInfo = 0x165FC68;

            public const int GameModeOffset = 0x728;
            public const int TireModel = 0x007C8318; // Pointer to the tires.lsp file structure in memory
            public const int WheelContact = 0x00893038; // Pointer to the Wheel Surface Contact structure in memory

        }
    }
}
