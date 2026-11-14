using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ChasingLight;
using ChasingLight.Network.DTOs;

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
            if (File.Exists(_guestPath))
            {
                File.Delete(_guestPath);
            }

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

            if (File.Exists(_guestPath))
            {
                File.Delete(_guestPath);
            }
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
    }
}
