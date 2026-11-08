# Resort Island

Monorepo pre hru Resort Island.

- `frontend/` – Unity klient (URP 2D)
- `backend/` – Laravel API (herné configy, hráči)

## Procedural island

`frontend/Assets/Scripts/Resort/IslandGenerator.cs` generates an isometric island on the scene `Grid`
and draws it with the terrain sprites from `frontend/Assets/ResortEmpire/Sprites/Terrain/Terrain.png`
(deep water, shallow water, sand, grass). It runs in edit mode too, so the island is visible in the
Scene view. It is attached to the `ResortManager` object in `frontend/Assets/Scenes/Island.unity`.
Change its map size, seed, coastline and noise fields in the Inspector, or use **Generate island**
from the component context menu to regenerate it.

`TileData` is the authoritative per-cell data source. Use `IslandGenerator.TryGetTile(x, y, out tile)`
for 0-based map coordinates, `IsBuildableArea` for multi-tile validation, and
`TrySetOccupiedArea` to reserve a rectangular area. Terrain-specific lists (`WaterLayer`, `SandLayer`,
`GrassLayer`) provide logical layer views. Existing centered resort `Vector3Int`
coordinates are accepted by the `TryGetTile(Vector3Int, ...)` overload. `GroundHeightAt(cell)` gives the
surface height above water (sand 0.0735, grass 0.147); `ResortManager.CellToWorld` adds it to buildings.

Buildings use `BuildingConfig.Footprint` (Reception, Restaurant, Pool, Bungalows 3×3; Beach bar 2×2) and
occupy every cell of it. On a new game `ResortManager` asks `IslandGenerator.TryFindSite` for a spot:
inland nearest to the island centre, or on the beach for buildings with `Prefer Shore`. Starting
buildings come from `ResortStartConfig` (`Auto Place` falls back to the configured cell). A save whose
buildings do not fit the generated island is discarded and a new game starts.

Sprite sheets (buildings, decor, paths, transport) are in `frontend/Assets/ResortEmpire/`, see its `README.md`.

## Branching (GitFlow)

| Vetva | Účel | Čo spustí CI/CD |
| --- | --- | --- |
| `feature/*`, `SCRUM-*` | práca na tasku, PR do `develop` | testy BE na PR |
| `develop` | integrácia | testy → Docker image → deploy BE na **dev** |
| `release/*`, `hotfix/*` | príprava vydania, PR do `main` | testy BE na PR |
| `main` | **prod** | testy → image → deploy BE na **prod** + Android build (`.aab` + `.apk`) ako artefakt |

Manuálne spustenie workflowu (Actions → CI/CD → Run workflow) zbehne testy a Android build vybranej vetvy.

## Prostredia

| | Lokálne | dev | prod |
| --- | --- | --- | --- |
| BE `APP_ENV` | `local` | `development` | `production` |
| Databáza | SQLite | PostgreSQL | PostgreSQL |
| Konfigurácia BE | `backend/.env` z `.env.example` | `.env` na serveri z `backend/deploy/server.env.example` | to isté |
| Hra | Editor | Development Build | release build |

V repozitári nie sú žiadne secrets — všetko sa nastavuje env premennými (`.env` lokálne a na serveri, v CI GitHub secrets).

Hra si prostredie vyberá podľa typu buildu (`frontend/Assets/Scripts/Backend/BackendEnvironment.cs`): Editor a Development Build → dev, release build → prod. URL sú v `frontend/Assets/Resources/BackendConfig.asset`; v Editore sa dá cez `editorOverrideUrl` pripojiť na lokálny BE.

### Server (dev / prod)

Linux s Dockerom a Compose v2. Každé prostredie má vlastný adresár (napr. `/srv/resort-island-dev`, `/srv/resort-island-prod`) s:

- `.env` — skopírovaný z `backend/deploy/server.env.example` a vyplnený (`APP_KEY`, `DB_PASSWORD`, `APP_URL`, …); ostáva len na serveri,
- `docker-compose.yml` a `deploy.sh` z `backend/deploy/`.

`sh deploy.sh <image>` stiahne image, spustí migrácie a seed, vymení kontajner a overí `/up`. Ak zlyhá ktorýkoľvek krok, skript skončí s chybou; pri chybe stiahnutia image alebo migrácie beží pôvodný kontajner ďalej. HTTPS (reverse proxy pred `APP_PORT`) nie je súčasťou tohto setupu.

