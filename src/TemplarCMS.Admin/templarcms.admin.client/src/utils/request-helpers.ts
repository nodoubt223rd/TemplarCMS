export async function fetchJson<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, init)

  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null
    throw new Error(problem?.detail ?? problem?.title ?? `Request failed with ${response.status}.`)
  }

  return await response.json() as T
}

export async function fetchWithNoContent(url: string, init?: RequestInit): Promise<void> {
  const response = await fetch(url, init)

  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null
    throw new Error(problem?.detail ?? problem?.title ?? `Request failed with ${response.status}.`)
  }
}

export async function sendMutation<T>(url: string, init: RequestInit): Promise<T> {
  return await fetchJson<T>(url, withJsonDefaults(init))
}

export function withContext(url: string, language: string, version: number): string {
  const separator = url.includes('?') ? '&' : '?'
  return `${url}${separator}lang=${encodeURIComponent(language)}&version=${encodeURIComponent(version)}`
}

export function withJsonDefaults(init: RequestInit): RequestInit {
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
