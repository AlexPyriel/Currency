using GDF.Currency.Config;
using Reflex.Core;
using UnityEngine;

namespace GDF.Currency.Installers.Reflex
{
    /// <summary>Registers <see cref="ICurrencyService"/> in a Reflex container.</summary>
    public sealed class CurrencyReflexInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField]
        [Tooltip("Currency repository asset. Empty = no currencies.")]
        private CurrencyRepository _repository;

        /// <inheritdoc />
        public void InstallBindings(ContainerBuilder builder)
        {
            if (_repository == null)
            {
                Debug.LogError("[Currency] Reflex installer requires a CurrencyRepository asset.");
                return;
            }

            builder.AddSingleton(_repository, typeof(CurrencyRepository));
            builder.AddSingleton(
                typeof(Internal.CurrencyService),
                typeof(ICurrencyService));
        }
    }
}
