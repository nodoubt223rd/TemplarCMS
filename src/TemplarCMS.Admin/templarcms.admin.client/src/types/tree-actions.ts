export type TreeActionTarget =
  | { kind: 'content' | 'template' | 'template-folder'; id: string }
  | { kind: 'templates-root'; id: null }
export type TreeAction = 'new-item' | 'new-template' | 'new-folder' | 'move-up' | 'move-down' | 'move-first' | 'move-last' | 'move' | 'rename' | 'delete'
export type TreeActionRequest = { target: TreeActionTarget; action: TreeAction }
export type TreeActionDescriptor = { action: TreeAction; label: string; disabled?: boolean }
export type TreeMenuRequest = { target: TreeActionTarget; label: string; x: number; y: number }
