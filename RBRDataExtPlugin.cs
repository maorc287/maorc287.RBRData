using SimHub.Plugins;
using System.Windows.Media;
using static maorc287.RBRDataExtPlugin.TelemetryData;


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

            pluginManager.AddProperty("RBR.Game.OnStage", GetType(), 0, "");

            pluginManager.AddProperty("RBR.Data.EngineOn", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Data.TireType", GetType(), 0, "");


            pluginManager.AddProperty("RBR.Data.OilPressure", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Data.RadiatorCoolantTemeperature", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Data.OilTemperature", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Data.BatteryVoltage", GetType(), 0, "");

            pluginManager.AddProperty("RBR.Info.OilPressureWarning", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Info.LowBatteryWarning", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Info.WaterTemperatureWarning", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Info.OilTemperatureWarning", GetType(), 0, "");

            pluginManager.AddProperty("RBR.Damage.Battery", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Damage.OilPump", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Damage.WaterPump", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Damage.ElectricSystem", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Damage.BrakeCircuit", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Damage.OilCooler", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Damage.Radiator", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Damage.Intercooler", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Damage.Starter", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Damage.GearboxActuator", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Damage.Hydraulics", GetType(), 0, "");

            pluginManager.AddProperty("RBR.LateralGrip.FL", GetType(), 0, "");
            pluginManager.AddProperty("RBR.LateralGrip.FR", GetType(), 0, "");
            pluginManager.AddProperty("RBR.LateralGrip.RL", GetType(), 0, "");
            pluginManager.AddProperty("RBR.LateralGrip.RR", GetType(), 0, "");

            pluginManager.AddProperty("RBR.LateralGrip.FL.Falloff", GetType(), 0, "");
            pluginManager.AddProperty("RBR.LateralGrip.FR.Falloff", GetType(), 0, "");
            pluginManager.AddProperty("RBR.LateralGrip.RL.Falloff", GetType(), 0, "");
            pluginManager.AddProperty("RBR.LateralGrip.RR.Falloff", GetType(), 0, "");

            pluginManager.AddProperty("RBR.LongitudinalGrip.FL", GetType(), 0, "");
            pluginManager.AddProperty("RBR.LongitudinalGrip.FR", GetType(), 0, "");
            pluginManager.AddProperty("RBR.LongitudinalGrip.RL", GetType(), 0, "");
            pluginManager.AddProperty("RBR.LongitudinalGrip.RR", GetType(), 0, "");

            pluginManager.AddProperty("RBR.LongitudinalGrip.FL.Falloff", GetType(), 0, "");
            pluginManager.AddProperty("RBR.LongitudinalGrip.FR.Falloff", GetType(), 0, "");
            pluginManager.AddProperty("RBR.LongitudinalGrip.RL.Falloff", GetType(), 0, "");
            pluginManager.AddProperty("RBR.LongitudinalGrip.RR.Falloff", GetType(), 0, "");

            pluginManager.AddProperty("RBR.GaugerPlugin.LockSlip", GetType(), 0, "");
            pluginManager.AddProperty("RBR.RBRHUD.DeltaTime", GetType(), 0, "");

            pluginManager.AddProperty("RBR.Time.Delta", GetType(), 0, "");
            pluginManager.AddProperty("RBR.Time.Best", GetType(), 0, "");


        }

        public void End(PluginManager pluginManager)
        {
            pointerCache.ClearAllCache();
        }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) => null;


        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            var rbrData = ReadTelemetryData(pluginManager);

            pluginManager.SetPropertyValue("RBR.Game.OnStage", GetType(), rbrData.IsOnStage);

            pluginManager.SetPropertyValue("RBR.Data.EngineOn", GetType(), rbrData.IsEngineOn);
            pluginManager.SetPropertyValue("RBR.Data.TireType", GetType(), rbrData.CurrentTireType);

            pluginManager.SetPropertyValue("RBR.Data.OilPressure", GetType(),
               rbrData.OilPressure);

            pluginManager.SetPropertyValue("RBR.Data.RadiatorCoolantTemperature", GetType(),
                rbrData.RadiatorCoolantTemperature);
            pluginManager.SetPropertyValue("RBR.Data.OilTemperature", GetType(),
                rbrData.OilTemperature);

            pluginManager.SetPropertyValue("RBR.Data.BatteryVoltage", GetType(), rbrData.BatteryVoltage);

            pluginManager.SetPropertyValue("RBR.Info.OilPressureWarning", GetType(), rbrData.OilPressureWarning);
            pluginManager.SetPropertyValue("RBR.Info.LowBatteryWarning", GetType(), rbrData.LowBatteryWarning);
            pluginManager.SetPropertyValue("RBR.Info.WaterTemperatureWarning", GetType(), rbrData.WaterTemperatureWarning);
            pluginManager.SetPropertyValue("RBR.Info.OilTemperatureWarning", GetType(), rbrData.OilTemperatureWarning);

            pluginManager.SetPropertyValue("RBR.Damage.Battery", GetType(), rbrData.BatteryWearLevel);
            pluginManager.SetPropertyValue("RBR.Damage.OilPump", GetType(), rbrData.OilPumpDamage);
            pluginManager.SetPropertyValue("RBR.Damage.WaterPump", GetType(), rbrData.WaterPumpDamage);
            pluginManager.SetPropertyValue("RBR.Damage.ElectricSystem", GetType(), rbrData.ElectricSystemDamage);
            pluginManager.SetPropertyValue("RBR.Damage.BrakeCircuit", GetType(), rbrData.BrakeCircuitDamage);
            pluginManager.SetPropertyValue("RBR.Damage.OilCooler", GetType(), rbrData.OilCoolerDamage);
            pluginManager.SetPropertyValue("RBR.Damage.Radiator", GetType(), rbrData.RadiatorDamage);
            pluginManager.SetPropertyValue("RBR.Damage.Intercooler", GetType(), rbrData.IntercoolerDamage);
            pluginManager.SetPropertyValue("RBR.Damage.Starter", GetType(), rbrData.StarterDamage);
            pluginManager.SetPropertyValue("RBR.Damage.GearboxActuator", GetType(), rbrData.GearboxActuatorDamage);
            pluginManager.SetPropertyValue("RBR.Damage.Hydraulics", GetType(), rbrData.HydraulicsDamage);

            pluginManager.SetPropertyValue("RBR.LateralGrip.FL", GetType(), rbrData.FLWheelPercentLateral);
            pluginManager.SetPropertyValue("RBR.LateralGrip.FR", GetType(), rbrData.FRWheelPercentLateral);
            pluginManager.SetPropertyValue("RBR.LateralGrip.RL", GetType(), rbrData.RLWheelPercentLateral);
            pluginManager.SetPropertyValue("RBR.LateralGrip.RR", GetType(), rbrData.RRWheelPercentLateral);

            pluginManager.SetPropertyValue("RBR.LateralGrip.FL.Falloff", GetType(), rbrData.FLWheelExcessLateral);
            pluginManager.SetPropertyValue("RBR.LateralGrip.FR.Falloff", GetType(), rbrData.FRWheelExcessLateral);
            pluginManager.SetPropertyValue("RBR.LateralGrip.RL.Falloff", GetType(), rbrData.RLWheelExcessLateral);
            pluginManager.SetPropertyValue("RBR.LateralGrip.RR.Falloff", GetType(), rbrData.RRWheelExcessLateral);

            pluginManager.SetPropertyValue("RBR.LongitudinalGrip.FL", GetType(), rbrData.FLWheelPercentLongitudinal);
            pluginManager.SetPropertyValue("RBR.LongitudinalGrip.FR", GetType(), rbrData.FRWheelPercentLongitudinal);
            pluginManager.SetPropertyValue("RBR.LongitudinalGrip.RL", GetType(), rbrData.RLWheelPercentLongitudinal);
            pluginManager.SetPropertyValue("RBR.LongitudinalGrip.RR", GetType(), rbrData.RRWheelPercentLongitudinal);

            pluginManager.SetPropertyValue("RBR.LongitudinalGrip.FL.Falloff", GetType(), rbrData.FLWheelExcessLongitudinal);
            pluginManager.SetPropertyValue("RBR.LongitudinalGrip.FR.Falloff", GetType(), rbrData.FRWheelExcessLongitudinal);
            pluginManager.SetPropertyValue("RBR.LongitudinalGrip.RL.Falloff", GetType(), rbrData.RLWheelExcessLongitudinal);
            pluginManager.SetPropertyValue("RBR.LongitudinalGrip.RR.Falloff", GetType(), rbrData.RRWheelExcessLongitudinal);

            pluginManager.SetPropertyValue("RBR.GaugerPlugin.LockSlip", GetType(), rbrData.GaugerSlip);
            pluginManager.SetPropertyValue("RBR.RBRHUD.DeltaTime", GetType(), rbrData.RBRHUDDeltaTime);

            pluginManager.SetPropertyValue("RBR.Time.Delta", GetType(), rbrData.DeltaTime);
            pluginManager.SetPropertyValue("RBR.Time.Best", GetType(), rbrData.BestTime);

        }
    }
}
