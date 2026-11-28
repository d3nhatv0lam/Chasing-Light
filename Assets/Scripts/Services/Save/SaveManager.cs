using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// Quản lý dữ liệu lưu trữ của người chơi:
    /// - Lưu cục bộ dưới dạng JSON trong Application.persistentDataPath.
    /// - Quản lý trạng thái chơi: số sao, thời gian tốt nhất, số bước ít nhất.
    /// - Hỗ trợ chế độ Khách và đồng bộ lên Cloud khi đăng ký/đăng nhập tài khoản.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private static SaveManager _instance;

        public static SaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SaveManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("SaveManager");
                        _instance = go.AddComponent<SaveManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Config")]
        [SerializeField] private AuthConfig _config;

        public GameSaveData CurrentSave { get; private set; } = new();

        public bool IsGuest => CurrentSave == null || CurrentSave.isGuest;
        public int TotalStars => CurrentSave?.totalStars ?? 0;

        public event Action<GameSaveData> OnSaveUpdated;
        public event Action<StageRecordItem> OnStageRecordUpdated;

        private bool _isSyncing = false;
        private bool _pendingResync = false;
        private Action<bool, string> _pendingSyncCallbacks;
        private bool _isRefreshingToken = false;
        private readonly List<Action<bool, string>> _pendingTokenRefreshCallbacks = new();

        private string GuestSaveFilePath => Path.Combine(Application.persistentDataPath, "guest_save.json");
        private string GetUserSaveFilePath(string userId) =>
            Path.Combine(Application.persistentDataPath, $"user_save_{SanitizeFileName(userId)}.json");

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (_config == null)
            {
                _config = Resources.Load<AuthConfig>("AuthConfig");
            }

            AutoInitialize();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && !IsGuest && CurrentSave != null && CurrentSave.hasUnsyncedChanges)
            {
                SyncUnsyncedRecordsToServer();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Initialization & Auto Load
        // ─────────────────────────────────────────────────────────────────────

        public void SetConfig(AuthConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Tự động load save dựa theo trạng thái session hiện có.
        /// </summary>
        public void AutoInitialize()
        {
            string savedJwt = GetSavedJwt();
            string savedUserId = GetSavedUserId();

            if (!string.IsNullOrEmpty(savedJwt) && !string.IsNullOrEmpty(savedUserId))
            {
                InitializeForUser(savedUserId);
                if (CurrentSave != null && CurrentSave.hasUnsyncedChanges)
                {
                    SyncUnsyncedRecordsToServer();
                }
                else
                {
                    RefreshFromCloud();
                }
            }
            else
            {
                InitializeForGuest();
            }
        }

        /// <summary>
        /// Khởi tạo và load dữ liệu chế độ Khách.
        /// </summary>
        public void InitializeForGuest()
        {
            CurrentSave = LoadSaveFileWithFallback(GuestSaveFilePath, "guest", true);
            RecalculateTotalStars();
            OnSaveUpdated?.Invoke(CurrentSave);
        }

        /// <summary>
        /// Khởi tạo và load dữ liệu theo User ID đã đăng nhập.
        /// </summary>
        public void InitializeForUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                InitializeForGuest();
                return;
            }

            string path = GetUserSaveFilePath(userId);
            bool fileExisted = File.Exists(path) || File.Exists(path + ".bak");
            CurrentSave = LoadSaveFileWithFallback(path, userId, false);

            if (!fileExisted)
            {
                SaveToDisk();
            }

            RecalculateTotalStars();
            OnSaveUpdated?.Invoke(CurrentSave);
        }

        /// <summary>
        /// Đọc file save với cơ chế tự động fallback khôi phục từ file backup (.bak) nếu file chính hỏng.
        /// </summary>
        private GameSaveData LoadSaveFileWithFallback(string filePath, string defaultUserId, bool isGuest)
        {
            string backupPath = filePath + ".bak";

            // 1. Thử load file chính
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var data = JsonUtility.FromJson<GameSaveData>(json);
                        if (data != null)
                        {
                            return data;
                        }
                    }
                    Debug.LogWarning($"[SaveManager] File save '{filePath}' rỗng hoặc không parse được.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveManager] Lỗi đọc file save '{filePath}': {ex.Message}");
                }
            }

            // 2. Nếu file chính hỏng/lỗi, thử fallback đọc từ file backup (.bak)
            if (File.Exists(backupPath))
            {
                try
                {
                    string bakJson = File.ReadAllText(backupPath);
                    if (!string.IsNullOrWhiteSpace(bakJson))
                    {
                        var bakData = JsonUtility.FromJson<GameSaveData>(bakJson);
                        if (bakData != null)
                        {
                            Debug.Log($"[SaveManager] Khôi phục save thành công từ backup: '{backupPath}'");
                            try { File.Copy(backupPath, filePath, true); } catch { }
                            return bakData;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveManager] Lỗi đọc backup save '{backupPath}': {ex.Message}");
                }
            }

            // 3. Nếu không có cả hai hoặc đều hỏng, tạo save mặc định
            return CreateDefaultSave(defaultUserId, isGuest);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Guest Mode Checks & Actions
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Kiểm tra xem trên máy có tiến trình chơi của khách nào chưa.
        /// </summary>
        public bool HasGuestProgress()
        {
            var guestData = GetGuestSaveData();
            return guestData != null && guestData.stageRecords != null && guestData.stageRecords.Count > 0;
        }

        /// <summary>
        /// Lấy toàn bộ dữ liệu guest hiện lưu trong file hoặc RAM (nếu có).
        /// </summary>
        public GameSaveData GetGuestSaveData()
        {
            if (IsGuest && CurrentSave != null && CurrentSave.stageRecords != null && CurrentSave.stageRecords.Count > 0)
            {
                return CurrentSave;
            }

            if (!File.Exists(GuestSaveFilePath) && !File.Exists(GuestSaveFilePath + ".bak"))
            {
                return null;
            }

            return LoadSaveFileWithFallback(GuestSaveFilePath, "guest", true);
        }

        /// <summary>
        /// Xóa bỏ file lưu trữ của khách và dọn sạch trạng thái Guest trong bộ nhớ RAM.
        /// </summary>
        public void DiscardGuestProgress()
        {
            try
            {
                if (File.Exists(GuestSaveFilePath))
                {
                    File.Delete(GuestSaveFilePath);
                    Debug.Log("[SaveManager] Đã xóa tiến trình Guest cũ.");
                }

                string backupPath = GuestSaveFilePath + ".bak";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Không thể xóa file guest save: {ex.Message}");
            }

            if (IsGuest)
            {
                CurrentSave = CreateDefaultSave("guest", true);
                OnSaveUpdated?.Invoke(CurrentSave);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Query Stage Records
        // ─────────────────────────────────────────────────────────────────────

        public StageRecordItem GetStageRecord(string stageId)
        {
            if (CurrentSave?.stageRecords == null || string.IsNullOrEmpty(stageId))
                return null;

            return CurrentSave.stageRecords.FirstOrDefault(r => r.stageId == stageId);
        }

        public int GetStageStars(string stageId)
        {
            var record = GetStageRecord(stageId);
            return record?.starsEarned ?? 0;
        }

        public bool IsStageCompleted(string stageId)
        {
            var record = GetStageRecord(stageId);
            return record != null && record.starsEarned > 0;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Save Stage Result (Gameplay)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Lưu kết quả khi hoàn thành màn chơi: cập nhật local nếu là kỷ lục mới,
        /// và tự động đẩy lên server nếu đã đăng nhập tài khoản.
        /// </summary>
        public bool SaveStageResult(
            string stageId,
            int stageNumber,
            Difficulty difficulty,
            int starsEarned,
            int timeDoneSeconds,
            int stepCount)
        {
            if (string.IsNullOrEmpty(stageId))
            {
                Debug.LogError("[SaveManager] stageId không được để trống!");
                return false;
            }

            starsEarned = Mathf.Clamp(starsEarned, 1, 3);
            timeDoneSeconds = Mathf.Max(1, timeDoneSeconds);
            stepCount = Mathf.Max(1, stepCount);

            if (CurrentSave.stageRecords == null)
                CurrentSave.stageRecords = new List<StageRecordItem>();

            bool hadPendingSync = CurrentSave.hasUnsyncedChanges;
            var existing = CurrentSave.stageRecords.FirstOrDefault(r => r.stageId == stageId);
            bool isNewBest = false;

            if (existing == null)
            {
                isNewBest = true;
                existing = new StageRecordItem(stageId, stageNumber, (int)difficulty, starsEarned, timeDoneSeconds, stepCount);
                existing.needsSync = true;
                CurrentSave.stageRecords.Add(existing);
            }
            else if (IsBetterRecord(starsEarned, timeDoneSeconds, stepCount, existing))
            {
                isNewBest = true;
                existing.stageNumber = stageNumber;
                existing.difficulty = (int)difficulty;
                existing.starsEarned = starsEarned;
                existing.bestTimeSeconds = timeDoneSeconds;
                existing.bestStepCount = stepCount;
                existing.updatedAt = DateTime.UtcNow.ToString("o");
                existing.needsSync = true;
            }

            if (isNewBest)
            {
                CurrentSave.hasUnsyncedChanges = true;
                RecalculateTotalStars();
                SaveToDisk();
                OnStageRecordUpdated?.Invoke(existing);
                OnSaveUpdated?.Invoke(CurrentSave);
            }

            // Nếu người chơi đã đăng nhập, tự động đẩy lên Cloud
            if (!IsGuest)
            {
                // Nếu trước đó đã có kỷ lục offline chưa đồng bộ, sync toàn bộ lên để tránh sót
                if (hadPendingSync)
                {
                    SyncUnsyncedRecordsToServer();
                }
                else if (isNewBest)
                {
                    PushStageCompleteToServer(stageId, timeDoneSeconds, stepCount, starsEarned);
                }
            }

            return isNewBest;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Cloud Sync Methods
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Gửi kết quả hoàn thành màn chơi lên server (POST /api/v1/records/complete).
        /// </summary>
        public void PushStageCompleteToServer(string stageId, int timeDoneSeconds, int stepCount, int starsEarned)
        {
            PushStageCompleteToServerInternal(stageId, timeDoneSeconds, stepCount, starsEarned, allowRefreshRetry: true);
        }

        private void PushStageCompleteToServerInternal(
            string stageId,
            int timeDoneSeconds,
            int stepCount,
            int starsEarned,
            bool allowRefreshRetry)
        {
            string jwt = GetSavedJwt();
            if (string.IsNullOrEmpty(jwt) || _config == null) return;

            if (!Guid.TryParse(stageId, out _))
            {
                Debug.LogWarning($"[SaveManager] stageId '{stageId}' không phải là GUID hợp lệ. Bỏ qua gửi backend.");
                return;
            }

            int sentTime = Mathf.Max(1, timeDoneSeconds);
            int sentSteps = Mathf.Max(1, stepCount);
            int sentStars = Mathf.Clamp(starsEarned, 1, 3);

            var req = new CompleteStageRequest
            {
                stageId = stageId,
                timeDoneSeconds = sentTime,
                stepCount = sentSteps,
                starsEarned = sentStars
            };

            StartCoroutine(RecordApiClient.CompleteStage(_config, jwt, req, result =>
            {
                if (result.IsSuccess && result.Data != null)
                {
                    Debug.Log($"[SaveManager] Đồng bộ màn chơi lên server thành công! TotalStars={result.Data.totalStars}");
                    var rec = CurrentSave?.stageRecords?.FirstOrDefault(r => r.stageId == stageId);
                    if (rec != null && !IsBetterRecord(rec.starsEarned, rec.bestTimeSeconds, rec.bestStepCount, sentStars, sentTime, sentSteps))
                    {
                        rec.needsSync = false;
                    }
                    if (CurrentSave != null)
                    {
                        CurrentSave.hasUnsyncedChanges = CurrentSave.stageRecords != null && CurrentSave.stageRecords.Any(r => r.needsSync);
                    }
                    SaveToDisk();
                }
                else if (allowRefreshRetry && result.HttpStatusCode == 401)
                {
                    TryRefreshToken((refreshed, err) =>
                    {
                        if (refreshed)
                        {
                            PushStageCompleteToServerInternal(stageId, timeDoneSeconds, stepCount, starsEarned, allowRefreshRetry: false);
                        }
                        else
                        {
                            Debug.LogWarning($"[SaveManager] Refresh token thất bại khi đồng bộ màn chơi: {err}");
                            CurrentSave.hasUnsyncedChanges = true;
                            SaveToDisk();
                        }
                    });
                }
                else
                {
                    Debug.LogWarning($"[SaveManager] Đồng bộ màn chơi lên server thất bại: {result.ErrorMessage}");
                    var rec = CurrentSave?.stageRecords?.FirstOrDefault(r => r.stageId == stageId);
                    if (rec != null)
                    {
                        rec.needsSync = true;
                    }
                    if (CurrentSave != null)
                    {
                        CurrentSave.hasUnsyncedChanges = true;
                    }
                    SaveToDisk();
                }
            }));
        }

        /// <summary>
        /// Đẩy bù toàn bộ kỷ lục local chưa đồng bộ lên server (POST /api/v1/records/sync).
        /// Thường gọi khi có mạng lại, khi mở game sau khi chơi offline, hoặc sau khi login.
        /// </summary>
        public void SyncUnsyncedRecordsToServer(Action<bool, string> onComplete = null)
        {
            SyncUnsyncedRecordsToServerInternal(onComplete, allowRefreshRetry: true);
        }

        private void SyncUnsyncedRecordsToServerInternal(Action<bool, string> onComplete, bool allowRefreshRetry)
        {
            if (_isSyncing)
            {
                _pendingResync = true;
                if (onComplete != null)
                {
                    _pendingSyncCallbacks += onComplete;
                }
                return;
            }

            if (IsGuest)
            {
                onComplete?.Invoke(false, "Không thể sync dữ liệu Guest khi chưa đăng nhập.");
                return;
            }

            string jwt = GetSavedJwt();
            if (string.IsNullOrEmpty(jwt) || _config == null)
            {
                onComplete?.Invoke(false, "Chưa đăng nhập hoặc AuthConfig chưa được gán.");
                return;
            }

            if (CurrentSave?.stageRecords == null || CurrentSave.stageRecords.Count == 0)
            {
                if (CurrentSave != null)
                {
                    CurrentSave.hasUnsyncedChanges = false;
                    SaveToDisk();
                }
                onComplete?.Invoke(true, "Không có kỷ lục nào cần sync.");
                return;
            }

            var syncReq = new SyncRecordsRequest();
            var sentStageIds = new HashSet<string>();
            foreach (var r in CurrentSave.stageRecords)
            {
                if (Guid.TryParse(r.stageId, out _))
                {
                    syncReq.records.Add(new SyncRecordItem
                    {
                        stageId = r.stageId,
                        timeDoneSeconds = Mathf.Max(1, r.bestTimeSeconds),
                        stepCount = Mathf.Max(1, r.bestStepCount),
                        starsEarned = Mathf.Clamp(r.starsEarned, 1, 3)
                    });
                    sentStageIds.Add(r.stageId);
                }
            }

            if (syncReq.records.Count == 0)
            {
                CurrentSave.hasUnsyncedChanges = false;
                SaveToDisk();
                onComplete?.Invoke(true, "Không có kỷ lục hợp lệ để sync.");
                return;
            }

            _isSyncing = true;
            StartCoroutine(RecordApiClient.SyncRecords(_config, jwt, syncReq, result =>
            {
                _isSyncing = false;
                bool shouldResync = _pendingResync;
                _pendingResync = false;
                var queuedCallbacks = _pendingSyncCallbacks;
                _pendingSyncCallbacks = null;

                if (result.IsSuccess && result.Data != null)
                {
                    Debug.Log($"[SaveManager] Sync bù kỷ lục offline lên server thành công! TotalStars={result.Data.totalStars}");
                    MergeCloudRecords(result.Data.records, sentStageIds);
                    onComplete?.Invoke(true, "Progress synced successfully!");

                    if (shouldResync && !IsGuest && CurrentSave != null && CurrentSave.hasUnsyncedChanges)
                    {
                        SyncUnsyncedRecordsToServer(queuedCallbacks);
                    }
                    else
                    {
                        queuedCallbacks?.Invoke(true, "Progress synced successfully!");
                    }
                }
                else if (allowRefreshRetry && result.HttpStatusCode == 401)
                {
                    TryRefreshToken((refreshed, err) =>
                    {
                        if (refreshed)
                        {
                            SyncUnsyncedRecordsToServerInternal(onComplete, allowRefreshRetry: false);
                        }
                        else
                        {
                            Debug.LogWarning($"[SaveManager] Refresh token thất bại khi sync offline: {err}");
                            CurrentSave.hasUnsyncedChanges = true;
                            SaveToDisk();
                            onComplete?.Invoke(false, err ?? "Session expired.");
                        }
                    });
                }
                else
                {
                    Debug.LogWarning($"[SaveManager] Sync bù kỷ lục offline thất bại: {result.ErrorMessage}");
                    string errorMessage = result.ErrorMessage ?? "Failed to sync offline records.";
                    if (CurrentSave != null)
                    {
                        CurrentSave.hasUnsyncedChanges = true;
                        SaveToDisk();
                    }
                    onComplete?.Invoke(false, errorMessage);
                    queuedCallbacks?.Invoke(false, errorMessage);
                }
            }));
        }

        /// <summary>
        /// Đồng bộ toàn bộ tiến trình của Guest lên tài khoản mới đăng nhập (POST /api/v1/records/sync).
        /// </summary>
        public void SyncGuestProgressToAccount(string jwt, string userId, Action<bool, string> onComplete)
        {
            var guestData = GetGuestSaveData();
            if (guestData == null || guestData.stageRecords == null || guestData.stageRecords.Count == 0)
            {
                // Không có guest records, chỉ cần load từ cloud
                FetchAndApplyCloudRecords(jwt, userId, onComplete);
                return;
            }

            if (_config == null)
            {
                onComplete?.Invoke(false, "AuthConfig chưa được gán.");
                return;
            }

            var syncReq = new SyncRecordsRequest();
            var sentStageIds = new HashSet<string>();
            foreach (var r in guestData.stageRecords)
            {
                if (Guid.TryParse(r.stageId, out _))
                {
                    syncReq.records.Add(new SyncRecordItem
                    {
                        stageId = r.stageId,
                        timeDoneSeconds = Mathf.Max(1, r.bestTimeSeconds),
                        stepCount = Mathf.Max(1, r.bestStepCount),
                        starsEarned = Mathf.Clamp(r.starsEarned, 1, 3)
                    });
                    sentStageIds.Add(r.stageId);
                }
            }

            StartCoroutine(RecordApiClient.SyncRecords(_config, jwt, syncReq, result =>
            {
                if (result.IsSuccess && result.Data != null)
                {
                    // Chuyển sang save của user
                    CurrentSave = CreateDefaultSave(userId, false);
                    MergeCloudRecords(result.Data.records, sentStageIds);

                    // Xóa guest save vì đã hợp nhất thành công
                    DiscardGuestProgress();

                    onComplete?.Invoke(true, "Progress synced successfully!");
                }
                else
                {
                    onComplete?.Invoke(false, result.ErrorMessage ?? "Failed to sync progress.");
                }
            }));
        }

        /// <summary>
        /// Làm mới dữ liệu kỷ lục từ Cloud cho tài khoản đang đăng nhập.
        /// Sử dụng token và userId đã lưu trong session.
        /// </summary>
        public void RefreshFromCloud(Action<bool, string> onComplete = null)
        {
            if (IsGuest)
            {
                onComplete?.Invoke(false, "Chế độ Guest không có Cloud Save.");
                return;
            }

            string jwt = GetSavedJwt();
            string userId = GetSavedUserId();
            if (string.IsNullOrEmpty(jwt) || string.IsNullOrEmpty(userId))
            {
                onComplete?.Invoke(false, "Chưa đăng nhập.");
                return;
            }

            FetchAndApplyCloudRecords(jwt, userId, onComplete);
        }

        /// <summary>
        /// Đồng bộ kỷ lục giữa Local và Cloud khi đăng nhập / đổi tài khoản:
        /// Áp dụng flow ưu tiên: Nếu local đang có dữ liệu (kể cả vừa chơi offline),
        /// luôn PUSH lên server trước (POST /records/sync) để merge và lấy về state mới nhất.
        /// Chỉ khi local hoàn toàn rỗng mới gọi GET /records/me.
        /// </summary>
        public void FetchAndApplyCloudRecords(string jwt, string userId, Action<bool, string> onComplete)
        {
            FetchAndApplyCloudRecordsInternal(jwt, userId, onComplete, allowRefreshRetry: true);
        }

        private void FetchAndApplyCloudRecordsInternal(string jwt, string userId, Action<bool, string> onComplete, bool allowRefreshRetry)
        {
            if (_config == null)
            {
                onComplete?.Invoke(false, "AuthConfig is not assigned.");
                return;
            }

            // Đảm bảo dữ liệu local hiện tại của user được nạp
            if (CurrentSave == null || CurrentSave.userId != userId || CurrentSave.isGuest)
            {
                InitializeForUser(userId);
            }

            // Nếu local đang có kỷ lục (kể cả chơi offline hoặc chưa kịp sync), PUSH lên server trước để merge
            if (CurrentSave?.stageRecords != null && CurrentSave.stageRecords.Count > 0)
            {
                Debug.Log("[SaveManager] Phát hiện kỷ lục local, đẩy lên server trước để merge...");
                SyncUnsyncedRecordsToServer(onComplete);
                return;
            }

            // Nếu local hoàn toàn rỗng, PULL dữ liệu từ server về
            StartCoroutine(RecordApiClient.GetMyRecords(_config, jwt, result =>
            {
                if (result.IsSuccess && result.Data != null)
                {
                    MergeCloudRecords(result.Data.records);
                    onComplete?.Invoke(true, null);
                }
                else if (allowRefreshRetry && result.HttpStatusCode == 401)
                {
                    TryRefreshToken((refreshed, err) =>
                    {
                        if (refreshed)
                        {
                            FetchAndApplyCloudRecordsInternal(GetSavedJwt(), userId, onComplete, allowRefreshRetry: false);
                        }
                        else
                        {
                            InitializeForUser(userId);
                            onComplete?.Invoke(false, err ?? "Session expired.");
                        }
                    });
                }
                else
                {
                    // Fallback load local file nếu mạng lỗi
                    InitializeForUser(userId);
                    onComplete?.Invoke(false, result.ErrorMessage ?? "Failed to load records from server.");
                }
            }));
        }

        /// <summary>
        /// Hợp nhất dữ liệu kỷ lục nhận từ Cloud (Two-Way Merge):
        /// - Cập nhật hoặc thêm mới các bản ghi từ server.
        /// - Bảo toàn các bản ghi local đang chờ sync (needsSync = true) chưa được gửi trong request này.
        /// - Đánh dấu needsSync = false cho các bản ghi mà server đã xác nhận.
        /// </summary>
        public void MergeCloudRecords(List<UserRecordDto> serverRecords, HashSet<string> sentStageIds = null)
        {
            if (CurrentSave == null) return;
            if (CurrentSave.stageRecords == null)
                CurrentSave.stageRecords = new List<StageRecordItem>();

            var recordDict = CurrentSave.stageRecords.ToDictionary(r => r.stageId, r => r);

            if (serverRecords != null)
            {
                foreach (var serverRec in serverRecords)
                {
                    if (string.IsNullOrEmpty(serverRec.stageId)) continue;

                    if (recordDict.TryGetValue(serverRec.stageId, out var localRec))
                    {
                        if (IsBetterRecord(serverRec.starsEarned, serverRec.bestTimeSeconds, serverRec.bestStepCount, localRec))
                        {
                            localRec.starsEarned = serverRec.starsEarned;
                            localRec.bestTimeSeconds = serverRec.bestTimeSeconds;
                            localRec.bestStepCount = serverRec.bestStepCount;
                            localRec.stageNumber = serverRec.stageNumber;
                            localRec.difficulty = serverRec.difficulty;
                            localRec.updatedAt = serverRec.updatedAt;
                            localRec.needsSync = false;
                        }
                        else if (sentStageIds != null && sentStageIds.Contains(serverRec.stageId)
                                 && !IsBetterRecord(localRec.starsEarned, localRec.bestTimeSeconds, localRec.bestStepCount,
                                                    serverRec.starsEarned, serverRec.bestTimeSeconds, serverRec.bestStepCount))
                        {
                            localRec.needsSync = false;
                        }
                    }
                    else
                    {
                        var newRec = new StageRecordItem
                        {
                            stageId = serverRec.stageId,
                            stageNumber = serverRec.stageNumber,
                            difficulty = serverRec.difficulty,
                            starsEarned = serverRec.starsEarned,
                            bestTimeSeconds = serverRec.bestTimeSeconds,
                            bestStepCount = serverRec.bestStepCount,
                            updatedAt = serverRec.updatedAt,
                            needsSync = false
                        };
                        CurrentSave.stageRecords.Add(newRec);
                        recordDict[serverRec.stageId] = newRec;
                    }
                }
            }

            RecalculateTotalStars();
            CurrentSave.hasUnsyncedChanges = CurrentSave.stageRecords.Any(r => r.needsSync);
            SaveToDisk();
            OnSaveUpdated?.Invoke(CurrentSave);
        }

        private void TryRefreshToken(Action<bool, string> onComplete)
        {
            string refreshToken = GetSavedRefreshToken();
            if (_config == null || string.IsNullOrEmpty(refreshToken))
            {
                onComplete?.Invoke(false, "Session expired.");
                return;
            }

            if (onComplete != null)
                _pendingTokenRefreshCallbacks.Add(onComplete);

            if (_isRefreshingToken)
                return;

            _isRefreshingToken = true;
            StartCoroutine(AuthApiClient.RefreshToken(_config, refreshToken, result =>
            {
                _isRefreshingToken = false;

                bool refreshed = false;
                string errorMessage = null;

                if (result.IsSuccess && result.Data != null && !string.IsNullOrEmpty(result.Data.accessToken))
                {
                    string jwtKey = _config.PrefKeyJwt;
                    string refreshKey = _config.PrefKeyRefresh;
                    string emailKey = _config.PrefKeyEmail;
                    string userIdKey = _config.PrefKeyUserId;
                    string expiresAtKey = string.IsNullOrEmpty(_config.PrefKeyExpiresAt) ? "auth_expires_at" : _config.PrefKeyExpiresAt;

                    PlayerPrefs.SetString(jwtKey, result.Data.accessToken);
                    if (!string.IsNullOrEmpty(result.Data.refreshToken))
                        PlayerPrefs.SetString(refreshKey, result.Data.refreshToken);
                    if (!string.IsNullOrEmpty(result.Data.email))
                        PlayerPrefs.SetString(emailKey, result.Data.email);
                    if (!string.IsNullOrEmpty(result.Data.userId))
                        PlayerPrefs.SetString(userIdKey, result.Data.userId);
                    if (result.Data.expiresIn > 0)
                    {
                        long expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + result.Data.expiresIn;
                        PlayerPrefs.SetString(expiresAtKey, expiresAt.ToString());
                    }
                    PlayerPrefs.Save();
                    refreshed = true;
                }
                else
                {
                    if (result.HttpStatusCode == 401 || result.HttpStatusCode == 403 || result.HttpStatusCode == 400)
                    {
                        ClearSavedAuthSession();
                    }
                    errorMessage = result.ErrorMessage ?? "Session expired.";
                }

                if (_pendingTokenRefreshCallbacks.Count > 0)
                {
                    var callbacks = new List<Action<bool, string>>(_pendingTokenRefreshCallbacks);
                    _pendingTokenRefreshCallbacks.Clear();
                    foreach (var cb in callbacks)
                    {
                        cb?.Invoke(refreshed, errorMessage);
                    }
                }
            }));
        }

        private void ClearSavedAuthSession()
        {
            if (_config == null) return;

            string expiresAtKey = string.IsNullOrEmpty(_config.PrefKeyExpiresAt) ? "auth_expires_at" : _config.PrefKeyExpiresAt;
            PlayerPrefs.DeleteKey(_config.PrefKeyJwt);
            PlayerPrefs.DeleteKey(_config.PrefKeyEmail);
            PlayerPrefs.DeleteKey(_config.PrefKeyUserId);
            PlayerPrefs.DeleteKey(_config.PrefKeyRefresh);
            PlayerPrefs.DeleteKey(expiresAtKey);
            PlayerPrefs.Save();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static bool IsBetterRecord(int newStars, int newTime, int newSteps, StageRecordItem existing)
        {
            return IsBetterRecord(newStars, newTime, newSteps, existing.starsEarned, existing.bestTimeSeconds, existing.bestStepCount);
        }

        private static bool IsBetterRecord(int newStars, int newTime, int newSteps, int existingStars, int existingTime, int existingSteps)
        {
            if (newStars > existingStars)
                return true;

            if (newStars < existingStars)
                return false;

            if (newTime < existingTime)
                return true;

            if (newTime > existingTime)
                return false;

            return newSteps < existingSteps;
        }

        private void RecalculateTotalStars()
        {
            if (CurrentSave?.stageRecords == null) return;
            CurrentSave.totalStars = CurrentSave.stageRecords.Sum(r => r.starsEarned);
        }

        private void SaveToDisk()
        {
            if (CurrentSave == null) return;

            CurrentSave.lastSavedAt = DateTime.UtcNow.ToString("o");
            if (CurrentSave.stageRecords != null)
            {
                CurrentSave.hasUnsyncedChanges = CurrentSave.stageRecords.Any(r => r.needsSync);
            }

            string targetPath = IsGuest ? GuestSaveFilePath : GetUserSaveFilePath(CurrentSave.userId);
            string tempPath = targetPath + ".tmp";
            string backupPath = targetPath + ".bak";

            try
            {
                string json = JsonUtility.ToJson(CurrentSave, true);
                File.WriteAllText(tempPath, json);

                if (File.Exists(targetPath))
                {
                    File.Copy(targetPath, backupPath, true);
                    File.Delete(targetPath);
                }

                File.Move(tempPath, targetPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Ghi file thất bại ({targetPath}): {ex.Message}");
            }
        }

        private GameSaveData CreateDefaultSave(string userId, bool isGuest)
        {
            return new GameSaveData
            {
                userId = userId ?? "guest",
                isGuest = isGuest,
                totalStars = 0,
                stageRecords = new List<StageRecordItem>(),
                hasUnsyncedChanges = false,
                lastSavedAt = DateTime.UtcNow.ToString("o")
            };
        }

        private List<StageRecordItem> ConvertToRecordItems(List<UserRecordDto> dtos)
        {
            var list = new List<StageRecordItem>();
            if (dtos == null) return list;

            foreach (var dto in dtos)
            {
                list.Add(new StageRecordItem
                {
                    stageId = dto.stageId,
                    stageNumber = dto.stageNumber,
                    difficulty = dto.difficulty,
                    starsEarned = dto.starsEarned,
                    bestTimeSeconds = dto.bestTimeSeconds,
                    bestStepCount = dto.bestStepCount,
                    updatedAt = dto.updatedAt
                });
            }
            return list;
        }

        private string GetSavedJwt()
        {
            return _config != null ? PlayerPrefs.GetString(_config.PrefKeyJwt, "") : PlayerPrefs.GetString("auth_jwt", "");
        }

        private string GetSavedUserId()
        {
            return _config != null ? PlayerPrefs.GetString(_config.PrefKeyUserId, "") : PlayerPrefs.GetString("auth_user_id", "");
        }

        private string GetSavedRefreshToken()
        {
            return _config != null ? PlayerPrefs.GetString(_config.PrefKeyRefresh, "") : PlayerPrefs.GetString("auth_refresh", "");
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "default";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
