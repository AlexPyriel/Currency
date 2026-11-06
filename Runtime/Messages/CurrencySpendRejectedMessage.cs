namespace GDF.Currency.Messages
{
    /// <summary>
    /// Published when <see cref="ICurrencyService.Spend"/> is refused for insufficient funds, so a
    /// shop offer, a "not enough gems" popup or analytics can react without wrapping every call site.
    /// </summary>
    public readonly struct CurrencySpendRejectedMessage
    {
        /// <summary>Creates the message.</summary>
        /// <param name="currencyId">Identifier of the currency that was short.</param>
        /// <param name="requested">Amount the caller tried to spend.</param>
        /// <param name="available">Balance at the moment of the attempt.</param>
        /// <param name="reason">Caller-supplied reason of the attempted spend.</param>
        public CurrencySpendRejectedMessage(string currencyId, int requested, int available, string reason)
        {
            CurrencyId = currencyId;
            Requested = requested;
            Available = available;
            Reason = reason;
        }

        /// <summary>Identifier of the currency that was short.</summary>
        public string CurrencyId { get; }

        /// <summary>Amount the caller tried to spend.</summary>
        public int Requested { get; }

        /// <summary>Balance at the moment of the attempt; always below <see cref="Requested"/>.</summary>
        public int Available { get; }

        /// <summary>Caller-supplied reason of the attempted spend.</summary>
        public string Reason { get; }
    }
}
