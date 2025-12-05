# RBR Data Extension (RBRDataExt)

SimHub plugin that exposes additional Richard Burns Rally (RBR) telemetry data for use in dashboards, LEDs, and scripts.

## Main Data Groups

- **Game / Engine**
  - `RBR.Game.OnStage`
  - `RBR.Data.EngineOn`
  - `RBR.Data.OilPressure`
  - `RBR.Data.RadiatorCoolantTemeperature`
  - `RBR.Data.OilTemperature`
  - `RBR.Data.BatteryVoltage`

- **Warnings**
  - `RBR.Info.OilPressureWarning`
  - `RBR.Info.LowBatteryWarning`
  - `RBR.Info.WaterTemperatureWarning`
  - `RBR.Info.OilTemperatureWarning`

- **Damage**
  - `RBR.Damage.Battery`
  - `RBR.Damage.OilPump`
  - `RBR.Damage.WaterPump`
  - `RBR.Damage.ElectricSystem`
  - `RBR.Damage.BrakeCircuit`
  - `RBR.Damage.OilCooler`
  - `RBR.Damage.Radiator`
  - `RBR.Damage.Intercooler`
  - `RBR.Damage.Starter`
  - `RBR.Damage.GearboxActuator`
  - `RBR.Damage.Hydraulics`

- **Wheel Slip Angle**
  - Max seen: `RBR.Wheel.SlipAngle.FLMax/FRMax/RLMax/RRMax`
  - Over the limit value: `RBR.Wheel.SlipAngle.FLOverMax/...`

- **Wheel Slip Ratio**
  - Max seen: `RBR.Wheel.SlipRatio.FLMax/FRMax/RLMax/RRMax`
  - Over the limit value: `RBR.Wheel.SlipRatio.FLOverMax/...`

- **Other**
  - `RBR.GaugerPlugin.LockSlip`
  - `RBR.RBRHUD.DeltaTime`

## Installation (Quick)

- Build the project and copy the generated `RBRDataExt.dll` to the SimHub folder.
- Restart SimHub and enable the plugin in the Additional Plugins section.
