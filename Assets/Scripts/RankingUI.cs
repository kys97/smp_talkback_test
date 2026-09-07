using System;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace NamnyeoChilse
{
    public sealed class RankingUI : MonoBehaviour
    {
        public static Func<IRankingBackend> BackendFactory = () => new UgsRankingBackend();
        private RankingService service;
        private Text status, mine;
        private Text[] rows;
        private Button refresh;
        private AccessibleButtonGroup accessibility;
        private bool loading;
        private int generation;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetFactory() => BackendFactory = () => new UgsRankingBackend();

        public void Initialize(PlayerAccountService account, Text message, Text personal, Text[] entries,
            Button refreshButton, AccessibleButtonGroup group)
        {
            service = new RankingService(account, BackendFactory());
            status = message;
            mine = personal;
            rows = entries;
            refresh = refreshButton;
            accessibility = group;
            refresh.onClick.AddListener(Refresh);
            if (isActiveAndEnabled) Refresh();
        }
        private void OnEnable() { if (service != null) Refresh(); }
        private void OnDisable() { generation++; loading = false; }
        private void OnDestroy() { if (refresh != null) refresh.onClick.RemoveListener(Refresh); }

        public async void Refresh()
        {
            if (!isActiveAndEnabled || service == null || loading) return;
            loading = true;
            int request = ++generation;
            refresh.interactable = false;
            foreach (Text row in rows) row.gameObject.SetActive(false);
            SetText(status, "랭킹을 불러오는 중입니다.");
            SetText(mine, "내 순위: 조회 중");
            accessibility.RefreshButtons();
            Announce(status.text);
            RankingSnapshot result = await service.FetchAsync();
            // SDK requests cannot be aborted; discard results for a closed/destroyed view.
            if (this == null || !isActiveAndEnabled || request != generation) return;
            loading = false;
            refresh.interactable = true;
            SetText(status, result.AuthenticationError ?? (result.TopFailed
                ? result.TopError ?? "랭킹 조회 실패. 연결과 서버 설정을 확인한 뒤 새로고침하세요."
                : result.Top.Length == 0 ? "아직 등록된 랭킹이 없습니다." : "최신 TOP 10"));
            if (!result.TopFailed)
            {
                for (int i = 0; i < Math.Min(rows.Length, result.Top.Length); i++)
                {
                    RankingEntry entry = result.Top[i];
                    if (entry == null) continue;
                    // Limit only the visual name; accessibility retains the complete server name.
                    string display = entry.Nickname.Length > 14 ? entry.Nickname.Substring(0, 14) + "…" : entry.Nickname;
                    SetText(rows[i], $"{entry.Rank}위  |  {display}  |  {entry.ScoreText}점", entry.SpokenText);
                    rows[i].gameObject.SetActive(true);
                }
            }
            SetText(mine, result.MineFailed ? result.MineError ?? "내 순위: 조회 실패. 새로고침하세요."
                : result.Mine == null ? "내 순위: 등록된 기록 없음"
                : $"내 순위: {result.Mine.Rank}위 / {result.Mine.ScoreText}점");
            Canvas.ForceUpdateCanvases();
            accessibility.RefreshButtons();
            Announce(status.text + " " + mine.text);
        }
        private static void SetText(Text target, string value, string spoken = null)
        {
            target.text = value;
            target.GetComponent<AccessibleReadOnlyText>().AccessibilityLabel = spoken ?? value;
        }
        private static void Announce(string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (AssistiveSupport.isScreenReaderEnabled) AssistiveSupport.notificationDispatcher.SendAnnouncement(message);
#endif
        }
    }
}
