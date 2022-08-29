using StackExchange.Redis;

namespace RedisStatusPage.Data
{
    public class GlobalAppState
    {
        private const string DarkModeKey = "APP_CONFIG:" + nameof(IsDarkMode);

        private readonly ConnectionMultiplexer _cn;

        public GlobalAppState(ConnectionMultiplexer cn)
        {
            _cn = cn;
        }

        public bool IsDarkMode
        {
            get => _cn.GetDatabase().StringGet(DarkModeKey) == "1";
            set
            {
                _cn.GetDatabase().StringSet(DarkModeKey, value);
                NotifyStateChanged();
            }
        }

        public event Action? OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
