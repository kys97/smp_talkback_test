using System;
using System.Globalization;
using System.Threading.Tasks;

namespace NamnyeoChilse
{
    public interface IPlayerAccountBackend
    {
        bool IsAuthorized { get; }
        string PlayerId { get; }
        Task InitializeAsync();
        Task SignInAsync();
        Task<string> ReadNameAsync();
        Task<string> SaveNameAsync(string name);
    }

    // All name requests go through authentication. SDK owns the session token/cache.
    public sealed class PlayerAccountService
    {
        private readonly IPlayerAccountBackend backend;
        private Task<bool> operation;
        public bool IsBusy { get; private set; }
        public string PlayerName { get; private set; }
        public string PlayerId => backend.IsAuthorized ? backend.PlayerId : null;
        public string Status { get; private set; } = "로그인 대기 중";
        public event Action Changed;

        public PlayerAccountService(IPlayerAccountBackend backend) { this.backend = backend; }

        public Task<bool> InitializeAsync()
        {
            if (IsBusy) return operation ?? Task.FromResult(false);
            return operation = RunAsync(null);
        }

        public Task<bool> SaveAsync(string name)
        {
            if (IsBusy) return Task.FromResult(false);
            if (!ValidateName(name, out string error))
            {
                Status = error;
                Changed?.Invoke();
                return Task.FromResult(false);
            }
            return operation = RunAsync(name);
        }

        private async Task<bool> RunAsync(string name)
        {
            IsBusy = true;
            Status = name == null ? "로그인 및 닉네임 확인 중" : "닉네임 저장 중";
            Changed?.Invoke();
            try
            {
                await backend.InitializeAsync();
                if (!backend.IsAuthorized) await backend.SignInAsync();
                if (!backend.IsAuthorized) throw new InvalidOperationException("Authentication incomplete");
                if (name == null) { Status = "사용자 정보 확인 중"; Changed?.Invoke(); }
                // Read with autoGenerate=false: do not create a random name on first launch.
                string saved = name == null ? await backend.ReadNameAsync() : await backend.SaveNameAsync(name);
                if (name != null && string.IsNullOrWhiteSpace(saved)) throw new InvalidOperationException("Empty server response");
                PlayerName = saved; // Preserve the actual server name, including its discriminator.
                UgsDiagnostics.Report(name == null ? "인증/닉네임 조회" : "닉네임 저장", PlayerId);
                Status = name == null ? (string.IsNullOrEmpty(saved) ? "닉네임을 설정해 주세요." : "로그인 완료") : "닉네임을 저장했습니다.";
                return true;
            }
            catch (Exception error)
            {
                UgsDiagnostics.Report(name == null ? "인증/닉네임 조회" : "닉네임 저장", PlayerId, error);
                // Never clear credentials or create a replacement account on an error.
                Status = name == null ? "연결하지 못했습니다. 닉네임 설정에서 다시 시도해 주세요."
                    : "저장하지 못했습니다. 연결 상태와 닉네임을 확인하고 다시 시도해 주세요.";
                return false;
            }
            finally { IsBusy = false; Changed?.Invoke(); }
        }

        public static bool ValidateName(string name, out string error)
        {
            error = "닉네임은 공백 없이 1~50자로 입력해 주세요.";
            if (string.IsNullOrWhiteSpace(name) || name.Length > 50) return false;
            for (int i = 0; i < name.Length; i++)
            {
                UnicodeCategory category = char.GetUnicodeCategory(name, i);
                if (char.IsWhiteSpace(name[i]) || char.IsControl(name[i]) || category == UnicodeCategory.Format
                    || name[i] == '<' || name[i] == '>') return false;
                if (char.IsHighSurrogate(name[i]))
                {
                    if (i + 1 >= name.Length || !char.IsLowSurrogate(name[i + 1])) return false;
                    i++;
                }
                else if (char.IsLowSurrogate(name[i])) return false;
            }
            error = null;
            return true;
        }
    }
}
