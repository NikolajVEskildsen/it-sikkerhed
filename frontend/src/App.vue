<script setup>
import { ref, computed } from 'vue'
import ResultView from './ResultView.vue'

// Tre selvstændige versioner. Hver har sit eget backend-endpoint.
const mode = ref('vulnerable')   // 'vulnerable' | 'filtered' | 'secure'

const MODES = {
  vulnerable: {
    name: 'Usikker',
    login: '/api/Login/UnsecureLogin',
    search: '/api/Search/UnsecureSearch',
  },
  filtered: {
    name: 'Usikker med inputfiltrering',
    login: '/api/Login/FilteredLogin',
    search: '/api/Search/FilteredSearch',
  },
  secure: {
    name: 'Parameteriseret',
    login: '/api/Login/SecureLogin',
    search: '/api/Search/SecureSearch',
  },
}
const current = computed(() => MODES[mode.value])

const user = ref('')
const pass = ref('')
const term = ref('')
const loginResult = ref(null)
const searchResult = ref(null)

// Skift version: bevar de indtastede værdier, men ryd tidligere resultater,
// så det samme input kan sammenlignes på tværs af alle tre versioner.
function setMode(m) {
  mode.value = m
  loginResult.value = null
  searchResult.value = null
}

async function post(url, body) {
  const r = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  if (!r.ok) return { error: await r.text() }
  return await r.json()
}

async function runLogin() {
  searchResult.value = null
  loginResult.value = await post(current.value.login, { username: user.value, password: pass.value })
}

async function runSearch() {
  loginResult.value = null
  searchResult.value = await post(current.value.search, { term: term.value })
}

// Demo-knapper: udfylder KUN felterne — man trykker selv Log ind / Søg.
function loginNormal()    { user.value = 'mette'; pass.value = 'sommer2025' }        // gyldig bruger
function loginInjection() { user.value = "admin' --"; pass.value = '' }              // klassisk bypass
function loginCaught()    { user.value = 'admin'; pass.value = "' OR 1=1 --" }       // filteret fanger ordet OR
function searchNormal()   { term.value = 'met' }                                     // normal søgning
function searchInjection(){ term.value = "' UNION SELECT username, password FROM users --" }
</script>

<template>
  <div class="page" :class="'demo-' + mode">
    <header class="topbar">
      <strong>Min konto</strong>
      <nav class="versions">
        <button :class="{ active: mode === 'vulnerable' }" @click="setMode('vulnerable')">Usikker</button>
        <button :class="{ active: mode === 'filtered' }" @click="setMode('filtered')">Usikker med inputfiltrering</button>
        <button :class="{ active: mode === 'secure' }" @click="setMode('secure')">Parameteriseret</button>
      </nav>
    </header>

    <div class="cards">
      <!-- 1. LOGIN -->
      <section class="card">
        <h2>Log ind</h2>
        <label>Brugernavn</label>
        <input v-model="user" />
        <label>Adgangskode</label>
        <input v-model="pass" type="text" />
        <button class="primary" @click="runLogin">Log ind</button>

        <div class="demo-box">
          <span class="demo-label">Demo:</span>
          <button class="demo normal" @click="loginNormal">Normal brug</button>
          <button v-if="mode !== 'filtered'" class="demo attack" @click="loginInjection">Injection</button>
          <template v-else>
            <button class="demo attack" @click="loginCaught">Injection 1 — fanges af filter</button>
            <button class="demo attack" @click="loginInjection">Injection 2 — slipper forbi</button>
          </template>
        </div>

        <ResultView :result="loginResult" :success-when-rows="true"
                    success-text="Login lykkedes!" fail-text="Forkert brugernavn/adgangskode." />
      </section>

      <!-- 2. SØGNING -->
      <section class="card">
        <h2>Søg bruger</h2>
        <label>Brugernavn</label>
        <input v-model="term" />
        <button class="primary" @click="runSearch">Søg</button>

        <div class="demo-box">
          <span class="demo-label">Demo:</span>
          <button class="demo normal" @click="searchNormal">Normal brug</button>
          <button class="demo attack" @click="searchInjection">Injection</button>
        </div>

        <ResultView :result="searchResult" :success-when-rows="false" />
      </section>
    </div>
  </div>
</template>

<style>
* { box-sizing: border-box; }
body { margin: 0; font-family: system-ui, sans-serif; color: #222; background: #f5f5f5; }
.page { max-width: 1000px; margin: 0 auto; padding: 1rem; }

.topbar {
  display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between;
  gap: .75rem; padding: .75rem 1rem; background: #fff; border: 1px solid #ddd; border-radius: .5rem;
}
.topbar strong { font-size: 1.05rem; }
.versions button {
  border: 1px solid #ccc; background: #f1f1f1; color: #333;
  padding: .3rem .8rem; border-radius: .3rem; cursor: pointer; margin-left: .3rem; font-size: .85rem;
}
.versions button.active { background: #0d6efd; color: #fff; border-color: #0d6efd; font-weight: 600; }

.cards { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-top: 1rem; }
@media (max-width: 800px) { .cards { grid-template-columns: 1fr; } }

.card { background: #fff; border: 1px solid #ddd; border-radius: .5rem; padding: 1rem; }
.card h2 { font-size: 1.05rem; margin: 0 0 .75rem; }
label { display: block; font-size: .85rem; font-weight: 600; margin: .5rem 0 .2rem; }
input { width: 100%; padding: .5rem; border: 1px solid #ccc; border-radius: .3rem; }
button.primary { margin-top: .75rem; background: #0d6efd; color: #fff; border: 0;
  padding: .5rem 1rem; border-radius: .3rem; cursor: pointer; }

/* Eneste "forklaringsboks": demo-knapperne. */
.demo-box {
  margin-top: 1rem; padding: .55rem .7rem; background: #f8f9fa;
  border: 1px solid #e3e3e3; border-radius: .4rem;
  display: flex; flex-wrap: wrap; gap: .4rem; align-items: center;
}
.demo-label { font-size: .8rem; font-weight: 600; color: #666; }
button.demo { border: 0; padding: .35rem .7rem; border-radius: .3rem; cursor: pointer; font-size: .85rem; color: #fff; }
button.demo.normal { background: #198754; }
button.demo.attack { background: #dc3545; }

.result { margin-top: 1rem; }
.sql-label { font-size: .8rem; font-weight: 600; color: #555; margin: .6rem 0 .2rem; }
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

.filter-rule { font-size: .82rem; color: #7a4b05; background: #fff3cd; border: 1px solid #ffe69c;
  border-radius: .3rem; padding: .4rem .55rem; margin-bottom: .5rem; }
table.filter-table { margin-bottom: .6rem; }
table.filter-table code { font-size: .82rem; }
table.filter-table code.changed { background: #f8d7da; padding: 0 .2rem; border-radius: .2rem; }
.chg-tag { font-size: .7rem; margin-left: .4rem; color: #842029; font-weight: 600; }
.chg-tag.unchanged { color: #555; font-weight: 400; }
</style>
