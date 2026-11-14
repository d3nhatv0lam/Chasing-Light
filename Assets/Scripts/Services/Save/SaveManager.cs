using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ChasingLight.Network.DTOs;
using ChasingLight.Network.Clients;

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
                var authService = FindFirstObjectByType<AuthService>();
                if (authService != null)
                {
                    _config = authService.Config;
                }
            }

            AutoInitialize();
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
            if (File.Exists(GuestSaveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(GuestSaveFilePath);
                    CurrentSave = JsonUtility.FromJson<GameSaveData>(json) ?? CreateDefaultSave("guest", true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveManager] Lỗi đọc guest save: {ex.Message}. Khởi tạo save mặc định.");
                    CurrentSave = CreateDefaultSave("guest", true);
                }
            }
            else
            {
                CurrentSave = CreateDefaultSave("guest", true);
            }

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
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    CurrentSave = JsonUtility.FromJson<GameSaveData>(json) ?? CreateDefaultSave(userId, false);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveManager] Lỗi đọc user save {userId}: {ex.Message}. Khởi tạo save mặc định.");
                    CurrentSave = CreateDefaultSave(userId, false);
                }
            }
            else
            {
                CurrentSave = CreateDefaultSave(userId, false);
                SaveToDisk();
            }

            RecalculateTotalStars();
            OnSaveUpdated?.Invoke(CurrentSave);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Guest Mode Checks & Actions
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Kiểm tra xem trên máy có tiến trình chơi của khách nào chưa.
        /// </summary>
        public bool HasGuestProgress()
        {
            if (!File.Exists(GuestSaveFilePath)) return false;

            try
            {
                string json = File.ReadAllText(GuestSaveFilePath);
                var guestData = JsonUtility.FromJson<GameSaveData>(json);
                return guestData != null && guestData.stageRecords != null && guestData.stageRecords.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lấy toàn bộ dữ liệu guest hiện lưu trong file (nếu có).
        /// </summary>
        public GameSaveData GetGuestSaveData()
        {
            if (!File.Exists(GuestSaveFilePath)) return null;
            try
            {
                string json = File.ReadAllText(GuestSaveFilePath);
                return JsonUtility.FromJson<GameSaveData>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Xóa bỏ file lưu trữ của khách.
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
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Không thể xóa file guest save: {ex.Message}");
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

            var existing = CurrentSave.stageRecords.FirstOrDefault(r => r.stageId == stageId);
            bool isNewBest = false;

            if (existing == null)
            {
                isNewBest = true;
                existing = new StageRecordItem(stageId, stageNumber, (int)difficulty, starsEarned, timeDoneSeconds, stepCount);
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
                PushStageCompleteToServer(stageId, timeDoneSeconds, stepCount, starsEarned);
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
            string jwt = GetSavedJwt();
            if (string.IsNullOrEmpty(jwt) || _config == null) return;

            if (!Guid.TryParse(stageId, out _))
            {
                Debug.LogWarning($"[SaveManager] stageId '{stageId}' không phải là GUID hợp lệ. Bỏ qua gửi backend.");
                return;
            }

            var req = new CompleteStageRequest
            {
                stageId = stageId,
                timeDoneSeconds = timeDoneSeconds,
                stepCount = stepCount,
                starsEarned = starsEarned
            };

            StartCoroutine(RecordApiClient.CompleteStage(_config, jwt, req, result =>
            {
                if (result.IsSuccess && result.Data != null)
                {
                    Debug.Log($"[SaveManager] Đồng bộ màn chơi lên server thành công! TotalStars={result.Data.totalStars}");
                    CurrentSave.hasUnsyncedChanges = false;
                    SaveToDisk();
                }
                else
                {
                    Debug.LogWarning($"[SaveManager] Đồng bộ màn chơi lên server thất bại: {result.ErrorMessage}");
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
                }
            }

            StartCoroutine(RecordApiClient.SyncRecords(_config, jwt, syncReq, result =>
            {
                if (result.IsSuccess && result.Data != null)
                {
                    // Chuyển sang save của user
                    CurrentSave = CreateDefaultSave(userId, false);
                    CurrentSave.totalStars = result.Data.totalStars;
                    CurrentSave.stageRecords = ConvertToRecordItems(result.Data.records);
                    CurrentSave.hasUnsyncedChanges = false;
                    SaveToDisk();

                    // Xóa guest save vì đã hợp nhất thành công
                    DiscardGuestProgress();

                    OnSaveUpdated?.Invoke(CurrentSave);
                    onComplete?.Invoke(true, "Progress synced successfully!");
                }
                else
                {
                    onComplete?.Invoke(false, result.ErrorMessage ?? "Failed to sync progress.");
                }
            }));
        }

        /// <summary>
        /// Lấy toàn bộ kỷ lục từ Server về máy và ghi đè local (GET /api/v1/records/me).
        /// </summary>
        public void FetchAndApplyCloudRecords(string jwt, string userId, Action<bool, string> onComplete)
        {
            if (_config == null)
            {
                onComplete?.Invoke(false, "AuthConfig is not assigned.");
                return;
            }

            StartCoroutine(RecordApiClient.GetMyRecords(_config, jwt, result =>
            {
                if (result.IsSuccess && result.Data != null)
                {
                    CurrentSave = CreateDefaultSave(userId, false);
                    CurrentSave.totalStars = result.Data.totalStars;
                    CurrentSave.stageRecords = ConvertToRecordItems(result.Data.records);
                    CurrentSave.hasUnsyncedChanges = false;
                    SaveToDisk();

                    OnSaveUpdated?.Invoke(CurrentSave);
                    onComplete?.Invoke(true, null);
                }
                else
                {
                    // Fallback load local file nếu mạng lỗi
                    InitializeForUser(userId);
                    onComplete?.Invoke(false, result.ErrorMessage ?? "Failed to load records from server.");
                }
            }));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static bool IsBetterRecord(int newStars, int newTime, int newSteps, StageRecordItem existing)
        {
            if (newStars > existing.starsEarned)
                return true;

            if (newStars < existing.starsEarned)
                return false;

            if (newTime < existing.bestTimeSeconds)
                return true;

            if (newTime > existing.bestTimeSeconds)
                return false;

            return newSteps < existing.bestStepCount;
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
            string path = IsGuest ? GuestSaveFilePath : GetUserSaveFilePath(CurrentSave.userId);

            try
            {
                string json = JsonUtility.ToJson(CurrentSave, true);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Ghi file thất bại ({path}): {ex.Message}");
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
