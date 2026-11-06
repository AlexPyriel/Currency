# GDF Currency

Multi-currency wallet service for Unity. Config lives in `com.gdf.config-repository`;
state persists through `com.gdf.local-data-storage`. Soft / hard / premium currencies
with earned-total tracking for analytics and achievements.

Package name: `com.gdf.currency`
Assembly name: `GDF.Currency`

## Features

- `ICurrencyService` facade: `Get`, `GetEarnedTotal`, `Set`, `Add`, `Spend` (returns bool), `Reset` + `Initialize`.
- MessagePipe messages: `CurrencyChangedMessage` (old/new value, delta, reason) on every balance change and
  `CurrencySpendRejectedMessage` on every refused spend.
- Every mutation takes a mandatory `reason` string for analytics and debugging.
- Design-time definitions in a `CurrencyRepository` SO: id, display name, icon, default value, rarity.
- Persistent wallet (`Current`, `EarnedTotal`) through `com.gdf.local-data-storage`.
- Starter kit friendly: an empty repository means "no currencies registered", and the service becomes a safe no-op surface.
- VContainer installer; public `sealed` `CurrencyService` for any other DI container.

## Requirements

- Unity `6000.0+`
- `com.gdf.config-repository` `1.1.0+`
- `com.gdf.local-data-storage` `3.0.0+`
- `com.cysharp.messagepipe` `1.8.2+` (required). The host registers MessagePipe once at its composition root;
  the package installer does not.
- `jp.hadashikick.vcontainer` and `com.cysharp.messagepipe.vcontainer` when using the bundled installer.

## Installation (UPM via Git)

Add this dependency to `Packages/manifest.json`:

```json
"com.gdf.currency": "https://github.com/AlexPyriel/Currency.git#v2.0.0"
```

For development tracking (not recommended for production), use `#main` instead of a tag.

You can also install it from Unity Package Manager:

- `Window -> Package Manager -> + -> Add package from git URL...`
- `https://github.com/AlexPyriel/Currency.git#v2.0.0`

## Usage

### 1. Author a currency repository

`Create -> GDF -> Currency -> Currency Repository`. Add entries (`gold`, `gems`, `energy`) with
default values and icons.

### 2. Register the service

```csharp
using GDF.Currency.Installers.VContainer;
using LocalDataStorage.Installers.VContainer;
using MessagePipe;
using VContainer;

protected override void Configure(IContainerBuilder builder)
{
    builder.RegisterMessagePipe();      // once per application, in the root scope
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

_wallet.Add("gold", 50, "level_reward");

if (_wallet.Spend("gems", 10, "revive"))
    GrantRevive();

int goldSinceStart = _wallet.GetEarnedTotal("gold");
```

### 5. React to changes

State is read with queries (`Get`, `GetEarnedTotal`); facts arrive as messages. Subscribe through
`ISubscriber<T>` and dispose the subscription with its owner:

```csharp
using GDF.Currency.Messages;
using MessagePipe;

_subscription = _currencyChanged.Subscribe(message =>
{
    if (message.CurrencyId == "gold")
        _goldLabel.text = message.NewValue.ToString();
});

_spendRejected.Subscribe(message => _shop.OfferPack(message.CurrencyId, message.Requested - message.Available));
```

`CurrencyChangedMessage` is published after the wallet is persisted, only when the balance actually changes;
`Initialize` publishes nothing. `CurrencySpendRejectedMessage` is published only for insufficient funds, not for
unknown identifiers or non-positive amounts. A subscriber exception propagates to the mutating call, the same as a
C# event.

## Assembly Definition Notes

`GDF.Currency` is a dedicated runtime assembly that depends on `GDF.ConfigRepository`,
`GDF.LocalDataStorage` and `MessagePipe`. The VContainer installer lives in
`GDF.Currency.Installers.VContainer` and compiles only when `jp.hadashikick.vcontainer` is installed.
`GDF.Currency.Internal.CurrencyService` is `public sealed` so any container can construct it.

## Using with Zenject / Reflex

The package ships only a VContainer installer. Other containers bind the public service type and provide
the two message brokers.

Zenject (`MessagePipe.Zenject`, in the `ProjectContext` installer):

```csharp
using GDF.Currency;
using GDF.Currency.Internal;
using GDF.Currency.Messages;
using MessagePipe;

MessagePipeOptions options = Container.BindMessagePipe();
Container.BindMessageBroker<CurrencyChangedMessage>(options);
Container.BindMessageBroker<CurrencySpendRejectedMessage>(options);

Container.BindInstance(_currencyRepository);
Container.Bind<ICurrencyService>().To<CurrencyService>().AsSingle();
```

Reflex (MessagePipe's built-in container provides the brokers):

```csharp
using System;
using GDF.Currency;
using GDF.Currency.Internal;
using GDF.Currency.Messages;
using MessagePipe;

BuiltinContainerBuilder pipe = new BuiltinContainerBuilder();
pipe.AddMessagePipe();
pipe.AddMessageBroker<CurrencyChangedMessage>();
pipe.AddMessageBroker<CurrencySpendRejectedMessage>();
IServiceProvider provider = pipe.BuildServiceProvider();

builder.AddSingleton(provider.GetRequiredService<IPublisher<CurrencyChangedMessage>>(), typeof(IPublisher<CurrencyChangedMessage>));
builder.AddSingleton(provider.GetRequiredService<ISubscriber<CurrencyChangedMessage>>(), typeof(ISubscriber<CurrencyChangedMessage>));
builder.AddSingleton(provider.GetRequiredService<IPublisher<CurrencySpendRejectedMessage>>(), typeof(IPublisher<CurrencySpendRejectedMessage>));
builder.AddSingleton(provider.GetRequiredService<ISubscriber<CurrencySpendRejectedMessage>>(), typeof(ISubscriber<CurrencySpendRejectedMessage>));

builder.AddSingleton(_currencyRepository, typeof(CurrencyRepository));
builder.AddSingleton(typeof(CurrencyService), typeof(ICurrencyService));
```

Both snippets assume `ILocalDataStorage` is bound as shown in the `com.gdf.local-data-storage` README.

## License

MIT. See `LICENSE.md`.
