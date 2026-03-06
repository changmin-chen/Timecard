<script setup>
import { ref } from 'vue'
import { timecardApi } from '../timecardApi.js'
import { useToast } from '../composables/useToast.js'

const toast = useToast()

const fileInput = ref(null)
const dragging = ref(false)
const selectedFile = ref(null)
const importing = ref(false)
const result = ref(null)

function onFilePick(e) {
  const f = e.target.files?.[0]
  if (f) selectFile(f)
}

function onDrop(e) {
  dragging.value = false
  const f = e.dataTransfer?.files?.[0]
  if (f) selectFile(f)
}

function selectFile(f) {
  if (!f.name.toLowerCase().endsWith('.csv')) {
    toast.error('請上傳 .csv 檔案')
    return
  }
  selectedFile.value = f
  result.value = null
}

function clearFile() {
  selectedFile.value = null
  result.value = null
  if (fileInput.value) fileInput.value.value = ''
}

async function doImport() {
  if (!selectedFile.value || importing.value) return
  importing.value = true
  result.value = null
  try {
    result.value = await timecardApi.importCalendarCsv(selectedFile.value)
    toast.success(`匯入完成：共 ${result.value.totalRows} 筆`)
    clearFile()
  } catch (err) {
    toast.errorFrom(err)
  } finally {
    importing.value = false
  }
}

function fmtDate(iso) {
  if (!iso) return '-'
  return new Date(iso).toLocaleString('zh-TW', { dateStyle: 'short', timeStyle: 'short' })
}
</script>

<template>
  <section class="card cal-card">
    <div class="row">
      <div>
        <h2>行事曆管理</h2>
        <div class="hint">匯入人事行政總處 (DGPA) 行事曆 CSV，作為全系統唯一曆法來源。重複匯入會覆蓋既有資料。</div>
      </div>
    </div>

    <div class="import-layout">
      <!-- Drop zone -->
      <div
        class="drop-zone"
        :class="{ 'drop-zone--active': dragging, 'drop-zone--selected': !!selectedFile }"
        @dragover.prevent="dragging = true"
        @dragleave="dragging = false"
        @drop.prevent="onDrop"
        @click="fileInput?.click()"
      >
        <input ref="fileInput" type="file" accept=".csv" class="file-input-hidden" @change="onFilePick" />

        <template v-if="!selectedFile">
          <div class="drop-icon">📂</div>
          <div class="drop-label">拖曳 CSV 至此，或點擊選擇檔案</div>
          <div class="drop-hint">支援人事行政總處官網下載之 CSV 格式</div>
        </template>
        <template v-else>
          <div class="drop-icon">📄</div>
          <div class="drop-label selected-name">{{ selectedFile.name }}</div>
          <div class="drop-hint">{{ (selectedFile.size / 1024).toFixed(1) }} KB</div>
        </template>
      </div>

      <!-- Actions & Result -->
      <div class="import-panel">
        <h3>匯入操作</h3>

        <div v-if="!selectedFile" class="hint">尚未選擇檔案。</div>

        <template v-else>
          <div class="file-info">
            <div class="file-info-row">
              <span class="file-info-label">檔案</span>
              <span class="file-info-val mono">{{ selectedFile.name }}</span>
            </div>
            <div class="file-info-row">
              <span class="file-info-label">大小</span>
              <span class="file-info-val">{{ (selectedFile.size / 1024).toFixed(1) }} KB</span>
            </div>
          </div>

          <div class="btn-row">
            <button class="primary" :disabled="importing" @click="doImport">
              {{ importing ? '匯入中…' : '開始匯入' }}
            </button>
            <button class="ghost" :disabled="importing" @click="clearFile">取消</button>
          </div>
        </template>

        <!-- Result -->
        <div v-if="result" class="result-block">
          <div class="result-title">✓ 匯入結果</div>
          <div class="result-grid">
            <div class="result-stat">
              <div class="result-num">{{ result.totalRows }}</div>
              <div class="result-label">總筆數</div>
            </div>
            <div class="result-stat inserted">
              <div class="result-num">{{ result.insertedRows }}</div>
              <div class="result-label">新增</div>
            </div>
            <div class="result-stat updated">
              <div class="result-num">{{ result.updatedRows }}</div>
              <div class="result-label">更新</div>
            </div>
          </div>
          <div class="result-meta">
            匯入時間：{{ fmtDate(result.importedAtUtc) }}
            <span class="mono result-cal">{{ result.calendarId }}</span>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.cal-card {
  margin-top: 14px;
}

.import-layout {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
  margin-top: 16px;
}

/* Drop zone */
.drop-zone {
  border: 2px dashed var(--line);
  border-radius: 14px;
  padding: 32px 20px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  cursor: pointer;
  background: #fafbfc;
  transition: border-color 0.15s ease, background 0.15s ease;
  min-height: 160px;
  text-align: center;
  user-select: none;
}
.drop-zone:hover,
.drop-zone--active {
  border-color: var(--primary);
  background: rgba(37, 99, 235, 0.03);
}
.drop-zone--selected {
  border-color: rgba(16, 185, 129, 0.5);
  background: rgba(16, 185, 129, 0.03);
}

.file-input-hidden {
  display: none;
}

.drop-icon {
  font-size: 28px;
  line-height: 1;
}
.drop-label {
  font-size: 13.5px;
  font-weight: 600;
  color: var(--text);
}
.drop-hint {
  font-size: 12px;
  color: var(--muted);
}
.selected-name {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 13px;
  color: #059669;
  word-break: break-all;
}

/* Import panel */
.import-panel {
  border: 1px solid var(--line);
  border-radius: 12px;
  padding: 14px 16px;
  background: #fbfcfe;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.file-info {
  display: grid;
  gap: 6px;
}
.file-info-row {
  display: flex;
  gap: 8px;
  font-size: 13px;
  align-items: baseline;
}
.file-info-label {
  color: var(--muted);
  width: 36px;
  flex-shrink: 0;
}
.file-info-val {
  color: var(--text);
  font-weight: 500;
  word-break: break-all;
}

.btn-row {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

/* Result block */
.result-block {
  border: 1px solid rgba(16, 185, 129, 0.3);
  border-radius: 10px;
  padding: 12px 14px;
  background: rgba(16, 185, 129, 0.04);
}
.result-title {
  font-size: 13px;
  font-weight: 700;
  color: #059669;
  margin-bottom: 10px;
}
.result-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 8px;
  margin-bottom: 10px;
}
.result-stat {
  text-align: center;
  padding: 8px 4px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.7);
  border: 1px solid rgba(16, 185, 129, 0.15);
}
.result-stat.inserted .result-num { color: #059669; }
.result-stat.updated .result-num { color: var(--primary); }
.result-num {
  font-size: 22px;
  font-weight: 700;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  color: var(--text);
  line-height: 1.2;
}
.result-label {
  font-size: 11px;
  color: var(--muted);
  margin-top: 2px;
}
.result-meta {
  font-size: 12px;
  color: var(--muted);
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  align-items: center;
}
.result-cal {
  padding: 1px 6px;
  border-radius: 4px;
  background: rgba(0, 0, 0, 0.04);
  font-size: 11px;
}

@media (max-width: 960px) {
  .import-layout {
    grid-template-columns: 1fr;
  }
}
</style>
