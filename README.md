# Resort Island

Monorepo pre hru Resort Island.

- `frontend/` – Unity klient (URP 2D)
- `backend/` – Laravel API (herné configy, hráči)

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
