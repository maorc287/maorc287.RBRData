using SimHub;
using SqlNado;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace maorc287.RBRDataExtPlugin
{
    public class DeltaCalc
    {
        private static float[] _ghostSplitTimes;
        private static bool _isLoaded;
        private static int _lastStageId = -1;
        private static int _lastCarId = -1;
        private static DateTime _lastLoadAttempt = DateTime.MinValue;
        private static readonly TimeSpan LoadCooldown = TimeSpan.FromSeconds(2);

        // C#6: use string as cache key "stage_car"
        private static readonly Dictionary<string, int> _uidCache =
            new Dictionary<string, int>();
        private static readonly Dictionary<string, float[]> _splitsCache =
            new Dictionary<string, float[]>();
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);
        private static DateTime _cacheExpiry = DateTime.MinValue;

        private static float _stageLength = 0f;


        internal static void SetStageLength(float stageLengthMeters)
        {
            _stageLength = stageLengthMeters;
        }
        internal static void LoadDeltaData(int stageId, int carId)
        {
            if (DateTime.Now - _lastLoadAttempt < LoadCooldown)
                return;
            _lastLoadAttempt = DateTime.Now;

            Logging.Current.Debug(string.Format(
                "[RBRDataExt] LoadDeltaData - Stage: {0}, Car: {1}", stageId, carId));

            if (stageId == _lastStageId && carId == _lastCarId && _isLoaded)
            {
                Logging.Current.Debug("[RBRDataExt] Already loaded, skipping");
                return;
            }

            string dbPath = Path.Combine(MemoryReader.RBRGamePath ?? "",
                                         "Plugins", "RBRHUD", "delta_times.db");
            Logging.Current.Info("[RBRDataExt] DB Path: " + dbPath);

            if (!File.Exists(dbPath))
            {
                Logging.Current.Warn("[RBRDataExt] DB file NOT FOUND: " + dbPath);
                _isLoaded = false;
                return;
            }

            string key = stageId + "_" + carId;

            // Cache: if still valid, reuse splits
            if (DateTime.Now < _cacheExpiry && _splitsCache.ContainsKey(key))
            {
                Logging.Current.Info("[RBRDataExt] Using cached splits");
                _ghostSplitTimes = _splitsCache[key];
                _isLoaded = _ghostSplitTimes != null && _ghostSplitTimes.Length > 0;
                _lastStageId = stageId;
                _lastCarId = carId;
                return;
            }

            // Use SQLNado to find best UID in records table
            int bestUid = FindBestUidWithSqlNado(dbPath, stageId, carId);
            if (bestUid == 0)
            {
                Logging.Current.Warn(string.Format(
                    "[RBRDataExt] No matching UID found for stage {0}, car {1}",
                    stageId, carId));
                _isLoaded = false;
                return;
            }

            // Load splits with simple retry on file lock
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    _ghostSplitTimes = LoadSplitsForUid(dbPath, bestUid);
                    break;
                }
                catch (IOException ex)
                {
                    if (ex.HResult == -2147024864) // sharing violation
                    {
                        Logging.Current.Warn(string.Format(
                            "[RBRDataExt] Splits load locked (attempt {0}/3)", attempt));
                        if (attempt < 3)
                            System.Threading.Thread.Sleep(250);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            int splitCount = _ghostSplitTimes != null ? _ghostSplitTimes.Length : 0;
            Logging.Current.Info(string.Format(
                "[RBRDataExt] Loaded {0} splits for best UID {1}",
                splitCount, bestUid));

            _isLoaded = splitCount > 0;
            _lastStageId = stageId;
            _lastCarId = carId;

            if (_isLoaded)
            {
                _uidCache[key] = bestUid;
                _splitsCache[key] = _ghostSplitTimes;
                _cacheExpiry = DateTime.Now + CacheTtl;
            }
        }
        internal static float CalculateDelta(float currentDistanceM, float currentTimeS)
        {
            if (!_isLoaded || _ghostSplitTimes == null || _ghostSplitTimes.Length == 0)
                return 0f;

            float delta = 0f;
            float dist = currentDistanceM;

            // RBRHUD: idx = floor(dist / 10)
            int idx = (int)(dist / 10.0f);
            int count = _ghostSplitTimes.Length;

            if (count > 1)
            {
                if (idx < 0)
                {
                    // Before first split, just use first value
                    float ghostTime0 = _ghostSplitTimes[0];
                    delta = currentTimeS - ghostTime0;
                }
                else if (idx < count - 1)
                {
                    // Normal interpolation between idx and idx+1
                    float t0 = _ghostSplitTimes[idx];
                    float t1 = _ghostSplitTimes[idx + 1];

                    float segmentStartDist = idx * 10.0f;
                    float factor = (dist - segmentStartDist) / 10.0f;

                    float ghostTime = t0 + (t1 - t0) * factor;
                    delta = currentTimeS - ghostTime;
                }
                else
                {
                    // Past last split, clamp to last value
                    float ghostTimeLast = _ghostSplitTimes[count - 1];
                    delta = currentTimeS - ghostTimeLast;
                }
            }
            else
            {
                // Only one split, use it directly
                delta = currentTimeS - _ghostSplitTimes[0];
            }

            return delta;
        }


        internal static bool IsReady
        {
            get { return _isLoaded; }
        }

        internal static int SplitCount
        {
            get { return _ghostSplitTimes != null ? _ghostSplitTimes.Length : 0; }
        }

        // --------------------------------------------------------------------
        // SQLNado-based DB access
        // --------------------------------------------------------------------
        private static int FindBestUidWithSqlNado(string dbPath, int stageId, int carId)
        {
            try
            {
                using (var db = new SQLiteDatabase(dbPath))
                {
                    // Debug: total row count
                    object countObj = db.ExecuteScalar(
                        "SELECT COUNT(*) FROM records;",
                        (Func<SQLiteError, SQLiteOnErrorAction>)null,
                        new object[0]);
                    int total = Convert.ToInt32(countObj, CultureInfo.InvariantCulture);
                    Logging.Current.Info(string.Format("[RBRDataExt] records total rows: {0}", total));

                    // Debug: show a couple of rows for this stage+car to verify mapping
                    string debugSql = string.Format(CultureInfo.InvariantCulture, @"
                SELECT uid, stage_id, car_id, finish_time
                FROM records
                WHERE stage_id = {0} AND car_id = {1}
                ORDER BY finish_time ASC
                LIMIT 5;", stageId, carId);

                    foreach (var row in db.LoadRows(
                        debugSql,
                        (Func<SQLiteError, SQLiteOnErrorAction>)null,
                        new object[0]))
                    {
                        int duid = Convert.ToInt32(row["uid"], CultureInfo.InvariantCulture);
                        int dstage = Convert.ToInt32(row["stage_id"], CultureInfo.InvariantCulture);
                        int dcar = Convert.ToInt32(row["car_id"], CultureInfo.InvariantCulture);
                        float dt = Convert.ToSingle(row["finish_time"], CultureInfo.InvariantCulture);

                        Logging.Current.Info(string.Format(
                            "[RBRDataExt] DB match row: uid={0}, stage_id={1}, car_id={2}, time={3:F3}",
                            duid, dstage, dcar, dt));
                    }

                    // Actual best‑lap selection for this exact stage+car
                    string bestSql = string.Format(CultureInfo.InvariantCulture, @"
                SELECT uid, stage_id, car_id, finish_time
                FROM records
                WHERE stage_id = {0}
                  AND car_id   = {1}
                  AND finish_time > 0
                  AND finish_time < 20000
                ORDER BY finish_time ASC
                LIMIT 1;", stageId, carId);

                    foreach (var row in db.LoadRows(
                        bestSql,
                        (Func<SQLiteError, SQLiteOnErrorAction>)null,
                        new object[0]))
                    {
                        int uid = Convert.ToInt32(row["uid"], CultureInfo.InvariantCulture);
                        int dbStage = Convert.ToInt32(row["stage_id"], CultureInfo.InvariantCulture);
                        int dbCar = Convert.ToInt32(row["car_id"], CultureInfo.InvariantCulture);
                        float bestT = Convert.ToSingle(row["finish_time"], CultureInfo.InvariantCulture);

                        Logging.Current.Info(string.Format(
                            "[RBRDataExt] Best UID: {0} (time: {1:F3}s) for stage_id {2}, car_id {3}",
                            uid, bestT, dbStage, dbCar));

                        return uid;
                    }

                    Logging.Current.Info(string.Format(
                        "[RBRDataExt] No row found for stage_id={0}, car_id={1}", stageId, carId));
                }
            }
            catch (Exception ex)
            {
                Logging.Current.Warn("[RBRDataExt] FindBestUid error: " + ex.Message);
            }

            return 0;
        }

        private static float[] LoadSplitsForUid(string dbPath, int uid)
        {
            try
            {
                using (var db = new SQLiteDatabase(dbPath))
                {
                    string sql = string.Format(CultureInfo.InvariantCulture, @"
                SELECT distance, time
                FROM data
                WHERE uid = {0}
                ORDER BY distance;", uid);

                    Dictionary<int, float> splits = new Dictionary<int, float>();

                    foreach (var row in db.LoadRows(
                        sql,
                        (Func<SQLiteError, SQLiteOnErrorAction>)null,
                        new object[0]))
                    {
                        int distance = Convert.ToInt32(row["distance"], CultureInfo.InvariantCulture);
                        float t = Convert.ToSingle(row["time"], CultureInfo.InvariantCulture);

                        if (distance >= 0 && t >= 0f && !float.IsNaN(t))
                        {
                            int index = distance / 10;  // 10 m buckets, identical to RBRHUD
                            splits[index] = t;
                        }
                    }

                    if (splits.Count == 0)
                        return null;

                    // Build compact array 0..maxIndex, values exactly as in DB
                    int maxIndex = 0;
                    foreach (int key in splits.Keys)
                        if (key > maxIndex) maxIndex = key;

                    float[] arr = new float[maxIndex + 1];
                    foreach (var kv in splits)
                        if (kv.Key >= 0 && kv.Key < arr.Length)
                            arr[kv.Key] = kv.Value;

                    Logging.Current.Info(string.Format(
                        "[RBRDataExt] Loaded {0} splits (0..{1}) for uid={2}",
                        splits.Count, maxIndex, uid));

                    return arr;
                }
            }
            catch (Exception ex)
            {
                Logging.Current.Warn("[RBRDataExt] SQLNado LoadSplits error: " + ex.Message);
                return null;
            }
        }



        private static float[] BuildSplitArray(Dictionary<int, float> splits)
        {
            if (splits == null || splits.Count == 0)
                return null;

            int maxIndex = 0;
            foreach (int key in splits.Keys)
                if (key > maxIndex) maxIndex = key;

            float[] arr = new float[maxIndex + 1];
            foreach (KeyValuePair<int, float> kv in splits)
            {
                if (kv.Key >= 0 && kv.Key < arr.Length)
                    arr[kv.Key] = kv.Value;
            }

            return arr;
        }

    }
}
