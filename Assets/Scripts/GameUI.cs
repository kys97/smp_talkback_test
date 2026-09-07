using System;
using UnityEngine;
using UnityEngine.UI;

namespace NamnyeoChilse
{
    public sealed class GameUI : MonoBehaviour
    {
        private GameManager game;
        private Text nameText, scoreText, timeText, feedbackText, resultText;
        private Text comboText;
        private Button maleButton, femaleButton, restartButton;
        private GameObject resultPanel;
        private AccessibleButtonGroup accessibility;

        public void Initialize(GameManager manager, Text name, Text score, Text time,
            Text feedback, Text result, Button male, Button female, Button restart, GameObject panel, Text combo)
        {
            game = manager;
            accessibility = GetComponentInParent<AccessibleButtonGroup>();
            nameText = name;
            scoreText = score;
            comboText = combo;
            timeText = time;
            feedbackText = feedback;
            resultText = result;
            maleButton = male;
            femaleButton = female;
            restartButton = restart;
            resultPanel = panel;
            if (isActiveAndEnabled) Subscribe();
            Refresh();
        }

        private void OnEnable()
        {
            if (game == null) return;
            feedbackText.text = "";
            Subscribe();
            Refresh();
        }

        private void Subscribe()
        {
            game.StateChanged += Refresh;
            game.Answered += ShowFeedback;
            maleButton.onClick.AddListener(game.ChooseMale);
            femaleButton.onClick.AddListener(game.ChooseFemale);
            restartButton.onClick.AddListener(Restart);
        }

        private void OnDisable()
        {
            if (game == null) return;
            game.StateChanged -= Refresh;
            game.Answered -= ShowFeedback;
            maleButton.onClick.RemoveListener(game.ChooseMale);
            femaleButton.onClick.RemoveListener(game.ChooseFemale);
            restartButton.onClick.RemoveListener(Restart);
        }

        private void Restart()
        {
            feedbackText.text = "";
            game.StartGame();
        }

        private void ShowFeedback(AnswerResult result)
        {
            feedbackText.text = result == AnswerResult.Correct ? $"정답! +{game.Round.LastAwardedScore}" : "오답";
        }

        private void Refresh()
        {
            GameRound round = game.Round;
            nameText.text = round.IsPlaying ? round.CurrentName.Name : "게임 종료";
            if (round.IsPlaying && round.Sequence.Count > 1)
                nameText.text = game.PlaybackIndex >= 0
                    ? $"{game.PlaybackIndex + 1}/{round.Sequence.Count} {round.Sequence[game.PlaybackIndex].Name}"
                    : $"입력 {round.InputIndex + 1}/{round.Sequence.Count}";
            scoreText.text = $"점수 {round.Score}";
            comboText.text = $"콤보 {round.Combo} · x{round.Multiplier}";
            timeText.text = $"남은 시간 {(int)Math.Ceiling(round.RemainingSeconds)}초";
            maleButton.interactable = game.CanAcceptInput;
            femaleButton.interactable = game.CanAcceptInput;
            resultPanel.SetActive(!round.IsPlaying);
            if (accessibility != null) accessibility.SetModalRoot(round.IsPlaying ? null : resultPanel.transform);
            resultText.text = $"게임 종료\n최종 점수 {round.Score}점";
        }
    }
}
