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
        private static float[] _bestSplitTimes;
        private static bool _isLoaded;
        private static bool _noDataFound = false;
        private static int _lastStageId = -1;
        private static int _lastCarId = -1;
        private static bool _noSplitFound = false;

        // C#6: use string as cache key "stage_car"
        private static readonly Dictionary<string, int> _uidCache =
            new Dictionary<string, int>();
        private static readonly Dictionary<string, float[]> _splitsCache =
            new Dictionary<string, float[]>();

        internal static bool IsReady { get { return _isLoaded; } }
        internal static bool HasData { get { return !_noDataFound; } }
        internal static bool HasSplit { get { return !_noSplitFound; } }
        internal static int SplitCount { get { return _bestSplitTimes != null ? _bestSplitTimes.Length : 0; } }


        private static float _bestTime = 0f;
        internal static float BestTimeSeconds { get { return _bestTime; } }

        internal static void LoadDeltaData(int stageId, int carId, float countdownTime)
        {
            // CRITICAL: REFRESH PATH RIGHT BEFORE DB ACCESS
            MemoryReader.UpdateRBRGamePath();

            // NEW RUN: allow scanning again
            if (countdownTime > 2.9f && countdownTime < 4.9f)
            {
                _noDataFound = false;
                _noSplitFound = false;
                _isLoaded = false;        // ← recommended: reset loaded state for new run
                _lastStageId = -1;        // ← optional but helps force reload
                _lastCarId = -1;
            }

            if (_noSplitFound) return;    // no UID for this stage/car this run
            if (_noDataFound) return;     // DB missing or persistent error

            if (stageId <= 0)
            {
                Logging.Current.Debug("[RBRDataExt] Invalid stage ID");
                return;
            }

            if (stageId == _lastStageId && carId == _lastCarId && _isLoaded)
            {
                Logging.Current.Debug("[RBRDataExt] Already loaded, skipping");
                return;
            }

            string dbPath = Path.Combine(MemoryReader.RBRGamePath ?? "",
                                         "Plugins", "RBRHUD", "delta_times.db");
            Logging.Current.Info("[RBRDataExt] RBRHUD Delta Times DB Path: " + dbPath);

            if (!File.Exists(dbPath))
            {
                Logging.Current.Warn("[RBRDataExt]Data file NOT FOUND: " + dbPath);
                _isLoaded = false;
                _noDataFound = true;      // global: don’t try again until countdownTime reset
                return;
            }

            string key = stageId + "_" + carId;

            // Find best UID in records table
            int bestUid = FindBestUid(dbPath, stageId, carId);
            if (bestUid == 0)
            {
                Logging.Current.Warn(string.Format(
                    "[RBRDataExt] No matching UID found for stage {0}, car {1}",
                    stageId, carId));
                _isLoaded = false;        // keep false
                _noSplitFound = true;     // only this stage/car this run
                _lastStageId = stageId;   // optional but consistent
                _lastCarId = carId;
                return;
            }

            // Load splits with simple retry on file lock
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    _bestSplitTimes = LoadSplitsForUid(dbPath, bestUid);
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
                        // Some other IO error: mark as no data for this session
                        Logging.Current.Warn("[RBRDataExt] Error loading splits: " + ex.Message);
                        _isLoaded = false;
                        _noDataFound = true;   // global “don’t try again” until restart
                        return;
                    }
                }
            }

            int splitCount = _bestSplitTimes != null ? _bestSplitTimes.Length : 0;
            Logging.Current.Info(string.Format(
                "[RBRDataExt] Loaded {0} splits for best UID {1}",
                splitCount, bestUid));

            _isLoaded = splitCount > 0;
            _lastStageId = stageId;
            _lastCarId = carId;

            if (_isLoaded)
            {
                _uidCache[key] = bestUid;
                _splitsCache[key] = _bestSplitTimes;
            }
        }


        internal static float CalculateDelta(float travelledDistanceM, float currentTimeS)
        {
            if (!_isLoaded || _bestSplitTimes == null || _bestSplitTimes.Length == 0)
                return 0f;

            int idx = (int)(travelledDistanceM / 10.0f);  // ← METERS, not time!
            int count = _bestSplitTimes.Length;

            float ghostTime;
            if (idx < 0)
            {
                ghostTime = _bestSplitTimes[0];
            }
            else if (idx >= count - 1)
            {
                ghostTime = _bestSplitTimes[count - 1];
            }
            else
            {
                float t0 = _bestSplitTimes[idx];
                float t1 = _bestSplitTimes[idx + 1];
                float segmentStartDist = idx * 10.0f;
                float factor = (travelledDistanceM - segmentStartDist) / 10.0f;
                ghostTime = t0 + (t1 - t0) * factor;
            }

            return currentTimeS - ghostTime;  // Time - interpolated ghost time
        }



        // --------------------------------------------------------------------
        // Find best UID for given stageId and carId
        // --------------------------------------------------------------------
        private static int FindBestUid(string dbPath, int stageId, int carId)
        {
            try
            {
                _bestTime = 0f;

                using (var db = new SQLiteDatabase(dbPath))
                {
                    // 1) Exact match: same stage + car
                    string bestExactSql = string.Format(CultureInfo.InvariantCulture, @"
                SELECT uid, stage_id, car_id, finish_time
                FROM records
                WHERE stage_id = {0}
                  AND car_id   = {1}
                  AND finish_time > 0
                  AND finish_time < 20000
                ORDER BY finish_time ASC
                LIMIT 1;", stageId, carId);

                    foreach (var row in db.LoadRows(
                        bestExactSql,
                        (Func<SQLiteError, SQLiteOnErrorAction>)null,
                        new object[0]))
                    {
                        int uid = Convert.ToInt32(row["uid"], CultureInfo.InvariantCulture);
                        int dbStage = Convert.ToInt32(row["stage_id"], CultureInfo.InvariantCulture);
                        int dbCar = Convert.ToInt32(row["car_id"], CultureInfo.InvariantCulture);
                        float bestT = Convert.ToSingle(row["finish_time"], CultureInfo.InvariantCulture);

                        _bestTime = bestT;

                        Logging.Current.Info(string.Format(
                            "[RBRDataExt] Best UID (exact): {0} (time: {1:F3}s) for stage_id {2}, car_id {3}",
                            uid, bestT, dbStage, dbCar));

                        return uid;
                    }

                    // 2) No exact car match: find car_group for this car_id (any stage)
                    string groupSql = string.Format(CultureInfo.InvariantCulture, @"
                SELECT car_group
                FROM records
                WHERE car_id = {0}
                ORDER BY finish_time ASC
                LIMIT 1;", carId);

                    object groupObj = db.ExecuteScalar(
                        groupSql,
                        (Func<SQLiteError, SQLiteOnErrorAction>)null,
                        new object[0]);

                    if (groupObj == null || groupObj == DBNull.Value)
                    {
                        Logging.Current.Info(string.Format(
                            "[RBRDataExt] No car_group found for car_id={0}, cannot use group fallback", carId));
                        return 0;
                    }

                    string carGroup = Convert.ToString(groupObj, CultureInfo.InvariantCulture);

                    Logging.Current.Info(string.Format(
                        "[RBRDataExt] Using car_group '{0}' as fallback for car_id={1}", carGroup, carId));

                    // 3) Fallback: best time on this stage for same group
                    // Note: car_group is TEXT, so we quote it
                    string bestGroupSql = string.Format(CultureInfo.InvariantCulture, @"
                SELECT uid, stage_id, car_id, car_group, finish_time
                FROM records
                WHERE stage_id = {0}
                  AND car_group = '{1}'
                  AND finish_time > 0
                  AND finish_time < 20000
                ORDER BY finish_time ASC
                LIMIT 1;", stageId, carGroup.Replace("'", "''")); // escape quotes

                    foreach (var row in db.LoadRows(
                        bestGroupSql,
                        (Func<SQLiteError, SQLiteOnErrorAction>)null,
                        new object[0]))
                    {
                        int uid = Convert.ToInt32(row["uid"], CultureInfo.InvariantCulture);
                        int dbStage = Convert.ToInt32(row["stage_id"], CultureInfo.InvariantCulture);
                        int dbCar = Convert.ToInt32(row["car_id"], CultureInfo.InvariantCulture);
                        string dbGrp = Convert.ToString(row["car_group"], CultureInfo.InvariantCulture);
                        float bestT = Convert.ToSingle(row["finish_time"], CultureInfo.InvariantCulture);

                        _bestTime = bestT;

                        Logging.Current.Info(string.Format(
                            "[RBRDataExt] Best UID (group fallback): {0} (time: {1:F3}s) for stage_id {2}, car_id {3}, group '{4}'",
                            uid, bestT, dbStage, dbCar, dbGrp));

                        return uid;
                    }

                    Logging.Current.Info(string.Format(
                        "[RBRDataExt] No row found for stage_id={0} in car_group='{1}'", stageId, carGroup));
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
                Logging.Current.Warn("[RBRDataExt] LoadSplits error: " + ex.Message);
                return null;
            }
        }

    }
}
