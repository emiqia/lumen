using System.Collections;
using Lumen.Bot;
using Lumen.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lumen.Tests
{
    /// <summary>State-machine regressions in the real game flow (death → game over → title/retry).</summary>
    public class GameFlowTests
    {
        GameObject root;
        GameManager gm;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            AudioListener.volume = 0f;
            root = new GameObject("Lumen");
            gm = root.AddComponent<GameManager>();
            yield return null;
            gm.InputOverride = () => new AutoPilot();
            gm.StartRun(777);
            float limit = Time.realtimeSinceStartup + 10f;
            while (gm.Current != GameManager.State.Playing && Time.realtimeSinceStartup < limit) yield return null;
            Assert.AreEqual(GameManager.State.Playing, gm.Current);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            AudioListener.volume = 1f;
            if (root != null) Object.Destroy(root);
            yield return null; // Destroy is deferred: let it happen before the next test boots a new game
        }

        IEnumerator Die()
        {
            gm.Player.SetFuel(0f);
            float limit = Time.realtimeSinceStartup + 5f;
            while (gm.Current != GameManager.State.GameOver && Time.realtimeSinceStartup < limit) yield return null;
            Assert.AreEqual(GameManager.State.GameOver, gm.Current);
        }

        [UnityTest]
        public IEnumerator Dying_ShowsGameOverAfterTheDeathBeat()
        {
            yield return Die();
            Assert.IsFalse(gm.GameOverVisible, "the screen waits for the death animation");
            yield return new WaitForSecondsRealtime(2f);
            Assert.IsTrue(gm.GameOverVisible);
        }

        [UnityTest]
        public IEnumerator LeavingBeforeTheScreenAppears_NeverShowsItOverTheTitle()
        {
            yield return Die();
            gm.EnterTitle();
            yield return new WaitForSecondsRealtime(2f);
            Assert.AreEqual(GameManager.State.Title, gm.Current);
            Assert.IsFalse(gm.GameOverVisible, "a stale game-over coroutine covered the title");
        }

        [UnityTest]
        public IEnumerator RetryingBeforeTheScreenAppears_NeverShowsItOverTheNewRun()
        {
            yield return Die();
            gm.StartRun(778);
            yield return new WaitForSecondsRealtime(2.5f);
            Assert.AreNotEqual(GameManager.State.GameOver, gm.Current);
            Assert.IsFalse(gm.GameOverVisible, "a stale game-over coroutine covered the new run");
        }
    }
}
