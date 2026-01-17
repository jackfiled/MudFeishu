# 飞书OAuth登录演示 - 前端

基于 Vue 3 + TypeScript + Vite + Element Plus 构建的飞书OAuth登录演示前端应用。

## 技术栈

- Vue 3 (Composition API)
- TypeScript
- Vite
- Vue Router
- Pinia (状态管理)
- Element Plus (UI组件库)
- Axios (HTTP客户端)

## 项目结构

```
src/
├── api/           # API接口封装
├── components/    # 公共组件
├── router/        # 路由配置
├── stores/        # Pinia状态管理
├── views/         # 页面组件
├── App.vue        # 根组件
└── main.ts        # 应用入口
```

## 快速开始

### 安装依赖

```bash
npm install
```

### 启动开发服务器

```bash
npm run dev
```

访问 http://localhost:5173

### 构建生产版本

```bash
npm run build
```

### 预览生产构建

```bash
npm run preview
```

## 功能说明

### 登录流程

1. 用户点击"使用飞书登录"按钮
2. 前端调用后端API获取飞书授权URL
3. 将state参数存储到localStorage（防CSRF）
4. 重定向到飞书授权页面
5. 用户授权后，飞书回调到前端
6. 前端提取code和state，发送到后端
7. 后端验证state，使用code获取access_token
8. 后端获取用户信息，生成JWT令牌
9. 前端保存JWT令牌到localStorage
10. 跳转到首页

### 主要页面

- **LoginView**: 登录页面，提供飞书登录按钮
- **CallbackView**: OAuth回调处理页面
- **HomeView**: 登录成功后的首页，显示用户信息

### 状态管理

使用Pinia管理用户状态：
- `token`: JWT访问令牌
- `user`: 用户信息
- `isLoggedIn`: 登录状态

## 环境配置

### 开发环境

前端运行在 `http://localhost:5173`
后端API代理到 `http://localhost:5000`

### 生产环境

修改 `vite.config.ts` 中的proxy配置，或直接部署到后端服务器。

## 安全说明

1. **State参数验证**: 防止CSRF攻击
2. **JWT存储**: Token存储在localStorage（生产环境建议使用HttpOnly Cookie）
3. **Token验证**: 每次请求自动携带Token，并在401时自动登出
4. **路由守卫**: 需要登录的页面自动跳转到登录页

## 注意事项

1. 确保后端API已正确配置并启动
2. 飞书开放平台的应用配置中，重定向URL必须与配置一致
3. 开发环境使用HTTP，生产环境建议使用HTTPS
4. 本示例使用localStorage存储Token，生产环境建议使用更安全的方案
