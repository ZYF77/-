# 建筑材料审核及分析 Agent

一个本地可运行的建筑材料报审审核原型：后端使用 .NET Minimal API 实现多 Agent 审核编排，前端使用 React + Tailwind + Vite 实现审核工作台。

## 功能

- 文档解析 Agent：从报审文本中抽取材料名称、规格、品牌、批次和关键性能参数。
- 合规审核 Agent：核对必要资料、规范依据、设计要求和材料类别规则。
- 成本分析 Agent：比对预算价、历史价和内置参考价格区间。
- 风险复核 Agent：综合输出风险等级、审核结论、人工复核点和下一步动作。
- 规则配置化：材料类别、必备资料、规范依据和价格区间维护在 `rules/material-rules.json`。
- 审核台账：每次审核会写入本地 SQLite，支持历史列表和报告回看。

## 运行后端

```powershell
$env:DOTNET_CLI_HOME="$PWD\.dotnet-home"
dotnet run --urls http://127.0.0.1:5188
```

后端接口：

- `GET /api/health`
- `GET /api/sample`
- `GET /api/rules`
- `GET /api/audits?limit=20`
- `GET /api/audits/{reportId}`
- `POST /api/audit`

## 运行前端

```powershell
cd client
npm install
npm run dev
```

前端默认地址：`http://127.0.0.1:5173`

## 构建前端并由后端托管

```powershell
cd client
npm install
npm run build
cd ..
$env:DOTNET_CLI_HOME="$PWD\.dotnet-home"
dotnet run --urls http://127.0.0.1:5188
```

`npm run build` 会把前端产物输出到 `wwwroot/`，由 .NET 后端直接托管。

## Docker 运行

```powershell
docker build -t building-materials-audit-agent .
docker run --rm -p 8080:8080 building-materials-audit-agent
```

Docker 默认地址：`http://127.0.0.1:8080`

## 配置说明

材料规则文件位于：

```text
rules/material-rules.json
```

可以在这里维护：

- 材料类别和识别关键词
- 必备报审资料
- 推荐规范
- 关键性能参数
- 参考价格区间
- 高风险场景关键词

本地审核历史默认写入：

```text
App_Data/material-audit.db
```

该目录已加入 `.gitignore`，不会提交到仓库。
