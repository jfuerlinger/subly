// UI primitives: logo, badges, charts, modal helpers

const { useState, useEffect, useMemo, useRef, useCallback } = React;

// --- Subscription logo (initials over brand color)
const SubLogo = ({ sub, size = 'md' }) => {
  const cls = size === 'sm' ? 'sub-logo sub-logo-sm' : size === 'lg' ? 'sub-logo sub-logo-lg' : 'sub-logo';
  const fg = sub.logo.fg || '#fff';
  const initials = sub.logo.initials || sub.vendor.slice(0, 2);
  // Apple icon: render apple glyph
  const isApple = sub.id === 'icloud';
  return (
    <div className={cls} style={{ background: sub.logo.bg, color: fg }} aria-hidden>
      {isApple
        ? <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor"><path d="M11.18 8.43c-.02-2 1.63-2.95 1.7-3-.92-1.36-2.35-1.55-2.86-1.57-1.22-.12-2.38.71-3 .71-.62 0-1.58-.7-2.6-.68-1.33.02-2.57.78-3.26 1.97-1.4 2.4-.36 5.96.99 7.91.66.96 1.46 2.05 2.5 2.02 1-.04 1.39-.65 2.6-.65s1.56.65 2.63.63c1.09-.02 1.78-.99 2.45-1.96.77-1.12 1.09-2.22 1.1-2.27-.02-.01-2.11-.81-2.13-3.21zm-1.97-5.92c.55-.66.92-1.59.82-2.51-.79.03-1.74.52-2.31 1.18-.51.58-.96 1.53-.84 2.43.88.07 1.78-.45 2.33-1.1z"/></svg>
        : <span>{initials}</span>}
    </div>
  );
};

// --- Cycle pill
const CyclePill = ({ cycle }) => (
  <span className="badge">{cycle === 'yearly' ? 'jährlich' : 'monatlich'}</span>
);

// --- Status pill
const StatusPill = ({ status }) => {
  if (status === 'active')   return <span className="badge badge-good"><span className="badge-dot"/>Aktiv</span>;
  if (status === 'paused')   return <span className="badge badge-warn"><span className="badge-dot"/>Pausiert</span>;
  if (status === 'cancelled')return <span className="badge badge-bad"><span className="badge-dot"/>Gekündigt</span>;
  return <span className="badge">{status}</span>;
};

// --- Category chip
const CategoryChip = ({ id, withDot = true }) => {
  const c = CAT_BY_ID[id]; if (!c) return null;
  return <span className="tag"><span className="tag-dot" style={{ background: c.color }}/>{c.label}</span>;
};

// --- Delta indicator
const Delta = ({ value, suffix = '%', invert = false }) => {
  if (value == null) return null;
  const up = value > 0, down = value < 0;
  const positive = invert ? down : up;
  const cls = value === 0 ? 'delta-flat' : positive ? 'delta-up' : 'delta-down';
  const symbol = value === 0 ? '·' : up ? '↑' : '↓';
  return <span className={`delta ${cls}`}>{symbol} {Math.abs(value).toLocaleString('de-DE', { minimumFractionDigits: value % 1 ? 1 : 0, maximumFractionDigits: 1 })}{suffix}</span>;
};

// --- KPI tile
const KPI = ({ label, value, sub, delta, deltaSuffix = '%', deltaInvert = false, icon, accent }) => (
  <div className="kpi">
    <div className="kpi-label">{icon && <Icon name={icon} size={13}/>} {label}</div>
    <div className="kpi-value">{value}</div>
    <div className="kpi-foot">
      {delta != null && <Delta value={delta} suffix={deltaSuffix} invert={deltaInvert}/>}
      {sub && <span>{sub}</span>}
    </div>
    {accent && <div style={{ position:'absolute', top:0, right:0, width:90, height:90, background:`radial-gradient(circle at top right, ${accent}, transparent 60%)`, opacity:.45, pointerEvents:'none' }}/>}
  </div>
);

