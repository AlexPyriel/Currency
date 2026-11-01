using System;
using Currency.Config;
using VContainer;

namespace Currency.Installers.VContainer
{
    /// <summary>Registers <see cref="ICurrencyService"/> in a VContainer scope. Requires a <see cref="CurrencyRepository"/> asset.</summary>
    public sealed class CurrencyVContainerInstaller : IInstaller
    {
        private readonly CurrencyRepository _repository;

        public CurrencyVContainerInstaller(CurrencyRepository repository)
        {
            _repository = repository != null ? repository : throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc />
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_repository);
            builder
                .Register<Internal.CurrencyService>(Lifetime.Singleton)
                .As<ICurrencyService>();
        }
    }
}
