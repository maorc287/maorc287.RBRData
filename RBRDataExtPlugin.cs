using SimHub.Plugins;
using System.Windows.Media;
using static maorc287.RBRDataExtPlugin.TelemetryData;
using static maorc287.RBRDataExtPlugin.TelemetryProperties;
using static SimHub.Logging;


namespace maorc287.RBRDataExtPlugin
{
    [PluginDescription("Richard Burns Rally Additional Data Reader")]
    [PluginAuthor("maorc287")]
    [PluginName("RBRDataExt")]

    public class RBRDataExtPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "RBR Additional Data";

        public void Init(PluginManager pluginManager)
        {
            pluginManager.AddProperty(Game_OnStage, GetType(), 0, "");

            pluginManager.AddProperty(Data_EngineOn, GetType(), 0, "");
            pluginManager.AddProperty(Data_OilPressure, GetType(), 0, "");
            pluginManager.AddProperty(Data_RadiatorCoolantTemperature, GetType(), 0, "");
            pluginManager.AddProperty(Data_OilTemperature, GetType(), 0, "");
            pluginManager.AddProperty(Data_BatteryVoltage, GetType(), 0, "");

            pluginManager.AddProperty(Info_OilPressureWarning, GetType(), 0, "");
            pluginManager.AddProperty(Info_LowBatteryWarning, GetType(), 0, "");
            pluginManager.AddProperty(Info_WaterTemperatureWarning, GetType(), 0, "");
            pluginManager.AddProperty(Info_OilTemperatureWarning, GetType(), 0, "");
            pluginManager.AddProperty(Info_TireType, GetType(), 0, "");
            pluginManager.AddProperty(Info_CarSetup, GetType(), 0, "");

            pluginManager.AddProperty(Damage_Battery, GetType(), 0, "");
            pluginManager.AddProperty(Damage_OilPump, GetType(), 0, "");
            pluginManager.AddProperty(Damage_WaterPump, GetType(), 0, "");
            pluginManager.AddProperty(Damage_ElectricSystem, GetType(), 0, "");
            pluginManager.AddProperty(Damage_BrakeCircuit, GetType(), 0, "");
            pluginManager.AddProperty(Damage_OilCooler, GetType(), 0, "");
            pluginManager.AddProperty(Damage_Radiator, GetType(), 0, "");
            pluginManager.AddProperty(Damage_Intercooler, GetType(), 0, "");
            pluginManager.AddProperty(Damage_Starter, GetType(), 0, "");
            pluginManager.AddProperty(Damage_GearboxActuator, GetType(), 0, "");
            pluginManager.AddProperty(Damage_Hydraulics, GetType(), 0, "");

            pluginManager.AddProperty(LatGrip_FL, GetType(), 0, "");
            pluginManager.AddProperty(LatGrip_FR, GetType(), 0, "");
            pluginManager.AddProperty(LatGrip_RL, GetType(), 0, "");
            pluginManager.AddProperty(LatGrip_RR, GetType(), 0, "");

            pluginManager.AddProperty(LatGrip_FL_Falloff, GetType(), 0, "");
            pluginManager.AddProperty(LatGrip_FR_Falloff, GetType(), 0, "");
            pluginManager.AddProperty(LatGrip_RL_Falloff, GetType(), 0, "");
            pluginManager.AddProperty(LatGrip_RR_Falloff, GetType(), 0, "");

            pluginManager.AddProperty(LongGrip_FL, GetType(), 0, "");
            pluginManager.AddProperty(LongGrip_FR, GetType(), 0, "");
            pluginManager.AddProperty(LongGrip_RL, GetType(), 0, "");
            pluginManager.AddProperty(LongGrip_RR, GetType(), 0, "");

            pluginManager.AddProperty(LongGrip_FL_Falloff, GetType(), 0, "");
            pluginManager.AddProperty(LongGrip_FR_Falloff, GetType(), 0, "");
            pluginManager.AddProperty(LongGrip_RL_Falloff, GetType(), 0, "");
            pluginManager.AddProperty(LongGrip_RR_Falloff, GetType(), 0, "");

            pluginManager.AddProperty(RBRHUD_DeltaTime, GetType(), 0, "");

            pluginManager.AddProperty(Info_Delta, GetType(), 0, "");
            pluginManager.AddProperty(Info_Best, GetType(), 0, "");
            pluginManager.AddProperty(Info_TravelledDistance, GetType(), 0, "");
        }


