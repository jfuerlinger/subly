// Subscriptions list + Detail panel

const SubscriptionsList = ({ subs, onOpenSub, onAdd }) => {
  const [filter, setFilter] = useState({ q: '', cat: 'all', cycle: 'all', status: 'all' });
  const [sort, setSort] = useState({ key: 'monthly', dir: 'desc' });
  const [view, setView] = useState('table'); // 'table' | 'grid'

  const filtered = useMemo(() => {
    return subs.filter(s => {
      if (filter.q && !(s.name + ' ' + s.vendor).toLowerCase().includes(filter.q.toLowerCase())) return false;
      if (filter.cat !== 'all' && s.category !== filter.cat) return false;
      if (filter.cycle !== 'all' && s.cycle !== filter.cycle) return false;
      if (filter.status !== 'all' && s.status !== filter.status) return false;
      return true;
    }).sort((a, b) => {
      const dir = sort.dir === 'asc' ? 1 : -1;
      switch (sort.key) {
        case 'name':    return a.name.localeCompare(b.name) * dir;
        case 'next':    return (new Date(a.nextPayment) - new Date(b.nextPayment)) * dir;
        case 'price':   return (a.price - b.price) * dir;
        case 'monthly':
        default:        return (monthlyEquivalent(a) - monthlyEquivalent(b)) * dir;
      }
    });
  }, [subs, filter, sort]);

  const totalM = filtered.filter(s => s.status === 'active').reduce((a, s) => a + monthlyEquivalent(s), 0);

  const toggleSort = (key) => {
    setSort(prev => prev.key === key ? { key, dir: prev.dir === 'asc' ? 'desc' : 'asc' } : { key, dir: 'desc' });
  };
  const SortTh = ({ k, children, align }) => (
    <th onClick={() => toggleSort(k)} className="clickable" style={{ textAlign: align }}>
      <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}>{children}
        {sort.key === k && <Icon name={sort.dir === 'asc' ? 'arrow-up' : 'arrow-down'} size={12}/>}
      </span>
    </th>
  );

  return (
    <div className="stack">
      <div className="card card-pad">
        <div className="filter-bar">
          <div className="input-with-icon" style={{ flex: 1, maxWidth: 360 }}>
            <Icon name="search" size={16}/>
            <input className="input" style={{ width: '100%' }} placeholder="Abos durchsuchen…" value={filter.q} onChange={e => setFilter(f => ({ ...f, q: e.target.value }))}/>
          </div>
          <select className="select" value={filter.cat} onChange={e => setFilter(f => ({ ...f, cat: e.target.value }))}>
            <option value="all">Alle Kategorien</option>
            {CATEGORIES.map(c => <option key={c.id} value={c.id}>{c.label}</option>)}
          </select>
          <div className="seg">
            <button aria-pressed={filter.cycle === 'all'}     onClick={() => setFilter(f => ({ ...f, cycle: 'all' }))}>Alle</button>
            <button aria-pressed={filter.cycle === 'monthly'} onClick={() => setFilter(f => ({ ...f, cycle: 'monthly' }))}>Monatlich</button>
            <button aria-pressed={filter.cycle === 'yearly'}  onClick={() => setFilter(f => ({ ...f, cycle: 'yearly' }))}>Jährlich</button>
          </div>
          <div className="seg">
            <button aria-pressed={filter.status === 'all'}    onClick={() => setFilter(f => ({ ...f, status: 'all' }))}>Alle</button>
            <button aria-pressed={filter.status === 'active'} onClick={() => setFilter(f => ({ ...f, status: 'active' }))}>Aktiv</button>
            <button aria-pressed={filter.status === 'paused'} onClick={() => setFilter(f => ({ ...f, status: 'paused' }))}>Pausiert</button>
          </div>
          <div style={{ flex: 1 }}/>
          <div className="seg" title="Ansicht">
            <button aria-pressed={view === 'table'} onClick={() => setView('table')}><Icon name="list" size={14}/></button>
            <button aria-pressed={view === 'grid'}  onClick={() => setView('grid')}><Icon name="dashboard" size={14}/></button>
          </div>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 12, fontSize: 12, color: 'var(--text-3)' }}>
          <span>{filtered.length} Abos · {fmtEUR(totalM)} pro Monat · {fmtEUR(totalM * 12)} pro Jahr</span>
          <button className="btn btn-sm btn-ghost"><Icon name="share" size={14}/> Exportieren</button>
        </div>
      </div>

      {view === 'table' && (
        <div className="card" style={{ overflow: 'hidden' }}>
          <table className="tbl">
            <thead>
              <tr>
                <SortTh k="name">Abo</SortTh>
                <th>Kategorie</th>
                <th>Status</th>
                <SortTh k="price" align="right">Preis</SortTh>
                <SortTh k="monthly" align="right">Monatlich</SortTh>
                <SortTh k="next">Nächste Zahlung</SortTh>
                <th>Vertrag</th>
                <th style={{ width: 40 }}></th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(s => (
                <tr key={s.id} className="clickable" onClick={() => onOpenSub(s)}>
                  <td>
                    <div className="row">
                      <SubLogo sub={s}/>
                      <div className="col" style={{ gap: 0, minWidth: 0 }}>
                        <div style={{ fontWeight: 600 }}>{s.name}</div>
                        <div className="muted" style={{ fontSize: 11 }}>{s.vendor} · seit {fmtDate(s.startedAt, { month:'short', year:'numeric' })}</div>
                      </div>
                    </div>
                  </td>
                  <td><CategoryChip id={s.category}/></td>
                  <td><StatusPill status={s.status}/></td>
                  <td className="tbl-numeric num" style={{ fontWeight: 600 }}>{fmtEUR(s.price)}<div className="muted" style={{ fontSize: 11, fontWeight: 400 }}>{s.cycle === 'yearly' ? '/ Jahr' : '/ Monat'}</div></td>
                  <td className="tbl-numeric num">{fmtEUR(monthlyEquivalent(s))}</td>
                  <td>
                    <div className="num" style={{ fontWeight: 500 }}>{fmtDate(s.nextPayment)}</div>
                    <div className="muted" style={{ fontSize: 11 }}>in {daysUntil(s.nextPayment)} Tagen</div>
                  </td>
                  <td className="muted" style={{ fontSize: 12 }}>{s.contract.minTerm}</td>
                  <td><button className="btn btn-sm btn-ghost btn-icon" onClick={e => { e.stopPropagation(); onOpenSub(s); }}><Icon name="menu-dots-v" size={16}/></button></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {view === 'grid' && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))', gap: 12 }}>
          {filtered.map(s => (
            <div key={s.id} className="card card-pad clickable" onClick={() => onOpenSub(s)} style={{ position: 'relative' }}>
              <div className="row" style={{ justifyContent: 'space-between' }}>
                <SubLogo sub={s} size="lg"/>
                <StatusPill status={s.status}/>
              </div>
              <div style={{ marginTop: 14, fontWeight: 600, fontSize: 15 }}>{s.name}</div>
              <div className="muted" style={{ fontSize: 12 }}>{CAT_BY_ID[s.category].label}</div>
              <div style={{ marginTop: 14, display: 'flex', alignItems: 'baseline', gap: 6 }}>
                <span className="num" style={{ fontWeight: 700, fontSize: 22, letterSpacing: '-0.02em' }}>{fmtEUR(s.price)}</span>
                <span className="muted" style={{ fontSize: 12 }}>{s.cycle === 'yearly' ? '/ Jahr' : '/ Monat'}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 12, paddingTop: 12, borderTop: '1px solid var(--border)', fontSize: 12 }}>
                <span className="muted">Nächste Zahlung</span>
                <span className="num">{fmtDate(s.nextPayment, { day:'2-digit', month:'short' })} · in {daysUntil(s.nextPayment)}T</span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

// --- Detail panel
const SubDetail = ({ sub, onClose, onUpdate }) => {
  if (!sub) return null;
  const cat = CAT_BY_ID[sub.category];
  const mo = monthlyEquivalent(sub);
  const yr = yearlyEquivalent(sub);
  const daily = mo * 12 / 365;

  // Cost so far (since started)
  const months = Math.max(1, Math.round(daysBetween(sub.startedAt, TODAY) / 30.44));
  const spentSoFar = mo * months;

  // History bar chart from priceHistory
  const phData = (sub.priceHistory || [sub.price]).map((p, i, arr) => ({
    label: i === arr.length - 1 ? 'Jetzt' : `${arr.length - 1 - i}× vorher`,
    value: p, color: cat.color,
  }));

  const fmtUsed = (d) => d === '—' ? 'n/a' : `${fmtDate(d, { day: '2-digit', month: 'short', year: 'numeric' })}`;

  const setStatus = (status) => onUpdate({ ...sub, status });

  return (
    <>
      <div style={{ padding: '16px 20px', borderBottom: '1px solid var(--border)', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div className="row">
          <SubLogo sub={sub} size="lg"/>
          <div className="col" style={{ gap: 2 }}>
            <div style={{ fontWeight: 700, fontSize: 16 }}>{sub.name}</div>
            <div className="row" style={{ gap: 8 }}>
              <CategoryChip id={sub.category}/>
              <StatusPill status={sub.status}/>
            </div>
          </div>
        </div>
        <button className="btn btn-icon btn-ghost" onClick={onClose}><Icon name="close" size={18}/></button>
      </div>

      <div style={{ overflow: 'auto', padding: 20, flex: 1 }}>
        {/* Headline */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 10 }}>
          <div className="kpi" style={{ padding: 14 }}>
            <div className="kpi-label">Preis</div>
            <div style={{ fontSize: 20, fontWeight: 700, marginTop: 4 }}>{fmtEUR(sub.price)}</div>
            <div className="muted" style={{ fontSize: 11 }}>{sub.cycle === 'yearly' ? 'pro Jahr' : 'pro Monat'}</div>
          </div>
          <div className="kpi" style={{ padding: 14 }}>
            <div className="kpi-label">Monatlich</div>
            <div style={{ fontSize: 20, fontWeight: 700, marginTop: 4 }}>{fmtEUR(mo)}</div>
            <div className="muted" style={{ fontSize: 11 }}>{fmtEUR(daily)} / Tag</div>
          </div>
          <div className="kpi" style={{ padding: 14 }}>
            <div className="kpi-label">Bisher</div>
            <div style={{ fontSize: 20, fontWeight: 700, marginTop: 4 }}>{fmtEUR(spentSoFar, { cents: false })}</div>
            <div className="muted" style={{ fontSize: 11 }}>seit {fmtDate(sub.startedAt, { month:'short', year:'numeric' })}</div>
          </div>
        </div>

        {/* Next payment callout */}
        <div className="card card-pad" style={{ marginTop: 14, background: 'var(--brand-50)', borderColor: 'var(--brand-100)' }}>
          <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between' }}>
            <div>
              <div className="muted" style={{ fontSize: 12 }}>Nächste Zahlung</div>
              <div style={{ fontWeight: 700, fontSize: 18, marginTop: 2 }} className="num">{fmtDate(sub.nextPayment)} · {fmtEUR(sub.price)}</div>
            </div>
            <div className="badge badge-brand" style={{ fontSize: 12 }}>in {daysUntil(sub.nextPayment)} Tagen</div>
          </div>
          <div style={{ marginTop: 10, paddingTop: 10, borderTop: '1px solid var(--brand-100)', display: 'flex', gap: 18, fontSize: 12 }}>
            <div><Icon name="card" size={13}/> {sub.paymentMethod}</div>
            <div><Icon name="check-circle" size={13}/> Auto-Verlängerung {sub.autoRenew ? 'ein' : 'aus'}</div>
          </div>
        </div>

        {/* Sections */}
        <Section title="Details">
          <Row k="Anbieter" v={sub.vendor}/>
          <Row k="Kategorie" v={<CategoryChip id={sub.category}/>}/>
          <Row k="Zyklus" v={sub.cycle === 'yearly' ? 'Jährlich' : 'Monatlich'}/>
          <Row k="Beginn" v={fmtDate(sub.startedAt)}/>
          <Row k="Mindestlaufzeit" v={sub.contract.minTerm}/>
          <Row k="Kündigungsfrist" v={sub.contract.notice}/>
          <Row k="Zuletzt genutzt" v={fmtUsed(sub.lastUsed)}/>
        </Section>

        <Section title="Tags & Anhänge">
          <div className="row" style={{ flexWrap: 'wrap', gap: 6 }}>
            {sub.tags.map(t => <span key={t} className="tag">{t}</span>)}
            <button className="tag" style={{ borderStyle: 'dashed', cursor:'pointer' }}><Icon name="plus" size={12}/> Tag</button>
          </div>
          <div style={{ marginTop: 14, padding: 12, background: 'var(--surface-2)', borderRadius: 10, display: 'flex', alignItems: 'center', gap: 12 }}>
            <Icon name="attach" size={18}/>
            <div style={{ flex: 1 }}>
              <div style={{ fontWeight: 600 }}>vertrag-{sub.id}.pdf</div>
              <div className="muted" style={{ fontSize: 11 }}>184 KB · hinzugefügt am {fmtDate(sub.startedAt)}</div>
            </div>
            <button className="btn btn-sm btn-ghost">Öffnen</button>
          </div>
        </Section>

        <Section title="Preis-Historie">
          <BarChart data={phData} height={130} ySuffix="€"/>
          <div className="muted" style={{ fontSize: 12, marginTop: 4 }}>
            {sub.priceHistory && sub.priceHistory.length > 1
              ? `Von ${fmtEUR(sub.priceHistory[0])} auf ${fmtEUR(sub.price)} (${(((sub.price - sub.priceHistory[0]) / sub.priceHistory[0]) * 100).toFixed(0)}%)`
              : 'Preis stabil seit Vertragsbeginn'}
          </div>
        </Section>
      </div>

      <div style={{ padding: 16, borderTop: '1px solid var(--border)', display: 'flex', gap: 8, justifyContent: 'space-between', background: 'var(--surface-2)' }}>
        <div className="row" style={{ gap: 8 }}>
          <button className="btn btn-sm"><Icon name="edit" size={14}/> Bearbeiten</button>
          {sub.status === 'active'
            ? <button className="btn btn-sm" onClick={() => setStatus('paused')}><Icon name="pause" size={14}/> Pausieren</button>
            : <button className="btn btn-sm" onClick={() => setStatus('active')}><Icon name="play" size={14}/> Aktivieren</button>}
        </div>
        <button className="btn btn-sm btn-danger" onClick={() => setStatus('cancelled')}><Icon name="cancel" size={14}/> Kündigen</button>
      </div>
    </>
  );
};

const Section = ({ title, children }) => (
  <div style={{ marginTop: 22 }}>
    <div style={{ fontSize: 11, fontWeight: 600, letterSpacing: '.06em', textTransform: 'uppercase', color: 'var(--text-3)', marginBottom: 8 }}>{title}</div>
    <div>{children}</div>
  </div>
);
const Row = ({ k, v }) => (
  <div style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid var(--border)', fontSize: 13 }}>
    <span className="muted">{k}</span>
    <span style={{ fontWeight: 500 }}>{v}</span>
  </div>
);

window.SubscriptionsList = SubscriptionsList;
window.SubDetail = SubDetail;
