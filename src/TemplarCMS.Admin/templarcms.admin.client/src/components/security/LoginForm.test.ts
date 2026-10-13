import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import LoginForm from './LoginForm.vue'

describe('login form', () => {
  it('emits credentials once and immediately clears the password field', async () => {
    const wrapper = mount(LoginForm)
    await wrapper.get('[name="password"]').setValue('temporary-password')
    await wrapper.get('form').trigger('submit')
    expect(wrapper.emitted('submit')?.[0]?.[0]).toEqual({ loginName: 'TemplarAdmin', password: 'temporary-password' })
    expect((wrapper.get('[name="password"]').element as HTMLInputElement).value).toBe('')
    wrapper.unmount()
  })
  it('announces errors and prevents submission while busy', async () => {
    const wrapper = mount(LoginForm, { props: { busy: true, error: 'The credentials could not be accepted.' } })
    expect(wrapper.get('[role="alert"]').text()).toContain('credentials')
    await wrapper.get('form').trigger('submit')
    expect(wrapper.emitted('submit')).toBeUndefined()
    wrapper.unmount()
  })
})
