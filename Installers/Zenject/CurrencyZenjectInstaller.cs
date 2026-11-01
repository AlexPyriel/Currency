using Currency.Config;
using Zenject;

namespace Currency.Installers.Zenject
{
    /// <summary>Registers <see cref="ICurrencyService"/> in a Zenject container. Pass a repository as the Install arg.</summary>
    public sealed class CurrencyZenjectInstaller : Installer<CurrencyRepository, CurrencyZenjectInstaller>
    {
        [Inject]
        private CurrencyRepository _repository;

        /// <inheritdoc />
        public override void InstallBindings()
        {
            Container.BindInstance(_repository);
            Container
                .Bind<ICurrencyService>()
                .To<Internal.CurrencyService>()
                .AsSingle();
        }
    }
}
