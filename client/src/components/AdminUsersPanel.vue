<script setup>
import { onMounted, reactive, ref } from 'vue'
import { timecardApi } from '../timecardApi.js'
import { useToast } from '../composables/useToast.js'

const toast = useToast()

const users = ref([])
const loadingUsers = ref(false)
const creating = ref(false)
const resettingUserId = ref('')
const lastResetInfo = ref('')

const createForm = reactive({
  email: '',
  employeeId: '',
  displayName: '',
  temporaryPassword: 'ChangeMe123!',
})

function clearCreateForm() {
  createForm.email = ''
  createForm.employeeId = ''
  createForm.displayName = ''
}

async function loadUsers() {
  loadingUsers.value = true
  try {
    users.value = await timecardApi.listUsers()
  } catch (err) {
    toast.errorFrom(err)
  } finally {
    loadingUsers.value = false
  }
}

async function createUser() {
  if (creating.value) return

  creating.value = true
  try {
    const created = await timecardApi.createUser(
      createForm.email,
      createForm.employeeId,
      createForm.displayName,
      createForm.temporaryPassword
    )

    toast.success(`User created: ${created.email}`)
    clearCreateForm()
    await loadUsers()
  } catch (err) {
    toast.errorFrom(err)
  } finally {
    creating.value = false
  }
}

async function resetPassword(user) {
  if (resettingUserId.value) return

  const confirmed = window.confirm(`Reset password for ${user.email}?`)
  if (!confirmed) return

  resettingUserId.value = user.id
  try {
    const result = await timecardApi.resetUserPassword(user.id)
    lastResetInfo.value = `${result.email} / ${result.temporaryPassword}`
    toast.success(`Password reset for ${result.email}. Default: ${result.temporaryPassword}`, 5500)
    await loadUsers()
  } catch (err) {
    toast.errorFrom(err)
  } finally {
    resettingUserId.value = ''
  }
}

onMounted(async () => {
  await loadUsers()
})
</script>

<template>
  <section class="card admin-users-card">
    <div class="row">
      <div>
        <h2>Admin User Management</h2>
        <div class="hint">Create users and reset password with a default admin password.</div>
      </div>
      <button class="ghost" @click="loadUsers" :disabled="loadingUsers || creating || !!resettingUserId">
        {{ loadingUsers ? 'Loading...' : 'Refresh' }}
      </button>
    </div>

    <div class="admin-layout">
      <form class="form admin-panel" @submit.prevent="createUser">
        <h3>Create User</h3>
        <label>
          Email
          <input v-model.trim="createForm.email" type="email" required />
        </label>
        <label>
          Employee ID
          <input v-model.trim="createForm.employeeId" type="text" required />
        </label>
        <label>
          Display Name
          <input v-model.trim="createForm.displayName" type="text" />
        </label>
        <label>
          Temporary Password
          <input v-model="createForm.temporaryPassword" type="text" minlength="8" required />
        </label>
        <button class="primary" type="submit" :disabled="creating">
          {{ creating ? 'Creating...' : 'Create User' }}
        </button>
      </form>

      <div class="admin-panel">
        <h3>Password Reset</h3>
        <p class="hint">
          Reset action sets MustChangePassword to true and applies Admin:DefaultResetPassword.
          If not configured, fallback is ChangeMe123!.
        </p>
        <p v-if="lastResetInfo" class="reset-info mono">
          Last reset: {{ lastResetInfo }}
        </p>
      </div>
    </div>

    <div v-if="users.length" class="tableWrap">
      <table :style="{ minWidth: '760px' }">
        <thead>
          <tr>
            <th>Email</th>
            <th>Display Name</th>
            <th>Employee ID</th>
            <th>Role</th>
            <th>Password State</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="u in users" :key="u.id">
            <td class="admin-email">{{ u.email }}</td>
            <td>{{ u.displayName || '-' }}</td>
            <td class="mono">{{ u.employeeId }}</td>
            <td>
              <span class="badge">{{ u.isAdmin ? 'Admin' : 'User' }}</span>
            </td>
            <td>
              <span class="badge" :class="u.mustChangePassword ? 'reset-required' : ''">
                {{ u.mustChangePassword ? 'Must change' : 'OK' }}
              </span>
            </td>
            <td class="admin-actions">
              <button
                class="danger"
                type="button"
                :disabled="!!resettingUserId"
                @click="resetPassword(u)"
              >
                {{ resettingUserId === u.id ? 'Resetting...' : 'Reset Password' }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-else-if="!loadingUsers" class="hint">No users found.</div>
  </section>
</template>

<style scoped>
.admin-users-card {
  margin-top: 14px;
}

.admin-layout {
  display: grid;
  grid-template-columns: 1.4fr 1fr;
  gap: 12px;
  margin-top: 12px;
}

.admin-panel {
  border: 1px solid var(--line);
  border-radius: 12px;
  padding: 12px;
  background: #fbfcfe;
}

.admin-email {
  font-weight: 600;
}

.admin-actions {
  text-align: right;
}

.reset-info {
  margin-top: 10px;
  padding: 8px 10px;
  border: 1px dashed var(--line);
  border-radius: 10px;
  background: #ffffff;
  overflow-wrap: anywhere;
}

.reset-required {
  color: #92400e;
  border-color: rgba(146, 64, 14, 0.35);
  background: rgba(146, 64, 14, 0.08);
}

@media (max-width: 960px) {
  .admin-layout {
    grid-template-columns: 1fr;
  }

  .admin-actions {
    text-align: left;
  }
}
</style>
