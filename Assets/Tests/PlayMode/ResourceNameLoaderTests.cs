using System;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NamnyeoChilse.Tests
{
    public sealed class ResourceNameLoaderTests
    {
        [Test]
        public void InfersGenderKeepsClipAndRejectsInvalidAndDuplicateNames()
        {
            var clips = new[]
            {
                AudioClip.Create("영수", 32, 1, 8000, false),
                AudioClip.Create("민희", 32, 1, 8000, false),
                AudioClip.Create("시작", 32, 1, 8000, false),
                AudioClip.Create("영수".Normalize(NormalizationForm.FormD), 32, 1, 8000, false)
            };
            try
            {
                LogAssert.Expect(LogType.Warning, "이름 음성 '시작' 제외: 파일명은 수 또는 희로 끝나야 합니다.");
                LogAssert.Expect(LogType.Warning, "중복 이름 음성 '영수' 제외: 먼저 로드된 클립 하나만 사용합니다.");
                NameData[] names = ResourceNameLoader.CreateNames(clips);
                Assert.That(names.Length, Is.EqualTo(2));
                NameData male = Array.Find(names, n => n.Name == "영수");
                NameData female = Array.Find(names, n => n.Name == "민희");
                Assert.That(male.Gender, Is.EqualTo(Gender.Male));
                Assert.That(female.Gender, Is.EqualTo(Gender.Female));
                Assert.That(male.AudioClip, Is.SameAs(clips[0]));
                Assert.That(female.AudioClip, Is.SameAs(clips[1]));
            }
            finally
            {
                foreach (AudioClip clip in clips) UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void EmptyResourcesProduceAnEmptyCatalogAndPlayableFallback()
        {
            NameData[] names = ResourceNameLoader.CreateNames(Array.Empty<AudioClip>());
            Assert.That(names, Is.Empty);
            Assert.That(ResourceNameLoader.CreateNames(null), Is.Empty);
            var round = new GameRound(new NameManager(names, new System.Random(1)));
            round.Start(0);
            Assert.That(round.CurrentName.Name, Is.Not.Empty);
            Assert.That(round.CurrentName.AudioClip, Is.Null);
            Assert.That(round.IsPlaying, Is.True);
        }

        [Test]
        public void RealResourcesCreateEntriesWithNamesGendersAndAudioClips()
        {
            AudioClip[] clips = Resources.LoadAll<AudioClip>(ResourceNameLoader.ResourcePath);
            Assert.That(clips.Length, Is.GreaterThanOrEqualTo(20));
            NameData[] names = ResourceNameLoader.Load();
            Assert.That(names.Length, Is.EqualTo(clips.Length));
            foreach (NameData name in names)
            {
                Assert.That(name.AudioClip, Is.Not.Null);
                Assert.That(name.AudioClip.length, Is.GreaterThan(0));
                Assert.That(name.Name, Is.EqualTo(name.AudioClip.name));
                Assert.That(name.Gender, Is.EqualTo(name.Name.EndsWith("수", StringComparison.Ordinal)
                    ? Gender.Male : Gender.Female));
            }
            var round = new GameRound(new NameManager(names, new System.Random(3)));
            round.Start(0);
            Assert.That(round.CurrentName.AudioClip, Is.Not.Null);
        }
    }
}