Image sa dá zostaviť aj ručne: `docker build -t resort-island-backend backend`.

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
- `php artisan migrate --seed` je bezpečné spúšťať opakovane — deploy ho spúšťa vždy. Testovací používateľ `test@example.com` a admin `admin@example.com` (heslo `password`) sa seedujú len lokálne.
- Tabuľky: `users` (hráči), `admins` (účty do admin panelu), `personal_access_tokens` (API tokeny hráčov), `game_configs` (verzované herné configy).

### Hráči a API tokeny

Hra sa pri prvom spustení zaregistruje anonymne a dostane token (Laravel Sanctum). Token posiela v hlavičke `Authorization: Bearer <token>`.

| Endpoint | Auth | Popis |
| --- | --- | --- |
| `POST /api/v1/auth/anonymous` | — | nový anonymný hráč, vráti `{ data: player, token }` (max 10/min z IP) |
| `POST /api/v1/me/email` | token | prepojí email + heslo (`password_confirmation`, voliteľne `name`) s anonymným účtom |
| `POST /api/v1/auth/login` | — | email + heslo → nový token toho istého účtu, napr. na inom zariadení (max 5/min) |
| `POST /api/v1/auth/logout` | token | zruší aktuálny token |
| `GET /api/v1/me` | token | aktuálny hráč |

Zabanovaný hráč dostane na každý endpoint s tokenom aj na login `403 { "message": "This account is banned.", "reason": "..." }`. Ban a unban robí admin v paneli (*Players*); tokeny ostávajú platné, takže po unbane hra funguje ďalej.

### Analytika

`POST /api/v1/analytics/events` (token, max 30 dávok/min) uloží dávku udalostí hráča do `analytics_events` (hodnoty udalosti v JSONB stĺpci `payload`). Odpoveď `202 { "accepted": n }`; ak je čo i len jedna udalosť neplatná, neuloží sa nič (`422`).

```json
{
  "app_version": "0.3.0",
  "platform": "android",
  "events": [
    { "name": "wave_complete", "occurred_at": "2026-11-07T10:15:00+01:00", "session_id": "<uuid>",
      "payload": { "wave": 3, "rating": 4, "drain_duration_s": 21.5, "worker_moves_pre_wave": 2, "worker_moves_in_wave": 1 } }
  ]
}
```

Pravidlá: max 100 udalostí v dávke, `name` v snake_case (max 64 znakov), `payload` je objekt do 4 KB, `session_id` je UUID. V admin paneli sú udalosti v *Analytics events* (filter podľa názvu a dňa, detail s payloadom).

Návrh udalostí pre metriky z nefunkčných požiadaviek:

| Udalosť | Payload | Metrika |
| --- | --- | --- |
| `wave_start` | `wave` | wave_complete_rate (spolu s `wave_complete`) |
| `wave_complete` | `wave`, `rating`, `satisfaction`, `drain_duration_s`, `worker_moves_pre_wave`, `worker_moves_in_wave` | rozdelenie hodnotení, dĺžka dobehu, presuny pred vlnou / počas vlny |
| `upgrade_offered`, `upgrade_taken` | `building`, `level` | upgrade_take_rate |
| `session_start`, `session_end` | `duration_s` (pri `session_end`) | dĺžka session |

### Herné configy

Zdroj pravdy je databáza; configy sa menia v admin paneli (*Game configs*). Súbory `backend/database/game-configs/{key}.json` (`economy`, `wave_system`, `energy`, `needs`, `buildings`, `unlocks`, `waves`) sú počiatočné dáta: seed zo súboru pridá config, ktorý v DB ešte nie je, alebo verziu vyššiu než má DB. Úpravy z admin panelu nikdy neprepíše (len vypíše, že ponechal verziu z DB).

| | dev (aj lokálne) | prod |
| --- | --- | --- |
| Úprava / upload | prepíše aktuálnu verziu, zmeniť sa dá čokoľvek | vytvorí novú verziu |
| Čo sa smie zmeniť | všetko, aj nové configy | len čísla: celé ostane celým, nezáporné nezáporným; kľúče, dĺžky zoznamov a texty (napr. `id` budov) ostávajú |

