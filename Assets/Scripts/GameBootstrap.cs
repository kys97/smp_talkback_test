using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NamnyeoChilse
{
    // Explicit scene entry point. Builds and wires the small MVP view once.
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private Font koreanFont;
        [SerializeField] private ScoringSettings scoring = new ScoringSettings();
        [SerializeField] private SequenceSettings sequenceSettings = new SequenceSettings();
        private RectTransform safeArea;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            if (koreanFont == null)
            {
                Debug.LogError("GameBootstrap: Korean Font에 NanumGothic-Regular를 연결하세요.", this);
                enabled = false;
                return;
            }

            GameManager game = gameObject.AddComponent<GameManager>();
            game.enabled = false; // The menu owns the first start; no timer/audio on app launch.
            game.ConfigureNames(ResourceNameLoader.Load(), scoring, sequenceSettings);
            PlayerAccountHost account = gameObject.AddComponent<PlayerAccountHost>();
            gameObject.AddComponent<LeaderboardScoreHost>().Initialize(game, account.Service);
            var canvasObject = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            Image background = Panel("Background", canvasObject.transform, new Color(0.035f, 0.045f, 0.07f));
            safeArea = Rect("SafeArea", background.transform, Vector2.zero, Vector2.one);
            ApplySafeArea();

            RectTransform gameplay = Rect("GameplayScreen", safeArea, Vector2.zero, Vector2.one);

            Label("Title", gameplay, "남녀칠세 부동석", 62, new Vector2(0.03f, 0.88f), new Vector2(0.97f, 0.98f));
            Text score = Label("Score", gameplay, "", 48, new Vector2(0.03f, 0.79f), new Vector2(0.45f, 0.88f));
            Text time = Label("Time", gameplay, "", 48, new Vector2(0.45f, 0.79f), new Vector2(0.97f, 0.88f));
            Text combo = Label("Combo", gameplay, "", 48, new Vector2(0.03f, 0.70f), new Vector2(0.97f, 0.79f));
            Text name = Label("Name", gameplay, "", 112, new Vector2(0.04f, 0.60f), new Vector2(0.96f, 0.70f));
            Text feedback = Label("Feedback", gameplay, "", 48, new Vector2(0.03f, 0.51f), new Vector2(0.97f, 0.60f));
            Button male = MakeButton("MaleButton", gameplay, "남자", new Vector2(0.03f, 0.12f), new Vector2(0.485f, 0.50f));
            Button female = MakeButton("FemaleButton", gameplay, "여자", new Vector2(0.515f, 0.12f), new Vector2(0.97f, 0.50f));
            Label("Hint", gameplay, "수로 끝나면 남자 · 희로 끝나면 여자", 34,
                new Vector2(0.03f, 0.01f), new Vector2(0.97f, 0.11f));

            Image resultPanel = Panel("ResultPanel", gameplay, new Color(0.035f, 0.045f, 0.07f, 1));
            resultPanel.raycastTarget = true;
            Text result = Label("Result", resultPanel.transform, "", 76, new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.78f));
            Button restart = MakeButton("RestartButton", resultPanel.transform, "다시 시작",
                new Vector2(0.10f, 0.28f), new Vector2(0.90f, 0.44f));
            Button resultHome = MakeButton("ResultHomeButton", resultPanel.transform, "메인으로",
                new Vector2(0.10f, 0.07f), new Vector2(0.90f, 0.23f));

            RectTransform main = Rect("MainScreen", safeArea, Vector2.zero, Vector2.one);
            main.gameObject.SetActive(false); // Never publish main UI before authentication, even for one frame.
            Label("MainTitle", main, "남녀칠세 부동석", 76, new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.96f));
            Text nicknameDisplay = Label("NicknameDisplay", main, "현재 닉네임: 미설정", 48, new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.78f));
            nicknameDisplay.supportRichText = false;
            Button start = MakeButton("StartGameButton", main, "게임 시작", new Vector2(0.08f, 0.47f), new Vector2(0.92f, 0.64f));
            Button rankingButton = MakeButton("RankingButton", main, "랭킹", new Vector2(0.08f, 0.26f), new Vector2(0.92f, 0.43f));
            Button nicknameButton = MakeButton("NicknameButton", main, "닉네임 설정", new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.22f));

            RectTransform ranking = Rect("RankingScreen", safeArea, Vector2.zero, Vector2.one);
            Label("Title", ranking, "온라인 랭킹", 68, new Vector2(0.04f, 0.91f), new Vector2(0.96f, 0.99f));
            Text rankingStatus = RankingLabel("RankingStatus", ranking, 40, new Vector2(0.04f, 0.83f), new Vector2(0.96f, 0.91f));
            var rankingRows = new Text[10];
            for (int i = 0; i < rankingRows.Length; i++)
            {
                float top = 0.83f - i * 0.059f;
                rankingRows[i] = RankingLabel("RankingRow" + i, ranking, 40,
                    new Vector2(0.04f, top - 0.057f), new Vector2(0.96f, top));
                rankingRows[i].alignment = TextAnchor.MiddleLeft;
                rankingRows[i].gameObject.SetActive(false);
            }
            Text myRanking = RankingLabel("MyRanking", ranking, 48, new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.23f));
            Button rankingRefresh = MakeButton("RankingRefreshButton", ranking, "새로고침",
                new Vector2(0.04f, 0.02f), new Vector2(0.48f, 0.13f));
            Button rankingHome = MakeButton("RankingScreenHomeButton", ranking, "뒤로가기",
                new Vector2(0.52f, 0.02f), new Vector2(0.96f, 0.13f));
            RectTransform nickname = Rect("NicknameScreen", safeArea, Vector2.zero, Vector2.one);
            Label("Title", nickname, "닉네임 설정", 76, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.96f));
            Label("Rules", nickname, "공백 없이 1~50자\n저장 시 숫자 식별자가 붙습니다.", 44,
                new Vector2(0.06f, 0.65f), new Vector2(0.94f, 0.81f));
            RectTransform entry = Rect("NicknameInput", nickname, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.63f));
            Image entryBackground = entry.gameObject.AddComponent<Image>();
            entryBackground.color = new Color(0.12f, 0.20f, 0.30f);
            AccessibleTextInput input = entry.gameObject.AddComponent<AccessibleTextInput>();
            input.targetGraphic = entryBackground;
            input.textComponent = Label("InputText", entry, "", 60, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
            input.textComponent.supportRichText = false;
            input.placeholder = Label("Placeholder", entry, "닉네임 입력", 60, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
            input.characterLimit = 50;
            input.lineType = InputField.LineType.SingleLine;
            Text accountStatus = Label("AccountStatus", nickname, "", 42, new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.47f));
            Button confirmName = MakeButton("ConfirmNicknameButton", nickname, "확인", new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.27f));
            Button cancelName = MakeButton("CancelNicknameButton", nickname, "취소", new Vector2(0.52f, 0.08f), new Vector2(0.92f, 0.27f));

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }

            RectTransform loading = Rect("LoadingScreen", safeArea, Vector2.zero, Vector2.one);
            Text loadingStatus = RankingLabel("LoadingStatus", loading, 62, new Vector2(0.08f, 0.45f), new Vector2(0.92f, 0.75f));
            loadingStatus.text = "로그인 중";
            Button loginRetry = MakeButton("LoginRetryButton", loading, "재시도", new Vector2(0.12f, 0.18f), new Vector2(0.88f, 0.36f));
            loginRetry.gameObject.SetActive(false);
            // Only loading is visible when the accessibility hierarchy is first published.
            gameplay.gameObject.SetActive(false);
            ranking.gameObject.SetActive(false);
            nickname.gameObject.SetActive(false);
            AccessibleButtonGroup accessibility = canvasObject.AddComponent<AccessibleButtonGroup>();
            GameUI ui = gameplay.gameObject.AddComponent<GameUI>();
            ui.Initialize(game, name, score, time, feedback, result, male, female, restart, resultPanel.gameObject, combo);
            MainMenuNavigation navigation = canvasObject.AddComponent<MainMenuNavigation>();
            navigation.Initialize(game, accessibility, main.gameObject, gameplay.gameObject, ranking.gameObject, nickname.gameObject, loading.gameObject);
            ranking.gameObject.AddComponent<RankingUI>().Initialize(account.Service, rankingStatus, myRanking,
                rankingRows, rankingRefresh, accessibility);
            NicknameUI nicknameUI = canvasObject.AddComponent<NicknameUI>();
            nicknameUI.Initialize(account.Service, navigation, nickname.gameObject, input, nicknameDisplay, accountStatus, confirmName, cancelName);
            start.onClick.AddListener(navigation.StartGame);
            rankingButton.onClick.AddListener(navigation.ShowRanking);
            nicknameButton.onClick.AddListener(nicknameUI.Open);
            resultHome.onClick.AddListener(navigation.ShowMain);
            rankingHome.onClick.AddListener(navigation.ShowMain);
            canvasObject.AddComponent<StartupLoadingUI>().Initialize(account.Service, navigation, loading.gameObject,
                loadingStatus, loginRetry, start, accessibility);
        }

        private Text RankingLabel(string name, Transform parent, int size, Vector2 min, Vector2 max)
        {
            Text text = Label(name, parent, "", size, min, max);
            text.supportRichText = false;
            text.gameObject.AddComponent<AccessibleReadOnlyText>();
            return text;
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea || lastScreenSize != new Vector2Int(Screen.width, Screen.height))
                ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            lastSafeArea = Screen.safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Image Panel(string name, Transform parent, Color color)
        {
            Image image = Rect(name, parent, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text Label(string name, Transform parent, string value, int size, Vector2 min, Vector2 max)
        {
            Text text = Rect(name, parent, min, max).gameObject.AddComponent<Text>();
            text.font = koreanFont;
            text.text = value;
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 20;
            text.resizeTextMaxSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private Button MakeButton(string name, Transform parent, string caption, Vector2 min, Vector2 max)
        {
            RectTransform rect = Rect(name, parent, min, max);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.28f, 0.43f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Label("Label", rect, caption, 90, new Vector2(0.03f, 0.06f), new Vector2(0.97f, 0.94f));
            return button;
        }
    }
}
