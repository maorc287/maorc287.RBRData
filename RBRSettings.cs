// RBRSettings.cs
// Simple POCO for plugin settings, bound to the WPF settings control.
// One instance will live inside RBRDataExtPlugin.

namespace maorc287.RBRDataExtPlugin
{
    public class RBRSettings
    {
        // Enable reading and publishing damage telemetry from the damage structure.
        public bool EnableDamage { get; set; } = true;

        // Enable reading and publishing grip / tyre related telemetry.
        public bool EnableGrip { get; set; } = true;

        // Enable DB-based delta time (DeltaCalc: LoadDeltaData + CalculateDelta + ReadTimingData).
        public bool EnableDelta { get; set; } = true;

        // Enable extra DLL reads: Gauger slip and RBRHUD built-in delta.
        // RSF CarId/StartLine are always read, because DeltaCalc depends on them.
        public bool EnableExtras { get; set; } = true;
    }
}
