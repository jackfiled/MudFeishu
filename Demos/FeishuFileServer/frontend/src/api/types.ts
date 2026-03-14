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
  refreshToken: string
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

export interface RefreshTokenRequest {
  refreshToken: string
}

// 分享相关类型
export interface CreateShareRequest {
  resourceType: string
  resourceToken: string
  password?: string
  expireTime?: string
  maxAccessCount?: number
  allowDownload?: boolean
}

export interface UpdateShareRequest {
  password?: string
  expireTime?: string
  maxAccessCount?: number
  allowDownload?: boolean
  isActive?: boolean
}

export interface ShareResponse {
  id: number
  shareCode: string
  shareLink: string
  resourceType: string
  resourceName: string
  requirePassword: boolean
  expireTime?: string
  allowDownload: boolean
  createdTime: string
}

export interface ShareListResponse {
  shares: ShareResponse[]
  totalCount: number
}

export interface ShareContentResponse {
  resourceType: string
  resourceName: string
  file?: FileInfoResponse
  folderContents?: FolderContentsResponse
  allowDownload: boolean
}

// 操作日志相关类型
export interface OperationLog {
  id: number
  userId?: number
  username?: string
  operationType: string
  resourceType: string
  resourceToken?: string
  resourceName?: string
  details?: string
  ipAddress?: string
  operationTime: string
  isSuccess: boolean
  errorMessage?: string
}

export interface OperationLogListResponse {
  logs: OperationLog[]
  totalCount: number
}
