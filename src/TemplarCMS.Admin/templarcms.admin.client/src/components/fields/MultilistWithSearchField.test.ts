import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import MultilistWithSearchField from './MultilistWithSearchField.vue'

describe('MultilistWithSearchField', () => {
  it('emits selected values in the order established by the author', async () => {
    const wrapper = mount(MultilistWithSearchField, {
      props: {
        available: [
          { value: 'content', label: 'Content', description: 'content' },
          { value: 'metadata', label: 'Metadata', description: 'metadata' }
        ],
        modelValue: []
      }
    })

    const selects = wrapper.findAll('select')
    await selects[0]!.setValue(['content', 'metadata'])
    await wrapper.get('button').trigger('click')

    await wrapper.setProps({ modelValue: ['content', 'metadata'] })
    await selects[1]!.setValue('metadata')
    await wrapper.findAll('button')[2]!.trigger('click')

    expect(wrapper.emitted('update:modelValue')).toEqual([
      [['content', 'metadata']],
      [['metadata', 'content']]
    ])
  })
})
