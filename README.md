# Resort Island

Monorepo pre hru Resort Island.

- `frontend/` – Unity klient (URP 2D)
- `backend/` – Laravel API (herné configy, hráči)

## Backend lokálne

```bash
cd backend
composer install
cp .env.example .env && php artisan key:generate
touch database/database.sqlite
php artisan migrate --seed   # prázdna DB → schéma + počiatočné dáta, jedným príkazom
php artisan serve
```

Testy: `php artisan test --compact`, formátovanie: `vendor/bin/pint`.

### Databáza

- Schéma sa mení **len migráciami** (`php artisan make:migration ...`). Nasadenú migráciu už neupravujeme, pridáme novú.
- `php artisan migrate --seed` je bezpečné spúšťať opakovane — deploy ho spúšťa vždy. Testovací používateľ `test@example.com` sa seeduje len lokálne.
- Tabuľky: `users` (email + heslo), `game_configs` (verzované herné configy).

### Herné configy

Počiatočné dáta sú v `backend/database/game-configs/{key}.json` (`economy`, `wave_system`, `energy`, `needs`, `buildings`, `unlocks`, `waves`).
Zmena configu = upraviť JSON **a zvýšiť `version`**. Pri deployi sa nová verzia pridá, staré ostanú v DB. Ak niekto zmení dáta bez zvýšenia verzie, seed (a tým deploy) zlyhá.

`GET /api/v1/configs` vráti najnovšiu verziu každého configu:

```json
{ "data": [ { "key": "economy", "version": 1, "data": { "starting_coins": 1000, "...": "..." } } ] }
```

Cachovanie: odpoveď sa drží v Laravel cache (zmaže sa pri každom zápise configu) a má `Cache-Control: public, max-age=60` + `ETag`. Klient pošle `If-None-Match` s posledným ETagom a ak sa nič nezmenilo, dostane `304 Not Modified` bez tela.
