# SQL Injection — demo (tre versioner)

Lille 3-lags system til IT-sikkerhedseksamen: **Vue-frontend → REST API → MSSQL**.
Samme funktioner (login + brugersøgning) findes i **tre** versioner, så man kan
demonstrere et angreb, vise at en begrænset inputfiltrering kun fanger ét eksempel
og kan omgås, og at parameterisering fjerner selve årsagen.

**Problemformulering:** Hvordan opstår SQL Injection teknisk, og hvorfor forhindrer
parameteriserede forespørgsler (prepared statements) sårbarheden mere robust end
filtrering af brugerinput?

## De tre versioner

| # | Version | Endpoint-suffiks | Hvordan bygges forespørgslen |
|---|---|---|---|
| 1 | **Usikker** | `Unsecure*` | Input sættes direkte ind i SQL-strengen (`$"... '{input}'"`) og køres råt (`ExecuteRaw`). |
| 2 | **Usikker med inputfiltrering** | `Filtered*` | Input køres FØRST gennem en begrænset blacklist, og de **filtrerede** værdier sendes til NØJAGTIG samme usikre konstruktion (`ExecuteRaw`). |
| 3 | **Parameteriseret** | `Secure*` | Input bindes som parametre (`ExecuteParam`) — fast SQL med pladsholdere (`@param`). |

Version 2 genbruger bevidst version 1's query-metoder: den eneste forskel er, at
inputtet filtreres, før strengen bygges. Konstruktion og eksekvering er identiske.

### Filterregler (version 2)

Komponenten [`api/Services/InputFilter.cs`](api/Services/InputFilter.cs) er en
**bevidst begrænset blacklist**:

- Fjerner kun de **hele ord** `OR` og `UNION`.
- Case-insensitivt (`OR`, `or`, `Or`, `UNION`, `uNiOn` …).
- Ordgrænser (`\bOR\b` / `\bUNION\b`) gør, at bogstaver **inde i** almindelige ord
  bevares — fx `or` i "password" eller `union` i "reunion".
- **Specialtegn (`'`, `--`) filtreres IKKE.**

Regex: `\b(?:OR|UNION)\b` med `IgnoreCase`.

> Dette er et eksempel på **begrænset blacklist-filtrering**. Pointen med demoen er,
> at netop dette filter er omgåeligt, fordi brugerinput stadig bliver en del af
> SQL-sætningen: angreb der ikke bruger ordene `OR`/`UNION` (fx `admin' --`) slipper
> uændret igennem. Demoen viser altså, at man ved blacklisting skal "jagte" alle
> ulovlige tegn/ord. Den påstår **ikke**, at enhver tænkelig validering eller en
> allowlist-strategi er virkningsløs — kun at *dette* filter er utilstrækkeligt.

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

Lagdeling (standard .NET): `LoginController` / `SearchController` (endpoints) →
`QueryService` (logik: tre varianter af hver forespørgsel) → `Database`
(model/dataadgang, `ExecuteRaw` vs. `ExecuteParam`). `InputFilter` er filterkomponenten
til version 2. `DbSeeder` opretter + fylder databasen ved opstart.

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

Versionen vælges af **endpointet** (ikke af et flag i body). Request-modellerne har
stadig et `secure`-felt af bagudkompatibilitetshensyn, men det **ignoreres**.

| Metode | Endpoint | Body |
|---|---|---|
| POST | `/api/Login/UnsecureLogin`  | `{ "username": "...", "password": "..." }` |
| POST | `/api/Login/FilteredLogin`  | `{ "username": "...", "password": "..." }` |
| POST | `/api/Login/SecureLogin`    | `{ "username": "...", "password": "..." }` |
| POST | `/api/Search/UnsecureSearch` | `{ "term": "..." }` |
| POST | `/api/Search/FilteredSearch` | `{ "term": "..." }` |
| POST | `/api/Search/SecureSearch`   | `{ "term": "..." }` |

Svaret er `{ sql, parameters, columns, rows, error, filter }`:

- `sql` — den SQL backend faktisk kørte.
- `parameters` — bundne parametre (kun den parameteriserede version), ellers `null`.
- `filter` — filtreringsdetaljer (kun version 2), ellers `null`. Indeholder `rule`
  og per felt `{ name, original, filtered, changed }` — de værdier der FAKTISK blev
  brugt til at bygge SQL-strengen.

### Request-eksempler

```bash
# 1. Usikker: kommentar-bypass logger ind som admin uden adgangskode
curl -s -X POST http://localhost:5000/api/Login/UnsecureLogin \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin'\'' --","password":""}'

# 2. Filtreret: OR-angrebet fanges (ordet OR fjernes -> SQL-fejl)
curl -s -X POST http://localhost:5000/api/Login/FilteredLogin \
  -H 'Content-Type: application/json' \
  -d '{"username":"x","password":"x'\'' OR '\''1'\''='\''1"}'

# 2b. Filtreret: men admin' -- indeholder hverken OR eller UNION -> samme bypass består
curl -s -X POST http://localhost:5000/api/Login/FilteredLogin \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin'\'' --","password":""}'

# 3. Parameteriseret: samme payload -> input er data -> 0 rækker
curl -s -X POST http://localhost:5000/api/Login/SecureLogin \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin'\'' --","password":""}'
```

## Sådan demonstreres det i frontend

Siden ligner et helt almindelig "Min konto" med et login- og et søgefelt. Knapperne
**Usikker** / **Usikker med inputfiltrering** / **Parameteriseret** øverst skifter
version. De indtastede værdier **bevares** ved skift (tidligere resultater ryddes),
så det samme input kan sammenlignes på tværs af alle tre versioner.

Hvert felt har en lille **Demo**-boks med knapper, der kun **udfylder feltet** — du
trykker selv **Log ind** / **Søg**:

