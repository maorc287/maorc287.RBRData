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


        private void SetProperty(string propertyName, object value)
        {
            PluginManager.SetPropertyValue(propertyName, GetType(), value);
        }

        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            var rbrData = TelemetryData.ReadRBRData();

            string oilUnit = (string)PluginManager.GetPropertyValue("DataCorePlugin.GameData.OilPressureUnit");

            SetProperty("RBR.OnStage", rbrData.IsOnStage);
            SetProperty("RBR.EngineStatus", rbrData.IsEngineOn);

            SetProperty("RBR.OilPressure", TelemetryData.ConvertPressure(rbrData.OilPressure, oilUnit));
            SetProperty("RBR.TurboPressure", TelemetryData.ConvertPressure(rbrData.TurboPressure, oilUnit));

            SetProperty("RBR.OilTemperatureC", rbrData.OilTemperatureC);
            SetProperty("RBR.BatteryVoltage", rbrData.BatteryVoltage);
            SetProperty("RBR.BatteryStatus", rbrData.BatteryStatus);

            SetProperty("RBR.OilPressureWarning", rbrData.OilPressureWarning);
            SetProperty("RBR.LowBatteryWarning", rbrData.LowBatteryWarning);

            SetProperty("RBR.OilPumpDamage", rbrData.OilPumpDamage);
            SetProperty("RBR.WaterPumpDamage", rbrData.WaterPumpDamage);
            SetProperty("RBR.ElectricSystemDamage", rbrData.ElectricSystemDamage);
            SetProperty("RBR.BrakeCircuitDamage", rbrData.BrakeCircuitDamage);

            SetProperty("RBR.GroundSpeed", rbrData.GroundSpeed);
            SetProperty("RBR.WheelLock", rbrData.WheelLock);
            SetProperty("RBR.WheelSpin", rbrData.WheelSpin);
        }
    }
}
