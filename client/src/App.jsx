import { useEffect, useMemo, useState } from 'react';
import {
  AlertTriangle,
  CheckCircle2,
  CircleAlert,
  CircleX,
  ClipboardList,
  Download,
  FileSearch,
  GitBranch,
  Loader2,
  Plus,
  RotateCcw,
  ShieldAlert,
  WalletCards,
  X,
} from 'lucide-react';

const emptyRequest = {
  projectName: '',
  materialName: '',
  category: 'insulation',
  specification: '',
  supplier: '',
  brand: '',
  batchNo: '',
  unitPrice: 0,
  quantity: 0,
  unit: 'm2',
  budgetUnitPrice: '',
  historicalUnitPrice: '',
  designRequirement: '',
  submittedText: '',
  providedDocuments: ['材料报审单', '出厂合格证', '检测报告'],
  declaredStandards: [],
};

const categoryOptions = [
  { value: 'waterproof', label: '防水材料' },
  { value: 'insulation', label: '保温材料' },
  { value: 'rebar', label: '钢筋' },
  { value: 'concrete', label: '混凝土' },
  { value: 'cable', label: '电线电缆' },
  { value: 'pipe', label: '管材' },
];

const severityConfig = {
  Critical: {
    label: '重大',
    className: 'border-red-200 bg-red-50 text-red-800',
    icon: CircleX,
  },
  High: {
    label: '高',
    className: 'border-orange-200 bg-orange-50 text-orange-800',
    icon: ShieldAlert,
  },
  Medium: {
    label: '中',
    className: 'border-amber-200 bg-amber-50 text-amber-800',
    icon: AlertTriangle,
  },
  Low: {
    label: '低',
    className: 'border-emerald-200 bg-emerald-50 text-emerald-800',
    icon: CheckCircle2,
  },
};

const signalClasses = {
  Risk: 'border-orange-200 bg-orange-50 text-orange-800',
  Warning: 'border-amber-200 bg-amber-50 text-amber-800',
  Info: 'border-cyan-200 bg-cyan-50 text-cyan-800',
};

