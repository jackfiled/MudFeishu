import request from "@/utils/request";
import type {
  FileUploadResponse,
  FileListResponse,
  FileInfoResponse,
  FolderCreateRequest,
  FolderResponse,
  FolderListResponse,
  FolderContentsResponse,
  VersionListResponse,
  VersionCreateResponse,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  UserInfo,
  ChangePasswordRequest,
  UpdateProfileRequest,
  CreateShareRequest,
  UpdateShareRequest,
  ShareResponse,
  ShareListResponse,
  ShareContentResponse,
  OperationLogListResponse,
  BatchDeleteRequest,
  BatchMoveRequest,
  BatchCopyRequest,
  BatchRestoreRequest,
  BatchOperationResponse,
  InitChunkUploadRequest,
  InitChunkUploadResponse,
  ChunkUploadResponse,
  BatchDownloadRequest,
} from "./types";

export const fileApi = {
  upload: (
    file: FormData,
    folderToken?: string,
    onUploadProgress?: (progressEvent: any) => void,
  ) => {
    return request({
      url: "/api/files/upload",
      method: "post",
      data: file,
      params: { folderToken },
      headers: { "Content-Type": "multipart/form-data" },
      onUploadProgress,
    }) as Promise<FileUploadResponse>;
  },

  download: (fileToken: string, versionToken?: string) => {
    return request({
      url: `/api/files/${fileToken}/download`,
      method: "get",
      params: { versionToken },
      responseType: "blob",
    });
  },

  getList: (folderToken?: string, search?: string, page = 1, pageSize = 20) => {
    return request({
      url: "/api/files",
      method: "get",
      params: { folderToken, search, page, pageSize },
    }) as Promise<FileListResponse>;
  },

  getInfo: (fileToken: string) => {
    return request({
      url: `/api/files/${fileToken}`,
      method: "get",
    }) as Promise<FileInfoResponse>;
  },

  rename: (fileToken: string, newName: string) => {
    return request({
      url: `/api/files/${fileToken}/rename`,
      method: "put",
      data: { newName },
    }) as Promise<FileInfoResponse>;
  },

  delete: (fileToken: string) => {
    return request({
      url: `/api/files/${fileToken}`,
      method: "delete",
    });
  },
};

export const folderApi = {
  create: (data: FolderCreateRequest) => {
    return request({
      url: "/api/folders",
      method: "post",
      data,
    }) as Promise<FolderResponse>;
  },

  update: (
    folderToken: string,
    data: { name?: string; parentFolderToken?: string },
  ) => {
    return request({
      url: `/api/folders/${folderToken}`,
      method: "put",
      data,
    }) as Promise<FolderResponse>;
  },

  delete: (folderToken: string) => {
    return request({
      url: `/api/folders/${folderToken}`,
      method: "delete",
    });
  },

  getList: (parentFolderToken?: string, page = 1, pageSize = 50) => {
    return request({
      url: "/api/folders",
      method: "get",
      params: { parentFolderToken, page, pageSize },
    }) as Promise<FolderListResponse>;
  },

  getContents: (folderToken: string) => {
    return request({
      url: `/api/folders/${folderToken}/contents`,
      method: "get",
    }) as Promise<FolderContentsResponse>;
  },
};

export const versionApi = {
  getList: (fileToken: string) => {
    return request({
      url: `/api/files/${fileToken}/versions`,
      method: "get",
    }) as Promise<VersionListResponse>;
  },

  create: (fileToken: string, file: FormData) => {
    return request({
      url: `/api/files/${fileToken}/versions`,
      method: "post",
      data: file,
      headers: { "Content-Type": "multipart/form-data" },
    }) as Promise<VersionCreateResponse>;
  },

  download: (fileToken: string, versionToken: string) => {
    return request({
      url: `/api/files/${fileToken}/versions/${versionToken}/download`,
      method: "get",
      responseType: "blob",
    });
  },

  restore: (fileToken: string, versionToken: string) => {
    return request({
      url: `/api/files/${fileToken}/versions/${versionToken}/restore`,
      method: "put",
    });
  },

  delete: (fileToken: string, versionToken: string) => {
    return request({
      url: `/api/files/${fileToken}/versions/${versionToken}`,
      method: "delete",
    });
  },
};

export const authApi = {
  login: (data: LoginRequest) => {
    return request({
      url: "/api/auth/login",
      method: "post",
      data,
    }) as Promise<LoginResponse>;
  },

  register: (data: RegisterRequest) => {
    return request({
      url: "/api/auth/register",
      method: "post",
      data,
    }) as Promise<LoginResponse>;
  },

  getProfile: () => {
    return request({
      url: "/api/auth/profile",
      method: "get",
    }) as Promise<UserInfo>;
  },

  updateProfile: (data: UpdateProfileRequest) => {
    return request({
      url: "/api/auth/profile",
      method: "put",
      data,
    }) as Promise<UserInfo>;
  },

  changePassword: (data: ChangePasswordRequest) => {
    return request({
      url: "/api/auth/change-password",
      method: "post",
      data,
    });
  },

  refreshToken: (refreshToken: string) => {
    return request({
      url: "/api/auth/refresh-token",
      method: "post",
      data: { refreshToken },
    }) as Promise<LoginResponse>;
  },

  revokeToken: (refreshToken: string) => {
    return request({
      url: "/api/auth/revoke-token",
      method: "post",
      data: { refreshToken },
    });
  },
};

