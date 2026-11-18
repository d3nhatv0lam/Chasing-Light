using System.Collections.Generic;
using ChasingLight.Network.DTOs;
using NUnit.Framework;
using UnityEngine;

namespace ChasingLight.Tests
{
    [TestFixture]
    public class LeaderboardSerializationTests
    {
        [Test]
        public void ParseGlobalLeaderboard_ValidJsonArray_ReturnsCorrectList()
        {
            string rawJson = @"[
                {
                    ""userId"": ""u-101"",
                    ""displayName"": ""LightMaster"",
                    ""avatarUrl"": ""https://cdn.example.com/a.png"",
                    ""totalStars"": 42,
                    ""rank"": 1
                },
                {
                    ""userId"": ""u-102"",
                    ""displayName"": ""ShadowWalker"",
                    ""avatarUrl"": null,
                    ""totalStars"": 35,
                    ""rank"": 2
                }
            ]";

            string wrapped = "{\"items\":" + rawJson + "}";
            var wrapper = JsonUtility.FromJson<JsonArrayWrapper<GlobalLeaderboardEntryDto>>(wrapped);

            Assert.IsNotNull(wrapper);
            Assert.IsNotNull(wrapper.items);
            Assert.AreEqual(2, wrapper.items.Count);

            var top1 = wrapper.items[0];
            Assert.AreEqual("u-101", top1.userId);
            Assert.AreEqual("LightMaster", top1.displayName);
            Assert.AreEqual("https://cdn.example.com/a.png", top1.avatarUrl);
            Assert.AreEqual(42, top1.totalStars);
            Assert.AreEqual(1, top1.rank);

            var top2 = wrapper.items[1];
            Assert.AreEqual("u-102", top2.userId);
            Assert.AreEqual("ShadowWalker", top2.displayName);
            Assert.IsNull(top2.avatarUrl);
            Assert.AreEqual(35, top2.totalStars);
            Assert.AreEqual(2, top2.rank);
        }

        [Test]
        public void ParseStageLeaderboard_ValidJsonArray_ReturnsCorrectList()
        {
            string rawJson = @"[
                {
                    ""userId"": ""u-101"",
                    ""displayName"": ""SpeedRunner"",
                    ""avatarUrl"": ""https://cdn.example.com/s.png"",
                    ""bestTimeSeconds"": 65,
                    ""bestStepCount"": 22,
                    ""starsEarned"": 3,
                    ""rank"": 1
                }
            ]";

            string wrapped = "{\"items\":" + rawJson + "}";
            var wrapper = JsonUtility.FromJson<JsonArrayWrapper<StageLeaderboardEntryDto>>(wrapped);

            Assert.IsNotNull(wrapper);
            Assert.IsNotNull(wrapper.items);
            Assert.AreEqual(1, wrapper.items.Count);

            var item = wrapper.items[0];
            Assert.AreEqual("u-101", item.userId);
            Assert.AreEqual("SpeedRunner", item.displayName);
            Assert.AreEqual(65, item.bestTimeSeconds);
            Assert.AreEqual(22, item.bestStepCount);
            Assert.AreEqual(3, item.starsEarned);
            Assert.AreEqual(1, item.rank);
        }

        [Test]
        public void ParseEmptyArray_ReturnsEmptyList()
        {
            string rawJson = "[]";
            string wrapped = "{\"items\":" + rawJson + "}";
            var wrapper = JsonUtility.FromJson<JsonArrayWrapper<GlobalLeaderboardEntryDto>>(wrapped);

            Assert.IsNotNull(wrapper);
            Assert.IsNotNull(wrapper.items);
            Assert.AreEqual(0, wrapper.items.Count);
        }

        [Test]
        public void AuthConfig_GetLeaderboardUrls_FormatsCorrectly()
        {
            var config = ScriptableObject.CreateInstance<AuthConfig>();
            config.BaseUrl = "http://localhost:5013/";
            config.LeaderboardGlobalPath = "/api/v1/leaderboards/global";
            config.LeaderboardStagePath = "/api/v1/leaderboards/stage";

            string globalUrl = config.GetLeaderboardGlobalUrl(25);
            Assert.AreEqual("http://localhost:5013/api/v1/leaderboards/global?limit=25", globalUrl);

            string stageUrl = config.GetLeaderboardStageUrl("stage-uuid-1234", 10);
            Assert.AreEqual("http://localhost:5013/api/v1/leaderboards/stage/stage-uuid-1234?limit=10", stageUrl);
        }
    }
}
