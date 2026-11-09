// Verificerer de tre varianter mod en RIGTIG MSSQL via det kørende API.
//
// Forudsætning: API'en kører og er forbundet til en seedet database, fx:
//   docker compose up --build        (API på http://localhost:5000)
// Kør derefter:
//   node scripts/verify-db.mjs       (evt. API_BASE=http://... foran)
//
// Scriptet skelner mellem: SQL-FEJL, 0 matchende rækker, og uautoriseret match.

const BASE = process.env.API_BASE || 'http://localhost:5000';

async function post(path, body) {
  const r = await fetch(BASE + path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  if (!r.ok) return { error: `HTTP ${r.status}: ${await r.text()}` };
  return r.json();
}

// Klassificér et svar: SQL-fejl, intet match, eller match (n rækker).
function classify(res) {
  if (res.error) return { kind: 'ERROR', detail: res.error };
  const n = res.rows?.length ?? 0;
  return n > 0 ? { kind: 'MATCH', n } : { kind: 'NO_MATCH', n: 0 };
}

let failures = 0;
function check(label, actual, expectedKind, extra = () => true) {
  const ok = actual.kind === expectedKind && extra(actual);
  if (!ok) failures++;
  const badge = ok ? 'PASS' : 'FAIL';
  const detail = actual.kind === 'ERROR' ? ` (fejl: ${actual.detail})`
    : actual.kind === 'MATCH' ? ` (${actual.n} række(r))` : '';
  console.log(`  [${badge}] ${label}: ${actual.kind}${detail}`);
}

function section(t) { console.log(`\n=== ${t} ===`); }

async function main() {
  // Vent på at API'en svarer.
  for (let i = 0; i < 30; i++) {
    try { if ((await fetch(BASE + '/api/health')).ok) break; } catch {}
    await new Promise(r => setTimeout(r, 2000));
  }

  // --- Eksempel A: x / x' OR '1'='1 ---
  section("Eksempel A — Login: x / x' OR '1'='1");
  const aUser = 'x', aPass = "x' OR '1'='1";
  const aUns = await post('/api/Login/UnsecureLogin', { username: aUser, password: aPass });
  const aFil = await post('/api/Login/FilteredLogin', { username: aUser, password: aPass });
  const aSec = await post('/api/Login/SecureLogin',   { username: aUser, password: aPass });
  console.log('  SQL (usikker):   ' + aUns.sql);
  console.log('  SQL (filtreret): ' + aFil.sql);
  console.log('  Filter (pass):   ' + JSON.stringify(aFil.filter?.fields?.find(f => f.name === 'Adgangskode')));
  check('Usikker: OR giver match', classify(aUns), 'MATCH');
  check('Filtreret: OR fjernet -> fejler (ikke tilsigtet match)', classify(aFil), 'ERROR');
  check('Parameteriseret: intet match', classify(aSec), 'NO_MATCH');

  // --- Eksempel B: admin' -- / (tom) ---
  section("Eksempel B — Login: admin' -- / (tom)");
  const bUser = "admin' --", bPass = '';
  const bUns = await post('/api/Login/UnsecureLogin', { username: bUser, password: bPass });
  const bFil = await post('/api/Login/FilteredLogin', { username: bUser, password: bPass });
  const bSec = await post('/api/Login/SecureLogin',   { username: bUser, password: bPass });
  console.log('  SQL (usikker):   ' + bUns.sql);
  console.log('  SQL (filtreret): ' + bFil.sql);
  const bChanged = bFil.filter?.fields?.some(f => f.changed);
  check('Usikker: kommentar-bypass returnerer admin', classify(bUns), 'MATCH', a => a.n >= 1);
  check('Filtreret: uaendret -> SAMME bypass', classify(bFil), 'MATCH', a => a.n >= 1);
  check('Filtreret: input blev IKKE aendret', { kind: bChanged ? 'CHANGED' : 'UNCHANGED' }, 'UNCHANGED');
  check('Filtreret SQL == usikker SQL', { kind: bUns.sql === bFil.sql ? 'EQ' : 'NE' }, 'EQ');
  check('Parameteriseret: intet match', classify(bSec), 'NO_MATCH');

  // --- UNION-søgning ---
  section("UNION — Søgning: ' UNION SELECT username, password FROM users --");
  const uTerm = "' UNION SELECT username, password FROM users --";
  const uUns = await post('/api/Search/UnsecureSearch', { term: uTerm });
  const uFil = await post('/api/Search/FilteredSearch', { term: uTerm });
  const uSec = await post('/api/Search/SecureSearch',   { term: uTerm });
  console.log('  SQL (usikker):   ' + uUns.sql);
  console.log('  SQL (filtreret): ' + uFil.sql);
  const leaked = s => JSON.stringify(s.rows || []).includes('admin123');
  check('Usikker: UNION laekker passwords', classify(uUns), 'MATCH', () => leaked(uUns));
  // Filteret fjerner UNION -> ingen password-kolonne kommer ud. Afhængigt af payload
  // kan resultatet enten blive en SQL-fejl eller blot de normale rækker — men
  // det afgørende er, at passwords IKKE lækkes.
  const uFilClass = classify(uFil);
  console.log(`  Filtreret udfald: ${uFilClass.kind}` + (uFilClass.detail ? ` (${uFilClass.detail})` : ''));
  check('Filtreret: passwords laekkes IKKE', { kind: leaked(uFil) ? 'LEAK' : 'NO_LEAK' }, 'NO_LEAK');
  check('Parameteriseret: intet match', classify(uSec), 'NO_MATCH');

  // --- Normal brug virker i alle tre ---
  section('Normal brug (sanity) i alle tre versioner');
  check('Login mette (usikker)',       classify(await post('/api/Login/UnsecureLogin', { username: 'mette', password: 'sommer2025' })), 'MATCH');
  check('Login mette (filtreret)',     classify(await post('/api/Login/FilteredLogin', { username: 'mette', password: 'sommer2025' })), 'MATCH');
  check('Login mette (parameteriseret)', classify(await post('/api/Login/SecureLogin', { username: 'mette', password: 'sommer2025' })), 'MATCH');
  check('Søg "met" (usikker)',          classify(await post('/api/Search/UnsecureSearch', { term: 'met' })), 'MATCH');
  check('Søg "met" (filtreret)',        classify(await post('/api/Search/FilteredSearch', { term: 'met' })), 'MATCH');
  check('Søg "met" (parameteriseret)',  classify(await post('/api/Search/SecureSearch',   { term: 'met' })), 'MATCH');

  console.log(`\n${failures === 0 ? 'ALLE TJEK BESTÅET' : failures + ' TJEK FEJLEDE'}`);
  process.exit(failures === 0 ? 0 : 1);
}

main().catch(e => { console.error(e); process.exit(1); });
