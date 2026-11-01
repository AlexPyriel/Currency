# Changelog

## [1.0.0] - 2026-11-02

Initial public release.

- `ICurrencyService` facade: `Get`, `GetEarnedTotal`, `Set`, `Add`, `Spend`, `Reset` + `Initialize`.
- Event `CurrencyChanged(string currencyId)` fires on every balance mutation.
- `CurrencyConfig : Config<string>` stored in a `CurrencyRepository : ConfigRepository<string, CurrencyConfig>` asset. Config carries display name, icon, default value and rarity.
- Persistence through `com.gdf.local-data-storage`: `CurrencyWalletData` with per-currency `(Current, EarnedTotal)` entries.
- Default values from the config seed the wallet on first `Initialize`; later game sessions reuse the persisted wallet and backfill newly-added currencies.
- DI installers for VContainer, Zenject and Reflex.
- EditMode tests cover init / add / spend / reset / set / unknown currency / persistence round-trip.
