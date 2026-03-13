export function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

export function formatDate(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diff = now.getTime() - date.getTime()
  const days = Math.floor(diff / (1000 * 60 * 60 * 24))

  if (days === 0) {
    const hours = Math.floor(diff / (1000 * 60 * 60))
    if (hours === 0) {
      const minutes = Math.floor(diff / (1000 * 60))
      if (minutes < 1) return '刚刚'
      return `${minutes}分钟前`
    }
    return `${hours}小时前`
  } else if (days === 1) {
    return '昨天'
  } else if (days < 7) {
    return `${days}天前`
  } else {
    return date.toLocaleDateString('zh-CN', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    })
  }
}

export function formatDateTime(dateString: string): string {
  const date = new Date(dateString)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

export function getFileExtension(filename: string): string {
  const lastDot = filename.lastIndexOf('.')
  return lastDot > 0 ? filename.substring(lastDot + 1).toLowerCase() : ''
}

export function getFileTypeIcon(filename: string): string {
  const ext = getFileExtension(filename)
  switch (ext) {
    case 'docx':
    case 'doc':
      return 'Document'
    case 'xlsx':
    case 'xls':
      return 'Grid'
    case 'pptx':
    case 'ppt':
      return 'PictureFilled'
    case 'pdf':
      return 'Document'
    case 'png':
    case 'jpg':
    case 'jpeg':
    case 'gif':
    case 'bmp':
      return 'Picture'
    case 'mp4':
    case 'avi':
    case 'mov':
      return 'VideoCamera'
    case 'mp3':
    case 'wav':
      return 'Headset'
    default:
      return 'Document'
  }
}

export function isImageFile(filename: string): boolean {
  const ext = getFileExtension(filename)
  return ['png', 'jpg', 'jpeg', 'gif', 'bmp', 'webp', 'svg'].includes(ext)
}

export function isDocumentFile(filename: string): boolean {
  const ext = getFileExtension(filename)
  return ['docx', 'doc', 'xlsx', 'xls', 'pptx', 'ppt', 'pdf', 'txt'].includes(ext)
}

export function generateUUID(): string {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
    const r = (Math.random() * 16) | 0
    const v = c === 'x' ? r : (r & 0x3) | 0x8
    return v.toString(16)
  })
}

export function downloadBlob(blob: Blob, filename: string) {
  const url = window.URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  window.URL.revokeObjectURL(url)
}

export function getFileIcon(filename: string): string {
  return getFileTypeIcon(filename)
}

export function getFileIconClass(filename: string): string {
  const ext = getFileExtension(filename)
  return `file-icon ${ext}`
}
