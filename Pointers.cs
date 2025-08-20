using System;

namespace maorc287.RBRDataPluginExt
{
    internal class PointerCache
    {
        public IntPtr GameModeBasePtr { get; set; } = IntPtr.Zero;

        // pointer only valid in stage
        public IntPtr CarInfoBasePtr { get; set; } = IntPtr.Zero;
        public IntPtr CarMovBasePtr { get; set; } = IntPtr.Zero;
        public IntPtr DamageBasePtr { get; set; } = IntPtr.Zero;

        internal void ClearAll()
        {
            CarInfoBasePtr = IntPtr.Zero;
            CarMovBasePtr = IntPtr.Zero;
            GameModeBasePtr = IntPtr.Zero;
            DamageBasePtr = IntPtr.Zero;
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

            public const int FLWheel = 0x470; // Front Left Wheel Radius 
            public const int FRWheel = 0x474; // Front Right Wheel Radius
            public const int RLWheel = 0x478; // Rear Left Wheel Radius
            public const int RRWheel = 0x47C; // Rear Right Wheel Radius

            public const int WheelRadiusOffset = 0xAA0; // Offset to wheel radius in the CarMov structure
            public const int WheelRotationOffset = 0xA50; // Offset to wheel rotation in the CarMov structure 

            //Not sure about this, maybe it is related to steering angle in Radians (Offset for the Front Wheels Only)
            public const int FrontWheelSteeringAngle = 0x9E4;
        }

        //Incomplete offsets for damage structure (still need to be woked on)
        //These offsets are used to read the damage structure from the game memory

        public static class Damage
        {
            // Battery wear level, 1.0f is the best condition gradually decrease to 0.0f when starting the car
            public const int BatteryWearPercent = 0x8C;

            //Value is 1.0f when fine, 0.0 float Value means part is lost
            public const int OilPump = 0xF0;
            public const int IntercoolerDamage = 0xF8;
            public const int RadiatiorDamage = 0xE8;

            //Oil Cooler valus is 0 when it is working, 1 when it is damaged
            public const int OilCoolerDamage = 0xF4;

            // These Parts start all at Value 1, when Value is 0 means not working and lost
            public const int WaterPump = 0xDC;
            public const int ElectricSystem = 0x1E8;
            public const int BrakeCircuit = 0x80;
            public const int GearboxActuatorDamage = 0x78;
            public const int StarterDamage = 0x7C;
            public const int HydraulicsDamage = 0x90;

            // 10 Parameters for Gearbox Damage all float values, 1.0f is the best condition,
            //0x48 is the first parameter, 0x6C is the last (4bytes interval)
            //I will write only the first offset, the rest can be calculated
            //Need to create a method to read all 10 parameters and calculate the GearboxDamage
            public const int GearboxDamage = 0x48;
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
