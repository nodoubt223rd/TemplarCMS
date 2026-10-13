export type AuthState = 'checking' | 'anonymous' | 'change-password' | 'authenticated'
export interface AuthSession {
  userId: string
  loginName: string
  email: string
  language: string
  roles: string[]
  mustChangePassword: boolean
  _links: Record<string, { href: string; method?: string }>
}
export interface LoginCredentials { loginName: string; password: string }
export interface PasswordChange { currentPassword: string; newPassword: string }