Režim určuje `APP_ENV` (`local`/`development` = dev), dá sa prepísať premennou `GAME_CONFIGS_FREE_EDITING=true|false`.

V admin paneli: zoznam najnovších verzií, detail s rozbaľovacím stromom a históriou verzií, úprava v JSON editore (chyby s cestou, napr. `buildings.0.id`), **Download** jedného configu (formát súborov v repe), **Download all** (formát `GET /api/v1/configs`) a **Upload** jedného/viacerých súborov alebo celého „Download all“ — publikuje sa všetko alebo nič.

Wave sim v CI kontroluje len súbory v repe. Zmenu z admin panelu over pred playtestom: *Download all* → `python3 tools/wave_sim/wave_sim.py --configs <stiahnutý súbor>` → *Upload*.

`GET /api/v1/configs` vráti najnovšiu verziu každého configu:

```json
{ "data": [ { "key": "economy", "version": 1, "data": { "starting_coins": 1000, "...": "..." } } ] }
```

Cachovanie: odpoveď sa drží v Laravel cache (zmaže sa pri každom zápise configu) a má `Cache-Control: public, max-age=60` + `ETag`. Klient pošle `If-None-Match` s posledným ETagom a ak sa nič nezmenilo, dostane `304 Not Modified` bez tela.

## Admin panel (Web UI)

Filament panel na `/admin` (lokálne `http://localhost:8000/admin`, prihlásenie `admin@example.com` / `password`).
Admini sú oddelení od hráčov (tabuľka `admins`, guard `admin`) — hráčsky účet sa do panelu neprihlási.

Admina na dev/prod serveri vytvorí:

```bash
docker compose exec app php artisan make:filament-user
```

## CI/CD — nastavenie (raz, robí admin repa)

Workflow: `.github/workflows/ci-cd.yml`. Chýbajúce nastavenie nechá job spadnúť so správou, čo chýba.

### Deploy BE

Pri pushi na `develop` / `main` pipeline zostaví image do GHCR, nahrá `docker-compose.yml` + `deploy.sh` do `DEPLOY_PATH` na serveri a spustí `deploy.sh` (migrácie bežia tu). Adresár musí mať pripravený `.env`, viď *Server (dev / prod)*.

Settings → Environments → vytvoriť `development` a `production`, v každom:

| Názov | Typ | Hodnota |
| --- | --- | --- |
| `DEPLOY_SSH_HOST` | secret | adresa servera |
| `DEPLOY_SSH_USER` | secret | používateľ s prístupom k Dockeru |
| `DEPLOY_SSH_KEY` | secret | privátny SSH kľúč (verejný v `~/.ssh/authorized_keys` na serveri) |
| `DEPLOY_SSH_KNOWN_HOSTS` | secret, voliteľné | výstup `ssh-keyscan <host>`; bez neho sa host kľúč zoberie automaticky |
| `DEPLOY_PATH` | variable | absolútna cesta na serveri, napr. `/srv/resort-island-dev` |
| `DEPLOY_SSH_PORT` | variable, voliteľné | default `22` |
| `BACKEND_URL` | variable, voliteľné | verejná URL, po deployi sa skontroluje `/up` |

Pre `production` odporúčame v environmente zapnúť *Required reviewers*.

### Android build

Repository secrets:

| Názov | Povinné | Popis |
| --- | --- | --- |
| `UNITY_EMAIL`, `UNITY_PASSWORD` | áno | Unity účet |
| `UNITY_LICENSE` | áno pri Personal licencii | obsah `Unity_lic.ulf` z počítača s aktivovanou Unity (macOS: `/Library/Application Support/Unity/Unity_lic.ulf`) |
| `UNITY_SERIAL` | namiesto `UNITY_LICENSE` pri Pro | sériové číslo |
| `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME`, `ANDROID_KEYALIAS_PASS` | pre Google Play | upload keystore (`base64 -i user.keystore`); bez nich sa build podpíše debug kľúčom |

Verzia buildu sa odvodzuje z git tagov (`v0.1.0` na `main`). Výsledné `.aab` a `.apk` sú v behu workflowu v sekcii *Artifacts* (30 dní). Release build sa pripája na prod BE.