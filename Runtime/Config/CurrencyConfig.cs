using System;
using ConfigRepository;
using UnityEngine;

namespace GDF.Currency.Config
{
    /// <summary>
    /// Design-time definition of a currency. Lives inside a <see cref="CurrencyRepository"/> keyed by
    /// its own string identifier. <c>Id</c> is synchronized automatically by the config repository.
    /// </summary>
    [Serializable]
    public sealed class CurrencyConfig : Config<string>
    {
        [SerializeField]
        [Tooltip("Human-readable UI label (plain string; localize at the view level).")]
        private string _displayName;

        [SerializeField]
        [Tooltip("UI icon shown on wallet / shop screens.")]
        private Sprite _icon;

        [SerializeField]
        [Tooltip("Initial amount of this currency given to a fresh profile.")]
        private int _defaultValue;

        [SerializeField]
        [Tooltip("Rarity bucket index (0 = soft, 1 = hard, 2 = premium bundle, …). Game-defined order.")]
        private int _rarity;

        /// <summary>Human-readable name for debug / default UI labels.</summary>
        public string DisplayName => _displayName;

        /// <summary>Wallet icon.</summary>
        public Sprite Icon => _icon;

        /// <summary>Initial amount granted on first wallet init.</summary>
        public int DefaultValue => _defaultValue;

        /// <summary>Rarity bucket index assigned by the game. Interpretation is up to the host.</summary>
        public int Rarity => _rarity;
    }
}
