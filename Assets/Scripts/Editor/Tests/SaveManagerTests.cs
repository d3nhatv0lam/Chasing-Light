using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ChasingLight;

namespace ChasingLight.Tests
{
    [TestFixture]
    public class SaveManagerTests
    {
        private GameObject _saveManagerGo;
        private SaveManager _saveManager;
        private string _guestPath;

        [SetUp]
        public void Setup()
        {
            _guestPath = Path.Combine(Application.persistentDataPath, "guest_save.json");
            CleanupFiles();

            _saveManagerGo = new GameObject("TestSaveManager");
            _saveManager = _saveManagerGo.AddComponent<SaveManager>();
            _saveManager.InitializeForGuest();
        }

        [TearDown]
        public void Teardown()
        {
            if (_saveManagerGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_saveManagerGo);
            }

            CleanupFiles();
        }

        private void CleanupFiles()
        {
            if (File.Exists(_guestPath)) File.Delete(_guestPath);
            if (File.Exists(_guestPath + ".bak")) File.Delete(_guestPath + ".bak");
            if (File.Exists(_guestPath + ".tmp")) File.Delete(_guestPath + ".tmp");
        }

        [Test]
        public void SaveStageResult_NewStage_SavesSuccessfullyAndCalculatesTotalStars()
        {
            string stageId = Guid.NewGuid().ToString();

            bool isNewBest = _saveManager.SaveStageResult(
                stageId: stageId,
                stageNumber: 1,
                difficulty: Difficulty.Easy,
                starsEarned: 2,
                timeDoneSeconds: 15,
                stepCount: 8
            );

            Assert.IsTrue(isNewBest);
            Assert.AreEqual(2, _saveManager.TotalStars);
            Assert.AreEqual(2, _saveManager.GetStageStars(stageId));

            var record = _saveManager.GetStageRecord(stageId);
            Assert.IsNotNull(record);
            Assert.AreEqual(15, record.bestTimeSeconds);
            Assert.AreEqual(8, record.bestStepCount);
        }

        [Test]
        public void SaveStageResult_BetterStars_UpdatesRecord()
        {
            string stageId = Guid.NewGuid().ToString();

            _saveManager.SaveStageResult(stageId, 1, Difficulty.Easy, starsEarned: 2, timeDoneSeconds: 15, stepCount: 10);
            Assert.AreEqual(2, _saveManager.TotalStars);

            // Re-play with 3 stars (better)
            bool isNewBest = _saveManager.SaveStageResult(stageId, 1, Difficulty.Easy, starsEarned: 3, timeDoneSeconds: 18, stepCount: 12);

            Assert.IsTrue(isNewBest);
            Assert.AreEqual(3, _saveManager.TotalStars);
            Assert.AreEqual(3, _saveManager.GetStageStars(stageId));
        }

        [Test]
        public void SaveStageResult_SameStars_BetterTime_UpdatesRecord()
        {
            string stageId = Guid.NewGuid().ToString();

            _saveManager.SaveStageResult(stageId, 1, Difficulty.Easy, starsEarned: 3, timeDoneSeconds: 20, stepCount: 10);

            // Re-play with same stars (3) but faster time (14s)
            bool isNewBest = _saveManager.SaveStageResult(stageId, 1, Difficulty.Easy, starsEarned: 3, timeDoneSeconds: 14, stepCount: 12);

            Assert.IsTrue(isNewBest);
            var record = _saveManager.GetStageRecord(stageId);
            Assert.AreEqual(14, record.bestTimeSeconds);
            Assert.AreEqual(12, record.bestStepCount);
        }

        [Test]
        public void SaveStageResult_WorseResult_DoesNotOverwrite()
        {
            string stageId = Guid.NewGuid().ToString();

            _saveManager.SaveStageResult(stageId, 1, Difficulty.Easy, starsEarned: 3, timeDoneSeconds: 10, stepCount: 5);

            // Re-play with worse stars (2)
            bool isNewBest = _saveManager.SaveStageResult(stageId, 1, Difficulty.Easy, starsEarned: 2, timeDoneSeconds: 8, stepCount: 4);

            Assert.IsFalse(isNewBest);
            var record = _saveManager.GetStageRecord(stageId);
            Assert.AreEqual(3, record.starsEarned);
            Assert.AreEqual(10, record.bestTimeSeconds);
            Assert.AreEqual(5, record.bestStepCount);
        }

