using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NamnyeoChilse.Tests
{
    public sealed class NamePlaybackTests
    {
        private GameObject owner;
        private AudioClip clip;

        private GameManager CreateGame(float seconds)
        {
            owner = new GameObject("Name playback test");
            clip = AudioClip.Create("테스트수", (int)(8000 * seconds), 1, 8000, false);
            GameManager game = owner.AddComponent<GameManager>();
            game.ConfigureNames(new[] { new NameData("테스트수", Gender.Male, clip) }, null,
                new SequenceSettings(new SequenceTier(0, 1, 0f, 0.5f)));
            game.StartGame();
            return game;
        }

        [TearDown]
        public void Cleanup()
        {
            if (owner != null) Object.DestroyImmediate(owner);
            if (clip != null) Object.DestroyImmediate(clip);
        }

        [UnityTest]
        public IEnumerator ActualPlaybackBlocksInputAndUsesEngineCompletion()
        {
            GameManager game = CreateGame(0.4f);
            AudioSource source = owner.GetComponent<AudioSource>();
            yield return null;
            yield return null;
            Assert.That(source.isPlaying, Is.True, "Requires an active Unity audio engine.");
            Assert.That(game.CanAcceptInput, Is.False);
            game.ChooseMale();
            game.ChooseFemale();
            Assert.That(game.Round.Score, Is.Zero);
            yield return new WaitForSecondsRealtime(0.45f);
            Assert.That(source.isPlaying, Is.True);
            Assert.That(game.CanAcceptInput, Is.False, "Must not unlock using clip.length.");
            yield return GameFlowTests.WaitForInput(game);
            Assert.That(source.isPlaying, Is.False);
            game.ChooseMale();
            game.ChooseMale();
            game.ChooseFemale();
            Assert.That(game.Round.Score, Is.EqualTo(100));
            Assert.That(game.CanAcceptInput, Is.False);
            yield return GameFlowTests.WaitForInput(game);
            game.ChooseFemale();
            Assert.That(game.Round.Score, Is.EqualTo(100));
            Assert.That(game.CanAcceptInput, Is.False);
        }

        [UnityTest]
        public IEnumerator RestartAndDeadlineCancelPlaybackAndPreventLateUnlock()
        {
            GameManager game = CreateGame(1.2f);
            AudioSource source = owner.GetComponent<AudioSource>();
            yield return null;
            yield return null;
            Assert.That(source.isPlaying, Is.True);
            yield return new WaitForSecondsRealtime(0.7f);
            game.StartGame();
            Assert.That(source.isPlaying, Is.False);
            Assert.That(game.Round.Score, Is.Zero);
            Assert.That(game.CanAcceptInput, Is.False);
            yield return new WaitForSecondsRealtime(0.7f);
            Assert.That(source.isPlaying, Is.True);
            Assert.That(game.CanAcceptInput, Is.False, "Old completion must not unlock restarted round.");
            // Place the existing real-time deadline 0.1 seconds ahead, during playback.
            game.Round.Start(Time.realtimeSinceStartupAsDouble - 59.9);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(game.Round.IsPlaying, Is.False);
            Assert.That(source.isPlaying, Is.False);
            Assert.That(source.clip, Is.Null);
            game.ChooseMale();
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(game.CanAcceptInput, Is.False);
            Assert.That(source.isPlaying, Is.False);
            Assert.That(game.Round.Score, Is.Zero);
        }

        [UnityTest]
        public IEnumerator MissingClipAndDisableRemainRecoverable()
        {
            owner = new GameObject("Silent name test");
            GameManager game = owner.AddComponent<GameManager>();
            game.ConfigureNames(new[] { new NameData("무음수", Gender.Male) });
            game.StartGame();
            yield return GameFlowTests.WaitForInput(game);
            game.ChooseMale();
            game.ChooseMale();
            Assert.That(game.Round.Score, Is.EqualTo(100));
            game.enabled = false;
            Assert.That(game.CanAcceptInput, Is.False);
            game.enabled = true;
            yield return GameFlowTests.WaitForInput(game);
            game.ChooseMale();
            Assert.That(game.Round.Score, Is.EqualTo(200));
        }
    }
}
