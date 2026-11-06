namespace GDF.Currency.Messages
{
    /// <summary>
    /// Published after a currency balance changes through <see cref="ICurrencyService"/>. Carries the
    /// old and new values so subscribers (HUD, analytics, quests) need no extra query.
    /// </summary>
    public readonly struct CurrencyChangedMessage
    {
        /// <summary>Creates the message.</summary>
        /// <param name="currencyId">Identifier of the changed currency.</param>
        /// <param name="oldValue">Balance before the change.</param>
        /// <param name="newValue">Balance after the change.</param>
        /// <param name="reason">Caller-supplied reason, for example <c>"shop_purchase"</c>.</param>
        public CurrencyChangedMessage(string currencyId, int oldValue, int newValue, string reason)
        {
            CurrencyId = currencyId;
            OldValue = oldValue;
            NewValue = newValue;
            Reason = reason;
        }

        /// <summary>Identifier of the changed currency.</summary>
        public string CurrencyId { get; }

        /// <summary>Balance before the change.</summary>
        public int OldValue { get; }

        /// <summary>Balance after the change.</summary>
        public int NewValue { get; }

        /// <summary>Signed difference <c>NewValue - OldValue</c>; positive for income, negative for spending.</summary>
        public int Delta => NewValue - OldValue;

        /// <summary>Caller-supplied reason, for example <c>"shop_purchase"</c> or <c>"level_reward"</c>.</summary>
        public string Reason { get; }
    }
}
