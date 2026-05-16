// Other screens: Calendar, Analytics, Categories, Settings, AddModal

// ---------- Calendar ----------
const CalendarScreen = ({ subs, onOpenSub }) => {
  const [cursor, setCursor] = useState(new Date(TODAY.getFullYear(), TODAY.getMonth(), 1));
  const monthLabel = new Intl.DateTimeFormat('de-DE', { month: 'long', year: 'numeric' }).format(cursor);

  // Generate cells
  const firstDay = new Date(cursor.getFullYear(), cursor.getMonth(), 1);
  const lastDay  = new Date(cursor.getFullYear(), cursor.getMonth() + 1, 0);
  const startOffset = (firstDay.getDay() + 6) % 7; // Monday-first
  const cells = [];
  for (let i = 0; i < startOffset; i++) {
    const d = new Date(firstDay); d.setDate(d.getDate() - (startOffset - i));
    cells.push({ date: d, muted: true });
  }
  for (let d = 1; d <= lastDay.getDate(); d++) cells.push({ date: new Date(cursor.getFullYear(), cursor.getMonth(), d), muted: false });
  while (cells.length % 7 !== 0) {
    const last = cells[cells.length - 1].date; const d = new Date(last); d.setDate(d.getDate() + 1);
    cells.push({ date: d, muted: true });
  }

  // Events: for each active subscription, render its payment on its day if within month
  const events = {};
  subs.filter(s => s.status === 'active').forEach(s => {
    const pay = new Date(s.nextPayment);
    // also recur backward/forward within the visible month range
    const expand = (start) => {
      const d = new Date(start);
      // walk back up to 24 months
      for (let i = 0; i < 24; i++) {
        if (d.getFullYear() === cursor.getFullYear() && d.getMonth() === cursor.getMonth()) {
          const key = d.toISOString().slice(0, 10);
          (events[key] = events[key] || []).push(s);
          return;
        }
        if (s.cycle === 'yearly') d.setFullYear(d.getFullYear() - 1);
        else d.setMonth(d.getMonth() - 1);
      }
    };
    expand(pay);
  });

  const monthTotal = Object.values(events).flat().reduce((a, s) => a + s.price, 0);
  const todayKey = TODAY.toISOString().slice(0, 10);

  return (
    <div className="stack">
      <div className="card card-pad" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
        <div className="row" style={{ gap: 14 }}>
          <button className="btn btn-icon" onClick={() => setCursor(new Date(cursor.getFullYear(), cursor.getMonth() - 1, 1))}><Icon name="chevron-left" size={16}/></button>
          <div style={{ fontWeight: 700, fontSize: 18, letterSpacing: '-0.01em', textTransform: 'capitalize', minWidth: 180 }}>{monthLabel}</div>
          <button className="btn btn-icon" onClick={() => setCursor(new Date(cursor.getFullYear(), cursor.getMonth() + 1, 1))}><Icon name="chevron-right" size={16}/></button>
          <button className="btn btn-sm" onClick={() => setCursor(new Date(TODAY.getFullYear(), TODAY.getMonth(), 1))}>Heute</button>
        </div>
        <div className="row" style={{ gap: 18 }}>
          <div><div className="muted" style={{ fontSize: 11 }}>Zahlungen im Monat</div><div style={{ fontWeight: 700 }} className="num">{Object.values(events).flat().length}</div></div>
          <div><div className="muted" style={{ fontSize: 11 }}>Summe Monat</div><div style={{ fontWeight: 700 }} className="num">{fmtEUR(monthTotal)}</div></div>
        </div>
      </div>

      <div className="card card-pad">
        <div className="cal-grid">
          {['Mo','Di','Mi','Do','Fr','Sa','So'].map(d => <div key={d} className="cal-h">{d}</div>)}
          {cells.map((c, i) => {
            const key = c.date.toISOString().slice(0, 10);
            const evs = events[key] || [];
            const isToday = key === todayKey;
            return (
              <div key={i} className={`cal-cell ${c.muted ? 'muted' : ''} ${isToday ? 'today' : ''}`}>
                <div className="cal-date">{c.date.getDate()}</div>
                {evs.slice(0, 3).map(s => (
                  <div key={s.id} className="cal-event clickable" style={{ color: CAT_BY_ID[s.category].color }} onClick={() => onOpenSub(s)} title={`${s.name} — ${fmtEUR(s.price)}`}>
                    <SubLogo sub={s} size="sm" style={{ width: 14, height: 14, fontSize: 8 }}/>
                    <span className="truncate" style={{ color: 'var(--text)', flex: 1 }}>{s.vendor}</span>
                    <span className="num">{fmtEUR(s.price, { cents: false })}</span>
                  </div>
                ))}
                {evs.length > 3 && <div className="cal-more">+ {evs.length - 3} weitere</div>}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};

// ---------- Analytics ----------
const Analytics = ({ subs }) => {
  const active = subs.filter(s => s.status === 'active');
  const agg = aggregate(subs);
  const history = useMemo(() => simulatedMonthly(24), []);
  const last = history[history.length - 1].total;
  const prev = history[history.length - 13].total; // 12 mo ago
  const yoy = ((last - prev) / prev) * 100;

  // Forecast: next 12 months at current rate, with seasonal +energy +5% in winter
  const forecast = [];
  for (let i = 1; i <= 12; i++) {
    const d = new Date(TODAY.getFullYear(), TODAY.getMonth() + i, 1);
    const winter = [11, 0, 1].includes(d.getMonth());
    const factor = winter ? 1.05 : 1.0;
    forecast.push({ label: new Intl.DateTimeFormat('de-DE', { month: 'short' }).format(d), total: last * factor, monthIdx: d.getMonth() });
  }
  const forecastTotal = forecast.reduce((a, x) => a + x.total, 0);

  // Cost per category bars
  const catBars = CATEGORIES.map(c => ({
    label: c.label.split(' ')[0],
    value: agg.perCat[c.id],
    color: c.color,
  })).filter(b => b.value > 0).sort((a, b) => b.value - a.value);

  // Most expensive yearly
  const topYear = active.slice().sort((a, b) => yearlyEquivalent(b) - yearlyEquivalent(a)).slice(0, 5);

  // Price increases
  const increases = active
    .map(s => ({ s, delta: s.priceHistory && s.priceHistory.length >= 2 ? s.price - s.priceHistory[s.priceHistory.length - 2] : 0, base: s.priceHistory && s.priceHistory.length >= 2 ? s.priceHistory[s.priceHistory.length - 2] : s.price }))
    .filter(x => x.delta > 0)
    .sort((a, b) => (b.delta / b.base) - (a.delta / a.base));

  // Cost-per-use estimate
  const usageMap = { high: 28, medium: 10, low: 2, none: 0 };
  const cpu = active.map(s => ({
    s, perUse: monthlyEquivalent(s) / (usageMap[s.usage] || 1),
    usesPerMonth: usageMap[s.usage] || 0,
  })).sort((a, b) => b.perUse - a.perUse);

  return (
    <div className="stack">
      <div className="kpi-grid">
        <KPI label="Ø monatlich (12M)" icon="chart"
             value={fmtEUR(history.slice(-12).reduce((a,h)=>a+h.total,0) / 12, { cents: false })}
             delta={parseFloat(yoy.toFixed(1))} deltaInvert sub="vs. Vorjahr"/>
        <KPI label="Höchster Monat" icon="trend-up"
             value={fmtEUR(Math.max(...history.slice(-12).map(h => h.total)), { cents: false })}
             sub="im letzten Jahr"/>
        <KPI label="Niedrigster Monat" icon="trend-down"
             value={fmtEUR(Math.min(...history.slice(-12).map(h => h.total)), { cents: false })}
             sub="im letzten Jahr"/>
        <KPI label="Forecast 12M" icon="sparkles"
             value={fmtEUR(forecastTotal, { cents: false })}
             sub="bei aktuellem Stand"/>
      </div>

      <div className="card">
        <div className="card-head">
          <div>
            <div className="card-title"><Icon name="chart" size={14}/> 24 Monate · Verlauf & Forecast</div>
            <div className="card-sub">Tatsächliche Belastung + 12-Monats Hochrechnung</div>
          </div>
        </div>
        <div style={{ padding: '4px 12px 12px' }}>
          <LineChart data={[...history, ...forecast].map(h => ({ label: h.label, total: h.total }))} height={260} accent="#5B6CFF"/>
        </div>
        <div className="chart-legend">
          <div><span className="dot" style={{ background: '#5B6CFF' }}/>Tatsächlich (24M)</div>
          <div><span className="dot" style={{ background: '#5B6CFF', opacity:.3 }}/>Forecast (12M)</div>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 1fr', gap: 16 }}>
        <div className="card">
          <div className="card-head">
            <div className="card-title"><Icon name="tag" size={14}/> Ausgaben nach Kategorie</div>
          </div>
          <div style={{ padding: '8px 12px 16px' }}>
            <BarChart data={catBars} height={240} ySuffix="€"/>
          </div>
        </div>
        <div className="card">
          <div className="card-head">
            <div className="card-title"><Icon name="trend-up" size={14}/> Größte Kostentreiber (jährlich)</div>
          </div>
          <div style={{ padding: '12px 16px 16px' }}>
            {topYear.map(s => {
              const yr = yearlyEquivalent(s);
              const pct = (yr / aggregate(subs).yearly) * 100;
              return (
                <div key={s.id} style={{ padding: '8px 0', borderBottom: '1px solid var(--border)' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
                    <div className="row"><SubLogo sub={s} size="sm"/><span style={{ fontWeight: 600 }}>{s.name}</span></div>
                    <div className="num" style={{ fontWeight: 700 }}>{fmtEUR(yr, { cents: false })}<span className="muted" style={{ fontWeight: 400, fontSize: 11 }}> /Jahr</span></div>
                  </div>
                  <div className="progress"><span style={{ width: pct + '%', background: CAT_BY_ID[s.category].color }}/></div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        <div className="card">
          <div className="card-head">
            <div className="card-title"><Icon name="warn" size={14}/> Preiserhöhungen</div>
            <div className="card-sub">{increases.length} Abos teurer geworden</div>
          </div>
          <div style={{ padding: '8px 0 8px' }}>
            <table className="tbl">
              <thead><tr><th>Abo</th><th className="tbl-numeric">Alt</th><th className="tbl-numeric">Neu</th><th className="tbl-numeric">Δ</th></tr></thead>
              <tbody>
                {increases.map(({ s, delta, base }) => (
                  <tr key={s.id}>
                    <td><div className="row"><SubLogo sub={s} size="sm"/><span style={{ fontWeight: 500 }}>{s.name}</span></div></td>
                    <td className="tbl-numeric muted num">{fmtEUR(base)}</td>
                    <td className="tbl-numeric num" style={{ fontWeight: 600 }}>{fmtEUR(s.price)}</td>
                    <td className="tbl-numeric"><Delta value={parseFloat(((delta / base) * 100).toFixed(1))} invert/></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <div className="card">
          <div className="card-head">
            <div className="card-title"><Icon name="info" size={14}/> Cost-per-Use</div>
            <div className="card-sub">geschätzte Nutzung pro Monat</div>
          </div>
          <div style={{ padding: '8px 0 8px' }}>
            <table className="tbl">
              <thead><tr><th>Abo</th><th>Nutzung</th><th className="tbl-numeric">€/Nutzung</th></tr></thead>
              <tbody>
                {cpu.slice(0, 6).map(({ s, perUse, usesPerMonth }) => (
                  <tr key={s.id}>
                    <td><div className="row"><SubLogo sub={s} size="sm"/><span style={{ fontWeight: 500 }}>{s.name}</span></div></td>
                    <td className="muted">{usesPerMonth > 0 ? `~${usesPerMonth}× / Monat` : '—'}</td>
                    <td className="tbl-numeric num" style={{ fontWeight: 600, color: perUse > 10 ? 'var(--bad)' : 'var(--text)' }}>{usesPerMonth ? fmtEUR(perUse) : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
};

// ---------- Categories ----------
const CategoriesScreen = ({ subs, onOpenSub }) => {
  const agg = aggregate(subs);
  return (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: 16 }}>
      {CATEGORIES.map(c => {
        const items = subs.filter(s => s.category === c.id);
        const monthly = items.filter(s => s.status === 'active').reduce((a, s) => a + monthlyEquivalent(s), 0);
        const share = (monthly / agg.monthly) * 100;
        return (
          <div key={c.id} className="card">
            <div className="card-pad" style={{ borderBottom: '1px solid var(--border)' }}>
              <div className="row" style={{ justifyContent: 'space-between' }}>
                <div className="row">
                  <div style={{ width: 36, height: 36, borderRadius: 10, background: c.color, opacity: .15 }}/>
                  <div style={{ marginLeft: -36, width: 36, height: 36, display: 'grid', placeItems: 'center', color: c.color }}>
                    <Icon name={c.icon} size={18}/>
                  </div>
                  <div className="col" style={{ gap: 0, marginLeft: 4 }}>
                    <div style={{ fontWeight: 600 }}>{c.label}</div>
                    <div className="muted" style={{ fontSize: 11 }}>{items.length} Abo{items.length !== 1 ? 's' : ''}</div>
                  </div>
                </div>
                <div className="col" style={{ alignItems: 'flex-end', gap: 0 }}>
                  <div className="num" style={{ fontWeight: 700, fontSize: 18 }}>{fmtEUR(monthly, { cents: false })}</div>
                  <div className="muted" style={{ fontSize: 11 }}>{share.toFixed(1)}% · {fmtEUR(monthly * 12, { cents: false })}/J</div>
                </div>
              </div>
              <div className="progress" style={{ marginTop: 10 }}><span style={{ width: share + '%', background: c.color }}/></div>
            </div>
            <div style={{ padding: 8 }}>
              {items.length === 0 && <div className="muted" style={{ padding: 12, fontSize: 12 }}>Keine Abos in dieser Kategorie</div>}
              {items.map(s => (
                <div key={s.id} className="clickable" onClick={() => onOpenSub(s)} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '8px 10px', borderRadius: 8 }}>
                  <SubLogo sub={s} size="sm"/>
                  <div className="col" style={{ flex: 1, minWidth: 0, gap: 0 }}>
                    <div className="truncate" style={{ fontWeight: 500, fontSize: 13 }}>{s.name}</div>
                    <div className="muted truncate" style={{ fontSize: 11 }}>nächst. {fmtDate(s.nextPayment, { day: '2-digit', month: 'short' })}</div>
                  </div>
                  <div className="num" style={{ fontWeight: 600, fontSize: 13 }}>{fmtEUR(monthlyEquivalent(s))}<span className="muted" style={{ fontSize: 10 }}> /M</span></div>
                </div>
              ))}
            </div>
          </div>
        );
      })}
    </div>
  );
};

// ---------- Settings ----------
const SettingsScreen = () => {
  return (
    <div className="stack" style={{ maxWidth: 760 }}>
      <div className="card">
        <div className="card-pad">
          <div style={{ fontWeight: 700, fontSize: 16 }}>Konto</div>
          <div className="muted" style={{ fontSize: 12 }}>Persönliche Einstellungen</div>
        </div>
        <hr className="h-divider"/>
        <div className="card-pad stack" style={{ gap: 14 }}>
          <Field label="Name" value="Maximilian Weber"/>
          <Field label="E-Mail" value="max@weber.de"/>
          <Field label="Währung" value="Euro (€)"/>
          <Field label="Sprache" value="Deutsch"/>
          <Field label="Zeitzone" value="Europa/Berlin"/>
        </div>
      </div>

      <div className="card">
        <div className="card-pad">
          <div style={{ fontWeight: 700, fontSize: 16 }}>Benachrichtigungen</div>
          <div className="muted" style={{ fontSize: 12 }}>Wann möchtest du erinnert werden?</div>
        </div>
        <hr className="h-divider"/>
        <div className="card-pad stack" style={{ gap: 8 }}>
          <Toggle label="3 Tage vor Zahlung" subtitle="E-Mail + Push" on/>
          <Toggle label="14 Tage vor Kündigungsfrist" subtitle="E-Mail" on/>
          <Toggle label="Preiserhöhungen" subtitle="Push" on/>
          <Toggle label="Wöchentlicher Report" subtitle="E-Mail · jeden Sonntag"/>
        </div>
      </div>

      <div className="card">
        <div className="card-pad">
          <div style={{ fontWeight: 700, fontSize: 16 }}>Verbundene Konten</div>
          <div className="muted" style={{ fontSize: 12 }}>Automatischer Import von Buchungen</div>
        </div>
        <hr className="h-divider"/>
        <div className="card-pad stack" style={{ gap: 10 }}>
          <ConnAccount name="Sparkasse Berlin" sub="DE89 •• 1234" status="connected"/>
          <ConnAccount name="DKB" sub="DE12 •• 4421" status="connected"/>
          <ConnAccount name="PayPal" sub="max@weber.de" status="connected"/>
          <button className="btn btn-sm" style={{ alignSelf: 'flex-start' }}><Icon name="plus" size={14}/> Konto verbinden</button>
        </div>
      </div>
    </div>
  );
};
const Field = ({ label, value }) => (
  <div style={{ display: 'grid', gridTemplateColumns: '180px 1fr', gap: 16, alignItems: 'center' }}>
    <div className="muted" style={{ fontSize: 13 }}>{label}</div>
    <input className="input" defaultValue={value}/>
  </div>
);
const Toggle = ({ label, subtitle, on }) => {
  const [v, setV] = useState(!!on);
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid var(--border)' }}>
      <div><div style={{ fontWeight: 500 }}>{label}</div><div className="muted" style={{ fontSize: 12 }}>{subtitle}</div></div>
      <button onClick={() => setV(!v)} style={{ width: 38, height: 22, borderRadius: 999, background: v ? 'var(--brand-600)' : 'var(--border-strong)', position: 'relative', transition: 'all .15s' }}>
        <span style={{ position: 'absolute', top: 2, left: v ? 18 : 2, width: 18, height: 18, borderRadius: '50%', background: '#fff', transition: 'all .15s', boxShadow: '0 1px 3px rgba(0,0,0,.2)' }}/>
      </button>
    </div>
  );
};
const ConnAccount = ({ name, sub, status }) => (
  <div style={{ display: 'flex', alignItems: 'center', gap: 12, padding: '10px 0', borderBottom: '1px solid var(--border)' }}>
    <div style={{ width: 32, height: 32, borderRadius: 8, background: 'var(--surface-2)', display: 'grid', placeItems: 'center' }}><Icon name="card" size={16}/></div>
    <div className="col" style={{ flex: 1, gap: 0 }}>
      <div style={{ fontWeight: 500 }}>{name}</div>
      <div className="muted" style={{ fontSize: 11 }}>{sub}</div>
    </div>
    <span className="badge badge-good"><span className="badge-dot"/>verbunden</span>
    <button className="btn btn-sm btn-ghost"><Icon name="menu-dots-v" size={14}/></button>
  </div>
);

// ---------- Add Modal (wizard) ----------
const AddSubModal = ({ open, onClose, onAdd }) => {
  const [step, setStep] = useState(0);
  const [draft, setDraft] = useState({
    name: '', vendor: '', category: 'streaming', cycle: 'monthly', price: '',
    nextPayment: TODAY.toISOString().slice(0, 10), paymentMethod: 'Visa •• 4421', tags: [],
  });

  useEffect(() => { if (open) { setStep(0); setDraft(d => ({ ...d, name: '', vendor: '', price: '' })); } }, [open]);

  const presets = [
    { name: 'Netflix', vendor: 'Netflix', category: 'streaming', logo: { bg: '#E50914', initials: 'N' } },
    { name: 'Spotify', vendor: 'Spotify', category: 'streaming', logo: { bg: '#1DB954', initials: 'S' } },
    { name: 'Apple One', vendor: 'Apple',  category: 'streaming', logo: { bg: '#000', initials: '' } },
    { name: 'Disney+',  vendor: 'Disney+', category: 'streaming', logo: { bg: '#1A2A6E', initials: 'D+' } },
    { name: 'YouTube Premium', vendor: 'YouTube', category: 'streaming', logo: { bg: '#FF0033', initials: 'Yt' } },
    { name: 'ChatGPT Plus', vendor: 'OpenAI', category: 'software', logo: { bg: '#10A37F', initials: 'AI' } },
    { name: 'Notion',   vendor: 'Notion',  category: 'software', logo: { bg: '#000', initials: 'N' } },
    { name: 'Allianz Versicherung', vendor: 'Allianz', category: 'insurance', logo: { bg: '#0033A1', initials: 'A' } },
    { name: 'Telekom',  vendor: 'Telekom',  category: 'telecom', logo: { bg: '#E20074', initials: 'T' } },
  ];

  const filteredPresets = presets.filter(p => (p.name + p.vendor).toLowerCase().includes(draft.name.toLowerCase()));

  return (
    <Modal open={open} onClose={onClose} maxWidth={560}>
      <div className="modal-head">
        <div>
          <div style={{ fontWeight: 700, fontSize: 18 }}>Neues Abo hinzufügen</div>
          <div className="muted" style={{ fontSize: 12 }}>Schritt {step + 1} von 3</div>
        </div>
        <button className="btn btn-icon btn-ghost" onClick={onClose}><Icon name="close" size={18}/></button>
      </div>

      <div style={{ padding: '0 24px 0' }}>
        <div style={{ display: 'flex', gap: 6, marginBottom: 16 }}>
          {[0,1,2].map(i => <div key={i} style={{ flex: 1, height: 3, borderRadius: 999, background: i <= step ? 'var(--brand-500)' : 'var(--border)' }}/>)}
        </div>
      </div>

      <div className="modal-body" style={{ paddingTop: 4 }}>
        {step === 0 && (
          <div className="stack">
            <div>
              <div style={{ fontWeight: 600, marginBottom: 8 }}>Welches Abo?</div>
              <div className="input-with-icon">
                <Icon name="search" size={16}/>
                <input className="input" style={{ width: '100%' }} placeholder="z. B. Netflix, Versicherung, Notion…" autoFocus value={draft.name} onChange={e => setDraft(d => ({ ...d, name: e.target.value }))}/>
              </div>
            </div>
            <div>
              <div className="muted" style={{ fontSize: 12, marginBottom: 8 }}>Vorschläge</div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 8 }}>
                {filteredPresets.slice(0, 9).map((p, i) => (
                  <button key={i} className="card" style={{ padding: 10, display: 'flex', alignItems: 'center', gap: 8, textAlign: 'left' }} onClick={() => { setDraft(d => ({ ...d, name: p.name, vendor: p.vendor, category: p.category, logo: p.logo })); setStep(1); }}>
                    <SubLogo sub={{ logo: p.logo, vendor: p.vendor, id: p.name }} size="sm"/>
                    <div style={{ minWidth: 0 }}>
                      <div className="truncate" style={{ fontWeight: 500, fontSize: 13 }}>{p.name}</div>
                      <div className="muted truncate" style={{ fontSize: 11 }}>{CAT_BY_ID[p.category].label}</div>
                    </div>
                  </button>
                ))}
              </div>
            </div>
            <button className="btn btn-ghost btn-sm" style={{ alignSelf: 'center' }} onClick={() => setStep(1)}>Manuell anlegen <Icon name="arrow-right" size={14}/></button>
          </div>
        )}

        {step === 1 && (
          <div className="stack">
            <FormRow label="Name">
              <input className="input" style={{ width: '100%' }} value={draft.name} onChange={e => setDraft(d => ({ ...d, name: e.target.value }))}/>
            </FormRow>
            <FormRow label="Anbieter">
              <input className="input" style={{ width: '100%' }} value={draft.vendor} onChange={e => setDraft(d => ({ ...d, vendor: e.target.value }))}/>
            </FormRow>
            <FormRow label="Kategorie">
              <select className="select" style={{ width: '100%' }} value={draft.category} onChange={e => setDraft(d => ({ ...d, category: e.target.value }))}>
                {CATEGORIES.map(c => <option key={c.id} value={c.id}>{c.label}</option>)}
              </select>
            </FormRow>
            <FormRow label="Preis (€)">
              <input className="input" type="number" step="0.01" placeholder="0,00" style={{ width: '100%' }} value={draft.price} onChange={e => setDraft(d => ({ ...d, price: e.target.value }))}/>
            </FormRow>
            <FormRow label="Zyklus">
              <div className="seg" style={{ width: '100%' }}>
                <button style={{ flex: 1 }} aria-pressed={draft.cycle === 'monthly'} onClick={() => setDraft(d => ({ ...d, cycle: 'monthly' }))}>Monatlich</button>
                <button style={{ flex: 1 }} aria-pressed={draft.cycle === 'yearly'}  onClick={() => setDraft(d => ({ ...d, cycle: 'yearly' }))}>Jährlich</button>
              </div>
            </FormRow>
            <FormRow label="Nächste Zahlung">
              <input className="input" type="date" style={{ width: '100%' }} value={draft.nextPayment} onChange={e => setDraft(d => ({ ...d, nextPayment: e.target.value }))}/>
            </FormRow>
          </div>
        )}

        {step === 2 && (
          <div className="stack">
            <FormRow label="Zahlungsmittel">
              <select className="select" style={{ width: '100%' }} value={draft.paymentMethod} onChange={e => setDraft(d => ({ ...d, paymentMethod: e.target.value }))}>
                <option>Visa •• 4421</option>
                <option>Mastercard •• 0044</option>
                <option>PayPal</option>
                <option>Lastschrift</option>
                <option>Apple Pay</option>
              </select>
            </FormRow>
            <FormRow label="Erinnerung">
              <select className="select" style={{ width: '100%' }}>
                <option>3 Tage vor Zahlung</option>
                <option>1 Woche vor Zahlung</option>
                <option>14 Tage vor Zahlung</option>
                <option>Keine</option>
              </select>
            </FormRow>
            <FormRow label="Anhänge">
              <button className="btn" style={{ width: '100%', justifyContent: 'center', borderStyle: 'dashed' }}><Icon name="attach" size={14}/> Vertrag (PDF) anhängen</button>
            </FormRow>

            <div className="card" style={{ padding: 14, background: 'var(--brand-50)', borderColor: 'var(--brand-100)' }}>
              <div className="muted" style={{ fontSize: 11, textTransform: 'uppercase', letterSpacing: '.06em' }}>Zusammenfassung</div>
              <div className="row" style={{ marginTop: 8, gap: 12 }}>
                <SubLogo sub={{ logo: draft.logo || { bg: 'var(--brand-500)', initials: (draft.name || 'A')[0] }, vendor: draft.vendor || 'Abo', id: 'preview' }} size="lg"/>
                <div className="col" style={{ gap: 2 }}>
                  <div style={{ fontWeight: 700 }}>{draft.name || 'Neues Abo'}</div>
                  <div className="muted" style={{ fontSize: 12 }}>{CAT_BY_ID[draft.category]?.label}</div>
                  <div className="num" style={{ fontWeight: 600, marginTop: 4 }}>{fmtEUR(parseFloat(draft.price) || 0)}<span className="muted" style={{ fontWeight: 400 }}> / {draft.cycle === 'yearly' ? 'Jahr' : 'Monat'}</span></div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>

      <div className="modal-foot">
        <button className="btn btn-ghost" onClick={step === 0 ? onClose : () => setStep(s => s - 1)}>{step === 0 ? 'Abbrechen' : 'Zurück'}</button>
        {step < 2
          ? <button className="btn btn-primary" onClick={() => setStep(s => s + 1)}>Weiter <Icon name="arrow-right" size={14}/></button>
          : <button className="btn btn-primary" onClick={() => { onAdd(draft); onClose(); }}><Icon name="check" size={14}/> Hinzufügen</button>}
      </div>
    </Modal>
  );
};
const FormRow = ({ label, children }) => (
  <div>
    <div style={{ fontSize: 12, fontWeight: 500, color: 'var(--text-2)', marginBottom: 6 }}>{label}</div>
    {children}
  </div>
);

Object.assign(window, { CalendarScreen, Analytics, CategoriesScreen, SettingsScreen, AddSubModal });
