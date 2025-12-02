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

            PluginManager.AddProperty("RBR.Wheel.SlipAngle.FLMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.FRMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.RLMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.RRMax", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.SlipAngle.FLOverMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.FROverMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.RLOverMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.RROverMax", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.SlipAngle.FLLimit", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.FRLimit", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.RLLimit", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipAngle.RRLimit", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.SlipRatio.FL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.FR", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.RL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.RR", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.SlipRatio.FLMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.FRMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.RLMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.RRMax", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.SlipRatio.FLOverMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.FROverMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.RLOverMax", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.RROverMax", GetType(), 0, "");

            PluginManager.AddProperty("RBR.Wheel.SlipRatio.FLLimit", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.FRLimit", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.RLLimit", GetType(), 0, "");
            PluginManager.AddProperty("RBR.Wheel.SlipRatio.RRLimit", GetType(), 0, "");

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

            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.FLMax", GetType(), rbrData.FLWheelMaxSlipAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.FRMax", GetType(), rbrData.FRWheelMaxSlipAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.RLMax", GetType(), rbrData.RLWheelMaxSlipAngle);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.RRMax", GetType(), rbrData.RRWheelMaxSlipAngle);

            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.FLOverMax", GetType(), rbrData.FLWheelSlipAngleOver);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.FROverMax", GetType(), rbrData.FRWheelSlipAngleOver);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.RLOverMax", GetType(), rbrData.RLWheelSlipAngleOver);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.RROverMax", GetType(), rbrData.RRWheelSlipAngleOver);

            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.FLLimit", GetType(), rbrData.FLWheelLimitSlipAngleRad);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.FRLimit", GetType(), rbrData.FRWheelLimitSlipAngleRad);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.RLLimit", GetType(), rbrData.RLWheelLimitSlipAngleRad);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipAngle.RRLimit", GetType(), rbrData.RRWheelLimitSlipAngleRad);

            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.FL", GetType(), rbrData.FLWheelSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.FR", GetType(), rbrData.FRWheelSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.RL", GetType(), rbrData.RLWheelSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.RR", GetType(), rbrData.RRWheelSlipRatio);

            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.FLMax", GetType(), rbrData.FLWheelMaxSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.FRMax", GetType(), rbrData.FRWheelMaxSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.RLMax", GetType(), rbrData.RLWheelMaxSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.RRMax", GetType(), rbrData.RRWheelMaxSlipRatio);    

            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.FLOverMax", GetType(), rbrData.FLWheelSlipRatioOver);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.FROverMax", GetType(), rbrData.FRWheelSlipRatioOver);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.RLOverMax", GetType(), rbrData.RLWheelSlipRatioOver);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.RROverMax", GetType(), rbrData.RRWheelSlipRatioOver);

            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.FLLimit", GetType(), rbrData.FLWheelLimitSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.FRLimit", GetType(), rbrData.FRWheelLimitSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.RLLimit", GetType(), rbrData.RLWheelLimitSlipRatio);
            PluginManager.SetPropertyValue("RBR.Wheel.SlipRatio.RRLimit", GetType(), rbrData.RRWheelLimitSlipRatio);

            PluginManager.SetPropertyValue("RBR.GaugerPlugin.LockSlip", GetType(), rbrData.GaugerLockSlip);
            PluginManager.SetPropertyValue("RBR.RBRHUD.DeltaTime", GetType(), rbrData.RBRHUDDeltaTime);

        }
    }
}
