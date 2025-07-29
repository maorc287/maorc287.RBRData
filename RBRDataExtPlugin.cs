using maorc287.RBRDataPluginExt;
using SimHub.Plugins;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace maorc287.RBRDataExtPlugin
{
    [PluginDescription("Richard Burns Rally Additional Data Reader")]
    [PluginAuthor("maorc287")]
    [PluginName("RBRDataExt")]

    public class RBRDataExtPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "RBR Data Extension";

    

        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("[RBRDataExt] Plugin initialized.");

            PluginManager = pluginManager;

            PluginManager.AddProperty("RBR.OnStage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.TurboPressure", GetType(), 0, "");
            PluginManager.AddProperty("RBR.OilPressure", GetType(), 0, "");
            PluginManager.AddProperty("RBR.OilTemperatureC", GetType(), 0, "");
            PluginManager.AddProperty("RBR.EngineStatus", GetType(), 0, "");
            PluginManager.AddProperty("RBR.BatteryVoltage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.BatteryStatus", GetType(), 0, "");

            PluginManager.AddProperty("RBR.OilPressureWarning", GetType(), 0, "");
            PluginManager.AddProperty("RBR.LowBatteryWarning", GetType(), 0, "");

            PluginManager.AddProperty("RBR.OilPumpDamage", GetType(), 1, "");
            PluginManager.AddProperty("RBR.WaterPumpDamage", GetType(), 1, "");
            PluginManager.AddProperty("RBR.ElectricSystemDamage", GetType(), 1, "");
            PluginManager.AddProperty("RBR.BrakeCircuitDamage", GetType(), 1, "");

            PluginManager.AddProperty("RBR.GroundSpeed", GetType(), 0, "");
            PluginManager.AddProperty("RBR.WheelLock", GetType(), 0, "");
            PluginManager.AddProperty("RBR.WheelSpin", GetType(), 0, "");
        }

        public void End(PluginManager pluginManager) { }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) => null;


        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            var rbrData = TelemetryData.ReadRBRData();

            string oilUnit = (string)PluginManager.GetPropertyValue("DataCorePlugin.GameData.OilPressureUnit");

            PluginManager.SetPropertyValue("RBR.OnStage", GetType(), rbrData.IsOnStage);
            PluginManager.SetPropertyValue("RBR.EngineStatus", GetType(), rbrData.IsEngineOn);

            PluginManager.SetPropertyValue("RBR.OilPressure", GetType(), TelemetryData.ConvertPressure(rbrData.OilPressure, oilUnit));
            PluginManager.SetPropertyValue("RBR.TurboPressure", GetType(), TelemetryData.ConvertPressure(rbrData.TurboPressure, oilUnit));

            PluginManager.SetPropertyValue("RBR.OilTemperatureC", GetType(), rbrData.OilTemperatureC);
            PluginManager.SetPropertyValue("RBR.BatteryVoltage", GetType(), rbrData.BatteryVoltage);
            PluginManager.SetPropertyValue("RBR.BatteryStatus", GetType(), rbrData.BatteryStatus);

            PluginManager.SetPropertyValue("RBR.OilPressureWarning", GetType(), rbrData.OilPressureWarning);
            PluginManager.SetPropertyValue("RBR.LowBatteryWarning", GetType(), rbrData.LowBatteryWarning);

            PluginManager.SetPropertyValue("RBR.OilPumpDamage", GetType(), rbrData.OilPumpDamage);
            PluginManager.SetPropertyValue("RBR.WaterPumpDamage", GetType(), rbrData.WaterPumpDamage);
            PluginManager.SetPropertyValue("RBR.ElectricSystemDamage", GetType(), rbrData.ElectricSystemDamage);
            PluginManager.SetPropertyValue("RBR.BrakeCircuitDamage", GetType(), rbrData.BrakeCircuitDamage);

            PluginManager.SetPropertyValue("RBR.GroundSpeed", GetType(), rbrData.GroundSpeed);
            PluginManager.SetPropertyValue("RBR.WheelLock", GetType(), rbrData.WheelLock);
            PluginManager.SetPropertyValue("RBR.WheelSpin", GetType(), rbrData.WheelSpin);
        }
    }
}