// --- Line chart (SVG)
const LineChart = ({ data, height = 220, accent = 'var(--brand-500)', accent2 = 'rgba(91,108,255,.12)', ySuffix = '€', smooth = true }) => {
  const W = 800, H = height;
  const PAD = { l: 50, r: 16, t: 18, b: 28 };
  const values = data.map(d => d.total);
  const max = Math.max(...values) * 1.08;
  const min = Math.min(...values) * 0.92;
  const x = (i) => PAD.l + (i / (data.length - 1)) * (W - PAD.l - PAD.r);
  const y = (v) => PAD.t + (1 - (v - min) / (max - min || 1)) * (H - PAD.t - PAD.b);

  let path = '';
  if (smooth) {
    data.forEach((d, i) => {
      const px = x(i), py = y(d.total);
      if (i === 0) path = `M ${px} ${py}`;
      else {
        const pp = data[i - 1];
        const cx1 = x(i - 0.5);
        path += ` C ${cx1} ${y(pp.total)}, ${cx1} ${py}, ${px} ${py}`;
      }
    });
  } else {
    path = data.map((d, i) => (i === 0 ? 'M' : 'L') + ' ' + x(i) + ' ' + y(d.total)).join(' ');
  }
  const area = `${path} L ${x(data.length - 1)} ${H - PAD.b} L ${x(0)} ${H - PAD.b} Z`;

  // y ticks
  const ticks = 4;
  const tickVals = Array.from({ length: ticks + 1 }, (_, i) => min + (i / ticks) * (max - min));

  return (
    <div className="chart-wrap">
      <svg viewBox={`0 0 ${W} ${H}`} preserveAspectRatio="none" style={{ width: '100%', height: H }}>
        <defs>
          <linearGradient id="lc-grad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={accent} stopOpacity=".25"/>
            <stop offset="100%" stopColor={accent} stopOpacity="0"/>
          </linearGradient>
        </defs>
        {tickVals.map((tv, i) => (
          <g key={i}>
            <line x1={PAD.l} y1={y(tv)} x2={W - PAD.r} y2={y(tv)} stroke="#ECEDF2" strokeDasharray={i === 0 ? '' : '2 4'}/>
            <text x={PAD.l - 8} y={y(tv) + 4} textAnchor="end" fontSize="11" fill="#8A91A3">{Math.round(tv)}{ySuffix}</text>
          </g>
        ))}
        <path d={area} fill="url(#lc-grad)"/>
        <path d={path} fill="none" stroke={accent} strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"/>
        {data.map((d, i) => (
          <g key={i}>
            {(i === data.length - 1 || i % 2 === 0) && (
              <text x={x(i)} y={H - 8} textAnchor="middle" fontSize="11" fill="#8A91A3">{d.label}</text>
            )}
            {i === data.length - 1 && (
              <>
                <circle cx={x(i)} cy={y(d.total)} r="4.5" fill="#fff" stroke={accent} strokeWidth="2"/>
                <circle cx={x(i)} cy={y(d.total)} r="10" fill={accent} opacity=".12"/>
              </>
            )}
          </g>
        ))}
      </svg>
    </div>
  );
};

// --- Donut chart
const Donut = ({ segments, total, size = 200, thickness = 24, label, sub, onHover }) => {
  const r = size / 2;
  const inner = r - thickness;
  const c = 2 * Math.PI * (r - thickness / 2);
  const sum = segments.reduce((a, s) => a + s.value, 0);
  let acc = 0;
  return (
    <div style={{ position: 'relative', width: size, height: size }}>
      <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} style={{ transform: 'rotate(-90deg)' }}>
        <circle cx={r} cy={r} r={r - thickness / 2} fill="none" stroke="#F1F2F6" strokeWidth={thickness}/>
        {segments.map((s, i) => {
          const frac = s.value / sum;
          const dash = frac * c;
          const offset = -acc * c;
          acc += frac;
          return (
            <circle key={i} cx={r} cy={r} r={r - thickness / 2} fill="none"
                    stroke={s.color} strokeWidth={thickness}
                    strokeDasharray={`${dash} ${c - dash}`}
                    strokeDashoffset={offset}
                    style={{ transition: 'stroke-dasharray .4s ease' }}
                    onMouseEnter={() => onHover && onHover(s)}
                    onMouseLeave={() => onHover && onHover(null)}/>
          );
        })}
      </svg>
      <div style={{ position: 'absolute', inset: 0, display: 'grid', placeItems: 'center', textAlign: 'center' }}>
        <div>
          <div style={{ fontSize: 22, fontWeight: 700, letterSpacing: '-0.02em' }}>{label}</div>
          <div style={{ fontSize: 11, color: 'var(--text-3)', marginTop: 2 }}>{sub}</div>
        </div>
      </div>
    </div>
  );
};

