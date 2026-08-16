namespace TradeCopia.Domain.Engine
{
    public interface ILeaderIdentityLedger
    {
        bool Contains(string identity);

        void Remember(string identity);
    }

    public sealed class NullLedger : ILeaderIdentityLedger
    {
        public static readonly NullLedger Instance = new NullLedger();

        private NullLedger()
        {
        }

        public bool Contains(string identity)
        {
            return false;
        }

        public void Remember(string identity)
        {
        }
    }
}