function App() {
  const [request, setRequest] = useState(emptyRequest);
  const [rules, setRules] = useState([]);
  const [history, setHistory] = useState([]);
  const [report, setReport] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    fetch('/api/rules')
      .then((res) => (res.ok ? res.json() : []))
      .then(setRules)
      .catch(() => setRules([]));
  }, []);

  useEffect(() => {
    loadHistory();
  }, []);

  const selectedRule = useMemo(() => {
    return rules.find((rule) => rule.category === request.category);
  }, [request.category, rules]);

  const setField = (name, value) => {
    setRequest((current) => ({
      ...current,
      [name]: value,
    }));
  };

  const setNumberField = (name, value) => {
    setField(name, value === '' ? '' : Number(value));
  };

  const loadHistory = async () => {
    try {
      const response = await fetch('/api/audits?limit=20');
      if (!response.ok) return;
      setHistory(await response.json());
    } catch {
      setHistory([]);
    }
  };

  const openHistoryReport = async (reportId) => {
    setLoading(true);
    setError('');
    try {
      const response = await fetch(`/api/audits/${encodeURIComponent(reportId)}`);
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      setReport(await response.json());
    } catch (err) {
      setError(`读取历史报告失败：${err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const loadSample = async () => {
    setError('');
    const response = await fetch('/api/sample');
    const sample = await response.json();
    setRequest({
      ...sample,
      budgetUnitPrice: sample.budgetUnitPrice ?? '',
      historicalUnitPrice: sample.historicalUnitPrice ?? '',
    });
    setReport(null);
  };

  const resetForm = () => {
    setRequest(emptyRequest);
    setReport(null);
    setError('');
  };

  const runAudit = async () => {
    setLoading(true);
    setError('');

    const payload = {
      ...request,
      unitPrice: Number(request.unitPrice || 0),
      quantity: Number(request.quantity || 0),
      budgetUnitPrice: request.budgetUnitPrice === '' ? null : Number(request.budgetUnitPrice),
      historicalUnitPrice: request.historicalUnitPrice === '' ? null : Number(request.historicalUnitPrice),
    };

    try {
      const response = await fetch('/api/audit', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const data = await response.json();
      setReport(data);
      await loadHistory();
    } catch (err) {
      setError(`审核失败：${err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const exportReport = () => {
    if (!report) return;
    const markdown = buildMarkdownReport(report);
    const blob = new Blob([markdown], { type: 'text/markdown;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `${report.reportId}.md`;
    anchor.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="min-h-screen bg-slate-100">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-[1480px] flex-col gap-3 px-4 py-4 sm:px-6 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-cyan-700 text-white">
              <FileSearch size={21} aria-hidden="true" />
            </div>
            <div>
              <h1 className="text-lg font-semibold text-slate-950">建筑材料审核及分析 Agent</h1>
              <p className="text-sm text-slate-500">材料报审、规范比对、价格偏差与风险复核</p>
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <button
              type="button"
              onClick={loadSample}
              className="inline-flex h-10 items-center gap-2 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
            >
              <ClipboardList size={17} aria-hidden="true" />
              示例
            </button>
            <button
              type="button"
              onClick={resetForm}
              className="icon-button"
              title="重置"
              aria-label="重置"
            >
              <RotateCcw size={17} aria-hidden="true" />
            </button>
            <button
              type="button"
              onClick={runAudit}
              disabled={loading}
              className="inline-flex h-10 items-center gap-2 rounded-md bg-cyan-700 px-4 text-sm font-semibold text-white transition hover:bg-cyan-800 disabled:cursor-not-allowed disabled:bg-cyan-400"
            >
              {loading ? <Loader2 size={17} className="animate-spin" aria-hidden="true" /> : <ShieldAlert size={17} aria-hidden="true" />}
              开始审核
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto grid max-w-[1480px] gap-4 px-4 py-4 sm:px-6 xl:grid-cols-[minmax(520px,0.92fr)_minmax(520px,1.08fr)]">
        <InputWorkspace
          request={request}
          selectedRule={selectedRule}
          onField={setField}
          onNumberField={setNumberField}
        />
        <ReportWorkspace
          report={report}
          loading={loading}
          error={error}
          history={history}
          onExport={exportReport}
          onSelectHistory={openHistoryReport}
        />
      </main>
    </div>
  );
}

function InputWorkspace({ request, selectedRule, onField, onNumberField }) {
  return (
    <div className="overflow-hidden rounded-md border border-slate-200 bg-white shadow-panel">
      <div className="section">
        <div className="flex items-center justify-between gap-3">
          <div>
            <h2 className="text-sm font-semibold text-slate-900">报审信息</h2>
            <p className="mt-1 text-xs text-slate-500">项目、材料、供应商与批次</p>
          </div>
          <span className="rounded-md border border-slate-200 bg-slate-50 px-2 py-1 text-xs text-slate-600">
            {selectedRule?.displayName ?? '材料类别'}
          </span>
        </div>
        <div className="mt-4 grid gap-4 sm:grid-cols-2">
          <TextField label="项目名称" value={request.projectName} onChange={(value) => onField('projectName', value)} />
          <TextField label="材料名称" value={request.materialName} onChange={(value) => onField('materialName', value)} />
          <SelectField label="材料类别" value={request.category} onChange={(value) => onField('category', value)} options={categoryOptions} />
          <TextField label="规格型号" value={request.specification} onChange={(value) => onField('specification', value)} />
          <TextField label="供应商" value={request.supplier} onChange={(value) => onField('supplier', value)} />
          <TextField label="品牌" value={request.brand} onChange={(value) => onField('brand', value)} />
          <TextField label="批次/批号" value={request.batchNo} onChange={(value) => onField('batchNo', value)} />
          <TextField label="单位" value={request.unit} onChange={(value) => onField('unit', value)} />
        </div>
      </div>

      <div className="section">
        <h2 className="text-sm font-semibold text-slate-900">价格与数量</h2>
        <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <NumberField label="报审单价" value={request.unitPrice} onChange={(value) => onNumberField('unitPrice', value)} />
          <NumberField label="数量" value={request.quantity} onChange={(value) => onNumberField('quantity', value)} />
          <NumberField label="预算单价" value={request.budgetUnitPrice} onChange={(value) => onNumberField('budgetUnitPrice', value)} />
          <NumberField label="历史单价" value={request.historicalUnitPrice} onChange={(value) => onNumberField('historicalUnitPrice', value)} />
        </div>
        {selectedRule && (
          <p className="mt-3 text-xs text-slate-500">
            参考区间：{selectedRule.minReferencePrice}-{selectedRule.maxReferencePrice} {selectedRule.priceUnit}
          </p>
        )}
      </div>

      <div className="section">
        <h2 className="text-sm font-semibold text-slate-900">资料与规范</h2>
        <div className="mt-4 grid gap-4 lg:grid-cols-2">
          <TagEditor
            label="已提交资料"
            value={request.providedDocuments}
            suggestions={selectedRule?.requiredDocuments ?? []}
            onChange={(value) => onField('providedDocuments', value)}
          />
          <TagEditor
            label="声明规范"
            value={request.declaredStandards}
            suggestions={selectedRule?.recommendedStandards ?? []}
            onChange={(value) => onField('declaredStandards', value)}
          />
        </div>
      </div>

      <div className="section">
        <h2 className="text-sm font-semibold text-slate-900">设计要求</h2>
        <div className="mt-4">
          <textarea
            className="textarea min-h-24"
            value={request.designRequirement}
            onChange={(event) => onField('designRequirement', event.target.value)}
            placeholder="粘贴图纸说明、招标清单、材料表或技术规格要求"
          />
        </div>
      </div>

      <div className="bg-white px-4 py-4 sm:px-5">
        <h2 className="text-sm font-semibold text-slate-900">报审文本</h2>
        <div className="mt-4">
          <textarea
            className="textarea min-h-44"
            value={request.submittedText}
            onChange={(event) => onField('submittedText', event.target.value)}
            placeholder="粘贴检测报告摘要、合格证信息、材料报审单正文或OCR文本"
          />
        </div>
      </div>
    </div>
  );
}

function ReportWorkspace({ report, loading, error, history, onExport, onSelectHistory }) {
  if (loading) {
    return (
      <div className="flex min-h-[620px] items-center justify-center rounded-md border border-slate-200 bg-white shadow-panel">
        <div className="text-center">
          <Loader2 className="mx-auto animate-spin text-cyan-700" size={34} aria-hidden="true" />
          <p className="mt-3 text-sm font-medium text-slate-700">Agent 正在审核</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="space-y-4">
        <div className="rounded-md border border-red-200 bg-red-50 p-5 text-red-800 shadow-panel">
          <div className="flex items-center gap-2 text-sm font-semibold">
            <CircleAlert size={18} aria-hidden="true" />
            {error}
          </div>
        </div>
        <HistoryPanel history={history} onSelect={onSelectHistory} />
      </div>
    );
  }

  if (!report) {
    return (
      <div className="space-y-4">
        <div className="flex min-h-[430px] items-center justify-center rounded-md border border-dashed border-slate-300 bg-white p-8 text-center shadow-panel">
          <div>
            <ShieldAlert className="mx-auto text-slate-400" size={42} aria-hidden="true" />
            <h2 className="mt-3 text-base font-semibold text-slate-800">等待审核</h2>
            <p className="mt-2 max-w-md text-sm leading-6 text-slate-500">
              填写左侧材料信息后开始审核，结果会显示风险等级、成本信号、整改建议和 Agent 执行链路。
            </p>
          </div>
        </div>
        <HistoryPanel history={history} onSelect={onSelectHistory} />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="rounded-md border border-slate-200 bg-white shadow-panel">
        <div className="border-b border-slate-200 px-4 py-4 sm:px-5">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <RiskBadge riskLevel={report.riskLevel} />
                <span className="rounded-md border border-slate-200 bg-slate-50 px-2 py-1 text-xs text-slate-600">
                  {report.reportId}
                </span>
              </div>
              <h2 className="mt-3 text-xl font-semibold text-slate-950">{report.conclusion}</h2>
              <p className="mt-2 text-sm leading-6 text-slate-600">{report.executiveSummary}</p>
            </div>
            <button
              type="button"
              onClick={onExport}
              className="inline-flex h-10 items-center justify-center gap-2 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
            >
              <Download size={17} aria-hidden="true" />
              导出
            </button>
          </div>
        </div>
        <div className="grid gap-0 divide-y divide-slate-200 md:grid-cols-3 md:divide-x md:divide-y-0">
          <Metric title="风险分" value={`${report.riskScore}/100`} icon={ShieldAlert} />
          <Metric title="本批金额" value={`${formatMoney(report.cost.totalAmount)} 元`} icon={WalletCards} />
          <Metric title="审核发现" value={`${report.findings.length} 项`} icon={ClipboardList} />
        </div>
      </div>

      <div className="grid gap-4 2xl:grid-cols-[1.2fr_0.8fr]">
        <FindingsPanel findings={report.findings} />
        <CostPanel cost={report.cost} />
      </div>

      <div className="grid gap-4 2xl:grid-cols-2">
        <ActionPanel title="人工复核点" items={report.requiredManualChecks} icon={FileSearch} />
        <ActionPanel title="下一步动作" items={report.nextActions} icon={CheckCircle2} />
      </div>

      <TracePanel trace={report.trace} />
      <HistoryPanel history={history} onSelect={onSelectHistory} />
    </div>
  );
}

function TextField({ label, value, onChange }) {
  return (
    <label>
      <span className="field-label">{label}</span>
      <input className="input" value={value} onChange={(event) => onChange(event.target.value)} />
    </label>
  );
}

function NumberField({ label, value, onChange }) {
  return (
    <label>
      <span className="field-label">{label}</span>
      <input
        className="input"
        type="number"
        min="0"
        step="0.01"
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
    </label>
  );
}

function SelectField({ label, value, onChange, options }) {
  return (
    <label>
      <span className="field-label">{label}</span>
      <select className="input" value={value} onChange={(event) => onChange(event.target.value)}>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  );
}

function TagEditor({ label, value, suggestions, onChange }) {
  const [draft, setDraft] = useState('');

  const addTag = (tag) => {
    const normalized = tag.trim();
    if (!normalized || value.includes(normalized)) return;
    onChange([...value, normalized]);
    setDraft('');
  };

  const removeTag = (tag) => {
    onChange(value.filter((item) => item !== tag));
  };

  return (
    <div>
      <span className="field-label">{label}</span>
      <div className="min-h-24 rounded-md border border-slate-300 bg-white p-2">
        <div className="flex flex-wrap gap-2">
          {value.map((tag) => (
            <span
              key={tag}
              className="inline-flex items-center gap-1 rounded-md border border-slate-200 bg-slate-50 px-2 py-1 text-xs text-slate-700"
            >
              {tag}
              <button type="button" onClick={() => removeTag(tag)} className="text-slate-400 hover:text-slate-700" aria-label={`移除 ${tag}`}>
                <X size={13} aria-hidden="true" />
              </button>
            </span>
          ))}
        </div>
        <div className="mt-2 flex gap-2">
          <input
            className="h-8 min-w-0 flex-1 rounded-md border border-slate-200 px-2 text-xs outline-none focus:border-cyan-600 focus:ring-2 focus:ring-cyan-100"
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.preventDefault();
                addTag(draft);
              }
            }}
          />
          <button
            type="button"
            onClick={() => addTag(draft)}
            className="inline-flex h-8 w-8 items-center justify-center rounded-md bg-slate-800 text-white transition hover:bg-slate-950"
            aria-label="添加"
          >
            <Plus size={15} aria-hidden="true" />
          </button>
        </div>
      </div>
      {suggestions.length > 0 && (
        <div className="mt-2 flex flex-wrap gap-2">
          {suggestions.map((item) => (
            <button
              key={item}
              type="button"
              onClick={() => addTag(item)}
              className="rounded-md border border-cyan-200 bg-cyan-50 px-2 py-1 text-xs text-cyan-800 transition hover:bg-cyan-100"
            >
              {item}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

function RiskBadge({ riskLevel }) {
  const className =
    riskLevel === '重大风险'
      ? 'border-red-200 bg-red-50 text-red-800'
      : riskLevel === '高风险'
        ? 'border-orange-200 bg-orange-50 text-orange-800'
        : riskLevel === '中风险'
          ? 'border-amber-200 bg-amber-50 text-amber-800'
          : 'border-emerald-200 bg-emerald-50 text-emerald-800';

  return <span className={`rounded-md border px-2 py-1 text-xs font-semibold ${className}`}>{riskLevel}</span>;
}

function Metric({ title, value, icon: Icon }) {
  return (
    <div className="flex items-center gap-3 px-4 py-4 sm:px-5">
      <div className="flex h-9 w-9 items-center justify-center rounded-md bg-slate-100 text-slate-700">
        <Icon size={18} aria-hidden="true" />
      </div>
      <div>
        <p className="text-xs text-slate-500">{title}</p>
        <p className="mt-1 text-base font-semibold text-slate-950">{value}</p>
      </div>
    </div>
  );
}

function FindingsPanel({ findings }) {
  return (
    <section className="rounded-md border border-slate-200 bg-white shadow-panel">
      <PanelTitle icon={ShieldAlert} title="审核发现" />
      <div className="divide-y divide-slate-200">
        {findings.map((finding, index) => {
          const config = severityConfig[finding.severity] ?? severityConfig.Low;
          const Icon = config.icon;
          return (
            <article key={`${finding.title}-${index}`} className="px-4 py-4 sm:px-5">
              <div className="flex flex-wrap items-center gap-2">
                <span className={`inline-flex items-center gap-1 rounded-md border px-2 py-1 text-xs font-semibold ${config.className}`}>
                  <Icon size={13} aria-hidden="true" />
                  {config.label}
                </span>
                <span className="text-xs text-slate-500">{finding.ownerAgent}</span>
              </div>
              <h3 className="mt-3 text-sm font-semibold text-slate-950">{finding.title}</h3>
              <p className="mt-2 text-sm leading-6 text-slate-600">{finding.evidence}</p>
              <p className="mt-2 text-sm leading-6 text-slate-800">{finding.recommendation}</p>
            </article>
          );
        })}
      </div>
    </section>
  );
}

function CostPanel({ cost }) {
  return (
    <section className="rounded-md border border-slate-200 bg-white shadow-panel">
      <PanelTitle icon={WalletCards} title="成本分析" />
      <div className="px-4 py-4 sm:px-5">
        <p className="text-sm leading-6 text-slate-600">{cost.summary}</p>
        <div className="mt-4 grid gap-3 sm:grid-cols-2">
          <SmallMetric title="预算偏差" value={formatPercent(cost.budgetVarianceRate)} />
          <SmallMetric title="历史偏差" value={formatPercent(cost.historicalVarianceRate)} />
        </div>
        <div className="mt-4 space-y-2">
          {cost.signals.map((signal) => (
            <div
              key={`${signal.name}-${signal.detail}`}
              className={`rounded-md border px-3 py-2 text-sm ${signalClasses[signal.level] ?? signalClasses.Info}`}
            >
              <div className="font-semibold">{signal.name}</div>
              <div className="mt-1 leading-5">{signal.detail}</div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

function SmallMetric({ title, value }) {
  return (
    <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-3">
      <p className="text-xs text-slate-500">{title}</p>
      <p className="mt-1 text-sm font-semibold text-slate-950">{value}</p>
    </div>
  );
}

function ActionPanel({ title, items, icon }) {
  const Icon = icon;
  return (
    <section className="rounded-md border border-slate-200 bg-white shadow-panel">
      <PanelTitle icon={Icon} title={title} />
      <ul className="divide-y divide-slate-200">
        {items.map((item) => (
          <li key={item} className="flex gap-3 px-4 py-3 text-sm leading-6 text-slate-700 sm:px-5">
            <CheckCircle2 className="mt-0.5 shrink-0 text-emerald-600" size={17} aria-hidden="true" />
            <span>{item}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}

function TracePanel({ trace }) {
  return (
    <section className="rounded-md border border-slate-200 bg-white shadow-panel">
      <PanelTitle icon={GitBranch} title="Agent 流程" />
      <div className="grid gap-3 px-4 py-4 sm:px-5 lg:grid-cols-4">
        {trace.map((step, index) => (
          <div key={step.agent} className="rounded-md border border-slate-200 bg-slate-50 p-3">
            <div className="flex items-center gap-2">
              <span className="flex h-6 w-6 items-center justify-center rounded-md bg-cyan-700 text-xs font-semibold text-white">
                {index + 1}
              </span>
              <h3 className="text-sm font-semibold text-slate-900">{step.agent}</h3>
            </div>
            <p className="mt-3 text-xs leading-5 text-slate-600">{step.output}</p>
          </div>
        ))}
      </div>
    </section>
  );
}

function HistoryPanel({ history, onSelect }) {
  return (
    <section className="rounded-md border border-slate-200 bg-white shadow-panel">
      <PanelTitle icon={ClipboardList} title="历史审核" />
      {history.length === 0 ? (
        <div className="px-4 py-5 text-sm text-slate-500 sm:px-5">暂无历史记录</div>
      ) : (
        <div className="divide-y divide-slate-200">
          {history.map((item) => (
            <button
              key={item.reportId}
              type="button"
              onClick={() => onSelect(item.reportId)}
              className="block w-full px-4 py-3 text-left transition hover:bg-slate-50 sm:px-5"
            >
              <div className="flex flex-wrap items-center justify-between gap-2">
                <div>
                  <p className="text-sm font-semibold text-slate-950">
                    {item.materialName || item.category}
                  </p>
                  <p className="mt-1 text-xs text-slate-500">
                    {item.projectName || '未命名项目'} · {item.supplier || '未填供应商'}
                  </p>
                </div>
                <RiskBadge riskLevel={item.riskLevel} />
              </div>
              <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-500">
                <span>{item.conclusion}</span>
                <span>{item.riskScore}/100</span>
                <span>{formatMoney(item.totalAmount)} 元</span>
                <span>{formatDateTime(item.createdAt)}</span>
              </div>
            </button>
          ))}
        </div>
      )}
    </section>
  );
}

function PanelTitle({ icon: Icon, title }) {
  return (
    <div className="flex items-center gap-2 border-b border-slate-200 px-4 py-3 sm:px-5">
      <Icon size={18} className="text-slate-700" aria-hidden="true" />
      <h2 className="text-sm font-semibold text-slate-950">{title}</h2>
    </div>
  );
}

function formatDateTime(value) {
  if (!value) return '';
  return new Date(value).toLocaleString('zh-CN', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function formatMoney(value) {
  return Number(value || 0).toLocaleString('zh-CN', {
    maximumFractionDigits: 2,
  });
}

function formatPercent(value) {
  if (value === null || value === undefined) return '未录入';
  return Number(value).toLocaleString('zh-CN', {
    style: 'percent',
    maximumFractionDigits: 1,
  });
}

function buildMarkdownReport(report) {
  const findingLines = report.findings
    .map((item, index) => `${index + 1}. [${item.severity}] ${item.title}\n   - 依据：${item.evidence}\n   - 建议：${item.recommendation}`)
    .join('\n');
  const manualLines = report.requiredManualChecks.map((item) => `- ${item}`).join('\n');
  const nextLines = report.nextActions.map((item) => `- ${item}`).join('\n');
  const traceLines = report.trace.map((item, index) => `${index + 1}. ${item.agent}: ${item.output}`).join('\n');

  return `# ${report.profile.materialName || '建筑材料'}审核报告

- 报告编号：${report.reportId}
- 审核结论：${report.conclusion}
- 风险等级：${report.riskLevel}
- 风险分：${report.riskScore}/100

## 摘要

${report.executiveSummary}

## 成本分析

${report.cost.summary}

## 审核发现

${findingLines}

## 人工复核点

${manualLines}

## 下一步动作

${nextLines}

## Agent 流程

${traceLines}
`;
}

export default App;
