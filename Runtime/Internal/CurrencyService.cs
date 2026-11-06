using System;
using System.Collections.Generic;
using GDF.Currency.Config;
using GDF.Currency.Data;
using GDF.Currency.Messages;
using LocalDataStorage;
using MessagePipe;
using UnityEngine;

namespace GDF.Currency.Internal
{
    /// <summary>
    /// Default <see cref="ICurrencyService"/> implementation. Reads definitions from a
    /// <see cref="CurrencyRepository"/>, keeps an in-memory wallet, mirrors every mutation to
    /// <see cref="ILocalDataStorage"/> and publishes the outcome through MessagePipe. Unknown
    /// identifiers log a warning and are ignored.
    /// </summary>
    public sealed class CurrencyService : ICurrencyService
    {
        private readonly CurrencyRepository _repository;
        private readonly ILocalDataStorage _storage;
        private readonly IPublisher<CurrencyChangedMessage> _changedPublisher;
        private readonly IPublisher<CurrencySpendRejectedMessage> _spendRejectedPublisher;
        private readonly Dictionary<string, CurrencyWalletEntry> _wallet = new();

        /// <summary>Creates the wallet service.</summary>
        /// <param name="repository">Currency definitions and default balances.</param>
        /// <param name="storage">Storage that persists <see cref="CurrencyWalletData"/>.</param>
        /// <param name="changedPublisher">Channel for <see cref="CurrencyChangedMessage"/>.</param>
        /// <param name="spendRejectedPublisher">Channel for <see cref="CurrencySpendRejectedMessage"/>.</param>
        public CurrencyService(
            CurrencyRepository repository,
            ILocalDataStorage storage,
            IPublisher<CurrencyChangedMessage> changedPublisher,
            IPublisher<CurrencySpendRejectedMessage> spendRejectedPublisher)
        {
            _repository = repository != null ? repository : throw new ArgumentNullException(nameof(repository));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _changedPublisher = changedPublisher ?? throw new ArgumentNullException(nameof(changedPublisher));
            _spendRejectedPublisher = spendRejectedPublisher ?? throw new ArgumentNullException(nameof(spendRejectedPublisher));
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public int Get(string currencyId)
        {
            return _wallet.TryGetValue(currencyId ?? string.Empty, out CurrencyWalletEntry entry) ? entry.Current : 0;
        }

        /// <inheritdoc />
        public int GetEarnedTotal(string currencyId)
        {
            return _wallet.TryGetValue(currencyId ?? string.Empty, out CurrencyWalletEntry entry) ? entry.EarnedTotal : 0;
        }

        /// <inheritdoc />
        public void Set(string currencyId, int value, string reason)
        {
            RequireReason(reason);
            if (!EnsureKnown(currencyId))
                return;

            value = Math.Max(0, value);
            CurrencyWalletEntry entry = _wallet[currencyId];
            int oldValue = entry.Current;
            if (value == oldValue)
                return;

            if (value > oldValue)
                entry.EarnedTotal += value - oldValue;

            entry.Current = value;
            Commit(entry, oldValue, reason);
        }

        /// <inheritdoc />
        public void Add(string currencyId, int amount, string reason)
        {
            RequireReason(reason);
            if (amount <= 0 || !EnsureKnown(currencyId))
                return;

            CurrencyWalletEntry entry = _wallet[currencyId];
            int oldValue = entry.Current;
            entry.Current += amount;
            entry.EarnedTotal += amount;
            Commit(entry, oldValue, reason);
        }

        /// <inheritdoc />
        public bool Spend(string currencyId, int amount, string reason)
        {
            RequireReason(reason);
            if (amount <= 0)
                return true;

            if (!EnsureKnown(currencyId))
                return false;

            CurrencyWalletEntry entry = _wallet[currencyId];
            int oldValue = entry.Current;
            if (oldValue < amount)
            {
                _spendRejectedPublisher.Publish(new CurrencySpendRejectedMessage(currencyId, amount, oldValue, reason));
                return false;
            }

            entry.Current -= amount;
            Commit(entry, oldValue, reason);
            return true;
        }

        /// <inheritdoc />
        public void Reset(string currencyId, string reason)
        {
            RequireReason(reason);
            if (!EnsureKnown(currencyId))
                return;

            CurrencyWalletEntry entry = _wallet[currencyId];
            int oldValue = entry.Current;
            entry.Current = 0;
            entry.EarnedTotal = 0;
            _wallet[currencyId] = entry;
            Persist();
            if (oldValue != 0)
                _changedPublisher.Publish(new CurrencyChangedMessage(currencyId, oldValue, 0, reason));
        }

        private static void RequireReason(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                throw new ArgumentException("A non-empty reason is required for every wallet mutation.", nameof(reason));
        }

        private void Commit(CurrencyWalletEntry entry, int oldValue, string reason)
        {
            _wallet[entry.CurrencyId] = entry;
            Persist();
            _changedPublisher.Publish(new CurrencyChangedMessage(entry.CurrencyId, oldValue, entry.Current, reason));
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
