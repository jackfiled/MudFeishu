import request from '@/utils/request'
import type { 
  FileUploadResponse, 
  FileListResponse, 
  FileInfoResponse,
  FolderCreateRequest,
  FolderResponse,
  FolderListResponse,
  FolderContentsResponse,
  VersionResponse,
  VersionListResponse,
  VersionCreateResponse
} from './types'

export const fileApi = {
  upload: (file: FormData, folderToken?: string, onUploadProgress?: (progressEvent: any) => void) => {
    return request({
      url: '/api/files/upload',
      method: 'post',
      data: file,
      params: { folderToken },
      headers: { 'Content-Type': 'multipart/form-data' },
      onUploadProgress
    }) as Promise<FileUploadResponse>
  },

  download: (fileToken: string, versionToken?: string) => {
    return request({
      url: `/api/files/${fileToken}/download`,
      method: 'get',
      params: { versionToken },
      responseType: 'blob'
    })
  },

  getList: (folderToken?: string, page = 1, pageSize = 20) => {
    return request({
      url: '/api/files',
      method: 'get',
      params: { folderToken, page, pageSize }
    }) as Promise<FileListResponse>
  },

  getInfo: (fileToken: string) => {
    return request({
      url: `/api/files/${fileToken}`,
      method: 'get'
    }) as Promise<FileInfoResponse>
  },

  delete: (fileToken: string) => {
    return request({
      url: `/api/files/${fileToken}`,
      method: 'delete'
    })
  }
}

export const folderApi = {
  create: (data: FolderCreateRequest) => {
    return request({
      url: '/api/folders',
      method: 'post',
      data
    }) as Promise<FolderResponse>
  },

  update: (folderToken: string, data: { name?: string; parentFolderToken?: string }) => {
    return request({
      url: `/api/folders/${folderToken}`,
      method: 'put',
      data
    }) as Promise<FolderResponse>
  },

  delete: (folderToken: string) => {
    return request({
      url: `/api/folders/${folderToken}`,
      method: 'delete'
    })
  },

  getList: (parentFolderToken?: string, page = 1, pageSize = 50) => {
    return request({
      url: '/api/folders',
      method: 'get',
      params: { parentFolderToken, page, pageSize }
    }) as Promise<FolderListResponse>
  },

  getContents: (folderToken: string) => {
    return request({
      url: `/api/folders/${folderToken}/contents`,
      method: 'get'
    }) as Promise<FolderContentsResponse>
  }
}

export const versionApi = {
  getList: (fileToken: string) => {
    return request({
      url: `/api/files/${fileToken}/versions`,
      method: 'get'
    }) as Promise<VersionListResponse>
  },

  create: (fileToken: string, file: FormData) => {
    return request({
      url: `/api/files/${fileToken}/versions`,
      method: 'post',
      data: file,
      headers: { 'Content-Type': 'multipart/form-data' }
    }) as Promise<VersionCreateResponse>
  },

  download: (fileToken: string, versionToken: string) => {
    return request({
      url: `/api/files/${fileToken}/versions/${versionToken}/download`,
      method: 'get',
      responseType: 'blob'
    })
  },

  restore: (fileToken: string, versionToken: string) => {
    return request({
      url: `/api/files/${fileToken}/versions/${versionToken}/restore`,
      method: 'put'
    })
  },

  delete: (fileToken: string, versionToken: string) => {
    return request({
      url: `/api/files/${fileToken}/versions/${versionToken}`,
      method: 'delete'
    })
  }
}
