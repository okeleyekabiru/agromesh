import { ref } from 'vue'

// Shared open state for the mobile navigation drawer (the sidebar below 900px).
const open = ref(false)

export function useNavDrawer() {
  return {
    open,
    toggle: () => { open.value = !open.value },
    close: () => { open.value = false },
  }
}
