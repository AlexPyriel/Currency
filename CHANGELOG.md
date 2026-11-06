# Changelog

## [2.0.0] - 2026-11-06

Breaking release. The wallet publishes MessagePipe messages instead of a C# event, and every mutation
takes a reason.

### Changed

- `Set`, `Add`, `Spend` and `Reset` take a mandatory `string reason`; null or empty throws `ArgumentException`.
- `CurrencyService` is `public sealed`, so any DI container can bind it.
- `Set` and `Reset` no longer announce a change when the balance stays the same.
- Depends on `com.gdf.local-data-storage >= 3.0.0` and `com.cysharp.messagepipe >= 1.8.2`.

### Added

- `CurrencyChangedMessage` (`CurrencyId`, `OldValue`, `NewValue`, `Delta`, `Reason`), published after each balance change.
- `CurrencySpendRejectedMessage` (`CurrencyId`, `Requested`, `Available`, `Reason`), published when `Spend` is refused
  for insufficient funds.

### Removed

- `ICurrencyService.CurrencyChanged` event.
- Zenject installer (`CurrencyZenjectInstaller`) and Reflex installer (`CurrencyReflexInstaller`). The README section
  "Using with Zenject / Reflex" shows the replacement bindings.

### Migration

1. Register MessagePipe once in the root scope: `builder.RegisterMessagePipe();`.
2. Add a reason to each call: `Add("gold", 50)` becomes `Add("gold", 50, "level_reward")`.
3. Replace `CurrencyChanged += id => ...` with `ISubscriber<CurrencyChangedMessage>.Subscribe(message => ...)`;
   the message already carries `NewValue`.

## [1.0.1] - 2026-11-02

### Changed

- **Breaking:** root namespace renamed from `Currency` to `GDF.Currency`. Consumers must update
  `using Currency;` → `using GDF.Currency;` (and installer sub-namespaces similarly). Alignment
  with the GDF.* namespace discipline across the library family.

### Fixed

- VContainer installer compilation: added missing `using VContainer.Unity;` so `IInstaller`
  (which lives in `VContainer.Unity`, not `VContainer`) resolves. Without this fix the
  installer assembly failed with `CS0246`.

## [1.0.0] - 2026-11-02

Initial public release.

- `ICurrencyService` facade: `Get`, `GetEarnedTotal`, `Set`, `Add`, `Spend`, `Reset` + `Initialize`.
- Event `CurrencyChanged(string currencyId)` fires on every balance mutation.
- `CurrencyConfig : Config<string>` stored in a `CurrencyRepository : ConfigRepository<string, CurrencyConfig>` asset. Config carries display name, icon, default value and rarity.
- Persistence through `com.gdf.local-data-storage`: `CurrencyWalletData` with per-currency `(Current, EarnedTotal)` entries.
- Default values from the config seed the wallet on first `Initialize`; later game sessions reuse the persisted wallet and backfill newly-added currencies.
- DI installers for VContainer, Zenject and Reflex.
- EditMode tests cover init / add / spend / reset / set / unknown currency / persistence round-trip.
