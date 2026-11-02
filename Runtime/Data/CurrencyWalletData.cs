using System;
using System.Collections.Generic;
using LocalDataStorage;

namespace GDF.Currency.Data
{
    /// <summary>Persisted wallet state. One entry per currency defined in the <see cref="Config.CurrencyRepository"/>.</summary>
    [Serializable]
    public sealed class CurrencyWalletData : LocalData
    {
        /// <summary>Wallet entries indexed implicitly by order; look up by <c>CurrencyId</c>.</summary>
        public List<CurrencyWalletEntry> Entries = new List<CurrencyWalletEntry>();
    }
}
