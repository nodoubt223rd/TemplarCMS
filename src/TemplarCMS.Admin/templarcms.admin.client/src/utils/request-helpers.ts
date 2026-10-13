import { getAuthRequestContext, getAuthRequestGeneration } from './auth-request-context'

export class ApiRequestError extends Error {
  constructor(message: string, public readonly status: number, public readonly code: string | null,
    public readonly fieldErrors: Record<string, string[]> = {}) { super(message); this.name = 'ApiRequestError' }
}

function ensureCurrent(generation: number): void {
  if (generation !== getAuthRequestGeneration()) throw new DOMException('The session changed during this request.', 'AbortError')
}
function record(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}
async function request(url: string, init?: RequestInit): Promise<{ response: Response; generation: number }> {
  const context = getAuthRequestContext()
  const generation = getAuthRequestGeneration()
  const authRoute = url.startsWith('/api/v1/auth/')
  if (context?.canRequest && !authRoute && !context.canRequest())
    throw new ApiRequestError('Sign in to continue.', 401, 'authoring-authentication-required')
  let options = init
  if (context) {
    const headers = new Headers(init?.headers)
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes((init?.method ?? 'GET').toUpperCase())) {
      const token = await context.getRequestToken()
      if (token) headers.set('X-Templar-CSRF', token)
    }
    ensureCurrent(generation)
    options = { ...init, headers, credentials: 'same-origin' }
  }
  const response = await fetch(url, options)
  ensureCurrent(generation)
  if (!response.ok) {
    const value: unknown = await response.json().catch(() => null)
    ensureCurrent(generation)
    const problem = record(value) ? value : {}
    const errors: Record<string, string[]> = {}
    if (record(problem.errors)) for (const [key, messages] of Object.entries(problem.errors)) {
      if (Array.isArray(messages) && messages.every((message: unknown) => typeof message === 'string')) errors[key] = messages as string[]
    }
    const code = typeof problem.code === 'string' ? problem.code : null
    if (response.status === 401 && !authRoute) context?.onUnauthorized()
    if (response.status === 403 && code === 'password-change-required') context?.onPasswordChangeRequired()
    const detail = typeof problem.detail === 'string' ? problem.detail : typeof problem.title === 'string' ? problem.title : `Request failed with ${response.status}.`
    throw new ApiRequestError(detail, response.status, code, errors)
  }
  return { response, generation }
}

export async function fetchJson<T>(url: string, init?: RequestInit): Promise<T> {
  const { response, generation } = await request(url, init)
  const result: unknown = await response.json()
  ensureCurrent(generation)
  return result as T
}

export async function fetchWithNoContent(url: string, init?: RequestInit): Promise<void> {
  await request(url, init)
}

export async function sendMutation<T>(url: string, init: RequestInit): Promise<T> {
  return await fetchJson<T>(url, withJsonDefaults(init))
}

export function withContext(url: string, language: string, version: number): string {
  const separator = url.includes('?') ? '&' : '?'
  return `${url}${separator}lang=${encodeURIComponent(language)}&version=${encodeURIComponent(version)}`
}

export function withJsonDefaults(init: RequestInit): RequestInit {
  if (init.body instanceof FormData) return init
  if (init.headers instanceof Headers || Array.isArray(init.headers)) {
    const headers = new Headers(init.headers)
    if (!headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
    return { ...init, headers }
  }
  return {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(init.headers ?? {})
    }
  }
}

export function getErrorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message
  }

  return 'Something went wrong while talking to the authoring API.'
}
