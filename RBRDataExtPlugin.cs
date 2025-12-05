using SimHub.Plugins;
using System.Windows.Media;
using static maorc287.RBRDataExtPlugin.TelemetryData;
using static maorc287.RBRDataExtPlugin.TelemetryCalc;

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

            PluginManager.AddProperty("RBR.Data.EngineOn", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Data.OilPressure", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Data.RadiatorCoolantTemeperature", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Data.OilTemperature", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Data.BatteryVoltage", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Info.OilPressureWarning", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Info.LowBatteryWarning", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Info.WaterTemperatureWarning", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Info.OilTemperatureWarning", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Damage.Battery", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.OilPump", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.WaterPump", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.ElectricSystem", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.BrakeCircuit", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.OilCooler", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.Radiator", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.Intercooler", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.Starter", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.GearboxActuator", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.Hydraulics", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.SlipAngle.FLMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.FRMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.RLMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.RRMax", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.SlipAngle.FLOverMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.FROverMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.RLOverMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.RROverMax", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.SlipRatio.FLMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.FRMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.RLMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.RRMax", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.SlipRatio.FLOverMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.FROverMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.RLOverMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.RROverMax", GetType(), 0, "");

            PluginManager.AddProperty("RBR.GaugerPlugin.LockSlip", GetType(), 0, "");
            PluginManager.AddProperty("RBR.RBRHUD.DeltaTime", GetType(), 0, "");

        }

        public void End(PluginManager pluginManager) { }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) => null;


        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            var rbrData = ReadTelemetryData();

            string pressureUnit = (string)PluginManager.GetPropertyValue("DataCorePlugin.GameData.OilPressureUnit");
            string temperatureUnit = (string)PluginManager.GetPropertyValue("DataCorePlugin.GameData.TemperatureUnit");


            PluginManager.SetPropertyValue("RBR.Game.OnStage", GetType(), rbrData.IsOnStage);

            PluginManager.SetPropertyValue("RBR.Data.EngineOn", GetType(), rbrData.IsEngineOn);

            PluginManager.SetPropertyValue("RBR.Data.OilPressure", GetType(),
                FormatPressure(rbrData.OilPressure, pressureUnit));

            PluginManager.SetPropertyValue("RBR.Data.RadiatorCoolantTemeperature", GetType(),
                FormatTemperature(rbrData.RadiatorCoolantTemperature, temperatureUnit));
            PluginManager.SetPropertyValue("RBR.Data.OilTemperature", GetType(),
                FormatTemperature(rbrData.OilTemperature, temperatureUnit));

            PluginManager.SetPropertyValue("RBR.Data.BatteryVoltage", GetType(), rbrData.BatteryVoltage);

            PluginManager.SetPropertyValue("RBR.Info.OilPressureWarning", GetType(), rbrData.OilPressureWarning);
            PluginManager.SetPropertyValue("RBR.Info.LowBatteryWarning", GetType(), rbrData.LowBatteryWarning);
            PluginManager.SetPropertyValue("RBR.Info.WaterTemperatureWarning", GetType(), rbrData.WaterTemperatureWarning);
            PluginManager.SetPropertyValue("RBR.Info.OilTemperatureWarning", GetType(), rbrData.OilTemperatureWarning);

            PluginManager.SetPropertyValue("RBR.Damage.Battery", GetType(), rbrData.BatteryWearLevel);
            PluginManager.SetPropertyValue("RBR.Damage.OilPump", GetType(), rbrData.OilPumpDamage);
            PluginManager.SetPropertyValue("RBR.Damage.WaterPump", GetType(), rbrData.WaterPumpDamage);
            PluginManager.SetPropertyValue("RBR.Damage.ElectricSystem", GetType(), rbrData.ElectricSystemDamage);
            PluginManager.SetPropertyValue("RBR.Damage.BrakeCircuit", GetType(), rbrData.BrakeCircuitDamage);
            PluginManager.SetPropertyValue("RBR.Damage.OilCooler", GetType(), rbrData.OilCoolerDamage);
            PluginManager.SetPropertyValue("RBR.Damage.Radiator", GetType(), rbrData.RadiatorDamage);
            PluginManager.SetPropertyValue("RBR.Damage.Intercooler", GetType(), rbrData.IntercoolerDamage);
            PluginManager.SetPropertyValue("RBR.Damage.Starter", GetType(), rbrData.StarterDamage);
            PluginManager.SetPropertyValue("RBR.Damage.GearboxActuator", GetType(), rbrData.GearboxActuatorDamage);
            PluginManager.SetPropertyValue("RBR.Damage.Hydraulics", GetType(), rbrData.HydraulicsDamage);

            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.FLMax", GetType(), rbrData.FLWheelPercentSlipAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.FRMax", GetType(), rbrData.FRWheelPercentSlipAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.RLMax", GetType(), rbrData.RLWheelPercentSlipAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.RRMax", GetType(), rbrData.RRWheelPercentSlipAngle);

            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.FLOverMax", GetType(), rbrData.FLWheelExcessSlipAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.FROverMax", GetType(), rbrData.FRWheelExcessSlipAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.RLOverMax", GetType(), rbrData.RLWheelExcessSlipAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.RROverMax", GetType(), rbrData.RRWheelExcessSlipAngle);

            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.FLMax", GetType(), rbrData.FLWheelPercentSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.FRMax", GetType(), rbrData.FRWheelPercentSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.RLMax", GetType(), rbrData.RLWheelPercentSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.RRMax", GetType(), rbrData.RRWheelPercentSlipRatio);    

            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.FLOverMax", GetType(), rbrData.FLWheelExcessSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.FROverMax", GetType(), rbrData.FRWheelExcessSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.RLOverMax", GetType(), rbrData.RLWheelExcessSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.RROverMax", GetType(), rbrData.RRWheelExcessSlipRatio);

            PluginManager.SetPropertyValue("RBR.GaugerPlugin.LockSlip", GetType(), rbrData.GaugerLockSlip);
            PluginManager.SetPropertyValue("RBR.RBRHUD.DeltaTime", GetType(), rbrData.RBRHUDDeltaTime);

        }
    }
}
