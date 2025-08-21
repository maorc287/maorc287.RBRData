using maorc287.RBRDataPluginExt;
using SimHub.Plugins;
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

            PluginManager.AddProperty("RBR.Data.EngineOn", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Data.OilPressure", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Data.RadiatorCoolantTemeperature", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Data.OilTemperature", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Data.BatteryVoltage", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Data.GroundSpeed", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Data.WheelLockRatio", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Data.WheelSlipRatio", GetType(), 0, "");


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

            PluginManager.AddProperty("RBR.Wheel.Speed.FL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.Speed.FR", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.Speed.RL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.Speed.RR", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.SteeringAngle.FL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SteeringAngle.FR", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.SlipAngle.FL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.FR", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.RL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.RR", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.SlipRatio.FL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.FR", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.RL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.RR", GetType(), 0, "");

            PluginManager.AddProperty("RBR.GaugerPlugin.LockSlip", GetType(), 0, "");

        }

        public void End(PluginManager pluginManager) { }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) => null;


        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            var rbrData = TelemetryData.ReadTelemetryData();

            string pressureUnit = (string)PluginManager.GetPropertyValue("DataCorePlugin.GameData.OilPressureUnit");
            string temperatureUnit = (string)PluginManager.GetPropertyValue("DataCorePlugin.GameData.TemperatureUnit");


            PluginManager.SetPropertyValue("RBR.Game.OnStage", GetType(), rbrData.IsOnStage);

            PluginManager.SetPropertyValue("RBR.Data.EngineOn", GetType(), rbrData.IsEngineOn);

            PluginManager.SetPropertyValue("RBR.Data.OilPressure", GetType(),
                TelemetryData.FormatPressure(rbrData.OilPressure, pressureUnit));

            PluginManager.SetPropertyValue("RBR.Data.RadiatorCoolantTemeperature", GetType(),
                TelemetryData.FormatTemperature(rbrData.RadiatorCoolantTemperature, temperatureUnit));
            PluginManager.SetPropertyValue("RBR.Data.OilTemperature", GetType(),
                TelemetryData.FormatTemperature(rbrData.OilTemperature, temperatureUnit));

            PluginManager.SetPropertyValue("RBR.Data.BatteryVoltage", GetType(), rbrData.BatteryVoltage);

            PluginManager.SetPropertyValue("RBR.Data.GroundSpeed", GetType(), rbrData.GroundSpeed);
            PluginManager.SetPropertyValue("RBR.Data.WheelLockRatio", GetType(), rbrData.WheelLock);
            PluginManager.SetPropertyValue("RBR.Data.WheelSlipRatio", GetType(), rbrData.WheelSlip);

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

            PluginManager.SetPropertyValue("RBR.Wheel.Speed.FL", GetType(), rbrData.FLWheelSpeed);
            PluginManager.SetPropertyValue("RBR.Wheel.Speed.FR", GetType(), rbrData.FRWheelSpeed);
            PluginManager.SetPropertyValue("RBR.Wheel.Speed.RL", GetType(), rbrData.RLWheelSpeed);
            PluginManager.SetPropertyValue("RBR.Wheel.Speed.RR", GetType(), rbrData.RRWheelSpeed);

            PluginManager.SetPropertyValue("RBR.Wheel.SteeringAngle.FL", GetType(), rbrData.FLWheelSteeringAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.SteeringAngle.FR", GetType(), rbrData.FRWheelSteeringAngle);

            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.FL", GetType(), rbrData.FLWheelSlipAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.FR", GetType(), rbrData.FRWheelSlipAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.RL", GetType(), rbrData.RLWheelSlipAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.RR", GetType(), rbrData.RRWheelSlipAngle);

            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.FL", GetType(), rbrData.FLWheelSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.FR", GetType(), rbrData.FRWheelSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.RL", GetType(), rbrData.RLWheelSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.RR", GetType(), rbrData.RRWheelSlipRatio);

            PluginManager.SetPropertyValue("RBR.GaugerPlugin.LockSlip", GetType(), rbrData.GaugerLockSlip);

        }
    }
}
