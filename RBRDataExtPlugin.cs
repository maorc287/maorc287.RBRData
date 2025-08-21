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
            PluginManager.AddProperty("RBR.Data.WheelLock", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Data.WheelSlip", GetType(), 0, "");


            PluginManager.AddProperty("RBR.Info.OilPressureWarning", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Info.LowBatteryWarning", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Info.WaterTemperatureWarning", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Info.OilTemperatureWarning", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Damage.BatteryDamage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.OilPumpDamage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.WaterPumpDamage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.ElectricSystemDamage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.BrakeCircuitDamage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.OilCoolerDamage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.RadiatorDamage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.IntercoolerDamage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.StarterDamage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.GearboxActuatorDamage", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Damage.HydraulicsDamage", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.WheelSpeed.FL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.WheelSpeed.FR", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.WheelSpeed.RL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.WheelSpeed.RR", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.SteeringAngle.FL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SteeringAngle.FR", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.WheelSlipAngle.FL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.WheelSlipAngle.FR", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.WheelSlipAngle.RL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.WheelSlipAngle.RR", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.WheelSlipRatio.FL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.WheelSlipRatio.FR", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.WheelSlipRatio.RL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.WheelSlipRatio.RR", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Data.GaugerPlugin.LockSlip", GetType(), 0, "");

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
            PluginManager.SetPropertyValue("RBR.Data.WheelLock", GetType(), rbrData.WheelLock);
            PluginManager.SetPropertyValue("RBR.Data.WheelSlip", GetType(), rbrData.WheelSlip);

            PluginManager.SetPropertyValue("RBR.Info.OilPressureWarning", GetType(), rbrData.OilPressureWarning);
            PluginManager.SetPropertyValue("RBR.Info.LowBatteryWarning", GetType(), rbrData.LowBatteryWarning);
            PluginManager.SetPropertyValue("RBR.Info.WaterTemperatureWarning", GetType(), rbrData.WaterTemperatureWarning);
            PluginManager.SetPropertyValue("RBR.Info.OilTemperatureWarning", GetType(), rbrData.OilTemperatureWarning);

            PluginManager.SetPropertyValue("RBR.Damage.BatteryDamage", GetType(), rbrData.BatteryWearLevel);
            PluginManager.SetPropertyValue("RBR.Damage.OilPumpDamage", GetType(), rbrData.OilPumpDamage);
            PluginManager.SetPropertyValue("RBR.Damage.WaterPumpDamage", GetType(), rbrData.WaterPumpDamage);
            PluginManager.SetPropertyValue("RBR.Damage.ElectricSystemDamage", GetType(), rbrData.ElectricSystemDamage);
            PluginManager.SetPropertyValue("RBR.Damage.BrakeCircuitDamage", GetType(), rbrData.BrakeCircuitDamage);
            PluginManager.SetPropertyValue("RBR.Damage.OilCoolerDamage", GetType(), rbrData.OilCoolerDamage);
            PluginManager.SetPropertyValue("RBR.Damage.RadiatorDamage", GetType(), rbrData.RadiatorDamage);
            PluginManager.SetPropertyValue("RBR.Damage.IntercoolerDamage", GetType(), rbrData.IntercoolerDamage);
            PluginManager.SetPropertyValue("RBR.Damage.StarterDamage", GetType(), rbrData.StarterDamage);
            PluginManager.SetPropertyValue("RBR.Damage.GearboxActuatorDamage", GetType(), rbrData.GearboxActuatorDamage);
            PluginManager.SetPropertyValue("RBR.Damage.HydraulicsDamage", GetType(), rbrData.HydraulicsDamage);

            PluginManager.SetPropertyValue("RBR.Wheel.WheelSpeed.FL", GetType(), rbrData.FrontLeftWheelSpeed);
            PluginManager.SetPropertyValue("RBR.Wheel.WheelSpeed.FR", GetType(), rbrData.FrontRightWheelSpeed);
            PluginManager.SetPropertyValue("RBR.Wheel.WheelSpeed.RL", GetType(), rbrData.RearLeftWheelSpeed);
            PluginManager.SetPropertyValue("RBR.Wheel.WheelSpeed.RR", GetType(), rbrData.RearRightWheelSpeed);

            PluginManager.SetPropertyValue("RBR.Wheel.SteeringAngle.FL", GetType(), rbrData.FLWheelSteeringAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.SteeringAngle.FR", GetType(), rbrData.FRWheelSteeringAngle);

            PluginManager.SetPropertyValue("RBR.Wheel.WheelSlipAngle.FL", GetType(), rbrData.FLWheelSlipAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.WheelSlipAngle.FR", GetType(), rbrData.FRWheelSlipAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.WheelSlipAngle.RL", GetType(), rbrData.RLWheelSlipAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.WheelSlipAngle.RR", GetType(), rbrData.RRWheelSlipAngle);

            PluginManager.SetPropertyValue("RBR.Wheel.WheelSlipRatio.FL", GetType(), rbrData.FLWheelSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.WheelSlipRatio.FR", GetType(), rbrData.FRWheelSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.WheelSlipRatio.RL", GetType(), rbrData.RLWheelSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.WheelSlipRatio.RR", GetType(), rbrData.RRWheelSlipRatio);

            PluginManager.SetPropertyValue("RBR.Data.GaugerPlugin.LockSlip", GetType(), rbrData.GaugerLockSlip);

        }
    }
}
