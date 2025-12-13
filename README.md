# Richard Burns Rally Additional Data Reader (RBRDataExt)

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

- **Wheels Lateral Grip**
  - Percent Grip value 0 to 1: `RBR.LateralGrip.FL/FR/RL/RR`
  - Over the limit value 0 to 1: `RBR.LateralGrip.FL.Falloff/FR.Falloff/RL.Falloff/RR.Falloff`

- **Wheels Longitudinal Grip**
  - Percent Grip value 0 to 1: `RBR.LongitudinalGrip.FL/FR/RL/RR`
  - Over the limit value 0 to 1: `RBR.LongitudinalGrip.FL.Falloff/FR.Falloff/RL.Falloff/RR.Falloff`

- **Other**
  - `RBR.RBRHUD.DeltaTime`

  - `RBR.Info.Best`
  - `RBR.Info.Delta`
  - `RBR.Info.TravelledDistance`
  - `RBR.Info.TireType`
  - `RBR.Info.CarSetup`

## Installation

- Close SimHub if running.
- Extract the contents of the latest release zip file to the SimHub installation folder or build the project and copy the generated `RBRDataExt.dll` and `SqlNado.dll` to the SimHub installation folder.
- Usually located at `C:\Program Files (x86)\SimHub\`.
- Restart SimHub and enable the plugin in the Additional Plugins section.

## Usage of RBRHUD Delta Time Property

- To use the RBRHUD Delta time property (`RBR.RBRHUD.DeltaTime`) the overlay elements must be present in the active RBRHUD overlay.
- To activate those elements copy the `delta.ini` file from the zip file to the `RBRHUD\Overlays\` folder in your RBR Plugins directory and select the overlay in the in game RBRHUD menu (`F4`).
- If you already have an Active overlay without delta times you can merge the contents of `delta.ini` into your existing overlay ini file.
