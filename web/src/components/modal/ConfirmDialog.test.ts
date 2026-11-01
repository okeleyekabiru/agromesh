import { describe, expect, it } from 'vitest'
import { defineComponent, nextTick, ref } from 'vue'
import { mount } from '@vue/test-utils'
import ConfirmDialog from './ConfirmDialog.vue'

const Host = defineComponent({
  components: { ConfirmDialog },
  setup() {
    const open = ref(false)
    return { open }
  },
  template: `<div>
    <button id="opener" @click="open = true">Delete</button>
    <ConfirmDialog v-if="open" title="Delete?" message="Sure?" @cancel="open = false" @confirm="open = false" />
  </div>`
})

describe('ConfirmDialog focus management', () => {
  it('focuses Cancel on open and returns focus to the opener on Escape', async () => {
    const w = mount(Host, { attachTo: document.body })
    const opener = w.get('#opener').element as HTMLButtonElement
    opener.focus()
    await w.get('#opener').trigger('click')
    await nextTick()
    expect(document.activeElement?.textContent).toBe('Cancel')
    await w.get('[role="alertdialog"]').trigger('keydown', { key: 'Escape' })
    await nextTick()
    expect(w.find('[role="alertdialog"]').exists()).toBe(false)
    expect(document.activeElement).toBe(opener)
    w.unmount()
  })

  it('keeps Tab inside the dialog', async () => {
    const w = mount(Host, { attachTo: document.body })
    w.get('#opener').element.dispatchEvent(new Event('focus'))
    await w.get('#opener').trigger('click')
    await nextTick()
    const buttons = w.findAll('[role="alertdialog"] button')
    const last = buttons[buttons.length - 1].element as HTMLButtonElement
    last.focus()
    await w.get('[role="alertdialog"]').trigger('keydown', { key: 'Tab' })
    expect(document.activeElement).toBe(buttons[0].element)
    w.unmount()
  })
})