        public void End(PluginManager pluginManager)
        {
            pointerCache.ClearAllCache();
        }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) => null;


        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            var rbrData = ReadTelemetryData(pluginManager);

            pluginManager.SetPropertyValue(Game_OnStage, GetType(), rbrData.IsOnStage);

            pluginManager.SetPropertyValue(Data_EngineOn, GetType(), rbrData.IsEngineOn);
            pluginManager.SetPropertyValue(Data_OilPressure, GetType(), rbrData.OilPressure);
            pluginManager.SetPropertyValue(Data_RadiatorCoolantTemperature, GetType(), rbrData.RadiatorCoolantTemperature);
            pluginManager.SetPropertyValue(Data_OilTemperature, GetType(), rbrData.OilTemperature);
            pluginManager.SetPropertyValue(Data_BatteryVoltage, GetType(), rbrData.BatteryVoltage);

            pluginManager.SetPropertyValue(Info_OilPressureWarning, GetType(), rbrData.OilPressureWarning);
            pluginManager.SetPropertyValue(Info_LowBatteryWarning, GetType(), rbrData.LowBatteryWarning);
            pluginManager.SetPropertyValue(Info_WaterTemperatureWarning, GetType(), rbrData.WaterTemperatureWarning);
            pluginManager.SetPropertyValue(Info_OilTemperatureWarning, GetType(), rbrData.OilTemperatureWarning);

            pluginManager.SetPropertyValue(Info_TireType, GetType(), rbrData.TireType);
            pluginManager.SetPropertyValue(Info_CarSetup, GetType(), rbrData.CarSetup);

            pluginManager.SetPropertyValue(Damage_Battery, GetType(), rbrData.BatteryWearLevel);
            pluginManager.SetPropertyValue(Damage_OilPump, GetType(), rbrData.OilPumpDamage);
            pluginManager.SetPropertyValue(Damage_WaterPump, GetType(), rbrData.WaterPumpDamage);
            pluginManager.SetPropertyValue(Damage_ElectricSystem, GetType(), rbrData.ElectricSystemDamage);
            pluginManager.SetPropertyValue(Damage_BrakeCircuit, GetType(), rbrData.BrakeCircuitDamage);
            pluginManager.SetPropertyValue(Damage_OilCooler, GetType(), rbrData.OilCoolerDamage);
            pluginManager.SetPropertyValue(Damage_Radiator, GetType(), rbrData.RadiatorDamage);
            pluginManager.SetPropertyValue(Damage_Intercooler, GetType(), rbrData.IntercoolerDamage);
            pluginManager.SetPropertyValue(Damage_Starter, GetType(), rbrData.StarterDamage);
            pluginManager.SetPropertyValue(Damage_GearboxActuator, GetType(), rbrData.GearboxActuatorDamage);
            pluginManager.SetPropertyValue(Damage_Hydraulics, GetType(), rbrData.HydraulicsDamage);

            pluginManager.SetPropertyValue(LatGrip_FL, GetType(), rbrData.FLWheelPercentLateral);
            pluginManager.SetPropertyValue(LatGrip_FR, GetType(), rbrData.FRWheelPercentLateral);
            pluginManager.SetPropertyValue(LatGrip_RL, GetType(), rbrData.RLWheelPercentLateral);
            pluginManager.SetPropertyValue(LatGrip_RR, GetType(), rbrData.RRWheelPercentLateral);

            pluginManager.SetPropertyValue(LatGrip_FL_Falloff, GetType(), rbrData.FLWheelExcessLateral);
            pluginManager.SetPropertyValue(LatGrip_FR_Falloff, GetType(), rbrData.FRWheelExcessLateral);
            pluginManager.SetPropertyValue(LatGrip_RL_Falloff, GetType(), rbrData.RLWheelExcessLateral);
            pluginManager.SetPropertyValue(LatGrip_RR_Falloff, GetType(), rbrData.RRWheelExcessLateral);

            pluginManager.SetPropertyValue(LongGrip_FL, GetType(), rbrData.FLWheelPercentLongitudinal);
            pluginManager.SetPropertyValue(LongGrip_FR, GetType(), rbrData.FRWheelPercentLongitudinal);
            pluginManager.SetPropertyValue(LongGrip_RL, GetType(), rbrData.RLWheelPercentLongitudinal);
            pluginManager.SetPropertyValue(LongGrip_RR, GetType(), rbrData.RRWheelPercentLongitudinal);

            pluginManager.SetPropertyValue(LongGrip_FL_Falloff, GetType(), rbrData.FLWheelExcessLongitudinal);
            pluginManager.SetPropertyValue(LongGrip_FR_Falloff, GetType(), rbrData.FRWheelExcessLongitudinal);
            pluginManager.SetPropertyValue(LongGrip_RL_Falloff, GetType(), rbrData.RLWheelExcessLongitudinal);
            pluginManager.SetPropertyValue(LongGrip_RR_Falloff, GetType(), rbrData.RRWheelExcessLongitudinal);

            pluginManager.SetPropertyValue(RBRHUD_DeltaTime, GetType(), rbrData.RBRHUDDeltaTime);

            pluginManager.SetPropertyValue(Info_Delta, GetType(), rbrData.DeltaTime);
            pluginManager.SetPropertyValue(Info_Best, GetType(), rbrData.BestTime);
            pluginManager.SetPropertyValue(Info_TravelledDistance, GetType(), rbrData.TravelledDistance);
        }

    }
}
