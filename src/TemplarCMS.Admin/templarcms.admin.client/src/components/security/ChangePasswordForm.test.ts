import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import ChangePasswordForm from './ChangePasswordForm.vue'

describe('first password change', () => {
  it('rejects mismatched confirmation before sending credentials', async () => {
    const wrapper = mount(ChangePasswordForm)
    await wrapper.get('[name="currentPassword"]').setValue('temporary-password')
    await wrapper.get('[name="newPassword"]').setValue('new private passphrase')
    await wrapper.get('[name="confirmation"]').setValue('different private passphrase')
    await wrapper.get('form').trigger('submit')
    expect(wrapper.get('[role="alert"]').text()).toContain('match')
    expect(wrapper.emitted('submit')).toBeUndefined()
    wrapper.unmount()
  })
  it('emits a valid change and clears every password field', async () => {
    const wrapper = mount(ChangePasswordForm)
    await wrapper.get('[name="currentPassword"]').setValue('temporary-password')
    await wrapper.get('[name="newPassword"]').setValue('new private passphrase')
    await wrapper.get('[name="confirmation"]').setValue('new private passphrase')
    await wrapper.get('form').trigger('submit')
    expect(wrapper.emitted('submit')?.[0]?.[0]).toEqual({ currentPassword: 'temporary-password', newPassword: 'new private passphrase' })
    expect(wrapper.findAll('input[type="password"]').every(input => (input.element as HTMLInputElement).value === '')).toBe(true)
    wrapper.unmount()
  })
})
