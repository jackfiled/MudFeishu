# FeishuFileServer - 飞书云盘文件管理系统

基于飞书开放平台的云盘文件管理系统，支持大文件分片上传、文件分享、版本管理等功能。

## 目录

- [功能特性](#功能特性)
- [系统架构](#系统架构)
- [技术栈](#技术栈)
- [环境要求](#环境要求)
- [快速开始](#快速开始)
- [配置说明](#配置说明)
- [API文档](#api文档)
- [项目结构](#项目结构)

## 功能特性

### 文件管理

- 📁 **文件夹管理** - 创建、重命名、删除文件夹
- 📄 **文件上传** - 支持普通上传和分片上传
- 📥 **文件下载** - 单文件下载和批量下载
- 🔄 **文件操作** - 重命名、移动、复制、删除
- 🗑️ **回收站** - 文件回收站和恢复功能

### 高级功能

- 🚀 **大文件分片上传** - 支持断点续传
- ☁️ **飞书云空间集成** - 文件存储到飞书云盘
- 🔗 **文件分享** - 创建分享链接和密码保护
- 📜 **版本管理** - 文件版本历史和回滚
- 📊 **操作日志** - 完整的操作记录

### 用户系统

- 🔐 **用户认证** - JWT Token 认证
- 👤 **用户管理** - 个人资料和密码修改
- 🛡️ **权限控制** - 基于用户的文件权限

## 系统架构

```
┌─────────────────────────────────────────────────────────────────┐
│                        前端 (Vue 3 + Element Plus)               │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │  FileManager │  │  FileList   │  │  FolderTree │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ ChunkUploader│  │ ShareDialog │  │ VersionHist │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
├─────────────────────────────────────────────────────────────────┤
│                         API Layer (Axios)                        │
└───────────────────────────────┬─────────────────────────────────┘
                                │ HTTP/REST
┌───────────────────────────────▼─────────────────────────────────┐
│                      后端 (ASP.NET Core 8.0)                     │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ AuthController│ │FilesController│ │FoldersCtrl │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ BatchController│ │ShareController│ │ChunkUpload │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
├─────────────────────────────────────────────────────────────────┤
│                        Service Layer                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ FileService │  │FolderService│  │AuthService  │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ChunkUpload  │  │ShareService │  │FeishuDrive  │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
├─────────────────────────────────────────────────────────────────┤
│                      Data Access Layer                           │
│  ┌─────────────────────────────────────────────────┐            │
│  │           Entity Framework Core + SQLite         │            │
│  └─────────────────────────────────────────────────┘            │
└───────────────────────────────┬─────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│                     飞书开放平台 API                              │
│  ┌─────────────────────────────────────────────────┐            │
│  │              Mud.Feishu SDK                      │            │
│  └─────────────────────────────────────────────────┘            │
└─────────────────────────────────────────────────────────────────┘
```

## 技术栈

### 后端

| 技术                  | 版本  | 说明         |
| --------------------- | ----- | ------------ |
| .NET                  | 8.0   | 运行时框架   |
| ASP.NET Core          | 8.0   | Web API 框架 |
| Entity Framework Core | 8.0   | ORM 框架     |
| SQLite                | -     | 嵌入式数据库 |
| Serilog               | -     | 日志框架     |
| JWT Bearer            | 8.0   | 身份认证     |
| Swagger               | 6.5.0 | API 文档     |
| Mud.Feishu            | -     | 飞书 SDK     |

### 前端

| 技术         | 版本  | 说明        |
| ------------ | ----- | ----------- |
| Vue          | 3.5+  | 前端框架    |
| Vue Router   | 5.0+  | 路由管理    |
| Pinia        | 3.0+  | 状态管理    |
| Element Plus | 2.13+ | UI 组件库   |
| TypeScript   | 5.9+  | 类型支持    |
| Vite         | 8.0+  | 构建工具    |
| Axios        | 1.13+ | HTTP 客户端 |
| SCSS         | -     | 样式预处理  |

## 环境要求

### 后端

- .NET SDK 8.0 或更高版本
- 支持的操作系统：Windows、Linux、macOS

### 前端

- Node.js 18.0 或更高版本
- npm 9.0+ 或 pnpm 8.0+

## 快速开始

### 1. 克隆项目

```bash
git clone https://gitee.com/mudtools/MudFeishu.git
cd Demos/FeishuFileServer
```

### 2. 配置飞书应用

在 `backend/appsettings.local.json` 中配置飞书应用信息：

```json
{
  "FeishuApps": {
    "Default": {
      "AppId": "your_app_id",
      "AppSecret": "your_app_secret",
      "IsDefault": true
    }
  },
  "JwtSettings": {
    "SecretKey": "your_jwt_secret_key_at_least_32_characters_long"
  }
}
```

### 3. 启动后端

```bash
cd backend

# 安装依赖并运行
dotnet restore
dotnet run

# 后端服务将在 http://localhost:5000 启动
```

### 4. 启动前端

```bash
cd frontend

# 安装依赖
npm install
# 或使用 pnpm
pnpm install

# 开发模式运行
npm run dev

# 前端服务将在 http://localhost:3000 启动
```

### 5. 访问应用

打开浏览器访问 http://localhost:3000

**默认管理员账号**：

- 用户名：`admin`
- 密码：`admin123`

## 配置说明

### 后端配置 (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=FeishuFile.db"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  },
  "JwtSettings": {
    "Issuer": "FeishuFileServer",
    "Audience": "FeishuFileServer",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "FileUploadSettings": {
    "MaxFileSize": 524288000,
    "AllowedExtensions": [
      ".docx",
      ".xlsx",
      ".pptx",
      ".pdf",
      ".png",
      ".jpg",
      ".jpeg",
      ".tiff"
    ],
    "UploadDirectory": "uploads"
  }
}
```

### 前端配置 (.env.development)

```env
VITE_API_BASE_URL=http://localhost:5000
```

## API文档

启动后端后访问 Swagger UI：http://localhost:5000/swagger

### 主要API端点

| 模块         | 端点                                         | 说明           |
| ------------ | -------------------------------------------- | -------------- |
| **认证**     | `POST /api/auth/login`                       | 用户登录       |
|              | `POST /api/auth/register`                    | 用户注册       |
|              | `GET /api/auth/profile`                      | 获取用户信息   |
| **文件**     | `POST /api/files/upload`                     | 上传文件       |
|              | `GET /api/files`                             | 获取文件列表   |
|              | `GET /api/files/{token}/download`            | 下载文件       |
|              | `DELETE /api/files/{token}`                  | 删除文件       |
| **文件夹**   | `POST /api/folders`                          | 创建文件夹     |
|              | `PUT /api/folders/{token}`                   | 更新文件夹     |
|              | `DELETE /api/folders/{token}`                | 删除文件夹     |
| **分片上传** | `POST /api/files/chunk/init`                 | 初始化分片上传 |
|              | `POST /api/files/chunk/{id}/{seq}`           | 上传分片       |
|              | `POST /api/files/chunk/{id}/complete`        | 完成上传       |
| **批量操作** | `POST /api/batch/delete`                     | 批量删除       |
|              | `POST /api/batch/move`                       | 批量移动       |
|              | `POST /api/batch/copy`                       | 批量复制       |
|              | `POST /api/batch/download`                   | 批量下载       |
| **分享**     | `POST /api/shares`                           | 创建分享       |
|              | `GET /api/shares/{code}`                     | 访问分享       |
| **回收站**   | `GET /api/recyclebin/files`                  | 获取已删除文件 |
|              | `POST /api/recyclebin/files/{token}/restore` | 恢复文件       |

## 项目结构

```
FeishuFileServer/
├── backend/                          # 后端项目
│   ├── Controllers/                  # API 控制器
│   │   ├── AuthController.cs         # 认证控制器
│   │   ├── FilesController.cs        # 文件控制器
│   │   ├── FoldersController.cs      # 文件夹控制器
│   │   ├── BatchController.cs        # 批量操作控制器
│   │   ├── ChunkUploadController.cs  # 分片上传控制器
│   │   ├── SharesController.cs       # 分享控制器
│   │   ├── RecycleBinController.cs   # 回收站控制器
│   │   └── LogsController.cs         # 日志控制器
│   ├── Services/                     # 业务服务
│   │   ├── Feishu/                   # 飞书服务
│   │   │   ├── FeishuDriveService.cs
│   │   │   └── IFeishuDriveService.cs
│   │   ├── FileService.cs
│   │   ├── FolderService.cs
│   │   ├── AuthService.cs
│   │   ├── ChunkUploadService.cs
│   │   ├── BatchService.cs
│   │   └── ShareService.cs
│   ├── Models/                       # 数据模型
│   │   ├── DTOs/                     # 数据传输对象
│   │   ├── User.cs
│   │   ├── FileRecord.cs
│   │   ├── FolderRecord.cs
│   │   └── ChunkUploadRecord.cs
│   ├── Data/                         # 数据访问
│   │   └── FeishuFileDbContext.cs
│   ├── Middleware/                   # 中间件
│   │   ├── GlobalExceptionHandlingMiddleware.cs
│   │   └── RateLimitingMiddleware.cs
│   ├── Extensions/                   # 扩展方法
│   ├── Program.cs                    # 入口文件
│   ├── appsettings.json              # 配置文件
│   └── appsettings.local.json        # 本地配置
│
├── frontend/                         # 前端项目
│   ├── src/
│   │   ├── api/                      # API 调用
│   │   │   ├── index.ts
│   │   │   └── types.ts
│   │   ├── components/               # 组件
│   │   │   ├── FileList.vue          # 文件列表
│   │   │   ├── FolderTree.vue        # 文件夹树
│   │   │   ├── ChunkUploader.vue     # 分片上传
│   │   │   ├── FolderDialog.vue      # 文件夹对话框
│   │   │   ├── ShareDialog.vue       # 分享对话框
│   │   │   └── VersionHistory.vue    # 版本历史
│   │   ├── views/                    # 页面
│   │   │   ├── FileManager.vue       # 文件管理主页
│   │   │   ├── LoginView.vue         # 登录页
│   │   │   ├── RecycleBin.vue        # 回收站
│   │   │   └── ShareList.vue         # 分享列表
│   │   ├── stores/                   # 状态管理
│   │   │   ├── authStore.ts
│   │   │   ├── fileStore.ts
│   │   │   └── folderStore.ts
│   │   ├── router/                   # 路由配置
│   │   ├── styles/                   # 样式文件
│   │   ├── utils/                    # 工具函数
│   │   ├── App.vue                   # 根组件
│   │   └── main.ts                   # 入口文件
│   ├── index.html
│   ├── package.json
│   ├── vite.config.ts
│   └── tsconfig.json
│
└── README.md                         # 项目文档
```

## 开发指南

### 构建生产版本

```bash
# 后端
cd backend
dotnet build --configuration Release
dotnet publish --configuration Release --output ./publish

# 前端
cd frontend
npm run build
# 产物在 dist/ 目录
```

### 数据库迁移

```bash
cd backend

# 创建迁移
dotnet ef migrations add MigrationName

# 应用迁移
dotnet ef database update

# 回滚迁移
dotnet ef database update PreviousMigrationName
```

### 代码规范

- 后端遵循 C# 编码规范
- 前端遵循 Vue 3 组合式 API 风格
- 使用 TypeScript 类型检查
- 使用 ESLint 进行代码检查

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！
