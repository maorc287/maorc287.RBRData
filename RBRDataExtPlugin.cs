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

            PluginManager.AddProperty("RBR.Game.OnStage", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Data.EngineStatus", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Data.OilPressure", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Data.RadiatorCoolantTemeperature", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Data.OilTemperature", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Data.BatteryVoltage", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Data.GroundSpeed", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Data.WheelLock", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Data.WheelSpin", GetType(), 0, "");


            PluginManager.AddProperty("RBR.Info.OilPressureWarning", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Info.LowBatteryWarning", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Damage.BatteryWearLevel", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.OilPumpDamage", GetType(), 1, "");
            PluginManager.AddProperty("RBR.Damage.WaterPumpDamage", GetType(), 1, "");
            PluginManager.AddProperty("RBR.Damage.ElectricSystemDamage", GetType(), 1, "");
            PluginManager.AddProperty("RBR.Damage.BrakeCircuitDamage", GetType(), 1, "");

        }

        public void End(PluginManager pluginManager) { }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) => null;


        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            var rbrData = TelemetryData.ReadTelemetryData();

            string pressureUnit = (string)PluginManager.GetPropertyValue("DataCorePlugin.GameData.OilPressureUnit");
            string temperatureUnit = (string)PluginManager.GetPropertyValue("DataCorePlugin.GameData.TemperatureUnit");


            PluginManager.SetPropertyValue("RBR.Game.OnStage", GetType(), rbrData.IsOnStage);

            PluginManager.SetPropertyValue("RBR.Data.EngineStatus", GetType(), rbrData.IsEngineOn);

            PluginManager.SetPropertyValue("RBR.Data.OilPressure", GetType(), 
                TelemetryData.FormatPressure(rbrData.OilPressure, pressureUnit));

            PluginManager.SetPropertyValue("RBR.Data.RadiatorCoolantTemeperature", GetType(),
                TelemetryData.FormatTemperature(rbrData.RadiatorCoolantTemperature, temperatureUnit));
            PluginManager.SetPropertyValue("RBR.Data.OilTemperature", GetType(), 
                TelemetryData.FormatTemperature(rbrData.OilTemperature, temperatureUnit));

            PluginManager.SetPropertyValue("RBR.Data.BatteryVoltage", GetType(), rbrData.BatteryVoltage);

            PluginManager.SetPropertyValue("RBR.Data.GroundSpeed", GetType(), rbrData.GroundSpeed);
            PluginManager.SetPropertyValue("RBR.Data.WheelLock", GetType(), rbrData.WheelLock);
            PluginManager.SetPropertyValue("RBR.Data.WheelSpin", GetType(), rbrData.WheelSpin);

            PluginManager.SetPropertyValue("RBR.Info.OilPressureWarning", GetType(), rbrData.OilPressureWarning);
            PluginManager.SetPropertyValue("RBR.Info.LowBatteryWarning", GetType(), rbrData.LowBatteryWarning);

            PluginManager.SetPropertyValue("RBR.Damage.BatteryWearLevel", GetType(), rbrData.BatteryWearLevel);
            PluginManager.SetPropertyValue("RBR.Damage.OilPumpDamage", GetType(), rbrData.OilPumpDamage);
            PluginManager.SetPropertyValue("RBR.Damage.WaterPumpDamage", GetType(), rbrData.WaterPumpDamage);
            PluginManager.SetPropertyValue("RBR.Damage.ElectricSystemDamage", GetType(), rbrData.ElectricSystemDamage);
            PluginManager.SetPropertyValue("RBR.Damage.BrakeCircuitDamage", GetType(), rbrData.BrakeCircuitDamage);


        }
    }
}
