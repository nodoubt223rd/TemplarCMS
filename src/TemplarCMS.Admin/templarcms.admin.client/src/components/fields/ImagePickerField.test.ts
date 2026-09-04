import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import ImagePickerField from './ImagePickerField.vue'

const { fetchJson } = vi.hoisted(() => ({ fetchJson: vi.fn() }))

vi.mock('@/utils/request-helpers', () => ({ fetchJson }))

describe('ImagePickerField', () => {
  it('hydrates the saved asset preview without opening the picker', async () => {
    fetchJson.mockResolvedValue({
      assets: [{
        id: 'image-1',
        folderId: 'folder-1',
        fileName: 'hero.jpg',
        contentType: 'image/jpeg',
        length: 128,
        altText: 'Mountain sunrise',
        title: 'Hero image',
        createdUtc: '2026-09-03T00:00:00Z',
        contentUrl: '/api/v1/media/assets/image-1/content'
      }]
    })

    const wrapper = mount(ImagePickerField, {
      props: { modelValue: 'image-1' }
    })
    await flushPromises()

    expect(fetchJson).toHaveBeenCalledWith('/api/v1/media/assets')
    expect(wrapper.find('img').attributes('src')).toBe('/api/v1/media/assets/image-1/content')
    expect(wrapper.text()).toContain('Hero image')
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
  })
})
