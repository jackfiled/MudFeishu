import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export const useAppStore = defineStore('app', () => {
  const theme = ref<'light' | 'dark'>('light')
  const language = ref<'zh-CN' | 'en-US'>('zh-CN')
  const sidebarCollapsed = ref(false)
  const showUploadPanel = ref(false)

  const isDark = computed(() => theme.value === 'dark')

  function toggleTheme() {
    theme.value = theme.value === 'light' ? 'dark' : 'light'
    document.documentElement.setAttribute('data-theme', theme.value)
  }

  function setLanguage(lang: 'zh-CN' | 'en-US') {
    language.value = lang
  }

  function toggleSidebar() {
    sidebarCollapsed.value = !sidebarCollapsed.value
  }

  function setSidebarCollapsed(collapsed: boolean) {
    sidebarCollapsed.value = collapsed
  }

  function toggleUploadPanel() {
    showUploadPanel.value = !showUploadPanel.value
  }

  function setUploadPanel(show: boolean) {
    showUploadPanel.value = show
  }

  return {
    theme,
    language,
    sidebarCollapsed,
    showUploadPanel,
    isDark,
    toggleTheme,
    setLanguage,
    toggleSidebar,
    setSidebarCollapsed,
    toggleUploadPanel,
    setUploadPanel
  }
})
