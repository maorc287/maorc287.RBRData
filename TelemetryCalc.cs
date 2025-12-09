using System;
using System.Data;


namespace maorc287.RBRDataExtPlugin
{
    internal class TelemetryCalc
    {
        // Base adjustment for oil pressure calculation 
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

        /// <summary>
        /// Smart formatter: float→"m:ss.fff" OR string→float seconds
        /// </summary>
        internal static object FormatTime(object input)
        {
            if (input == null) return "0.000";

            // Input is float → format to "m:ss.fff"
            if (input is float f)
            {
                int minutes = (int)(f / 60);
                float seconds = f % 60;
                return $"{minutes}:{seconds:00.000}";
            }

            // Input is string → parse to float seconds
            if (input is string s)
            {
                try
                {
                    var parts = s.Split(':');
                    if (parts.Length == 2)
                    {
                        int minutes = int.Parse(parts[0]);
                        float seconds = float.Parse(parts[1]);
                        return minutes * 60f + seconds;
                    }
                    return float.Parse(s);
                }
                catch { }
            }

            return input ?? "0.000";
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

        /// Computes the ground speed from longitudinal and lateral speeds.
        internal static float ComputeGroundSpeed(float longitudinalSpeed, float lateralSpeed)
        {
            return (float)Math.Sqrt(longitudinalSpeed * longitudinalSpeed +
                                    lateralSpeed * lateralSpeed);
        }

        internal static float GetWheelSlipRatio(float groundSpeed, float wheelSpeed)
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

        //Calculates the maximum slip from the slip table in the tire model file
        //based on the load index.
        internal static float GetSlipSaturation(float[] arraySlpPk, int indexLoad1, int indexLoad2, float weight)
        {
            if (indexLoad1 < 0 || indexLoad1 >= arraySlpPk.Length ||
                indexLoad2 < 0 || indexLoad2 >= arraySlpPk.Length)
                return 0f;

            float maxSlip = (1.0f - weight) * arraySlpPk[indexLoad1] + weight * arraySlpPk[indexLoad2];
            return maxSlip;
        }

        //Calculates the slip angle in radians from longitudinal and lateral speeds of wheel.
        internal static float GetSlipAngleRad(float groundSpeed, float wheelSpeed,
            float longitudinalSpeed, float lateralSpeed, bool atan2Off = false,
            float correction = 0f)
        {
            const float epsSpeed = 1.5f;
            float slipAngle = 0f;

            if (Math.Abs(groundSpeed) < epsSpeed && Math.Abs(wheelSpeed) < epsSpeed)
                return slipAngle;

            if (!atan2Off)
                slipAngle = (float)Math.Atan2(lateralSpeed, longitudinalSpeed);

            // simple approximation without Atan2 used in RBR calculations with small correction factor
            else
                slipAngle = (lateralSpeed / longitudinalSpeed) - (correction * 0.1f);

            // the slip angle in radians
            return slipAngle;
        }

        /// <summary>   
        /// Fetch the grip levels and excess slip values. From RBR telemetry data.
        /// Outputs: excessAlpha, excessKappa, percentAlpha, percentKappa
        /// those are excess lateral slip, excess longitudinal slip,
        /// and percent lateral and longitudinal grip (0..1).
        /// values are normalized to 1.0 being the peak grip level for the percent values.
        internal static void GetGripLevel(float lateralGrip, float longitudinalGrip,
            out float excessAlpha, out float excessKappa, out float percentAlpha, out float percentKappa)
        {
            excessAlpha = Math.Max(0f, lateralGrip - 1f);
            excessKappa = Math.Max(0f, longitudinalGrip - 1f);
            percentAlpha = Clamp01(lateralGrip);
            percentKappa = Clamp01(longitudinalGrip);
        }

