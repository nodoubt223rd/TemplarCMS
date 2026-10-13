import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useAuthSession } from './useAuthSession'
import { fetchJson, sendMutation } from '../utils/request-helpers'
import { clearAuthRequestContext, invalidateAuthRequests } from '../utils/auth-request-context'

const session = { userId: 'admin-1', loginName: 'TemplarAdmin', email: 'admin@example.test', language: 'en', roles: ['TemplarAdmin'], mustChangePassword: true, _links: {} }
let loggedIn = false
let changed = false
const fetchMock = vi.fn()

beforeEach(() => {
  loggedIn = false; changed = false
  vi.stubGlobal('fetch', fetchMock)
  fetchMock.mockImplementation(async (url: string) => {
    if (url.endsWith('/antiforgery')) return new Response(JSON.stringify({ requestToken: 'csrf-token', headerName: 'X-Templar-CSRF' }))
    if (url.endsWith('/login')) { loggedIn = true; return new Response(JSON.stringify(session)) }
    if (url.endsWith('/change-password')) { changed = true; return new Response(JSON.stringify({ ...session, mustChangePassword: false })) }
    if (url.endsWith('/logout')) { loggedIn = false; return new Response(null, { status: 204 }) }
    if (url.endsWith('/session')) return loggedIn ? new Response(JSON.stringify({ ...session, mustChangePassword: !changed })) : new Response('{}', { status: 401 })
    return new Response(JSON.stringify({ data: 'private' }))
  })
})
afterEach(() => { clearAuthRequestContext(); vi.unstubAllGlobals(); vi.restoreAllMocks(); fetchMock.mockReset() })

describe('admin session', () => {
  it('requires the first password change and keeps secrets out of browser storage', async () => {
    const storage = vi.spyOn(Storage.prototype, 'setItem')
    const auth = useAuthSession()
    await auth.initialize()
    expect(auth.state.value).toBe('anonymous')
    await auth.login('TemplarAdmin', 'temporary-password')
    expect(auth.state.value).toBe('change-password')
    await expect(fetchJson('/api/v1/templates')).rejects.toMatchObject({ status: 401 })
    await auth.changePassword('temporary-password', 'new private passphrase')
    expect(auth.state.value).toBe('authenticated')
    expect(storage).not.toHaveBeenCalled()
    expect(auth.session.value).not.toHaveProperty('password')
  })

  it('adds antiforgery for JSON and multipart requests without forcing a multipart content type', async () => {
    const auth = useAuthSession()
    await auth.initialize(); await auth.login('TemplarAdmin', 'temporary-password'); await auth.changePassword('temporary-password', 'new private passphrase')
    await sendMutation('/api/v1/templates', { method: 'POST', body: '{}' })
    const jsonOptions = fetchMock.mock.calls[fetchMock.mock.calls.length - 1]![1] as RequestInit
    expect(new Headers(jsonOptions.headers).get('X-Templar-CSRF')).toBe('csrf-token')
    await sendMutation('/api/v1/media/assets', { method: 'POST', body: new FormData() })
    const formOptions = fetchMock.mock.calls[fetchMock.mock.calls.length - 1]![1] as RequestInit
    expect(new Headers(formOptions.headers).get('X-Templar-CSRF')).toBe('csrf-token')
    expect(new Headers(formOptions.headers).has('Content-Type')).toBe(false)
  })

  it('returns to login on expiry and never replays a failed mutation', async () => {
    const auth = useAuthSession()
    await auth.initialize(); await auth.login('TemplarAdmin', 'temporary-password'); await auth.changePassword('temporary-password', 'new private passphrase')
    fetchMock.mockResolvedValueOnce(new Response(JSON.stringify({ detail: 'Sign in again.', code: 'authoring-authentication-required' }), { status: 401 }))
    const before = fetchMock.mock.calls.length
    await expect(sendMutation('/api/v1/content', { method: 'POST', body: '{}' })).rejects.toMatchObject({ status: 401 })
    expect(auth.state.value).toBe('anonymous')
    expect(fetchMock.mock.calls.length - before).toBe(1)
  })

  it('rejects late successful results after the session generation changes', async () => {
    let complete: (response: Response) => void = () => undefined
    fetchMock.mockImplementationOnce(() => new Promise<Response>(resolve => { complete = resolve }))
    const pending = fetchJson('/api/v1/templates')
    invalidateAuthRequests()
    complete(new Response(JSON.stringify({ data: 'private' })))
    await expect(pending).rejects.toMatchObject({ name: 'AbortError' })
  })

  it('clears session state only after confirmed logout', async () => {
    const auth = useAuthSession()
    await auth.initialize(); await auth.login('TemplarAdmin', 'temporary-password'); await auth.changePassword('temporary-password', 'new private passphrase')
    await auth.logout()
    expect(auth.state.value).toBe('anonymous')
    expect(auth.session.value).toBeNull()
  })
})