        [Test]
        public void HasGuestProgress_ReturnsTrue_AfterGuestPlays()
        {
            Assert.IsFalse(_saveManager.HasGuestProgress());

            _saveManager.SaveStageResult(Guid.NewGuid().ToString(), 1, Difficulty.Easy, 3, 10, 5);

            Assert.IsTrue(_saveManager.HasGuestProgress());

            _saveManager.DiscardGuestProgress();
            Assert.IsFalse(_saveManager.HasGuestProgress());
        }

        [Test]
        public void RecordDtos_Serialization_RoundTripsCorrectly()
        {
            var req = new CompleteStageRequest
            {
                stageId = Guid.NewGuid().ToString(),
                timeDoneSeconds = 25,
                stepCount = 14,
                starsEarned = 3
            };

            string json = JsonUtility.ToJson(req);
            Assert.IsTrue(json.Contains(req.stageId));

            var deserialized = JsonUtility.FromJson<CompleteStageRequest>(json);
            Assert.AreEqual(req.stageId, deserialized.stageId);
            Assert.AreEqual(25, deserialized.timeDoneSeconds);
            Assert.AreEqual(14, deserialized.stepCount);
            Assert.AreEqual(3, deserialized.starsEarned);
        }

        [Test]
        public void SyncRecordsRequest_Serialization_RoundTripsCorrectly()
        {
            var syncReq = new SyncRecordsRequest();
            syncReq.records.Add(new SyncRecordItem
            {
                stageId = Guid.NewGuid().ToString(),
                timeDoneSeconds = 12,
                stepCount = 6,
                starsEarned = 2
            });

            string json = JsonUtility.ToJson(syncReq);
            var deserialized = JsonUtility.FromJson<SyncRecordsRequest>(json);

            Assert.AreEqual(1, deserialized.records.Count);
            Assert.AreEqual(syncReq.records[0].stageId, deserialized.records[0].stageId);
            Assert.AreEqual(12, deserialized.records[0].timeDoneSeconds);
            Assert.AreEqual(6, deserialized.records[0].stepCount);
            Assert.AreEqual(2, deserialized.records[0].starsEarned);
        }

        [Test]
        public void SaveToDisk_AtomicWrite_RecoversFromBackupWhenMainFileCorrupted()
        {
            string stageId = Guid.NewGuid().ToString();
            _saveManager.SaveStageResult(stageId, 1, Difficulty.Easy, starsEarned: 3, timeDoneSeconds: 10, stepCount: 5);

            // Ghi lần 2 để tạo file .bak chắc chắn
            _saveManager.SaveStageResult(stageId, 1, Difficulty.Easy, starsEarned: 3, timeDoneSeconds: 9, stepCount: 4);

            string bakPath = _guestPath + ".bak";
            Assert.IsTrue(File.Exists(bakPath), "File .bak phải được tạo sau khi cập nhật file save.");

            // Giả lập crash: Làm hỏng file chính (ghi nội dung rác hoặc 0-byte)
            File.WriteAllText(_guestPath, "{ invalid json corrupt content !!!");

            // Khởi tạo lại SaveManager từ đĩa
            _saveManager.InitializeForGuest();

            // Xác nhận: Hệ thống tự động phục hồi từ file .bak
            Assert.AreEqual(3, _saveManager.TotalStars);
            var record = _saveManager.GetStageRecord(stageId);
            Assert.IsNotNull(record);
            Assert.AreEqual(3, record.starsEarned);
        }

        [Test]
        public void DiscardGuestProgress_ClearsMemoryStateAndFiles()
        {
            string stageId = Guid.NewGuid().ToString();
            _saveManager.SaveStageResult(stageId, 1, Difficulty.Easy, starsEarned: 2, timeDoneSeconds: 20, stepCount: 10);
            Assert.IsTrue(_saveManager.HasGuestProgress());
            Assert.AreEqual(1, _saveManager.CurrentSave.stageRecords.Count);

            _saveManager.DiscardGuestProgress();

            Assert.IsFalse(_saveManager.HasGuestProgress());
            Assert.AreEqual(0, _saveManager.CurrentSave.stageRecords.Count);
            Assert.AreEqual(0, _saveManager.TotalStars);
            Assert.IsFalse(File.Exists(_guestPath));
            Assert.IsFalse(File.Exists(_guestPath + ".bak"));
        }

