import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  fetchJson,
  fetchWithNoContent,
  getErrorMessage,
  sendMutation,
  withContext,
  withJsonDefaults
} from './request-helpers'

describe('request helpers', () => {
  const originalFetch = globalThis.fetch

  afterEach(() => {
    globalThis.fetch = originalFetch
    vi.restoreAllMocks()
  })

  it('adds language and version context to a URL', () => {
    expect(withContext('/api/v1/content/root/branch', 'en-us', 2))
      .toBe('/api/v1/content/root/branch?lang=en-us&version=2')
  })

  it('merges json defaults into request init', () => {
    expect(withJsonDefaults({
      method: 'POST',
      headers: {
        Authorization: 'Bearer token'
      }
    })).toEqual({
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer token'
      }
    })
  })

  it('returns parsed json for successful responses', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: vi.fn().mockResolvedValue({ id: 1 })
    } as unknown as Response)

    await expect(fetchJson<{ id: number }>('/test')).resolves.toEqual({ id: 1 })
  })

  it('throws the problem detail message for failed json responses', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 400,
      json: vi.fn().mockResolvedValue({ detail: 'Bad request.' })
    } as unknown as Response)

    await expect(fetchJson('/test')).rejects.toThrow('Bad request.')
  })

  it('falls back to the status code for failed non-json responses', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 500,
      json: vi.fn().mockRejectedValue(new Error('No body'))
    } as unknown as Response)

    await expect(fetchWithNoContent('/test')).rejects.toThrow('Request failed with 500.')
  })

  it('sends mutations with json defaults applied', async () => {
    const fetchSpy = vi.fn().mockResolvedValue({
      ok: true,
      json: vi.fn().mockResolvedValue({ success: true })
    } as unknown as Response)
    globalThis.fetch = fetchSpy

    await expect(sendMutation<{ success: boolean }>('/mutation', {
      method: 'POST',
      body: '{}'
    })).resolves.toEqual({ success: true })

    expect(fetchSpy).toHaveBeenCalledWith('/mutation', {
      method: 'POST',
      body: '{}',
      headers: {
        'Content-Type': 'application/json'
      }
    })
  })

  it('returns friendly error messages', () => {
    expect(getErrorMessage(new Error('Boom'))).toBe('Boom')
    expect(getErrorMessage('oops')).toBe('Something went wrong while talking to the authoring API.')
  })
})
