using ConfigRepository;
using UnityEngine;

namespace GDF.Currency.Config
{
    /// <summary>
    /// ScriptableObject repository of currency definitions. Author one asset per project (or per
    /// profile) and drag it into the <see cref="Installers.VContainer.CurrencyVContainerInstaller"/>
    /// (and friends). Empty repository = no currencies, which is the default for prototype kits.
    /// </summary>
    [CreateAssetMenu(menuName = "GDF/Currency/Currency Repository", fileName = "CurrencyRepository", order = 0)]
    public sealed class CurrencyRepository : ConfigRepository<string, CurrencyConfig>
    {
    }
}
