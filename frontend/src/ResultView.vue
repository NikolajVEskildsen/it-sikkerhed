<script setup>
import { computed } from 'vue'

const props = defineProps({
  result: Object,
  successWhenRows: Boolean,
  successText: { type: String, default: '' },
  failText: { type: String, default: '' },
})

const ok = computed(() => props.result?.rows?.length > 0)
</script>

<template>
  <div class="result" v-if="result">
    <div class="sql-label">SQL sendt til databasen:</div>
    <pre class="sql">{{ result.sql }}</pre>

    <template v-if="result.boundParams">
      <div class="sql-label">Bundne parametre (data — aldrig fortolket som SQL):</div>
      <pre class="sql params">{{ result.boundParams }}</pre>
    </template>

    <div v-if="result.error" class="alert warn">
      <strong>SQL-fejl:</strong> {{ result.error }}
    </div>
    <div v-else-if="successWhenRows" class="alert" :class="ok ? 'danger' : 'success'">
      {{ ok ? successText : failText }}
      <span v-if="ok"> ({{ result.rows.length }} række(r) matchede)</span>
    </div>

    <table v-if="!result.error && result.rows.length" class="data">
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