        /// <summary>
        /// not used RBR already does this internally
        /// Computes the effective lateral and longitudinal slip limits, excess beyond those limits,
        /// and normalized percentages relative to the peak.
        /// </summary>
        internal static void GetCombinedSlipData(
            float slipAngleRad,          // Current lateral slip (α)
            float slipRatio,             // Current longitudinal slip (κ)
            float[] slipTableAlpha,      // Pure lateral peak slips from tyre table (SlpPkCrn_L#)
            float[] slipTableLong,       // Pure longitudinal peak slips from tyre table (SlpPkTrct_L#)
            int idx1, int idx2,          // Load band indices for interpolation
            float weightS,               // Load scaling factor
            float surfaceFriction,       // μ, already includes tyre wear, temp, surface effects
                        out float alphaLimit,        // Lateral slip at peak (limit)
            out float longLimit,         // Longitudinal slip at peak (limit)
            out float alphaExcess,       // α beyond limit (≥0)
            out float longExcess,        // κ beyond limit (≥0)
            out float alphaPercent,      // α / αLimit (0..1)
            out float longPercent)       // κ / κLimit (0..1)
        {
            // -----------------------------
            // 1. Pure tyre peak values
            // -----------------------------
            float alphaPeak = GetSlipSaturation(slipTableAlpha, idx1, idx2, weightS);
            float longPeak = GetSlipSaturation(slipTableLong, idx1, idx2, weightS);

            // -----------------------------
            // 2. Zero peak handling - early exit for invalid tyre data
            // -----------------------------
            bool alphaValid = alphaPeak > 0f;
            bool longValid = longPeak > 0f;

            // Initialize safe defaults
            alphaLimit = 0f;
            longLimit = 0f;
            alphaExcess = Math.Max(0f, Math.Abs(slipAngleRad));
            longExcess = Math.Max(0f, Math.Abs(slipRatio));
            alphaPercent = 0f;
            longPercent = 0f;

            // Early exit if both peaks are invalid
            if (!alphaValid && !longValid)
                return;

            // -----------------------------
            // 3. Normalize current slips to peaks (only if valid)
            // -----------------------------
            float aNorm = alphaValid ? Math.Min(Math.Abs(slipAngleRad) / alphaPeak, 1f) : 0f;
            float lNorm = longValid ? Math.Min(Math.Abs(slipRatio) / longPeak, 1f) : 0f;

            // -----------------------------
            // 4. Combined-slip ellipse factor
            // -----------------------------
            float ellipse = 1f - (aNorm * aNorm + lNorm * lNorm);
            ellipse = (ellipse <= 0f) ? 0f : (float)Math.Sqrt(ellipse);

            // -----------------------------
            // 5. Effective slip limits (scaled by surface/wear friction)
            // -----------------------------
            if (alphaValid)
            {
                alphaLimit = alphaPeak * ellipse * surfaceFriction;
                alphaExcess = Math.Max(0f, Math.Abs(slipAngleRad) - alphaLimit);
                alphaPercent = Math.Min(Math.Abs(slipAngleRad) / alphaLimit, 1f);
            }

            if (longValid)
            {
                longLimit = longPeak * ellipse * surfaceFriction;
                longExcess = Math.Max(0f, Math.Abs(slipRatio) - longLimit);
                longPercent = Math.Min(Math.Abs(slipRatio) / longLimit, 1f);
            }
        }


        // A) Physics-ish alpha_max from frictionC, load, cornering stiffness (N/rad)
        public static float ComputeMaxSlipAngleRad(float frictionC, float vLoadN, float corneringStiffnessNPerRad)
        {
            if (corneringStiffnessNPerRad <= 1e-6) return 0.0f;
            return (frictionC * vLoadN) / (corneringStiffnessNPerRad); // radians
        }

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

        internal static bool IsNewRunWindow(float countdownTime)
        {
            // Active just before start (RBR: ~3–5 seconds)
            return countdownTime > 3.9f && countdownTime < 6.9f;
        }


        internal static string GetTireType(int tireID)
        {
            switch (tireID)
            {
                case 0:
                    return "Dry Tarmac";
                case 1:
                    return "Intermediates Tarmac";
                case 2:
                    return "Wet Tarmac";
                case 3:
                    return "Dry Gravel";
                case 4:
                    return "Intermediates Gravel";
                case 5:
                    return "Wet Gravel";
                case 6:
                    return "Snow";
                default:
                    return "Unknown";
            }
        }


    }
}
