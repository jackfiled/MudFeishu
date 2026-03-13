export interface FileRecord {
  id: number
  fileToken: string
  folderToken?: string
  versionToken?: string
  fileName: string
  fileSize: number
  mimeType?: string
  fileMD5?: string
  uploadTime: string
  isDeleted: boolean
}

export interface FolderRecord {
  id: number
  folderToken: string
  folderName: string
  parentFolderToken?: string
  createdTime: string
  isDeleted: boolean
}

export interface VersionRecord {
  id: number
  fileToken: string
  versionToken: string
  versionNumber: number
  fileName: string
  fileSize: number
  fileMD5?: string
  createdTime: string
  isCurrentVersion: boolean
}

export interface FileUploadResponse {
  fileToken: string
  fileName: string
  fileSize: number
  mimeType: string
  uploadTime: string
}

export interface FileListResponse {
  files: FileInfoResponse[]
  totalCount: number
  page: number
  pageSize: number
}

export interface FileInfoResponse {
  fileToken: string
  folderToken?: string
  fileName: string
  fileSize: number
  mimeType?: string
  uploadTime: string
}

export interface FolderCreateRequest {
  name: string
  parentFolderToken?: string
}

export interface FolderResponse {
  id: number
  folderToken: string
  folderName: string
  parentFolderToken?: string
  createdTime: string
  isDeleted: boolean
}

export interface FolderListResponse {
  folders: FolderResponse[]
  totalCount: number
  page: number
  pageSize: number
}

export interface FolderContentsResponse {
  folders: FolderResponse[]
  files: FileInfoResponse[]
}

export interface VersionResponse {
  fileToken: string
  versionToken: string
  versionNumber: number
  fileName: string
  fileSize: number
  fileMD5?: string
  createdTime: string
  isCurrentVersion: boolean
}

export interface VersionListResponse {
  versions: VersionResponse[]
  totalCount: number
}

export interface VersionCreateResponse {
  versionToken: string
  versionNumber: number
  createdTime: string
}

// 用户认证相关类型
export interface UserInfo {
  id: number
  username: string
  email?: string
  displayName?: string
  role: string
}

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  token: string
  tokenType: string
  expiresIn: number
  user: UserInfo
}

export interface RegisterRequest {
  username: string
  password: string
  email?: string
  displayName?: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface UpdateProfileRequest {
  email?: string
  displayName?: string
}