- **Normal brug** (grøn) — et almindeligt, gyldigt input uden angreb.
- **Injection** (rød) — et injektionsforsøg.
- Kun i **Usikker med inputfiltrering** har login to injektionsknapper:
  - **Injection 1 — fanges af filter** (`' OR 1=1 --`): ordet `OR` fjernes → SQL-fejl.
  - **Injection 2 — slipper forbi** (`admin' --`): ingen `OR`/`UNION` → går uændret
    igennem → bypasset består. Pointen: *filteret er kun så godt som sin udvikler* —
    det fanger kun de mønstre, udvikleren har tænkt på.

For hvert request vises: SQL der blev kørt, filtrering (original → filtreret, version 2),
bundne parametre (version 3) og de faktiske rækker/fejl.

### Demonstrationssekvens

**Eksempel A — login `x` / `x' OR '1'='1`:**
1. **Usikker:** OR-betingelsen giver match → login "lykkes" (returnerer alle rækker).
2. **Usikker med inputfiltrering:** `OR` fjernes → `password` bliver `x'  '1'='1` →
   SQL-fejl (`Incorrect syntax near '1'.`) → angrebet virker ikke som tilsigtet.
3. **Parameteriseret:** værdierne forbliver data → 0 rækker.

**Eksempel B — login `admin' --` / (tom):**
1. **Usikker:** `--` udkommenterer password-tjekket → `admin`-rækken returneres.
2. **Usikker med inputfiltrering:** input indeholder hverken `OR` eller `UNION` →
   går **uændret** igennem → **samme bypass består**. (Det er pointen: filteret
   jagter kun `OR`/`UNION`, ikke `'` og `--`.)
3. **Parameteriseret:** hele strengen bindes som ét brugernavn → 0 rækker.

**UNION-søgning — `' UNION SELECT username, password FROM users --`:**
1. **Usikker:** UNION henter `password`-kolonnen med ud (data lækkes).
2. **Usikker med inputfiltrering:** `UNION` fjernes → den injicerede `SELECT` bliver
   en separat sætning, og da readeren kun læser første resultatsæt, kommer
   `password`-kolonnen **ikke** med ud. For netop denne payload neutraliseres lækken
   (resultatet bliver de normale `username, email`-rækker).
3. **Parameteriseret:** payloaden bindes som bogstavelig søgetekst → 0 resultater.

## Hvorfor virker parameterisering bedst

| | Usikker | Usikker + filter | Parameteriseret |
|---|---|---|---|
| Hvordan bygges forespørgslen | input ind i strengen | filtreret input ind i strengen | fast SQL + pladsholdere |
| Hvad er input | kan blive til **kode** | kan stadig blive til **kode** | er altid **data** |
| Hvad adresseres | — | kun kendte ord (`OR`/`UNION`) | selve årsagen |
| Omgåelig? | — | ja (fx `admin' --`) | nej for denne klasse af angreb |

Inputfiltrering med en blacklist fjerner kun de mønstre, den kender. Så snart et
angreb bruger andre tegn eller ord (her `'` og `--`), slipper det igennem — derfor
skal man ved blacklisting blive ved med at "jagte" ulovlige tegn. Parameterisering
fjerner årsagen: databasen får forespørgslens struktur først og behandler derefter
input udelukkende som værdier.

### Hvad demoen IKKE viser

Demoen viser, at **dette konkrete, bevidst begrænsede filter** er utilstrækkeligt og
omgåeligt. Den er **ikke** et bevis for, at al inputvalidering er nytteløs. En streng
**allowlist** (fx "brugernavn må kun indeholde `[a-z0-9]`") kan afvise mange angreb —
men korrekt forsvar mod SQL Injection er stadig parameterisering; validering er et
supplement, ikke en erstatning.

## Verifikation

- **Enhedstests (.NET, ingen DB):** [`api.Tests`](api.Tests) — `InputFilter`-regler
  og `QueryService`-SQL-konstruktion/filter-kobling.
  ```bash
  dotnet test api/api.sln
  ```
- **Mod en RIGTIG MSSQL via det kørende API:** [`scripts/verify-db.mjs`](scripts/verify-db.mjs)
  skelner mellem SQL-fejl, 0 rækker og uautoriseret match for alle eksempler ovenfor.
  ```bash
  docker compose up --build        # eller kør API'en mod en lokal MSSQL
  node scripts/verify-db.mjs       # evt. API_BASE=http://localhost:5000 foran
  ```

## Filer
- `api/Controllers/LoginController.cs`, `SearchController.cs` — REST-endpoints (3 pr. funktion)
- `api/Services/QueryService.cs` — logiklag: de tre varianter af hver forespørgsel
- `api/Services/InputFilter.cs` — filterkomponenten (begrænset blacklist) til version 2
- `api/Data/Database.cs` — model/dataadgang: `ExecuteRaw` vs. `ExecuteParam`
- `api/Data/DbSeeder.cs` — opretter + fylder databasen ved opstart
- `api/Models/` — `LoginRequest`, `SearchRequest`, `QueryResult` (+ `FilterInfo`/`FilterField`)
- `frontend/src/App.vue` — UI, 3-vejs versionsvælger, angrebsknapper
- `frontend/src/ResultView.vue` — viser filtrering + backend'ens SQL + resultat
- `api.Tests/` — enhedstests
- `scripts/verify-db.mjs` — ende-til-ende-verifikation mod MSSQL
- `frontend/nginx.conf` — serverer Vue + proxyer `/api` til API'en
- `compose.yaml` — db + api + web og de to netværk

> ⚠️ De usikre metoder er med vilje usikre og kun til undervisning. Lokalt demo-setup
> med fiktive data.
