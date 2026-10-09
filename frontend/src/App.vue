<script setup>
import { ref } from 'vue'
import ResultView from './ResultView.vue'

const mode = ref('vulnerable')      // 'vulnerable' | 'secure'
const user = ref('')
const pass = ref('')
const term = ref('')
const loginResult = ref(null)
const searchResult = ref(null)

function setMode(m) {
  mode.value = m
  loginResult.value = null
  searchResult.value = null
}

async function login() {
  const r = await fetch(`/api/${mode.value}/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: user.value, password: pass.value }),
  })
  loginResult.value = await r.json()
}

async function search() {
  const r = await fetch(`/api/${mode.value}/search?term=` + encodeURIComponent(term.value))
  searchResult.value = await r.json()
}

// Angrebs-payloads: sæt felterne og kør med det samme.
function injectAdminComment() { user.value = "admin' --"; pass.value = ''; login() }
function injectOrTrue()       { user.value = 'x'; pass.value = "x' OR '1'='1"; login() }
function injectUnion()        { term.value = "' UNION SELECT username, password FROM users --"; search() }
</script>

<template>
  <div class="page" :class="mode === 'vulnerable' ? 'demo-vuln' : 'demo-secure'">
    <div class="mode-bar">
      <span class="mode-label">
        {{ mode === 'vulnerable' ? '⚠️ SÅRBAR VERSION' : '✅ SIKKER VERSION' }}
      </span>
      <span class="mode-sub">
        {{ mode === 'vulnerable'
          ? 'Forespørgsler bygges med streng-sammensætning — data blandes med kode.'
          : 'Forespørgsler er parameteriserede — data holdes adskilt fra kode.' }}
      </span>
      <span class="mode-toggle">
        <button :class="{ active: mode === 'vulnerable' }" @click="setMode('vulnerable')">Sårbar</button>
        <button :class="{ active: mode === 'secure' }" @click="setMode('secure')">Sikker</button>
      </span>
    </div>

    <div class="cards">
      <!-- 1. LOGIN -->
      <section class="card">
        <h2>1. Login — omgå adgangskontrol</h2>
        <label>Brugernavn</label>
        <input v-model="user" />
        <label>Adgangskode</label>
        <input v-model="pass" />
        <button class="primary" @click="login">Log ind</button>

        <div class="attack-box">
          <div class="attack-title">Angreb (klik for at indsætte):</div>
          <button class="attack" @click="injectAdminComment">admin' --</button>
          <button class="attack" @click="injectOrTrue">' OR '1'='1</button>
          <div class="attack-hint">Mål: log ind som <code>admin</code> uden at kende adgangskoden.</div>
        </div>

        <ResultView :result="loginResult" :success-when-rows="true"
                    success-text="Login lykkedes!" fail-text="Forkert brugernavn/adgangskode." />
      </section>

      <!-- 2. SØGNING -->
      <section class="card">
        <h2>2. Brugersøgning — stjæl data (UNION)</h2>
        <label>Søg efter brugernavn</label>
        <input v-model="term" />
        <button class="primary" @click="search">Søg</button>

        <div class="attack-box">
          <div class="attack-title">Angreb (klik for at indsætte):</div>
          <button class="attack" @click="injectUnion">' UNION SELECT username, password ...</button>
          <div class="attack-hint">Mål: få <code>password</code>-kolonnen med ud selvom siden kun viser navn + email.</div>
        </div>

        <ResultView :result="searchResult" :success-when-rows="false" />
      </section>
    </div>
  </div>
</template>

<style>
* { box-sizing: border-box; }
body { margin: 0; font-family: system-ui, sans-serif; color: #222; background: #f5f5f5; }
.page { max-width: 1100px; margin: 0 auto; padding: 1rem; }

.mode-bar {
  display: flex; flex-wrap: wrap; align-items: center; gap: .75rem;
  padding: 1rem; border-radius: .5rem; color: #fff;
}
.demo-vuln   .mode-bar { background: #b02a37; }
.demo-secure .mode-bar { background: #146c43; }
.mode-label { font-weight: 700; font-size: 1.15rem; }
.mode-sub   { flex: 1 1 320px; font-size: .9rem; opacity: .95; }
.mode-toggle button {
  border: 1px solid #fff; background: transparent; color: #fff;
  padding: .3rem .8rem; border-radius: .3rem; cursor: pointer; margin-left: .3rem;
}
.mode-toggle button.active { background: #fff; color: #222; font-weight: 600; }

.cards { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-top: 1rem; }
@media (max-width: 800px) { .cards { grid-template-columns: 1fr; } }

.card { background: #fff; border: 1px solid #ddd; border-radius: .5rem; padding: 1rem; }
.card h2 { font-size: 1.05rem; margin: 0 0 .75rem; }
label { display: block; font-size: .85rem; font-weight: 600; margin: .5rem 0 .2rem; }
input { width: 100%; padding: .5rem; border: 1px solid #ccc; border-radius: .3rem; }
button.primary { margin-top: .75rem; background: #0d6efd; color: #fff; border: 0;
  padding: .5rem 1rem; border-radius: .3rem; cursor: pointer; }

.attack-box {
  margin-top: 1rem; padding: .6rem .75rem; background: #fff3cd;
  border: 1px solid #ffe69c; border-radius: .4rem;
  display: flex; flex-wrap: wrap; gap: .4rem; align-items: center;
}
.attack-title { width: 100%; font-weight: 600; font-size: .85rem; }
.attack-hint  { width: 100%; font-size: .8rem; color: #665; margin-top: .2rem; }
button.attack { background: #dc3545; color: #fff; border: 0; padding: .35rem .7rem;
  border-radius: .3rem; cursor: pointer; font-size: .85rem; }

.result { margin-top: 1rem; }
.sql-label { font-size: .8rem; font-weight: 600; color: #555; margin-bottom: .2rem; }
pre.sql { background: #1e1e1e; color: #d4d4d4; padding: .6rem .75rem; border-radius: .4rem;
  font-size: .82rem; white-space: pre-wrap; word-break: break-word; margin: 0 0 .6rem; }
pre.sql.params { background: #0d3b2e; color: #a7f3d0; }

.alert { padding: .5rem .75rem; border-radius: .3rem; margin-bottom: .6rem; font-size: .9rem; }
.alert.danger  { background: #f8d7da; color: #842029; }
.alert.success { background: #d1e7dd; color: #0f5132; }
.alert.warn    { background: #fff3cd; color: #664d03; }

table.data { width: 100%; border-collapse: collapse; font-size: .85rem; }
table.data th, table.data td { border: 1px solid #ccc; padding: .35rem .5rem; text-align: left; }
table.data th { background: #f1f1f1; }
</style>