// --- Bar chart (vertical)
const BarChart = ({ data, height = 200, ySuffix = '€' }) => {
  const W = 800, H = height, PAD = { l: 50, r: 12, t: 18, b: 28 };
  const max = Math.max(...data.map(d => d.value)) * 1.08;
  const barW = (W - PAD.l - PAD.r) / data.length * 0.6;
  const gap  = (W - PAD.l - PAD.r) / data.length * 0.4;
  const ticks = 4;
  const tickVals = Array.from({ length: ticks + 1 }, (_, i) => (i / ticks) * max);
  const y = v => PAD.t + (1 - v / max) * (H - PAD.t - PAD.b);
  return (
    <svg viewBox={`0 0 ${W} ${H}`} style={{ width: '100%', height: H }}>
      {tickVals.map((tv, i) => (
        <g key={i}>
          <line x1={PAD.l} y1={y(tv)} x2={W - PAD.r} y2={y(tv)} stroke="#ECEDF2" strokeDasharray={i === 0 ? '' : '2 4'}/>
          <text x={PAD.l - 8} y={y(tv) + 4} textAnchor="end" fontSize="11" fill="#8A91A3">{Math.round(tv)}{ySuffix}</text>
        </g>
      ))}
      {data.map((d, i) => {
        const bx = PAD.l + i * (barW + gap) + gap / 2;
        const bh = (H - PAD.t - PAD.b) - (y(d.value) - PAD.t);
        return (
          <g key={i}>
            <rect x={bx} y={y(d.value)} width={barW} height={bh} rx="4" fill={d.color || 'var(--brand-500)'} opacity={d.dim ? .4 : 1}/>
            <text x={bx + barW / 2} y={H - 8} textAnchor="middle" fontSize="11" fill="#8A91A3">{d.label}</text>
          </g>
        );
      })}
    </svg>
  );
};

// --- Modal
const Modal = ({ open, onClose, children, maxWidth = 540 }) => {
  useEffect(() => {
    if (!open) return;
    const onKey = e => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [open, onClose]);
  if (!open) return null;
  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" style={{ maxWidth }} onClick={e => e.stopPropagation()}>{children}</div>
    </div>
  );
};

// --- Side panel (right drawer)
const SidePanel = ({ open, onClose, children, width = 480 }) => {
  useEffect(() => {
    if (!open) return;
    const onKey = e => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [open, onClose]);
  if (!open) return null;
  return (
    <>
      <div className="panel-backdrop" onClick={onClose}/>
      <div className="panel" style={{ width }}>{children}</div>
    </>
  );
};

// Aggregations helpers used by multiple screens
function aggregate(subs) {
  const monthly = subs.filter(s => s.status === 'active').reduce((a, s) => a + monthlyEquivalent(s), 0);
  const yearly  = subs.filter(s => s.status === 'active').reduce((a, s) => a + yearlyEquivalent(s), 0);
  const perCat = {};
  CATEGORIES.forEach(c => perCat[c.id] = 0);
  subs.filter(s => s.status === 'active').forEach(s => perCat[s.category] += monthlyEquivalent(s));
  return { monthly, yearly, perCat };
}

Object.assign(window, {
  SubLogo, CyclePill, StatusPill, CategoryChip, Delta, KPI,
  LineChart, Donut, BarChart, Modal, SidePanel, aggregate
});