export const shareApi = {
  create: (data: CreateShareRequest) => {
    return request({
      url: "/api/shares",
      method: "post",
      data,
    }) as Promise<ShareResponse>;
  },

  access: (shareCode: string, password?: string) => {
    return request({
      url: `/api/shares/${shareCode}`,
      method: "get",
      params: { password },
    }) as Promise<ShareContentResponse>;
  },

  getMyShares: (page = 1, pageSize = 20) => {
    return request({
      url: "/api/shares",
      method: "get",
      params: { page, pageSize },
    }) as Promise<ShareListResponse>;
  },

  getShareInfo: (shareCode: string) => {
    return request({
      url: `/api/shares/${shareCode}/info`,
      method: "get",
    }) as Promise<ShareResponse>;
  },

  download: (shareCode: string, password?: string) => {
    return request({
      url: `/api/shares/${shareCode}/download`,
      method: "get",
      params: { password },
      responseType: "blob",
    });
  },

  update: (shareId: number, data: UpdateShareRequest) => {
    return request({
      url: `/api/shares/${shareId}`,
      method: "put",
      data,
    }) as Promise<ShareResponse>;
  },

  delete: (shareId: number) => {
    return request({
      url: `/api/shares/${shareId}`,
      method: "delete",
    });
  },
};

export const recycleBinApi = {
  getDeletedFiles: (page = 1, pageSize = 20) => {
    return request({
      url: "/api/recyclebin/files",
      method: "get",
      params: { page, pageSize },
    }) as Promise<FileListResponse>;
  },

  getDeletedFolders: (page = 1, pageSize = 20) => {
    return request({
      url: "/api/recyclebin/folders",
      method: "get",
      params: { page, pageSize },
    }) as Promise<FolderListResponse>;
  },

  restoreFile: (fileToken: string) => {
    return request({
      url: `/api/recyclebin/files/${fileToken}/restore`,
      method: "post",
    });
  },

  restoreFolder: (folderToken: string) => {
    return request({
      url: `/api/recyclebin/folders/${folderToken}/restore`,
      method: "post",
    });
  },

  permanentlyDeleteFile: (fileToken: string) => {
    return request({
      url: `/api/recyclebin/files/${fileToken}`,
      method: "delete",
    });
  },

  permanentlyDeleteFolder: (folderToken: string) => {
    return request({
      url: `/api/recyclebin/folders/${folderToken}`,
      method: "delete",
    });
  },

  emptyRecycleBin: () => {
    return request({
      url: "/api/recyclebin/empty",
      method: "delete",
    });
  },
};

export const operationLogApi = {
  getByUser: (page = 1, pageSize = 50) => {
    return request({
      url: "/api/logs/user",
      method: "get",
      params: { page, pageSize },
    }) as Promise<OperationLogListResponse>;
  },

  getByResource: (resourceToken: string, page = 1, pageSize = 50) => {
    return request({
      url: `/api/logs/resource/${resourceToken}`,
      method: "get",
      params: { page, pageSize },
    }) as Promise<OperationLogListResponse>;
  },
};

export const batchApi = {
  delete: (data: BatchDeleteRequest) => {
    return request({
      url: "/api/batch/delete",
      method: "post",
      data,
    }) as Promise<BatchOperationResponse>;
  },

  move: (data: BatchMoveRequest) => {
    return request({
      url: "/api/batch/move",
      method: "post",
      data,
    }) as Promise<BatchOperationResponse>;
  },

  copy: (data: BatchCopyRequest) => {
    return request({
      url: "/api/batch/copy",
      method: "post",
      data,
    }) as Promise<BatchOperationResponse>;
  },

  restore: (data: BatchRestoreRequest) => {
    return request({
      url: "/api/batch/restore",
      method: "post",
      data,
    }) as Promise<BatchOperationResponse>;
  },

  download: (data: BatchDownloadRequest) => {
    return request({
      url: "/api/batch/download",
      method: "post",
      data,
      responseType: "blob",
    });
  },
};

export const chunkUploadApi = {
  init: (data: InitChunkUploadRequest) => {
    return request({
      url: "/api/files/chunk/init",
      method: "post",
      data,
    }) as Promise<InitChunkUploadResponse>;
  },

  uploadChunk: (uploadId: string, chunkNumber: number, chunk: Blob, onUploadProgress?: (progressEvent: any) => void) => {
    const formData = new FormData();
    formData.append("chunk", chunk);
    return request({
      url: `/api/files/chunk/${uploadId}/${chunkNumber}`,
      method: "post",
      data: formData,
      headers: { "Content-Type": "multipart/form-data" },
      onUploadProgress,
    }) as Promise<ChunkUploadResponse>;
  },

  complete: (uploadId: string) => {
    return request({
      url: `/api/files/chunk/${uploadId}/complete`,
      method: "post",
    }) as Promise<ChunkUploadResponse>;
  },

  cancel: (uploadId: string) => {
    return request({
      url: `/api/files/chunk/${uploadId}/cancel`,
      method: "delete",
    });
  },
};
