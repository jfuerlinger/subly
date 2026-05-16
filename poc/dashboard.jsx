// Dashboard screen

const Dashboard = ({ subs, onOpenSub, onAdd, onNav }) => {
  const active = subs.filter(s => s.status === 'active');
  const agg = aggregate(subs);

  // Upcoming next 30 days
  const upcoming = active
    .filter(s => daysUntil(s.nextPayment) <= 30 && daysUntil(s.nextPayment) >= 0)
    .sort((a,b) => new Date(a.nextPayment) - new Date(b.nextPayment));

  // Year cost & forecast
  const yearCost = agg.yearly;
  const dailyCost = agg.monthly * 12 / 365;

  // Price changes in last 12 mo
  const priceIncreases = active.filter(s => s.priceHistory && s.priceHistory.length >= 2 && s.priceHistory[s.priceHistory.length - 1] > s.priceHistory[s.priceHistory.length - 2]);

  // Unused (last used > 60 days)
  const unused = active.filter(s => s.lastUsed && s.lastUsed !== '—' && daysBetween(s.lastUsed, TODAY) > 60);

  // Cost history (12 mo)
  const history = useMemo(() => simulatedMonthly(12), []);
  const lastM = history[history.length - 1].total;
  const prevM = history[history.length - 2].total;
  const mom = ((lastM - prevM) / prevM) * 100;

  // Category breakdown segments
  const segments = CATEGORIES
    .map(c => ({ id: c.id, label: c.label, value: agg.perCat[c.id], color: c.color }))
    .filter(s => s.value > 0)
    .sort((a,b) => b.value - a.value);

  const [hovered, setHovered] = useState(null);
  const donutLabel = hovered ? fmtEUR(hovered.value, { cents: false }) : fmtEUR(agg.monthly, { cents: false });
  const donutSub = hovered ? hovered.label : 'pro Monat';

  // Recommendations
  const recos = [];
  unused.slice(0, 2).forEach(s => recos.push({
    kind: 'reco-bad', icon: 'warn',
    title: `${s.name} – seit ${Math.floor(daysBetween(s.lastUsed, TODAY)/30)} Monaten ungenutzt`,
    text: `Du zahlst ${fmtEUR(monthlyEquivalent(s))}/Monat. Möchtest du das Abo kündigen?`,
    action: 'Kündigen', sub: s,
  }));
  priceIncreases.slice(0, 1).forEach(s => {
    const old = s.priceHistory[s.priceHistory.length - 2];
    const diff = ((s.price - old) / old) * 100;
    recos.push({
      kind: 'reco-warn', icon: 'trend-up',
      title: `${s.name} ist um ${diff.toFixed(0)}% teurer geworden`,
      text: `Von ${fmtEUR(old)} auf ${fmtEUR(s.price)} ${s.cycle === 'yearly' ? 'pro Jahr' : 'pro Monat'}. Sonderkündigungsrecht prüfen.`,
      action: 'Details', sub: s,
    });
  });
  // contract ending soon
  const ending = active.find(s => /bis (\d{2})\/(\d{4})/.test(s.contract.minTerm));
  if (ending) {
    recos.push({
      kind: 'reco-info', icon: 'clock',
      title: `${ending.name} – Laufzeit endet bald`,
      text: `Mindestlaufzeit ${ending.contract.minTerm}. Kündigungsfrist: ${ending.contract.notice}.`,
      action: 'Erinnern', sub: ending,
    });
  }
  recos.push({
    kind: 'reco-good', icon: 'sparkles',
    title: `Spotify Duo wäre ${fmtEUR(7)} günstiger`,
    text: 'Mit Spotify Duo (statt Family) könntest du ~7€/Monat sparen, falls nur 2 Personen nutzen.',
    action: 'Mehr erfahren', sub: subs.find(s => s.id === 'spotify'),
  });

  return (
    <div className="stack" style={{ gap: 20 }}>
      {/* KPIs */}
      <div className="kpi-grid">
        <KPI label="Monatlich" icon="wallet"
             value={<span>{fmtEUR(agg.monthly, { cents: false })}<span className="cents">,{agg.monthly.toFixed(2).split('.')[1]}</span></span>}
             delta={mom} deltaInvert sub="vs. Vormonat"
             accent="var(--brand-200)"/>
        <KPI label="Jährliche Belastung" icon="chart"
             value={fmtEUR(yearCost, { cents: false })}
             sub={`${active.length} aktive Abos`}/>
        <KPI label="Pro Tag" icon="clock"
             value={fmtEUR(dailyCost)}
             sub="Durchschnitt 30 Tage"/>
        <KPI label="Anstehend (30T)" icon="bell"
             value={<span>{fmtEUR(upcoming.reduce((a,s)=>a+s.price,0), { cents: false })}</span>}
             sub={`${upcoming.length} Zahlungen`}/>
      </div>

      {/* Chart + Donut */}
      <div style={{ display: 'grid', gridTemplateColumns: '1.6fr 1fr', gap: 16 }}>
        <div className="card">
          <div className="card-head">
            <div>
              <div className="card-title"><Icon name="chart" size={14}/> Kosten-Verlauf</div>
              <div className="card-sub">Monatliche Belastung – letzte 12 Monate</div>
            </div>
            <div className="row">
              <div className="pill-tabs">
                <button aria-selected="false">6M</button>
                <button aria-selected="true">12M</button>
                <button aria-selected="false">24M</button>
                <button aria-selected="false">All</button>
              </div>
              <button className="btn btn-sm btn-ghost btn-icon"><Icon name="menu-dots" size={16}/></button>
            </div>
          </div>
          <div style={{ padding: '8px 12px 12px' }}>
            <LineChart data={history} accent="#5B6CFF"/>
          </div>
          <div style={{ display:'flex', gap: 32, padding: '4px 24px 18px', borderTop: '1px solid var(--border)' }}>
            <div>
              <div className="muted" style={{ fontSize: 12 }}>Aktueller Monat</div>
              <div style={{ fontWeight: 700, fontSize: 18, marginTop: 2 }}>{fmtEUR(lastM)}</div>
            </div>
            <div>
              <div className="muted" style={{ fontSize: 12 }}>Veränderung MoM</div>
              <div style={{ marginTop: 4 }}><Delta value={parseFloat(mom.toFixed(1))} invert/></div>
            </div>
            <div>
              <div className="muted" style={{ fontSize: 12 }}>12-Monats Forecast</div>
              <div style={{ fontWeight: 700, fontSize: 18, marginTop: 2 }}>{fmtEUR(lastM * 12)}</div>
            </div>
          </div>
        </div>

        <div className="card">
          <div className="card-head">
            <div>
              <div className="card-title"><Icon name="tag" size={14}/> Verteilung</div>
              <div className="card-sub">nach Kategorie (monatlich)</div>
            </div>
            <button className="btn btn-sm btn-ghost btn-icon"><Icon name="menu-dots" size={16}/></button>
          </div>
          <div style={{ display:'flex', justifyContent: 'center', padding: '12px 0 4px' }}>
            <Donut segments={segments} size={200} thickness={26}
                   label={donutLabel} sub={donutSub} onHover={setHovered}/>
          </div>
          <div style={{ padding: '8px 18px 16px', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '6px 14px' }}>
            {segments.map(s => (
              <div key={s.id} style={{ display:'flex', alignItems:'center', justifyContent:'space-between', fontSize: 12 }}>
                <div style={{ display:'flex', alignItems:'center', gap: 6 }}>
                  <span style={{ width: 8, height: 8, borderRadius: 2, background: s.color }}/>
                  <span>{CAT_BY_ID[s.id].label}</span>
                </div>
                <span className="num muted">{fmtEUR(s.value, { cents: false })}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Upcoming + Recommendations */}
      <div style={{ display: 'grid', gridTemplateColumns: '1.4fr 1fr', gap: 16 }}>
        <div className="card">
          <div className="card-head">
            <div>
              <div className="card-title"><Icon name="calendar" size={14}/> Anstehende Zahlungen</div>
              <div className="card-sub">nächste 30 Tage · {fmtEUR(upcoming.reduce((a,s)=>a+s.price,0))} gesamt</div>
            </div>
            <button className="btn btn-sm btn-ghost" onClick={() => onNav('calendar')}>Kalender öffnen <Icon name="arrow-right" size={14}/></button>
          </div>
          <div>
            <table className="tbl">
              <thead>
                <tr>
                  <th style={{ width: '36%' }}>Abo</th>
                  <th>Datum</th>
                  <th>In</th>
                  <th>Zahlungsmittel</th>
                  <th className="tbl-numeric">Betrag</th>
                </tr>
              </thead>
              <tbody>
                {upcoming.slice(0, 7).map(s => {
                  const days = daysUntil(s.nextPayment);
                  return (
                    <tr key={s.id} className="clickable" onClick={() => onOpenSub(s)}>
                      <td>
                        <div className="row">
                          <SubLogo sub={s} size="sm"/>
                          <div className="col" style={{ gap: 0 }}>
                            <div style={{ fontWeight: 600 }}>{s.name}</div>
                            <div className="muted" style={{ fontSize: 11 }}>{s.vendor} · {CAT_BY_ID[s.category].label}</div>
                          </div>
                        </div>
                      </td>
                      <td className="num">{fmtDate(s.nextPayment)}</td>
                      <td className="num">
                        {days === 0 ? <span className="badge badge-warn">Heute</span> :
                         days === 1 ? <span className="badge badge-warn">Morgen</span> :
                         <span className="muted">in {days} Tagen</span>}
                      </td>
                      <td className="muted" style={{ fontSize: 12 }}>{s.paymentMethod}</td>
                      <td className="tbl-numeric num" style={{ fontWeight: 600 }}>{fmtEUR(s.price)}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>

        <div className="card">
          <div className="card-head">
            <div>
              <div className="card-title"><Icon name="sparkles" size={14}/> Empfehlungen</div>
              <div className="card-sub">{recos.length} Hinweise · ~{fmtEUR(unused.reduce((a,s)=>a+monthlyEquivalent(s),0))} mögliches Sparpotenzial</div>
            </div>
          </div>
          <div className="stack" style={{ gap: 10, padding: 16 }}>
            {recos.map((r, i) => (
              <div key={i} className={`reco ${r.kind}`}>
                <div className="reco-ic"><Icon name={r.icon} size={16}/></div>
                <div style={{ minWidth: 0, flex: 1 }}>
                  <h4>{r.title}</h4>
                  <p>{r.text}</p>
                  <div className="row" style={{ gap: 6 }}>
                    <button className="btn btn-sm" onClick={() => r.sub && onOpenSub(r.sub)}>{r.action}</button>
                    <button className="btn btn-sm btn-ghost">Ignorieren</button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Top abos */}
      <div className="card">
        <div className="card-head">
          <div>
            <div className="card-title"><Icon name="trend-up" size={14}/> Teuerste Abos</div>
            <div className="card-sub">monatliche Belastung, sortiert</div>
          </div>
          <button className="btn btn-sm btn-ghost" onClick={() => onNav('list')}>Alle anzeigen <Icon name="arrow-right" size={14}/></button>
        </div>
        <div style={{ padding: '12px 20px 20px' }}>
          {active.slice().sort((a,b) => monthlyEquivalent(b) - monthlyEquivalent(a)).slice(0, 6).map(s => {
            const mo = monthlyEquivalent(s);
            const pct = (mo / agg.monthly) * 100;
            return (
              <div key={s.id} className="clickable" onClick={() => onOpenSub(s)} style={{ display: 'grid', gridTemplateColumns: '220px 1fr 120px 80px', gap: 16, alignItems: 'center', padding: '10px 0', borderBottom: '1px solid var(--border)' }}>
                <div className="row">
                  <SubLogo sub={s} size="sm"/>
                  <div className="col" style={{ gap: 0, minWidth: 0 }}>
                    <div className="truncate" style={{ fontWeight: 600 }}>{s.name}</div>
                    <div className="muted truncate" style={{ fontSize: 11 }}>{CAT_BY_ID[s.category].label}</div>
                  </div>
                </div>
                <div className="progress"><span style={{ width: pct + '%', background: CAT_BY_ID[s.category].color }}/></div>
                <div className="tbl-numeric muted num" style={{ fontSize: 12 }}>{pct.toFixed(1)}% der Ausgaben</div>
                <div className="tbl-numeric num" style={{ fontWeight: 700 }}>{fmtEUR(mo)}</div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};

window.Dashboard = Dashboard;
