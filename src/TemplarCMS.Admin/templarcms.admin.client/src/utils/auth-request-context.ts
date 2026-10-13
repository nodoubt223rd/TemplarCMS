export interface AuthRequestContext {
  getRequestToken: () => string | null | Promise<string | null>
  onUnauthorized: () => void
  onPasswordChangeRequired: () => void
  canRequest?: () => boolean
}

let context: AuthRequestContext | null = null
let generation = 0

export function configureAuthRequestContext(value: AuthRequestContext): void {
  context = value
  generation++
}
export function clearAuthRequestContext(): void { context = null; generation++ }
export function invalidateAuthRequests(): void { generation++ }
export function getAuthRequestContext(): AuthRequestContext | null { return context }
export function getAuthRequestGeneration(): number { return generation }

export async function withinSession<T>(operation: Promise<T>, expectedGeneration: number): Promise<T> {
  const result = await operation
  if (expectedGeneration !== generation) throw new DOMException('The session changed.', 'AbortError')
  return result
}
