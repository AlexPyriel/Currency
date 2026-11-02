# GDF Currency

Multi-currency wallet service for Unity. Config lives in `com.gdf.config-repository`;
state persists through `com.gdf.local-data-storage`. Soft / hard / premium currencies
with earned-total tracking for analytics and achievements.

Package name: `com.gdf.currency`
Assembly name: `GDF.Currency`

## Features

- `ICurrencyService` facade: `Get`, `GetEarnedTotal`, `Set`, `Add`, `Spend` (returns bool), `Reset` + `Initialize`.
- `CurrencyChanged(string)` event on every mutation.
- Design-time definitions in a `CurrencyRepository` SO: id, display name, icon, default value, rarity.
- Persistent wallet (`Current`, `EarnedTotal`) through `com.gdf.local-data-storage`.
- Starter kit friendly: an empty repository means "no currencies registered", and the service becomes a safe no-op surface.
- DI installers for VContainer, Zenject and Reflex.

## Requirements

- Unity `6000.0+`
- `com.gdf.config-repository` `1.1.0+`
- `com.gdf.local-data-storage` `2.0.0+`
- One of the supported DI containers (optional, required only when using the matching installer):
  - `jp.hadashikick.vcontainer`
  - `com.mathijsbakker.extenject`
  - `com.gustavopsantos.reflex`

## Installation (UPM via Git)

Add this dependency to `Packages/manifest.json`:

```json
"com.gdf.currency": "https://github.com/AlexPyriel/Currency.git#v1.0.1"
```

For development tracking (not recommended for production), use `#main` instead of a tag.

You can also install it from Unity Package Manager:

- `Window -> Package Manager -> + -> Add package from git URL...`
- `https://github.com/AlexPyriel/Currency.git#v1.0.1`

## Usage

### 1. Author a currency repository

`Create -> GDF -> Currency -> Currency Repository`. Add entries (`gold`, `gems`, `energy`) with
default values and icons.

### 2. Register the service

```csharp
using GDF.Currency.Installers.VContainer;
using LocalDataStorage.Installers.VContainer;
using VContainer;

protected override void Configure(IContainerBuilder builder)
{
    new LocalDataStorageVContainerInstaller().WithFileBackend().Install(builder);
    new CurrencyVContainerInstaller(_currencyRepository).Install(builder);
}
```

### 3. Initialize at startup

```csharp
using GDF.Currency;
using VContainer.Unity;

public sealed class BootstrapEntryPoint : IStartable
{
    [Inject] private readonly ICurrencyService _wallet;

    public void Start() => _wallet.Initialize();
}
```

### 4. Use from gameplay

```csharp
using GDF.Currency;

_wallet.Add("gold", 50);

if (_wallet.Spend("gems", 10))
    GrantRevive();

int goldSinceStart = _wallet.GetEarnedTotal("gold");

_wallet.CurrencyChanged += id =>
{
    if (id == "gold")
        _goldLabel.text = _wallet.Get("gold").ToString();
};
```

## Assembly Definition Notes

`GDF.Currency` is a dedicated runtime assembly that depends on `GDF.ConfigRepository` and
`GDF.LocalDataStorage`. DI installers live in their own assemblies
(`GDF.Currency.Installers.VContainer`, `.Zenject`, `.Reflex`) so games pulling in only one DI
container do not get the others. Internals are accessible to installers through
`InternalsVisibleTo`.

## License

MIT. See `LICENSE.md`.
