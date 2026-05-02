# 建筑材料审核及分析 Agent

一个本地可运行的建筑材料报审审核原型：后端使用 .NET Minimal API 实现多 Agent 审核编排，前端使用 React + Tailwind + Vite 实现审核工作台。

## 功能

- 文档解析 Agent：从报审文本中抽取材料名称、规格、品牌、批次和关键性能参数。
- 合规审核 Agent：核对必要资料、规范依据、设计要求和材料类别规则。
- 成本分析 Agent：比对预算价、历史价和内置参考价格区间。
- 风险复核 Agent：综合输出风险等级、审核结论、人工复核点和下一步动作。

## 运行后端

```powershell
$env:DOTNET_CLI_HOME="$PWD\.dotnet-home"
dotnet run --urls http://127.0.0.1:5188
```

后端接口：

- `GET /api/health`
- `GET /api/sample`
- `GET /api/rules`
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
