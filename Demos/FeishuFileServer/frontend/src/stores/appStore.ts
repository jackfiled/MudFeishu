import { defineStore } from 'pinia'
import { ref, computed, watch } from 'vue'

export type Theme = 'light' | 'dark' | 'system'

export const useAppStore = defineStore('app', () => {
  const theme = ref<Theme>(getStoredTheme())
  const language = ref<'zh-CN' | 'en-US'>(getStoredLanguage())
  const sidebarCollapsed = ref(false)
  const showUploadPanel = ref(false)
  const isRefreshing = ref(false)
  const refreshingProgress = ref(0)

  const isDark = computed(() => {
    if (theme.value === 'system') {
      return window.matchMedia('(prefers-color-scheme: dark)').matches
    }
    return theme.value === 'dark'
  })

  const actualTheme = computed(() => isDark.value ? 'dark' : 'light')

  function getStoredTheme(): Theme {
    const stored = localStorage.getItem('theme')
    if (stored === 'light' || stored === 'dark' || stored === 'system') {
      return stored
    }
    return 'system'
  }

  function getStoredLanguage(): 'zh-CN' | 'en-US' {
    const stored = localStorage.getItem('language')
    if (stored === 'zh-CN' || stored === 'en-US') {
      return stored
    }
    return 'zh-CN'
  }

  function applyTheme(dark: boolean) {
    const html = document.documentElement
    html.style.transition = 'background-color 0.3s ease, color 0.3s ease'
    
    if (dark) {
      html.setAttribute('data-theme', 'dark')
    } else {
      html.removeAttribute('data-theme')
    }
    
    const metaThemeColor = document.querySelector('meta[name="theme-color"]')
    if (metaThemeColor) {
      metaThemeColor.setAttribute('content', dark ? '#0f172a' : '#f8fafc')
    }
  }

  function setTheme(newTheme: Theme) {
    theme.value = newTheme
    localStorage.setItem('theme', newTheme)
    applyTheme(isDark.value)
  }

  function toggleTheme() {
    const newTheme = isDark.value ? 'light' : 'dark'
    setTheme(newTheme)
  }

  function setLanguage(lang: 'zh-CN' | 'en-US') {
    language.value = lang
    localStorage.setItem('language', lang)
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

  function startRefreshing() {
    isRefreshing.value = true
    refreshingProgress.value = 0
  }

  function setRefreshingProgress(progress: number) {
    refreshingProgress.value = Math.min(100, Math.max(0, progress))
  }

  function stopRefreshing() {
    isRefreshing.value = false
    refreshingProgress.value = 100
    setTimeout(() => {
      refreshingProgress.value = 0
    }, 300)
  }

  watch(isDark, (dark) => {
    applyTheme(dark)
  }, { immediate: true })

  if (typeof window !== 'undefined') {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
    mediaQuery.addEventListener('change', (e) => {
      if (theme.value === 'system') {
        applyTheme(e.matches)
      }
    })
  }

  return {
    theme,
    language,
    sidebarCollapsed,
    showUploadPanel,
    isRefreshing,
    refreshingProgress,
    isDark,
    actualTheme,
    setTheme,
    toggleTheme,
    setLanguage,
    toggleSidebar,
    setSidebarCollapsed,
    toggleUploadPanel,
    setUploadPanel,
    startRefreshing,
    setRefreshingProgress,
    stopRefreshing
  }
})
