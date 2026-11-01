import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import TemplatePicker from './TemplatePicker.vue'

const keys = ['ngf', 'anchor', 'angle', 'band', 'callout', 'cards', 'duo', 'ledger', 'minimal', 'sidebar']
const templates = keys.map((k, i) => ({
  key: k, name: k === 'ngf' ? 'NGF - Invoice' : k[0].toUpperCase() + k.slice(1), description: `About ${k}`,
  isDefault: i === 0, defaultPrimaryColor: '#138A44', defaultAccentColor: '#F5C90C'
}))

describe('TemplatePicker', () => {
  it('renders every template as a radio in a labelled radiogroup', () => {
    const w = mount(TemplatePicker, { props: { templates, modelValue: 'ngf', labelledby: 'h' } })
    expect(w.find('[role="radiogroup"]').attributes('aria-labelledby')).toBe('h')
    expect(w.findAll('input[type="radio"]')).toHaveLength(10)
    expect(w.text()).toContain('NGF - Invoice')
    expect(w.text()).toContain('About sidebar')
    expect(w.text()).toContain('Default')
  })

  it('checks the selected template', () => {
    const w = mount(TemplatePicker, { props: { templates, modelValue: 'band' } })
    const checked = w.findAll('input[type="radio"]').filter(i => (i.element as HTMLInputElement).checked)
    expect(checked).toHaveLength(1)
    expect((checked[0].element as HTMLInputElement).value).toBe('band')
    expect(w.find('.tp-card--on').attributes('data-key')).toBe('band')
  })

  it('emits the key of the template that is chosen', async () => {
    const w = mount(TemplatePicker, { props: { templates, modelValue: 'ngf' } })
    await w.findAll('input[type="radio"]')[3].setValue()
    expect(w.emitted('update:modelValue')).toEqual([['band']])
  })

  it('shows the template default colours as a visual hint', () => {
    const w = mount(TemplatePicker, {
      props: { templates: [{ ...templates[1], defaultPrimaryColor: '#065F46', defaultAccentColor: '#A3E635' }], modelValue: 'anchor' }
    })
    expect(w.find('.tp-head').attributes('style')).toMatch(/rgb\(6, 95, 70\)|#065F46/i)
    expect(w.find('.tp-stripe').attributes('style')).toMatch(/rgb\(163, 230, 53\)|#A3E635/i)
  })
})
