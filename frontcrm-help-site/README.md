# FrontCRM Help Site

独立帮助文档网站，基于 [Astro](https://astro.build) + [Starlight](https://starlight.astro.build) 构建，支持英文和中文。

## 目录结构

```
frontcrm-help-site/
├── astro.config.mjs      # Astro + Starlight 配置
├── package.json
├── tsconfig.json
├── public/               # 静态资源
├── src/
│   ├── content/docs/     # 文档内容
│   │   ├── en/           # 英文文档
│   │   └── zh/           # 中文文档
│   ├── env.d.ts
│   └── styles/
└── README.md
```

## 本地开发

```bash
cd frontcrm-help-site
npm install
npm run dev
```

默认开发服务器地址：http://localhost:4321

## 构建

```bash
npm run build
```

构建产物输出到 `dist/` 目录。

## 部署到 Vercel

1. 在 Vercel 控制台新建项目
2. 导入 `d:/MyProject/FrontCRM_CSharp` 仓库
3. 设置 **Root Directory** 为 `frontcrm-help-site`
4. 构建命令：`npm run build`
5. 输出目录：`dist`

## 多语言

- 默认语言：英文（`en`）
- 中文：`zh`

访问路径示例：

| 语言 | URL |
|------|-----|
| 英文 | `https://help.frontcrm.com/en/customer` |
| 中文 | `https://help.frontcrm.com/zh/customer` |

## 与 FrontCRM 集成

FrontCRM 通过 URL 参数打开帮助页面，例如：

```
https://help.frontcrm.com/zh/customer?source=frontcrm&route=CustomerList
```

帮助页面 slug 需要与 `CRM.Web/src/assets/help-mapping.json` 中的页面 ID 保持一致。

## 贡献文档

1. 在 `src/content/docs/en/` 或 `src/content/docs/zh/` 下创建/编辑 `.mdx` 文件
2. 保持中英文文件结构对称
3. 提交 PR 到 `developV3` 分支
