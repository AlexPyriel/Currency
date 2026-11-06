namespace GDF.Currency
{
    /// <summary>
    /// Multi-currency wallet. Config is pulled from <see cref="Config.CurrencyRepository"/>; state
    /// persists through <see cref="LocalDataStorage.ILocalDataStorage"/>. Balances are read with
    /// queries; every change is published as <see cref="Messages.CurrencyChangedMessage"/> and every
    /// refused spend as <see cref="Messages.CurrencySpendRejectedMessage"/> through MessagePipe.
    /// Mutations are safe no-ops on unknown identifiers and emit a warning rather than throwing.
    /// </summary>
    public interface ICurrencyService
    {
        /// <summary>Initializes the wallet from persisted state (adds entries for currencies defined in the repository but missing in the save). Publishes nothing.</summary>
        public void Initialize();

        /// <summary>Returns the current balance of a currency, or 0 when the id is unknown.</summary>
        /// <param name="currencyId">Currency identifier from the repository.</param>
        /// <returns>Current balance.</returns>
        public int Get(string currencyId);

        /// <summary>Returns the running total of amounts ever added to this currency.</summary>
        /// <param name="currencyId">Currency identifier from the repository.</param>
        /// <returns>Earned total.</returns>
        public int GetEarnedTotal(string currencyId);

        /// <summary>
        /// Sets the current balance to an exact value (clamped at 0). Earned-total grows by any positive
        /// delta. Publishes <see cref="Messages.CurrencyChangedMessage"/> when the balance actually changes.
        /// </summary>
        /// <param name="currencyId">Currency identifier from the repository.</param>
        /// <param name="value">New balance.</param>
        /// <param name="reason">Why the balance changes, for example <c>"debug_cheat"</c>; must not be null or empty.</param>
        /// <exception cref="System.ArgumentException"><paramref name="reason"/> is null or empty.</exception>
        public void Set(string currencyId, int value, string reason);

        /// <summary>
        /// Adds the given amount to the balance and to the earned-total. Zero or negative amounts are
        /// ignored (use <see cref="Spend"/>). Publishes <see cref="Messages.CurrencyChangedMessage"/>.
        /// </summary>
        /// <param name="currencyId">Currency identifier from the repository.</param>
        /// <param name="amount">Positive amount to add.</param>
        /// <param name="reason">Why the currency is granted, for example <c>"level_reward"</c>; must not be null or empty.</param>
        /// <exception cref="System.ArgumentException"><paramref name="reason"/> is null or empty.</exception>
        public void Add(string currencyId, int amount, string reason);

        /// <summary>
        /// Spends the given amount if the balance is sufficient and publishes
        /// <see cref="Messages.CurrencyChangedMessage"/>. Otherwise leaves the balance untouched, publishes
        /// <see cref="Messages.CurrencySpendRejectedMessage"/> and returns <see langword="false"/>. Zero or
        /// negative <paramref name="amount"/> is a no-op that returns <see langword="true"/>.
        /// </summary>
        /// <param name="currencyId">Currency identifier from the repository.</param>
        /// <param name="amount">Positive amount to spend.</param>
        /// <param name="reason">What the currency is spent on, for example <c>"shop_purchase"</c>; must not be null or empty.</param>
        /// <returns><see langword="true"/> when the amount was spent (or was not positive).</returns>
        /// <exception cref="System.ArgumentException"><paramref name="reason"/> is null or empty.</exception>
        public bool Spend(string currencyId, int amount, string reason);

        /// <summary>
        /// Resets the balance and the earned-total to zero. Publishes
        /// <see cref="Messages.CurrencyChangedMessage"/> when the balance was not already zero.
        /// </summary>
        /// <param name="currencyId">Currency identifier from the repository.</param>
        /// <param name="reason">Why the currency is reset, for example <c>"season_end"</c>; must not be null or empty.</param>
        /// <exception cref="System.ArgumentException"><paramref name="reason"/> is null or empty.</exception>
        public void Reset(string currencyId, string reason);
    }
}
