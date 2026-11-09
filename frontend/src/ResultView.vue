<script setup>
import { computed } from 'vue'

const props = defineProps({
  result: Object,            // { sql, parameters, columns, rows, error } eller null
  successWhenRows: Boolean,
  successText: { type: String, default: '' },
  failText: { type: String, default: '' },
})

const ok = computed(() => props.result?.rows?.length > 0)

// Tom streng skal vises synligt i før/efter-tabellen.
function show(v) { return v === '' ? '(tom)' : v }
</script>

<template>
  <div class="result" v-if="result">
    <template v-if="result.filter">
      <div class="sql-label">Inputfiltrering (udført i backend før SQL bygges):</div>
      <div class="filter-rule">{{ result.filter.rule }}</div>
      <table class="data filter-table">
        <thead>
          <tr><th>Felt</th><th>Oprindeligt input</th><th>Filtreret input (brugt i SQL)</th></tr>
        </thead>
        <tbody>
          <tr v-for="f in result.filter.fields" :key="f.name">
            <td>{{ f.name }}</td>
            <td><code>{{ show(f.original) }}</code></td>
            <td>
              <code :class="{ changed: f.changed }">{{ show(f.filtered) }}</code>
              <span v-if="f.changed" class="chg-tag">ændret</span>
              <span v-else class="chg-tag unchanged">uændret</span>
            </td>
          </tr>
        </tbody>
      </table>
    </template>

    <template v-if="result.sql">
      <div class="sql-label">SQL udført i backend:</div>
      <pre class="sql">{{ result.sql }}</pre>
    </template>
    <template v-if="result.parameters">
      <div class="sql-label">Parametre (data — aldrig fortolket som SQL):</div>
      <pre class="sql params">{{ result.parameters }}</pre>
    </template>

    <div v-if="result.error" class="alert warn">
      <strong>Fejl:</strong> {{ result.error }}
    </div>
    <div v-else-if="successWhenRows" class="alert" :class="ok ? 'danger' : 'success'">
      {{ ok ? successText : failText }}
      <span v-if="ok"> ({{ result.rows.length }} række(r) matchede)</span>
    </div>

    <table v-if="!result.error && result.rows && result.rows.length" class="data">
      <thead>
        <tr><th v-for="c in result.columns" :key="c">{{ c }}</th></tr>
      </thead>
      <tbody>
        <tr v-for="(row, i) in result.rows" :key="i">
          <td v-for="(cell, j) in row" :key="j">{{ cell }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
