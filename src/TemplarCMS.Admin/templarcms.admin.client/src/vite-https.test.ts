// @vitest-environment node
import childProcess from 'node:child_process'
import fs from 'node:fs'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { ConfigEnv, UserConfig } from 'vite'
import configuration from '../vite.config'

const configure = configuration as (environment: ConfigEnv) => UserConfig

beforeEach(() => {
  vi.spyOn(fs, 'existsSync').mockReturnValue(true)
  vi.spyOn(fs, 'mkdirSync').mockReturnValue(undefined)
  vi.spyOn(fs, 'readFileSync').mockReturnValue(Buffer.from('test certificate'))
  vi.spyOn(childProcess, 'spawnSync').mockReturnValue({ status: 0 } as ReturnType<typeof childProcess.spawnSync>)
})

afterEach(() => vi.restoreAllMocks())

describe('development HTTPS certificates', () => {
  it('refreshes existing certificate files from the currently trusted .NET certificate', () => {
    configure({ command: 'serve', mode: 'development' })
    expect(childProcess.spawnSync).toHaveBeenCalledWith('dotnet', ['dev-certs', 'https', '--check', '--trust'], expect.any(Object))
    expect(childProcess.spawnSync).toHaveBeenCalledWith('dotnet', expect.arrayContaining(['--export-path', '--format', 'Pem', '--no-password']), expect.any(Object))
  })

  it('stops with a repair command when the development certificate is untrusted', () => {
    vi.mocked(childProcess.spawnSync).mockReturnValue({ status: 1 } as ReturnType<typeof childProcess.spawnSync>)
    expect(() => configure({ command: 'serve', mode: 'development' })).toThrow(/dotnet dev-certs https --trust/)
    expect(fs.readFileSync).not.toHaveBeenCalled()
  })

  it('does not silently reuse old files when exporting the current certificate fails', () => {
    vi.mocked(childProcess.spawnSync)
      .mockReturnValueOnce({ status: 0 } as ReturnType<typeof childProcess.spawnSync>)
      .mockReturnValueOnce({ status: 1 } as ReturnType<typeof childProcess.spawnSync>)
    expect(() => configure({ command: 'serve', mode: 'development' })).toThrow(/export/i)
  })

  it('keeps production builds independent of local certificates', () => {
    configure({ command: 'build', mode: 'production' })
    expect(childProcess.spawnSync).not.toHaveBeenCalled()
    expect(fs.readFileSync).not.toHaveBeenCalled()
  })
})