        [Test]
        public void MergeCloudRecords_TwoWayMerge_PreservesPendingOfflineRecord()
        {
            string stage1Id = Guid.NewGuid().ToString();
            string stage2Id = Guid.NewGuid().ToString();

            // Lưu Màn 1
            _saveManager.SaveStageResult(stage1Id, 1, Difficulty.Easy, starsEarned: 2, timeDoneSeconds: 20, stepCount: 10);

            // Giả lập: Snapshot gửi Màn 1 lên server
            var sentIds = new HashSet<string> { stage1Id };

            // Giả lập: Trong lúc mạng đang bay, người chơi thắng Màn 2 ở local
            _saveManager.SaveStageResult(stage2Id, 2, Difficulty.Easy, starsEarned: 3, timeDoneSeconds: 15, stepCount: 8);

            // Server phản hồi: Chỉ chứa kết quả của Màn 1
            var serverRecords = new List<UserRecordDto>
            {
                new UserRecordDto
                {
                    stageId = stage1Id,
                    stageNumber = 1,
                    difficulty = (int)Difficulty.Easy,
                    starsEarned = 2,
                    bestTimeSeconds = 20,
                    bestStepCount = 10,
                    updatedAt = DateTime.UtcNow.ToString("o")
                }
            };

            // Thực hiện merge kết quả từ Cloud
            _saveManager.MergeCloudRecords(serverRecords, sentIds);

            // Xác nhận:
            // 1. Màn 1 đã được xác nhận, needsSync = false
            var rec1 = _saveManager.GetStageRecord(stage1Id);
            Assert.IsNotNull(rec1);
            Assert.IsFalse(rec1.needsSync);

            // 2. Màn 2 KHÔNG BỊ XÓA, vẫn còn nguyên và needsSync = true
            var rec2 = _saveManager.GetStageRecord(stage2Id);
            Assert.IsNotNull(rec2);
            Assert.IsTrue(rec2.needsSync);

            // 3. Save vẫn giữ cờ hasUnsyncedChanges = true vì Màn 2 chưa được sync
            Assert.IsTrue(_saveManager.CurrentSave.hasUnsyncedChanges);
            Assert.AreEqual(5, _saveManager.TotalStars);
        }

        [Test]
        public void MergeCloudRecords_PullsBetterServerRecord_AndNewStages()
        {
            string stage1Id = Guid.NewGuid().ToString();
            string stage2Id = Guid.NewGuid().ToString();

            // Local chỉ có Màn 1 với 1 sao
            _saveManager.SaveStageResult(stage1Id, 1, Difficulty.Easy, starsEarned: 1, timeDoneSeconds: 30, stepCount: 20);

            // Cloud trả về Màn 1 đạt 3 sao (chơi từ máy khác) và Màn 2 đạt 2 sao (chơi từ máy khác)
            var serverRecords = new List<UserRecordDto>
            {
                new UserRecordDto
                {
                    stageId = stage1Id,
                    stageNumber = 1,
                    difficulty = (int)Difficulty.Easy,
                    starsEarned = 3,
                    bestTimeSeconds = 12,
                    bestStepCount = 7,
                    updatedAt = DateTime.UtcNow.ToString("o")
                },
                new UserRecordDto
                {
                    stageId = stage2Id,
                    stageNumber = 2,
                    difficulty = (int)Difficulty.Easy,
                    starsEarned = 2,
                    bestTimeSeconds = 18,
                    bestStepCount = 11,
                    updatedAt = DateTime.UtcNow.ToString("o")
                }
            };

            _saveManager.MergeCloudRecords(serverRecords);

            var rec1 = _saveManager.GetStageRecord(stage1Id);
            Assert.AreEqual(3, rec1.starsEarned);
            Assert.AreEqual(12, rec1.bestTimeSeconds);

            var rec2 = _saveManager.GetStageRecord(stage2Id);
            Assert.IsNotNull(rec2);
            Assert.AreEqual(2, rec2.starsEarned);

            Assert.AreEqual(5, _saveManager.TotalStars);
        }
    }
}
