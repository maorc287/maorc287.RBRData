// PluginAccessor.cs
// Allows internal static classes to access the plugin instance safely.

namespace maorc287.RBRDataExtPlugin
{
    internal static class PluginAccessor
    {
        // Will be set once in RBRDataExtPlugin.Init.
        public static RBRDataExtPlugin Instance;
    }
}
