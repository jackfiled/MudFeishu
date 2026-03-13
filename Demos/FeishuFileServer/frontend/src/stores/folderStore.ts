import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { FolderResponse } from '@/api/types'

export interface FolderTreeNode extends FolderResponse {
  children?: FolderTreeNode[]
  loading?: boolean
}

export const useFolderStore = defineStore('folders', () => {
  const folders = ref<FolderTreeNode[]>([])
  const currentFolder = ref<FolderResponse | null>(null)
  const expandedFolders = ref<Set<string>>(new Set())
  const loading = ref(false)
  const rootFolder = ref<FolderResponse | null>(null)

  const hasExpanded = computed(() => (token: string) => expandedFolders.value.has(token))
  
  const currentFolderPath = computed(() => {
    const path: FolderResponse[] = []
    let folder = currentFolder.value
    while (folder) {
      path.unshift(folder)
      folder = null
    }
    return path
  })

  function setFolders(newFolders: FolderResponse[]) {
    folders.value = buildTree(newFolders)
  }

  function buildTree(folderList: FolderResponse[]): FolderTreeNode[] {
    const map = new Map<string, FolderTreeNode>()
    const roots: FolderTreeNode[] = []

    folderList.forEach(folder => {
      map.set(folder.folderToken, { ...folder, children: [], loading: false })
    })

    folderList.forEach(folder => {
      const node = map.get(folder.folderToken)!
      if (folder.parentFolderToken) {
        const parent = map.get(folder.parentFolderToken)
        if (parent) {
          parent.children = parent.children || []
          parent.children.push(node)
        } else {
          roots.push(node)
        }
      } else {
        roots.push(node)
      }
    })

    return roots
  }

  function setCurrentFolder(folder: FolderResponse | null) {
    currentFolder.value = folder
  }

  function toggleExpand(token: string) {
    if (expandedFolders.value.has(token)) {
      expandedFolders.value.delete(token)
    } else {
      expandedFolders.value.add(token)
    }
  }

  function setExpanded(tokens: string[]) {
    expandedFolders.value = new Set(tokens)
  }

  function setLoading(isLoading: boolean) {
    loading.value = isLoading
  }

  function setRootFolder(folder: FolderResponse | null) {
    rootFolder.value = folder
  }

  function addFolder(folder: FolderResponse) {
    const node: FolderTreeNode = { ...folder, children: [], loading: false }
    if (folder.parentFolderToken) {
      const parent = findNode(folders.value, folder.parentFolderToken)
      if (parent) {
        parent.children = parent.children || []
        parent.children.push(node)
      }
    } else {
      folders.value.push(node)
    }
  }

  function updateFolder(token: string, updates: Partial<FolderResponse>) {
    const node = findNode(folders.value, token)
    if (node) {
      Object.assign(node, updates)
    }
  }

  function removeFolder(token: string) {
    removeNode(folders.value, token)
  }

  function findNode(nodes: FolderTreeNode[], token: string): FolderTreeNode | null {
    for (const node of nodes) {
      if (node.folderToken === token) return node
      if (node.children?.length) {
        const found = findNode(node.children, token)
        if (found) return found
      }
    }
    return null
  }

  function removeNode(nodes: FolderTreeNode[], token: string): boolean {
    const index = nodes.findIndex(n => n.folderToken === token)
    if (index > -1) {
      nodes.splice(index, 1)
      return true
    }
    for (const node of nodes) {
      if (node.children?.length && removeNode(node.children, token)) {
        return true
      }
    }
    return false
  }

  return {
    folders,
    currentFolder,
    expandedFolders,
    loading,
    rootFolder,
    hasExpanded,
    currentFolderPath,
    setFolders,
    setCurrentFolder,
    toggleExpand,
    setExpanded,
    setLoading,
    setRootFolder,
    addFolder,
    updateFolder,
    removeFolder,
    findNode
  }
})
