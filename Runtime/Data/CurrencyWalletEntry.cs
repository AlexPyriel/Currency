using System;

namespace GDF.Currency.Data
{
    /// <summary>Serialized per-currency wallet entry. Carries both current and total-earned amounts.</summary>
    [Serializable]
    public struct CurrencyWalletEntry
    {
        /// <summary>Currency identifier matching the <c>CurrencyConfig.Id</c>.</summary>
        public string CurrencyId;

        /// <summary>Current balance (never negative).</summary>
        public int Current;

        /// <summary>Running total of all amounts ever received (for analytics / achievements).</summary>
        public int EarnedTotal;
    }
}
