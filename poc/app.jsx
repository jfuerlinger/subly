// Main app shell + routing

const NAV_ITEMS = [
  { id: 'dashboard',   label: 'Dashboard',   icon: 'dashboard' },
  { id: 'list',        label: 'Alle Abos',   icon: 'list', badge: () => SUBSCRIPTIONS.filter(s => s.status === 'active').length },
  { id: 'calendar',    label: 'Kalender',    icon: 'calendar' },
  { id: 'analytics',   label: 'Analyse',     icon: 'chart' },
  { id: 'categories',  label: 'Kategorien',  icon: 'tag' },
];
const NAV_BOTTOM = [
  { id: 'settings',    label: 'Einstellungen', icon: 'settings' },
];

const TITLE_BY_ROUTE = {
  dashboard: 'Dashboard',
  list: 'Alle Abos',
  calendar: 'Kalender',
  analytics: 'Analyse',
  categories: 'Kategorien',
  settings: 'Einstellungen',
};

const App = () => {
  const [route, setRoute] = useState('dashboard');
  const [subs, setSubs] = useState(SUBSCRIPTIONS);
  const [openSub, setOpenSub] = useState(null);
  const [addOpen, setAddOpen] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  const openWith = (s) => setOpenSub(s);
  const closeDetail = () => setOpenSub(null);
  const onUpdateSub = (next) => {
    setSubs(prev => prev.map(s => s.id === next.id ? next : s));
    setOpenSub(next);
  };
  const onAddSub = (draft) => {
    const id = 'new-' + Date.now();
    const price = parseFloat(draft.price) || 0;
    const newSub = {
      id, name: draft.name || 'Neues Abo', vendor: draft.vendor || draft.name || 'Neu',
      category: draft.category, price, cycle: draft.cycle,
      nextPayment: draft.nextPayment, startedAt: TODAY.toISOString().slice(0, 10),
      logo: draft.logo || { bg: '#5B6CFF', initials: (draft.name || 'A')[0].toUpperCase() },
      paymentMethod: draft.paymentMethod, autoRenew: true, status: 'active',
      usage: 'medium', priceHistory: [price], tags: ['neu'],
      contract: { minTerm: 'monatlich', notice: 'jederzeit' }, lastUsed: '—',
    };
    setSubs(prev => [newSub, ...prev]);
  };

  const renderScreen = () => {
    switch (route) {
      case 'dashboard':  return <Dashboard subs={subs} onOpenSub={openWith} onNav={setRoute}/>;
      case 'list':       return <SubscriptionsList subs={subs} onOpenSub={openWith} onAdd={() => setAddOpen(true)}/>;
      case 'calendar':   return <CalendarScreen subs={subs} onOpenSub={openWith}/>;
      case 'analytics':  return <Analytics subs={subs}/>;
      case 'categories': return <CategoriesScreen subs={subs} onOpenSub={openWith}/>;
      case 'settings':   return <SettingsScreen/>;
      default:           return null;
    }
  };

  return (
    <div className="app" data-sidebar={sidebarCollapsed ? 'collapsed' : 'expanded'} data-screen-label="00 App">
      {/* Sidebar */}
      <aside className="sidebar">
        <div className="sidebar-top">
          {!sidebarCollapsed
            ? <div className="brand">
                <div className="brand-mark">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M5 7l4-4 4 4"/><path d="M9 3v9"/><circle cx="15" cy="16" r="5"/><path d="m13 16 2 2 4-4"/></svg>
                </div>
                <div>
                  <div className="brand-name">Subly</div>
                  <div className="brand-tag">Subscription Manager</div>
                </div>
              </div>
            : <div className="brand-mark" title="Subly"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M5 7l4-4 4 4M9 3v9"/><circle cx="15" cy="16" r="5"/></svg></div>}
          <button className="icon-btn" onClick={() => setSidebarCollapsed(v => !v)} title="Sidebar ein-/ausklappen"><Icon name="sidebar" size={16}/></button>
        </div>

        <nav className="nav">
          {!sidebarCollapsed && <div className="nav-section-label">Übersicht</div>}
          {NAV_ITEMS.map(n => (
            <a key={n.id} className="nav-item" aria-current={route === n.id ? 'page' : undefined} onClick={() => setRoute(n.id)}>
              <Icon name={n.icon} size={18} className="nav-icon"/>
              {!sidebarCollapsed && <span>{n.label}</span>}
              {!sidebarCollapsed && n.badge && <span className="nav-badge">{n.badge()}</span>}
            </a>
          ))}
          <div style={{ flex: 1 }}/>
          {!sidebarCollapsed && <div className="nav-section-label">System</div>}
          {NAV_BOTTOM.map(n => (
            <a key={n.id} className="nav-item" aria-current={route === n.id ? 'page' : undefined} onClick={() => setRoute(n.id)}>
              <Icon name={n.icon} size={18} className="nav-icon"/>
              {!sidebarCollapsed && <span>{n.label}</span>}
            </a>
          ))}
        </nav>

        {!sidebarCollapsed && (
          <div style={{ padding: '0 12px 12px' }}>
            <div className="card" style={{ padding: 14, background: 'linear-gradient(155deg, var(--brand-ink), var(--brand-700))', color: 'white', border: 0 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontWeight: 700, fontSize: 12 }}>
                <Icon name="sparkles" size={14}/> Subly Plus
              </div>
              <div style={{ fontSize: 12, opacity: .85, marginTop: 6, lineHeight: 1.4 }}>
                Automatischer Import von Buchungen, unbegrenzte Anhänge & Familien-Sharing.
              </div>
              <button className="btn btn-sm" style={{ marginTop: 10, background: 'white', color: 'var(--brand-700)', borderColor: 'transparent', width: '100%', justifyContent: 'center', fontWeight: 600 }}>
                14 Tage testen
              </button>
            </div>
          </div>
        )}

        <div className="sidebar-footer">
          <div className="user-card">
            <div className="avatar">MW</div>
            {!sidebarCollapsed && (
              <>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div className="user-name truncate">Maximilian W.</div>
                  <div className="user-plan">Free Plan</div>
                </div>
                <Icon name="chevron-down" size={14}/>
              </>
            )}
          </div>
        </div>
      </aside>

      {/* Main */}
      <main className="main">
        <header className="topbar">
          <h1>{TITLE_BY_ROUTE[route]}</h1>
          <div className="topbar-spacer"/>
          <div className="input-with-icon" style={{ width: 280 }}>
            <Icon name="search" size={16}/>
            <input className="input" style={{ width: '100%' }} placeholder="Abo suchen…  ⌘K"/>
          </div>
          <button className="btn btn-icon" title="Teilen"><Icon name="share" size={16}/></button>
          <button className="btn btn-icon" title="Benachrichtigungen" style={{ position: 'relative' }}>
            <Icon name="bell" size={16}/>
            <span style={{ position: 'absolute', top: 8, right: 9, width: 7, height: 7, borderRadius: '50%', background: 'var(--bad)', border: '2px solid var(--bg)' }}/>
          </button>
          <button className="btn btn-primary" onClick={() => setAddOpen(true)}>
            <Icon name="plus" size={16}/> Hinzufügen
          </button>
        </header>

        <div className="content" key={route}>
          {renderScreen()}
        </div>
      </main>

      {/* Detail panel */}
      <SidePanel open={!!openSub} onClose={closeDetail} width={520}>
        <SubDetail sub={openSub} onClose={closeDetail} onUpdate={onUpdateSub}/>
      </SidePanel>

      {/* Add modal */}
      <AddSubModal open={addOpen} onClose={() => setAddOpen(false)} onAdd={onAddSub}/>
    </div>
  );
};

ReactDOM.createRoot(document.getElementById('root')).render(<App/>);
