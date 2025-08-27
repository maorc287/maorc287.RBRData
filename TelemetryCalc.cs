using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace maorc287.RBRDataExtPlugin
{
    internal class TelemetryCalc
    {
        // Base adjustment for oil pressure calculation 
        // BitConverter.ToSingle(BitConverter.GetBytes(0x3f8460fe), 0);
        private const float OilPressureBaseAdjustment = 1.03421f;
        private const float OilPressureBaseLimit = 0.02f;

        // Conversion constants
        internal const float pascal_Bar = 1e-5f;
        internal const float bar_Psi = 14.5038f;
        internal const float bar_Kpa = 100f;
        internal const float kelvin_Celcius = 273.15f;

        /// Computes the oil pressure from raw values using RBRHUD logic.
        internal static float ComputeOilPressure(float rawBase, float pressureRaw)
        {
            float pressureBase = (rawBase > OilPressureBaseLimit) ? OilPressureBaseAdjustment :
                (rawBase * OilPressureBaseAdjustment) / OilPressureBaseLimit;
            float pressureRawBar = pressureRaw * pascal_Bar;
            return pressureBase + pressureRawBar;
        }

        /// Formats the pressure value based on the specified unit.
        internal static float FormatPressure(float pressure, string unit)
        {
            if (string.IsNullOrEmpty(unit)) return pressure;

            unit = unit.Trim().ToLowerInvariant();
            switch (unit)
            {
                case "psi":
                    return pressure * bar_Psi;
                case "kpa":
                    return pressure * bar_Kpa;
                case "bar":
                default:
                    return pressure; // default is Bar
            }
        }

        /// Formats the temperature value based on the specified unit.
        internal static float FormatTemperature(float temperature, string unit)
        {
            float tempC = temperature - kelvin_Celcius;
            if (tempC < 0f) tempC = 0f;

            if (string.IsNullOrEmpty(unit)) return tempC;

            unit = unit.Trim().ToLowerInvariant();
            switch (unit)
            {
                case "celcius":
                    return tempC;
                case "fahrenheit":
                    return (tempC * 9f / 5f) + 32f;
                case "kelvin":
                    return temperature;
                default:
                    return tempC; // default is Celsius
            }
        }

        /// Function to compute single whheel rotatinon speed in km/h.
        internal static float ComputeWheelSpeed(float wheelRadius, float wheelOmega)
        {
            float wheelSpeed = Math.Abs(wheelRadius * wheelOmega * 3.6f);
            if (wheelSpeed < 1.0f)
                return 0.0f; // Avoid very small values

            return wheelSpeed;
        }

        /// Computes the ground speed based on velocity and forward direction vectors.
        internal static float ComputeGroundSpeed(
            float velocityX,
            float velocityY,
            float velocityZ,
            float forwardX,
            float forwardY,
            float forwardZ)
        {
            return (velocityX * forwardX +
                    velocityY * forwardY +
                    velocityZ * forwardZ) * -3.6f;
        }

        internal static float ComputeWheelSlipRatio(float groundSpeed, float wheelSpeed)
        {
            const float epsilon = 0.5f; // small threshold

            if (Math.Abs(groundSpeed) < epsilon)
            {
                // Car is almost stationary; if wheel spinning, return +1
                if (wheelSpeed > epsilon)
                    return 1f;
                else
                    return 0f; // wheel not spinning
            }

            // Standard slip ratio calculation
            float slipRatio = (wheelSpeed - groundSpeed) / Math.Abs(groundSpeed);

            // Clamp to [-1, +1]
            if (slipRatio < -1f) return -1f;
            if (slipRatio > 1f) return 1f;

            return slipRatio;
        }


        // Helper to transform world velocity to car local frame (forward/right)
        internal static void WorldToLocal(float velX,
            float velY, float fwdX, float fwdY,
            out float vX_local, out float vY_local)
        {
            float fwdLength = (float)Math.Sqrt(fwdX * fwdX + fwdY * fwdY);
            if (fwdLength < 1e-6f) { vX_local = 0; vY_local = 0; return; }

            float fx = fwdX / fwdLength;
            float fy = fwdY / fwdLength;

            // right vector perpendicular to forward
            float rx = fy;
            float ry = -fx;

            vX_local = velX * fx + velY * fy; // forward velocity
            vY_local = velX * rx + velY * ry; // lateral velocity
        }

        //Calculates the slip angle in radians from longitudinal and lateral speeds of wheel.
        internal static float GetSlipAngleRad(float groundSpeed, float wheelSpeed,
            float longitudinalSpeed, float lateralSpeed , float steeringAngle = 0)
        {
            const float epsSpeed = 1.5f;
            if (Math.Abs(groundSpeed) < epsSpeed | Math.Abs(wheelSpeed) < epsSpeed)
                return 0f;
            if (Math.Abs(longitudinalSpeed) < epsSpeed)
                return 0f;

            // the slip angle in radians
            return (float)Math.Atan2(lateralSpeed, Math.Abs(longitudinalSpeed)) - steeringAngle;
        }

        /// <summary>
        /// Gets slip angle limit for given tire cornering stiffness.
        /// </summary>
        internal static float GetSlipAngleLimit(float currentCrnStiff, float[] cornerStiffnessTable, float[] slipTable)
        {
            if (cornerStiffnessTable == null || slipTable == null || cornerStiffnessTable.Length != slipTable.Length)
                return 0f;

            if (currentCrnStiff <= cornerStiffnessTable[0]) return slipTable[0];
            if (currentCrnStiff >= cornerStiffnessTable[cornerStiffnessTable.Length - 1]) return slipTable[slipTable.Length - 1];

            // find interval
            int i = 0;
            while (i < cornerStiffnessTable.Length - 1 && currentCrnStiff > cornerStiffnessTable[i + 1]) i++;

            float loadLow = cornerStiffnessTable[i];
            float loadHigh = cornerStiffnessTable[i + 1];
            float slipLow = slipTable[i];
            float slipHigh = slipTable[i + 1];

            float t = (currentCrnStiff - loadLow) / (loadHigh - loadLow);
            return slipLow + t * (slipHigh - slipLow); // radians
        }

        /// <summary>
        /// Normalizes current slip angle against the load-dependent limit.
        /// Returns 0.0 (no slip) to 1.0 (at limit).
        /// </summary>
        internal static float GetNormalizedSlip(float currentSlipRad, float currentCrnStiff, float[] cornerStiffnessTable, float[] slipTable)
        {
            float limit = GetSlipAngleLimit(currentCrnStiff, cornerStiffnessTable, slipTable);
            if (limit <= 0.001f) return 0f;
            if (currentSlipRad == 0.00f) return 0f;

            // How far beyond peak (in radians and as a ratio)
            // compute signed excess
            float excessRad = Math.Abs(currentSlipRad) - limit;
            if (excessRad < 0f) excessRad = 0f; // not past limit → zero

            // normalize to [-1, 1] by dividing by limit (optional scaling factor)
            float normalized = Math.Min((excessRad/2f) / limit, 1f);
            normalized *= Math.Sign(currentSlipRad); // now in -1..1
            return normalized;

            // limit current slip to [0..limit]
            //return Math.Max(0.0f, Math.Min(1.0f, currentSlipRad / limit));
        }

        //private static float prevTimestamp = 0f; // in seconds

        /// Clamps a value between 0 and 1.
        internal static float Clamp01(float val) => val < 0f ? 0f : (val > 1f ? 1f : val);


        /// <summary>
        /// Determines the damage level of the oil pump based on its current health oilPumpCondition.
        /// </summary>
        /// <param name="oilPumpCondition">
        /// The oil pump status as a float:
        /// - 1.0 means the oil pump is fully functional (no damage).
        /// - Values decrease as damage increases.
        /// - 0.0 or below means the oil pump has completely failed.
        /// </param>
        /// <returns>
        /// An integer damage level code:
        /// 1 = Fine (near perfect condition),
        /// 2 = Light damage,
        /// 3 = Medium damage,
        /// 4 = Severe damage (significant loss of performance),
        /// 5 = Lost, Oil pump failure (no longer working).
        /// </returns>
        internal static uint OilPumpDamageLevel(float oilPumpCondition)
        {
            if (oilPumpCondition > 0.9f)
                return 1u;

            if (oilPumpCondition > 0.6f)
                return 2u;

            if (oilPumpCondition > 0.2f)
                return 3u;

            if (oilPumpCondition > 0.0f)
                return 4u;

            return 5u;
        }


        /// <summary>
        /// Determines the battery wear level based on the raw battery status batteryCondition normalized to 0.0–1.0 scale.
        /// </summary>
        /// <param name="batteryCondition">
        /// Battery status normalized as a float between 0.0 and 1.0, where 1.0 represents full health.
        /// 
        /// Raw thresholds for in-game warnings (out of 12 in BatterySatauts 0x2B4):
        /// - Below 0.833 (≈ 10.0) turns battery warning light on.
        /// - Below 0.667  (≈ 8.0) starts blinking battery warning light.
        /// - Below 0.5  (≈  6.0) triggers Co-Driver call about battery issues.
        /// </param>
        /// <returns>
        /// An integer wear level code:
        /// 1 = Fine (above ~0.9),
        /// 2 = Light (above ~0.8),
        /// 3 = Medium (above ~0.65),
        /// 4 = Severe (above ~0.5),
        /// 5 = Battery failure or very poor condition (0.5 or below). Not able to start the car.
        /// </returns>
        internal static uint BatteryHealthLevel(float batteryCondition)
        {
            if (batteryCondition > 0.9f)
                return 1u;  // Fine

            if (batteryCondition > 0.8f)
                return 2u;  // Light

            if (batteryCondition > 0.65f)
                return 3u;  // Medium

            if (batteryCondition > 0.5f)
                return 4u;  // Severe

            return 5u;      // Lost
        }


        /// <summary>
        /// Categorizes the intercooler damage based on the damage float intercoolerCondition.
        /// Lower values indicate better condition. Higher values indicate more damage.
        /// </summary>
        /// <param name="intercoolerCondition">
        /// The intercooler damage intercoolerCondition (0.0 = no damage, 0.4 = fully damaged).</param>
        /// <returns>
        /// An integer representing damage severity:
        /// 1 = Fine, 2 = Light, 3 = Medium, 4 = Severe, 5 = Lost.
        /// </returns>
        internal static uint IntercoolerDamageLevel(float intercoolerCondition)
        {
            if (intercoolerCondition < 0.01f)
                return 1u; // Fine

            if (intercoolerCondition < 0.05f)
                return 2u; // Light

            if (intercoolerCondition < 0.1f)
                return 3u; // Medium

            if (intercoolerCondition < 0.4f)
                return 4u; // Severe

            return 5u; // Lost
        }

        /// <summary>
        /// Categorizes radiator damage based on a damage float radiatorCondition.
        /// Lower values mean healthier radiator. Higher values indicate more severe damage.
        /// </summary>
        /// <param name="radiatorCondition">
        /// Radiator damage radiatorCondition (0.0 = perfect, 0.2 = lost).</param>
        /// <returns>
        /// Damage severity level:
        /// 1 = Fine, 2 = Light, 3 = Medium, 5 = Lost.
        /// </returns>
        internal static uint RadiatorDamageLevel(float radiatorCondition)
        {
            if (radiatorCondition < 0.005f)
                return 1u; // Fine

            if (radiatorCondition < 0.03f)
                return 2u; // Light

            if (radiatorCondition < 0.2f)
                return 3u; // Medium

            return 5u; // Lost
        }


        /// Determines if the part is lost or working no intermediate values.
        internal static uint PartWorkingStatus(int value)
        {
            return value == 0 ? 5u : 1u;
        }

        /// Determines if the part is lost or working no intermediate values.
        internal static uint InversePartWorkingStatus(int value)
        {
            return value == 0 ? 1u : 5u;
        }
    }
}
