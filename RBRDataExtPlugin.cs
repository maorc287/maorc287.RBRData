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

            PluginManager.AddProperty("RBR.LateralGrip.FL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.LateralGrip.FR", GetType(), 0, "");
            PluginManager.AddProperty("RBR.LateralGrip.RL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.LateralGrip.RR", GetType(), 0, "");

            PluginManager.AddProperty("RBR.LateralGrip.FL.Falloff", GetType(), 0, "");
            PluginManager.AddProperty("RBR.LateralGrip.FR.Falloff", GetType(), 0, "");
            PluginManager.AddProperty("RBR.LateralGrip.RL.Falloff", GetType(), 0, "");
            PluginManager.AddProperty("RBR.LateralGrip.RR.Falloff", GetType(), 0, "");

            PluginManager.AddProperty("RBR.LongitudinalGrip.FL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.LongitudinalGrip.FR", GetType(), 0, "");
            PluginManager.AddProperty("RBR.LongitudinalGrip.RL", GetType(), 0, "");
            PluginManager.AddProperty("RBR.LongitudinalGrip.RR", GetType(), 0, "");

            PluginManager.AddProperty("RBR.LongitudinalGrip.FL.Falloff", GetType(), 0, "");
            PluginManager.AddProperty("RBR.LongitudinalGrip.FR.Falloff", GetType(), 0, "");
            PluginManager.AddProperty("RBR.LongitudinalGrip.RL.Falloff", GetType(), 0, "");
            PluginManager.AddProperty("RBR.LongitudinalGrip.RR.Falloff", GetType(), 0, "");

            PluginManager.AddProperty("RBR.GaugerPlugin.LockSlip", GetType(), 0, "");
            PluginManager.AddProperty("RBR.RBRHUD.DeltaTime", GetType(), 0, "");

            //PluginManager.AddProperty("RBR.DeltaTime", GetType(), 0, "");

        }

        public void End(PluginManager pluginManager) { }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) => null;


        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            var rbrData = ReadTelemetryData();

            // Data Needed from SimHub Core Plugin For Delta calculation:
             
            int carModelId = (int)PluginManager.GetPropertyValue("DataCorePlugin.GameRawData.CarModelId");
            int trackId = (int)PluginManager.GetPropertyValue("DataCorePlugin.GameRawData.TrackId");
            string carClass = (string)PluginManager.GetPropertyValue("DataCorePlugin.GameData.CarClass");
            float travelledDistance = (float)PluginManager.GetPropertyValue("DataCorePlugin.GameRawData.TravelledDistance");
            float raceTime = (float)PluginManager.GetPropertyValue("DataCorePlugin.GameRawData.RaceTime");


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

            PluginManager.SetPropertyValue("RBR.LateralGrip.FL", GetType(), rbrData.FLWheelPercentLateral);
            PluginManager.SetPropertyValue("RBR.LateralGrip.FR", GetType(), rbrData.FRWheelPercentLateral);
            PluginManager.SetPropertyValue("RBR.LateralGrip.RL", GetType(), rbrData.RLWheelPercentLateral);
            PluginManager.SetPropertyValue("RBR.LateralGrip.RR", GetType(), rbrData.RRWheelPercentLateral);

            PluginManager.SetPropertyValue("RBR.LateralGrip.FL.Falloff", GetType(), rbrData.FLWheelExcessLateral);
            PluginManager.SetPropertyValue("RBR.LateralGrip.FR.Falloff", GetType(), rbrData.FRWheelExcessLateral);
            PluginManager.SetPropertyValue("RBR.LateralGrip.RL.Falloff", GetType(), rbrData.RLWheelExcessLateral);
            PluginManager.SetPropertyValue("RBR.LateralGrip.RR.Falloff", GetType(), rbrData.RRWheelExcessLateral);

            PluginManager.SetPropertyValue("RBR.LongitudinalGrip.FL", GetType(), rbrData.FLWheelPercentLongitudinal);
            PluginManager.SetPropertyValue("RBR.LongitudinalGrip.FR", GetType(), rbrData.FRWheelPercentLongitudinal);
            PluginManager.SetPropertyValue("RBR.LongitudinalGrip.RL", GetType(), rbrData.RLWheelPercentLongitudinal);
            PluginManager.SetPropertyValue("RBR.LongitudinalGrip.RR", GetType(), rbrData.RRWheelPercentLongitudinal);    

            PluginManager.SetPropertyValue("RBR.LongitudinalGrip.FL.Falloff", GetType(), rbrData.FLWheelExcessLongitudinal);
            PluginManager.SetPropertyValue("RBR.LongitudinalGrip.FR.Falloff", GetType(), rbrData.FRWheelExcessLongitudinal);
            PluginManager.SetPropertyValue("RBR.LongitudinalGrip.RL.Falloff", GetType(), rbrData.RLWheelExcessLongitudinal);
            PluginManager.SetPropertyValue("RBR.LongitudinalGrip.RR.Falloff", GetType(), rbrData.RRWheelExcessLongitudinal);

            PluginManager.SetPropertyValue("RBR.GaugerPlugin.LockSlip", GetType(), rbrData.GaugerLockSlip);
            PluginManager.SetPropertyValue("RBR.RBRHUD.DeltaTime", GetType(), rbrData.RBRHUDDeltaTime);

            // Delta Time Calculation
            /*
            DeltaCalculator.LoadDeltaData(trackId, carModelId);
            PluginManager.SetPropertyValue("RBR.DeltaTime", GetType(), 
                DeltaCalculator.CalculateDelta(travelledDistance,raceTime));*/

        }
    }
}
