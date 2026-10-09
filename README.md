# SQL Injection — demo (sårbar vs. sikker)

Lille 3-lags system til IT-sikkerhedseksamen: **Vue-frontend → REST API → MSSQL**.
Samme funktioner (login + brugersøgning) findes i en sårbar og en sikker version,
så man kan demonstrere et angreb på den ene og vise at rettelsen virker på den anden.

**Problemformulering:** Hvordan opstår SQL Injection teknisk, og hvorfor forhindrer
parameteriserede forespørgsler (prepared statements) sårbarheden mere robust end
filtrering af brugerinput?

## Arkitektur

```
            http://localhost:8080            /api  (proxy)
  Browser ───────────────────────► web (nginx + Vue) ───────────► api (.NET REST) ───► db (MSSQL)
                                     [frontend-net]                [frontend+backend]    [backend-net]
                                                                        ▲
  DataGrip / SSMS ──────────────────────────────────────────────────────┼─ localhost:1433 (kun host)
                                                                         │
  curl / Postman ───────────────────────────────────────── localhost:5000 (api direkte)
```

- **db** ligger kun på `backend`-netværket.
- **web** ligger kun på `frontend`-netværket → kan **ikke** nå databasen. Al dataadgang
  fra applikationen går gennem API'en.
- **api** ligger på begge → den eneste bro mellem frontend og database.

## Kør

```bash
docker compose up --build
```

Første opstart tager et øjeblik: MSSQL-containeren skal bootes, og API'en venter
på at den er klar og seeder derefter databasen (`SqliDemo` + tabellen `users`
med mock-data). Alt data lever i databasen — intet er hardcodet i frontend.

| Hvad | URL |
|---|---|
| Frontend (demoen) | http://localhost:8080 |
| REST API direkte | http://localhost:5000/api/health |
| MSSQL (DataGrip/SSMS) | `localhost:1433` |

### Forbind med DataGrip / SSMS
- Host: `localhost`, Port: `1433`
- Bruger: `sa`  ·  Password: `Str0ng!Passw0rd`
- Database: `SqliDemo`  ·  slå "Trust server certificate" til

## REST API

Frontend sender kun data + et `secure`-flag. **Backend afgør ud fra flaget**, om
forespørgslen køres rå (sårbar) eller parameteriseret (sikker).

| Metode | Endpoint | Body |
|---|---|---|
| POST | `/api/login`  | `{ "username": "...", "password": "...", "secure": false }` |
| POST | `/api/search` | `{ "term": "...", "secure": false }` |

- **`secure: false`** → backend sætter input direkte ind i SQL-strengen og kører den rå.
- **`secure: true`** → backend kører en **parameteriseret** forespørgsel. Det er den
  eneste forskel — ingen filtrering eller sanitering af input. Parameteriseringen alene
  stopper angrebet.

Svaret er `{ sql, parameters, columns, rows, error }` (`sql` er den SQL backend faktisk kørte).

Lagdeling (standard .NET): `LoginController` / `SearchController` (endpoints) →
`QueryService` (logik, vælger rå vs. parameteriseret ud fra flaget) → `Database`
(model/dataadgang, `ExecuteRaw` vs. `ExecuteParam`). Forskellen mellem sårbar og
sikker ligger udelukkende i `QueryService` — rå streng vs. bundne parametre.
`DbSeeder` opretter + fylder databasen ved opstart.

Demonstrér angrebet direkte mod API'en:
```bash
# Sårbar: logger ind som admin uden adgangskode
curl -s -X POST http://localhost:5000/api/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin'\'' --","password":"","secure":false}'

# Sikker: samme payload -> parameteriseret -> 0 rækker
curl -s -X POST http://localhost:5000/api/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin'\'' --","password":"","secure":true}'
```

## Sådan demonstreres det i frontend

Knapperne **Sårbar** / **Sikker** øverst skifter mode (sætter `secure`-flaget).
Angrebsknapperne **udfylder kun feltet** — du trykker selv **Log ind** / **Søg** for
at sende. Resultatet viser den SQL backend kørte (+ parametre i den sikre version).

### 1. Login — omgå adgangskontrol
Klik **`admin' --`** (udfylder feltet), tryk **Log ind**.
- **Sårbar:** logger ind som `admin` uden adgangskode. SQL bliver
  `... WHERE username = 'admin' --' AND password = ''` — `--` udkommenterer password-tjekket.
- **Sikker:** login fejler. Hele `admin' --` bindes som ét brugernavn.

### 2. Brugersøgning — stjæl data (UNION)
Klik **`' UNION SELECT username, password ...`**, tryk **Søg**.
- **Sårbar:** siden viser normalt kun navn + email, men UNION-angrebet henter
  `password`-kolonnen med ud for alle brugere.
- **Sikker:** payloaden bindes som bogstavelig søgetekst og giver 0 resultater.

## Hvorfor virker rettelsen

| | Sårbar | Sikker |
|---|---|---|
| Hvordan bygges forespørgslen | input sættes ind i SQL-strengen (`$"... '{input}'"`) | fast SQL med pladsholdere (`@param`) |
| Hvad er input | kan blive til **kode** | er altid **data** |
| Hvad adresseres | — | selve årsagen: adskillelse af kode og data |

Inputfiltrering (fjern `'`, blokér ordet `UNION`, …) kan omgås med fx anden casing,
encoding eller alternative payloads, fordi input stadig ender som kode.
Parameterisering fjerner årsagen: databasen får forespørgslens struktur først og
behandler derefter input udelukkende som værdier.

## Filer
- `api/Controllers/LoginController.cs`, `SearchController.cs` — REST-endpoints + validering
- `api/Services/QueryService.cs` — logiklag: vælger rå vs. parameteriseret ud fra flaget
- `api/Data/Database.cs` — model/dataadgang: `ExecuteRaw` vs. `ExecuteParam`
- `api/Data/DbSeeder.cs` — opretter + fylder databasen ved opstart
- `api/Models/` — `LoginRequest`, `SearchRequest`, `QueryResult`
- `frontend/src/App.vue` — UI, mode-toggle (secure-flag), angrebsknapper
- `frontend/src/ResultView.vue` — viser backend'ens SQL + resultat
- `frontend/nginx.conf` — serverer Vue + proxyer `/api` til API'en
- `compose.yaml` — db + api + web og de to netværk

> ⚠️ De sårbare metoder er med vilje usikre og kun til undervisning.
