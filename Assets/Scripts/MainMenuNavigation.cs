using UnityEngine;
using UnityEngine.EventSystems;

namespace NamnyeoChilse
{
    // Owns screen lifetime only. Scoring, playback and restart stay in the existing game.
    public sealed class MainMenuNavigation : MonoBehaviour
    {
        private GameManager game;
        private AccessibleButtonGroup accessibility;
        private GameObject main, gameplay, ranking, nickname, loading;
        private bool startupReady;
        public GameObject CurrentScreen { get; private set; }

        public void Initialize(GameManager manager, AccessibleButtonGroup group,
            GameObject mainScreen, GameObject gameplayScreen, GameObject rankingScreen, GameObject nicknameScreen, GameObject loadingScreen)
        {
            game = manager;
            accessibility = group;
            main = mainScreen;
            gameplay = gameplayScreen;
            ranking = rankingScreen;
            nickname = nicknameScreen;
            loading = loadingScreen;
            startupReady = false;
            Show(loading);
        }

        public void CompleteStartup()
        {
            startupReady = true;
            ShowMain();
        }

        public void StartGame()
        {
            if (CurrentScreen != main) return;
            Show(gameplay);
            game.enabled = true;
            game.StartGame();
        }

        public void ShowMain()
        {
            // Return is offered on the result screen, never during an active round.
            if (!startupReady || game.Round.IsPlaying) return;
            Show(main);
            game.enabled = false;
        }

        public void ShowRanking() { if (CurrentScreen == main) Show(ranking); }
        public void ShowNickname() { if (CurrentScreen == main) Show(nickname); }

        private void Show(GameObject screen)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            main.SetActive(screen == main);
            gameplay.SetActive(screen == gameplay);
            ranking.SetActive(screen == ranking);
            nickname.SetActive(screen == nickname);
            loading.SetActive(screen == loading);
            CurrentScreen = screen;
            accessibility.SetModalRoot(screen == gameplay ? null : screen.transform);
            accessibility.RefreshButtons();
        }
    }
}
