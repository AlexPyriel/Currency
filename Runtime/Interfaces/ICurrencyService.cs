using System;

namespace Currency
{
    /// <summary>
    /// Multi-currency wallet. Config is pulled from <see cref="Config.CurrencyRepository"/>; state
    /// persists through <see cref="LocalDataStorage.ILocalDataStorage"/>. Add / Spend / Reset are
    /// safe no-ops on unknown identifiers and emit a warning rather than throwing.
    /// </summary>
    public interface ICurrencyService
    {
        /// <summary>Raised when any currency balance changes. Argument is the currency identifier.</summary>
        public event Action<string> CurrencyChanged;

        /// <summary>Initializes the wallet from persisted state (adds entries for currencies defined in the repository but missing in the save).</summary>
        public void Initialize();

        /// <summary>Returns the current balance of a currency, or 0 when the id is unknown.</summary>
        public int Get(string currencyId);

        /// <summary>Returns the running total of amounts ever added to this currency.</summary>
        public int GetEarnedTotal(string currencyId);

        /// <summary>Sets the current balance to an exact value. Earned-total grows by any positive delta.</summary>
        public void Set(string currencyId, int value);

        /// <summary>Adds the given amount to the balance. Negative deltas are ignored (use <see cref="Spend"/>).</summary>
        public void Add(string currencyId, int amount);

        /// <summary>
        /// Spends the given amount if the balance is sufficient. Returns <see langword="false"/> and
        /// leaves the balance untouched otherwise. Zero or negative <paramref name="amount"/> is a no-op that returns <see langword="true"/>.
        /// </summary>
        public bool Spend(string currencyId, int amount);

        /// <summary>Resets the balance (and the earned-total) back to zero.</summary>
        public void Reset(string currencyId);
    }
}
