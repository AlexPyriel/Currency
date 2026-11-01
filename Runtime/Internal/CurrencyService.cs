using System;
using System.Collections.Generic;
using Currency.Config;
using Currency.Data;
using LocalDataStorage;
using UnityEngine;

namespace Currency.Internal
{
    /// <summary>
    /// Default <see cref="ICurrencyService"/> implementation. Reads definitions from a
    /// <see cref="CurrencyRepository"/>, keeps an in-memory wallet and mirrors every mutation to
    /// <see cref="ILocalDataStorage"/>. Unknown identifiers log a warning and are ignored.
    /// </summary>
    internal sealed class CurrencyService : ICurrencyService
    {
        private readonly CurrencyRepository _repository;
        private readonly ILocalDataStorage _storage;
        private readonly Dictionary<string, CurrencyWalletEntry> _wallet = new();

        public event Action<string> CurrencyChanged;

        public CurrencyService(CurrencyRepository repository, ILocalDataStorage storage)
        {
            _repository = repository != null ? repository : throw new ArgumentNullException(nameof(repository));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public void Initialize()
        {
            _wallet.Clear();

            // Pull previous wallet, if any.
            if (_storage.TryLoad(out CurrencyWalletData loaded) && loaded.Entries != null)
            {
                for (int i = 0; i < loaded.Entries.Count; i++)
                {
                    CurrencyWalletEntry entry = loaded.Entries[i];
                    if (string.IsNullOrEmpty(entry.CurrencyId))
                        continue;

                    _wallet[entry.CurrencyId] = entry;
                }
            }

            // Backfill entries for currencies defined in the repository but missing from the save.
            bool dirty = false;
            foreach (KeyValuePair<string, CurrencyConfig> pair in _repository.Enumerate())
            {
                if (_wallet.ContainsKey(pair.Key))
                    continue;

                int def = pair.Value != null ? pair.Value.DefaultValue : 0;
                _wallet[pair.Key] = new CurrencyWalletEntry
                {
                    CurrencyId = pair.Key,
                    Current = def,
                    EarnedTotal = def,
                };
                dirty = true;
            }

            if (dirty)
                Persist();
        }

        public int Get(string currencyId)
        {
            return _wallet.TryGetValue(currencyId ?? string.Empty, out CurrencyWalletEntry entry) ? entry.Current : 0;
        }

        public int GetEarnedTotal(string currencyId)
        {
            return _wallet.TryGetValue(currencyId ?? string.Empty, out CurrencyWalletEntry entry) ? entry.EarnedTotal : 0;
        }

        public void Set(string currencyId, int value)
        {
            if (!EnsureKnown(currencyId))
                return;

            value = Math.Max(0, value);
            CurrencyWalletEntry entry = _wallet[currencyId];
            int delta = value - entry.Current;
            if (delta > 0)
                entry.EarnedTotal += delta;

            entry.Current = value;
            _wallet[currencyId] = entry;
            Persist();
            CurrencyChanged?.Invoke(currencyId);
        }

        public void Add(string currencyId, int amount)
        {
            if (amount <= 0 || !EnsureKnown(currencyId))
                return;

            CurrencyWalletEntry entry = _wallet[currencyId];
            entry.Current += amount;
            entry.EarnedTotal += amount;
            _wallet[currencyId] = entry;
            Persist();
            CurrencyChanged?.Invoke(currencyId);
        }

        public bool Spend(string currencyId, int amount)
        {
            if (amount <= 0)
                return true;

            if (!EnsureKnown(currencyId))
                return false;

            CurrencyWalletEntry entry = _wallet[currencyId];
            if (entry.Current < amount)
                return false;

            entry.Current -= amount;
            _wallet[currencyId] = entry;
            Persist();
            CurrencyChanged?.Invoke(currencyId);
            return true;
        }

        public void Reset(string currencyId)
        {
            if (!EnsureKnown(currencyId))
                return;

            CurrencyWalletEntry entry = _wallet[currencyId];
            entry.Current = 0;
            entry.EarnedTotal = 0;
            _wallet[currencyId] = entry;
            Persist();
            CurrencyChanged?.Invoke(currencyId);
        }

        private bool EnsureKnown(string currencyId)
        {
            if (!string.IsNullOrEmpty(currencyId) && _wallet.ContainsKey(currencyId))
                return true;

            Debug.LogWarning($"[Currency] Unknown currency id '{currencyId}'. Add it to the CurrencyRepository and call Initialize() again.");
            return false;
        }

        private void Persist()
        {
            CurrencyWalletData payload = new CurrencyWalletData
            {
                Entries = new List<CurrencyWalletEntry>(_wallet.Values),
            };
            _storage.Save(payload);
        }
    }
}
