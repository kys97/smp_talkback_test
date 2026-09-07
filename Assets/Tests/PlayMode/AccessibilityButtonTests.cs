using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NamnyeoChilse.Tests
{
    public sealed class AccessibilityButtonTests
    {
        [UnityTest]
        public IEnumerator SceneLabelsActivationLockAndResultModalUseExistingButtons()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return GameFlowTests.WaitForMain();
            GameObject.Find("StartGameButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            var group = Object.FindAnyObjectByType<AccessibleButtonGroup>();
            var game = Object.FindAnyObjectByType<GameManager>();
            var male = GameObject.Find("MaleButton").GetComponent<Button>();
            var female = GameObject.Find("FemaleButton").GetComponent<Button>();
            group.RefreshButtons();
            AccessibilityNode maleNode = group.GetNode(male);
            Assert.That(maleNode.label, Is.EqualTo("남자"));
            Assert.That(group.GetNode(female).label, Is.EqualTo("여자"));
            Assert.That(maleNode.role, Is.EqualTo(AccessibilityRole.Button));
            Assert.That(maleNode.isActive, Is.True);
            Assert.That(maleNode.state, Is.EqualTo(AccessibilityState.Disabled));
            Assert.That(group.TryActivate(male), Is.False);
            yield return GameFlowTests.WaitForInput(game);
            group.RefreshButtons();
            Assert.That(maleNode.state, Is.EqualTo(AccessibilityState.None));
            Rect left = maleNode.frameGetter();
            Rect right = group.GetNode(female).frameGetter();
            Assert.That(left.width, Is.GreaterThan(0));
            Assert.That(left.xMax, Is.LessThan(right.xMin));
            Button correct = game.Round.CurrentName.Gender == Gender.Male ? male : female;
            Assert.That(group.TryActivate(correct), Is.True);
            Assert.That(game.Round.Score, Is.EqualTo(100));
            Assert.That(group.TryActivate(correct), Is.False);
            yield return GameFlowTests.WaitForInput(game);
            Button wrong = game.Round.CurrentName.Gender == Gender.Male ? female : male;
            Assert.That(group.TryActivate(wrong), Is.True);
            Assert.That(game.Round.Score, Is.EqualTo(100));
            Assert.That(game.Round.Combo, Is.Zero);
            game.Round.Start(Time.realtimeSinceStartupAsDouble - 60);
            yield return null;
            yield return null;
            group.RefreshButtons();
            Assert.That(maleNode.isActive, Is.False, "Covered gameplay buttons must leave TalkBack navigation.");
            var restart = GameObject.Find("RestartButton").GetComponent<Button>();
            Assert.That(group.GetNode(restart).label, Is.EqualTo("다시 시작"));
            Assert.That(group.GetNode(restart).isActive, Is.True);
            Assert.That(group.TryActivate(male), Is.False);
            Assert.That(group.TryActivate(restart), Is.True);
            Assert.That(game.Round.Score, Is.Zero);
        }

        [UnityTest]
        public IEnumerator GenericLabelsRaycastSuppressionAndCleanupAreReusable()
        {
            var root = new GameObject("Generic UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var group = root.AddComponent<AccessibleButtonGroup>();
                var child = new GameObject("Action", typeof(RectTransform), typeof(Image), typeof(Button));
                child.transform.SetParent(root.transform, false);
                var rect = child.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f, 0.1f);
                rect.anchorMax = new Vector2(0.4f, 0.4f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var labelObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(child.transform, false);
                var text = labelObject.GetComponent<Text>();
                text.text = "랭킹";
                text.raycastTarget = false;
                Button button = child.GetComponent<Button>();
                int clicks = 0;
                button.onClick.AddListener(() => clicks++);
                yield return null;
                Canvas.ForceUpdateCanvases();
                group.RefreshButtons();
                Assert.That(group.GetNode(button).label, Is.EqualTo("랭킹"));
                text.text = "닉네임 설정";
                group.RefreshButtons();
                Assert.That(group.GetNode(button).label, Is.EqualTo("닉네임 설정"));
#if UNITY_EDITOR
                group.PreviewScreenReaderInput(true);
                Assert.That(root.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                var events = new GameObject("Test EventSystem", typeof(EventSystem));
                try
                {
                    var hits = new System.Collections.Generic.List<RaycastResult>();
                    Rect frame = group.GetNode(button).frameGetter();
                    root.GetComponent<GraphicRaycaster>().Raycast(new PointerEventData(events.GetComponent<EventSystem>())
                    {
                        position = new Vector2(frame.center.x, Screen.height - frame.center.y)
                    }, hits);
                    Assert.That(hits, Is.Empty, "A synthesized tap must not produce a second UI click.");
                    Assert.That(group.TryActivate(button), Is.True);
                    Assert.That(clicks, Is.EqualTo(1));
                }
                finally { Object.DestroyImmediate(events); }
                group.PreviewScreenReaderInput(false);
                Assert.That(root.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
#endif
                button.interactable = false;
                Assert.That(group.TryActivate(button), Is.False);
                button.interactable = true;
                child.SetActive(false);
                group.RefreshButtons();
                Assert.That(group.GetNode(button).isActive, Is.False);
                child.SetActive(true);
                group.enabled = false;
                Assert.That(group.Hierarchy.rootNodes.Count, Is.Zero);
                group.enabled = true;
                group.RefreshButtons();
                Assert.That(group.Hierarchy.rootNodes.Count, Is.EqualTo(1));
                Assert.That(group.TryActivate(button), Is.True);
                Assert.That(clicks, Is.EqualTo(2));
                Object.DestroyImmediate(child);
                group.RefreshButtons();
                Assert.That(group.Hierarchy.rootNodes.Count, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
