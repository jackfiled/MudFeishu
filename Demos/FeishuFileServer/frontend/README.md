# 飞书云文件管理系统 - 前端

基于 Vue 3.4 + Vite 5.x + Element Plus 2.13.5 开发的飞书云文件管理系统前端。

## 技术栈

- **核心框架**: Vue 3.4 (Composition API)
- **构建工具**: Vite 5.x
- **UI组件库**: Element Plus 2.13.5
- **状态管理**: Pinia
- **路由**: Vue Router 4
- **HTTP请求**: Axios

## 项目结构

```
src/
├── api/              # API接口封装
│   ├── index.ts      # API 方法
│   └── types.ts      # TypeScript 类型定义
├── components/       # 通用组件
│   ├── FolderTree.vue        # 文件夹树形导航
│   ├── FileList.vue          # 文件列表组件
│   ├── FileUpload.vue        # 文件上传组件
│   ├── CreateFolderDialog.vue # 创建文件夹对话框
│   ├── RenameDialog.vue      # 重命名对话框
│   ├── MoveDialog.vue        # 移动对话框
│   └── VersionHistory.vue    # 版本历史弹窗
├── views/            # 页面视图
│   ├── FileManager.vue       # 文件管理器主页面
│   ├── FileDetail.vue        # 文件详情页
│   └── NotFound.vue          # 404页面
├── stores/           # Pinia状态管理
│   ├── fileStore.ts          # 文件状态
│   ├── folderStore.ts        # 文件夹状态
│   ├── uploadStore.ts        # 上传状态
│   └── appStore.ts           # 应用状态
├── router/           # 路由配置
│   └── index.ts
├── utils/            # 工具函数
│   ├── request.ts            # Axios请求封装
│   └── format.ts             # 格式化工具
└── styles/           # 全局样式
    └── main.scss
```

## 功能特性

### 文件管理
- ✅ 文件列表展示（列表/网格视图）
- ✅ 文件上传（拖拽上传、多文件选择）
- ✅ 文件下载
- ✅ 文件删除
- ✅ 文件重命名
- ✅ 文件移动

### 文件夹管理
- ✅ 文件夹树形导航
- ✅ 创建文件夹
- ✅ 删除文件夹
- ✅ 右键菜单操作

### 版本管理
- ✅ 版本历史列表
- ✅ 版本下载
- ✅ 版本恢复
- ✅ 版本删除

### 用户体验
- ✅ 响应式布局
- ✅ 加载状态
- ✅ 空状态展示
- ✅ 操作反馈提示
- ✅ 面包屑导航

## 安装与运行

### 安装依赖
```bash
npm install
```

### 开发环境运行
```bash
npm run dev
```

### 生产环境构建
```bash
npm run build
```

## 环境配置

### 开发环境 (.env.development)
```
VITE_API_BASE_URL=http://localhost:5000
VITE_UPLOAD_CHUNK_SIZE=5242880
VITE_ENABLE_CHUNK_UPLOAD=true
```

### 生产环境 (.env.production)
```
VITE_API_BASE_URL=/api
VITE_UPLOAD_CHUNK_SIZE=5242880
VITE_ENABLE_CHUNK_UPLOAD=true
```

## API 接口

### 文件接口
- `POST /api/files/upload` - 上传文件
- `GET /api/files/{fileToken}/download` - 下载文件
- `GET /api/files` - 文件列表
- `GET /api/files/{fileToken}` - 文件详情
- `DELETE /api/files/{fileToken}` - 删除文件

### 文件夹接口
- `POST /api/folders` - 创建文件夹
- `PUT /api/folders/{folderToken}` - 更新文件夹
- `DELETE /api/folders/{folderToken}` - 删除文件夹
- `GET /api/folders` - 文件夹列表
- `GET /api/folders/{folderToken}/contents` - 文件夹内容

### 版本接口
- `GET /api/files/{fileToken}/versions` - 版本历史
- `POST /api/files/{fileToken}/versions` - 创建版本
- `GET /api/files/{fileToken}/versions/{versionToken}/download` - 下载版本
- `PUT /api/files/{fileToken}/versions/{versionToken}/restore` - 恢复版本
- `DELETE /api/files/{fileToken}/versions/{versionToken}` - 删除版本

## 开发说明

### 状态管理
使用 Pinia 进行状态管理，分为四个 Store：
- **fileStore**: 管理文件列表、当前文件、选中文件等
- **folderStore**: 管理文件夹树、当前文件夹等
- **uploadStore**: 管理上传队列、上传任务状态
- **appStore**: 管理全局设置、主题、侧边栏状态

### 组件通信
- 父子组件：使用 Props 和 Emits
- 跨组件：使用 Pinia Store

### 样式规范
- 使用 SCSS 预处理器
- 遵循 Element Plus 设计规范
- 使用 CSS 变量支持主题切换

## 浏览器支持

- Chrome >= 80
- Firefox >= 78
- Safari >= 14
- Edge >= 80

## 许可证

MIT
