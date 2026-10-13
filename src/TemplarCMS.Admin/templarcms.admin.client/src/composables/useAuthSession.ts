import { ref } from 'vue'
import { ApiRequestError, fetchJson, fetchWithNoContent, getErrorMessage, sendMutation } from '../utils/request-helpers'
import { configureAuthRequestContext, clearAuthRequestContext, invalidateAuthRequests } from '../utils/auth-request-context'
import type { AuthSession, AuthState } from '../types/auth'

export function useAuthSession() {
  const state = ref<AuthState>('checking')
  const session = ref<AuthSession | null>(null)
  const error = ref<string | null>(null)
  const busy = ref(false)
  let requestToken: string | null = null

  configureAuthRequestContext({
    getRequestToken: async () => requestToken ?? await refreshToken(),
    onUnauthorized: expire,
    onPasswordChangeRequired: () => { state.value = 'change-password'; invalidateAuthRequests() },
    canRequest: () => state.value === 'authenticated'
  })

  function expire(): void {
    session.value = null
    state.value = 'anonymous'
    requestToken = null
    invalidateAuthRequests()
  }
  function apply(value: AuthSession): void {
    session.value = value
    requestToken = null
    state.value = value.mustChangePassword ? 'change-password' : value.roles.includes('TemplarAdmin') ? 'authenticated' : 'anonymous'
    if (state.value === 'anonymous') error.value = 'This account does not have access to the workspace.'
    invalidateAuthRequests()
  }
  function message(cause: unknown): string {
    if (cause instanceof ApiRequestError) {
      const details = Object.values(cause.fieldErrors).flat().join(' ')
      if (details) return details
    }
    return getErrorMessage(cause)
  }
  async function refreshToken(): Promise<string> {
    const token = await fetchJson<{ requestToken: string; headerName: string }>('/api/v1/auth/antiforgery')
    requestToken = token.requestToken
    return requestToken
  }
  async function initialize(): Promise<void> {
    state.value = 'checking'
    error.value = null
    try {
      await refreshToken()
      apply(await fetchJson<AuthSession>('/api/v1/auth/session'))
      await refreshToken()
    } catch (cause) {
      expire()
      if (!(cause instanceof ApiRequestError && cause.status === 401)) error.value = message(cause)
    }
  }
  async function login(loginName: string, password: string): Promise<void> {
    if (busy.value) return
    busy.value = true
    error.value = null
    try {
      apply(await sendMutation<AuthSession>('/api/v1/auth/login', { method: 'POST', body: JSON.stringify({ loginName, password }) }))
      await refreshToken()
    } catch (cause) { error.value = message(cause) }
    finally { busy.value = false }
  }
  async function changePassword(currentPassword: string, newPassword: string): Promise<void> {
    if (busy.value) return
    busy.value = true
    error.value = null
    try {
      apply(await sendMutation<AuthSession>('/api/v1/auth/change-password', { method: 'POST', body: JSON.stringify({ currentPassword, newPassword }) }))
      await refreshToken()
    } catch (cause) { error.value = message(cause) }
    finally { busy.value = false }
  }
  async function logout(): Promise<boolean> {
    if (busy.value) return false
    busy.value = true
    error.value = null
    try { await fetchWithNoContent('/api/v1/auth/logout', { method: 'POST' }) }
    catch (cause) {
      if (!(cause instanceof ApiRequestError && cause.status === 401)) { error.value = message(cause); return false }
    } finally { busy.value = false }
    expire()
    return true
  }
  function dispose(): void { requestToken = null; clearAuthRequestContext() }
  return { state, session, error, busy, initialize, login, changePassword, logout, dispose }
}
