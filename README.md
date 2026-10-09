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
| REST API direkte | http://localhost:5000/api/query/health |
| MSSQL (DataGrip/SSMS) | `localhost:1433` |

### Forbind med DataGrip / SSMS
- Host: `localhost`, Port: `1433`
- Bruger: `sa`  ·  Password: `Str0ng!Passw0rd`
- Database: `SqliDemo`  ·  slå "Trust server certificate" til

## REST API

Backend gemmer **ingen** forespørgsler. Den har ét generisk endpoint, der udfører
den SQL frontend sender. De konkrete demo-forespørgsler ligger som consts i
`frontend/src/App.vue`.

| Metode | Endpoint | Body |
|---|---|---|
| POST | `/api/query` | `{ "sql": "...", "parameters": { "@navn": "værdi" } }` |

- **Sårbar:** frontend sætter input ind i `sql`-strengen og sender uden `parameters`.
- **Sikker:** frontend sender fast `sql` med pladsholdere + `parameters` adskilt.

Svaret er `{ columns, rows, error }`.

Lagdeling (standard .NET): `QueryController` (endpoints) → `QueryService` (logik)
→ `Database` (model/dataadgang). `DbSeeder` opretter + fylder databasen ved opstart.

Demonstrér angrebet direkte mod API'en:
```bash
# Sårbar: rå SQL, logger ind som admin uden adgangskode
curl -s -X POST http://localhost:5000/api/query \
  -H 'Content-Type: application/json' \
  -d '{"sql":"SELECT id, username, email FROM users WHERE username = '\''admin'\'' --'\'' AND password = '\'''\''"}'

# Sikker: samme payload som parameter -> 0 rækker
curl -s -X POST http://localhost:5000/api/query \
  -H 'Content-Type: application/json' \
  -d '{"sql":"SELECT id, username, email FROM users WHERE username = @username AND password = @password","parameters":{"@username":"admin'\'' --","@password":""}}'
```

## Sådan demonstreres det i frontend

Knapperne **Sårbar** / **Sikker** øverst skifter mode. Under hvert felt vises live den
SQL, der bliver sendt. Angrebsknapperne **udfylder kun feltet** — du trykker selv
**Log ind** / **Søg** for at sende.

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
| Hvordan bygges forespørgslen | input sættes ind i SQL-strengen (`` `... '${input}'` ``) | fast SQL med pladsholdere (`@param`) |
| Hvad er input | kan blive til **kode** | er altid **data** |
| Hvad adresseres | — | selve årsagen: adskillelse af kode og data |

Inputfiltrering (fjern `'`, blokér ordet `UNION`, …) kan omgås med fx anden casing,
encoding eller alternative payloads, fordi input stadig ender som kode.
Parameterisering fjerner årsagen: databasen får forespørgslens struktur først og
behandler derefter input udelukkende som værdier.

## Filer
- `api/Controllers/QueryController.cs` — REST-endpoints
- `api/Services/QueryService.cs` — logiklag
- `api/Data/Database.cs` — model/dataadgang (udfører SQL)
- `api/Data/DbSeeder.cs` — opretter + fylder databasen ved opstart
- `api/Models/` — `QueryRequest`, `QueryResult`
- `frontend/src/App.vue` — UI + de rå SQL-consts, mode-toggle, angrebsknapper
- `frontend/src/ResultView.vue` — viser resultatet
- `frontend/nginx.conf` — serverer Vue + proxyer `/api` til API'en
- `compose.yaml` — db + api + web og de to netværk

> ⚠️ De sårbare metoder er med vilje usikre og kun til undervisning.
