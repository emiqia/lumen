using System.Collections;
using Lumen.Bot;
using Lumen.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lumen.Tests
{
    /// <summary>
    /// End-to-end smoke test: boots the real game (camera, lighting, UI, procedural level)
    /// and lets the AutoPilot play. Any exception or error log fails the test.
    /// </summary>
    public class BotSmokeTest
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            AudioListener.volume = 1f;
            if (root != null) Object.Destroy(root);
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator AutoPilot_ClearsFloorOne_WithoutErrors()
        {
            AudioListener.volume = 0f;
            root = new GameObject("Lumen");
            var gm = root.AddComponent<GameManager>();
            yield return null; // Start() enters the title / attract mode

            gm.Headless = true;
            gm.SimSpeed = 4f;
            gm.InputOverride = () => new AutoPilot();
            gm.StartRun(4242);

            float limit = Time.realtimeSinceStartup + 120f;
            while (gm.Floor < 2 && gm.Current != GameManager.State.GameOver && Time.realtimeSinceStartup < limit)
                yield return null;

            Assert.AreEqual(2, gm.Floor, $"bot did not clear floor 1 (state {gm.Current}, fuel {gm.Player?.Fuel})");
            Assert.Greater(gm.Score, 0);
        }
    }
}
