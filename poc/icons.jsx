// SVG icon set
const Icon = ({ name, size = 18, stroke = 1.6, ...rest }) => {
  const common = { width: size, height: size, viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', strokeWidth: stroke, strokeLinecap: 'round', strokeLinejoin: 'round', ...rest };
  switch (name) {
    case 'dashboard':   return <svg {...common}><rect x="3" y="3" width="7" height="9" rx="1.5"/><rect x="14" y="3" width="7" height="5" rx="1.5"/><rect x="14" y="12" width="7" height="9" rx="1.5"/><rect x="3" y="16" width="7" height="5" rx="1.5"/></svg>;
    case 'list':        return <svg {...common}><path d="M8 6h13M8 12h13M8 18h13"/><circle cx="4" cy="6"  r="1.2"/><circle cx="4" cy="12" r="1.2"/><circle cx="4" cy="18" r="1.2"/></svg>;
    case 'calendar':    return <svg {...common}><rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/></svg>;
    case 'chart':       return <svg {...common}><path d="M4 19V5M4 19h16"/><rect x="7" y="12" width="3" height="5" rx=".5"/><rect x="12" y="8" width="3" height="9" rx=".5"/><rect x="17" y="5" width="3" height="12" rx=".5"/></svg>;
    case 'tag':         return <svg {...common}><path d="M3 12l9-9h6a3 3 0 0 1 3 3v6l-9 9a2 2 0 0 1-2.8 0L3 14.8a2 2 0 0 1 0-2.8z"/><circle cx="16" cy="8" r="1.4"/></svg>;
    case 'settings':    return <svg {...common}><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 0 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 0 1 0-4h.1a1.7 1.7 0 0 0 1.5-1.1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 0 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 0 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1z"/></svg>;
    case 'search':      return <svg {...common}><circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></svg>;
    case 'plus':        return <svg {...common}><path d="M12 5v14M5 12h14"/></svg>;
    case 'sidebar':     return <svg {...common}><rect x="3" y="4" width="18" height="16" rx="2"/><path d="M9 4v16"/></svg>;
    case 'chevron-right':return <svg {...common}><path d="m9 6 6 6-6 6"/></svg>;
    case 'chevron-down': return <svg {...common}><path d="m6 9 6 6 6-6"/></svg>;
    case 'chevron-left': return <svg {...common}><path d="m15 6-6 6 6 6"/></svg>;
    case 'arrow-up':    return <svg {...common}><path d="M12 19V5M5 12l7-7 7 7"/></svg>;
    case 'arrow-down':  return <svg {...common}><path d="M12 5v14M5 12l7 7 7-7"/></svg>;
    case 'arrow-right': return <svg {...common}><path d="M5 12h14M13 6l6 6-6 6"/></svg>;
    case 'close':       return <svg {...common}><path d="M18 6 6 18M6 6l12 12"/></svg>;
    case 'play':        return <svg {...common}><path d="M7 4v16l13-8z" fill="currentColor" stroke="none"/></svg>;
    case 'code':        return <svg {...common}><path d="m16 18 6-6-6-6M8 6l-6 6 6 6"/></svg>;
    case 'shield':      return <svg {...common}><path d="M12 3 4 6v6c0 5 3.5 8 8 9 4.5-1 8-4 8-9V6l-8-3z"/></svg>;
    case 'signal':      return <svg {...common}><path d="M2 20h2M7 20V14M12 20V8M17 20V4M22 20h-2"/></svg>;
    case 'bolt':        return <svg {...common}><path d="M13 2 4 14h7l-1 8 9-12h-7l1-8z"/></svg>;
    case 'heart':       return <svg {...common}><path d="M20.8 5.6a5.5 5.5 0 0 0-7.8 0L12 6.6l-1-1a5.5 5.5 0 0 0-7.8 7.8l1 1L12 22l7.8-7.6 1-1a5.5 5.5 0 0 0 0-7.8z"/></svg>;
    case 'paper':       return <svg {...common}><path d="M4 4h13a2 2 0 0 1 2 2v12a2 2 0 0 0 2 2H6a2 2 0 0 1-2-2V4z"/><path d="M8 8h7M8 12h7M8 16h5"/></svg>;
    case 'cloud':       return <svg {...common}><path d="M17 18a5 5 0 0 0 .5-9.95A6 6 0 0 0 6 9.5 4.5 4.5 0 0 0 6.5 18z"/></svg>;
    case 'badge':       return <svg {...common}><path d="M12 2 4 5v6c0 5 3.5 9 8 11 4.5-2 8-6 8-11V5l-8-3z"/><path d="m9 12 2 2 4-4"/></svg>;
    case 'bell':        return <svg {...common}><path d="M6 8a6 6 0 1 1 12 0c0 7 3 9 3 9H3s3-2 3-9"/><path d="M10 21a2 2 0 0 0 4 0"/></svg>;
    case 'share':       return <svg {...common}><path d="M4 12v7a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-7M16 6l-4-4-4 4M12 2v13"/></svg>;
    case 'edit':        return <svg {...common}><path d="M17 3a2.85 2.85 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5z"/></svg>;
    case 'trash':       return <svg {...common}><path d="M3 6h18M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6M14 11v6"/></svg>;
    case 'pause':       return <svg {...common}><rect x="6" y="4" width="4" height="16" rx="1"/><rect x="14" y="4" width="4" height="16" rx="1"/></svg>;
    case 'check':       return <svg {...common}><path d="m5 12 5 5L20 7"/></svg>;
    case 'check-circle':return <svg {...common}><circle cx="12" cy="12" r="9"/><path d="m9 12 2 2 4-4"/></svg>;
    case 'attach':      return <svg {...common}><path d="m21 11-9 9a5 5 0 0 1-7-7l9-9a3.5 3.5 0 0 1 5 5l-9 9a2 2 0 0 1-3-3l8-8"/></svg>;
    case 'filter':      return <svg {...common}><path d="M3 5h18l-7 8v6l-4 2v-8z"/></svg>;
    case 'sort':        return <svg {...common}><path d="M7 3v18M3 7l4-4 4 4M17 21V3M13 17l4 4 4-4"/></svg>;
    case 'sparkles':    return <svg {...common}><path d="M12 3v4M12 17v4M3 12h4M17 12h4M6 6l2.5 2.5M15.5 15.5 18 18M6 18l2.5-2.5M15.5 8.5 18 6"/><circle cx="12" cy="12" r="1.5"/></svg>;
    case 'trend-up':    return <svg {...common}><path d="m3 17 6-6 4 4 8-8"/><path d="M14 7h7v7"/></svg>;
    case 'trend-down':  return <svg {...common}><path d="m3 7 6 6 4-4 8 8"/><path d="M14 17h7v-7"/></svg>;
    case 'wallet':      return <svg {...common}><rect x="3" y="6" width="18" height="14" rx="2"/><path d="M3 10h18M17 14h2"/></svg>;
    case 'card':        return <svg {...common}><rect x="2" y="5" width="20" height="14" rx="2"/><path d="M2 10h20M6 15h4"/></svg>;
    case 'menu-dots':   return <svg {...common}><circle cx="5"  cy="12" r="1.4"/><circle cx="12" cy="12" r="1.4"/><circle cx="19" cy="12" r="1.4"/></svg>;
    case 'menu-dots-v': return <svg {...common}><circle cx="12" cy="5"  r="1.4"/><circle cx="12" cy="12" r="1.4"/><circle cx="12" cy="19" r="1.4"/></svg>;
    case 'cancel':      return <svg {...common}><circle cx="12" cy="12" r="9"/><path d="m8 8 8 8M16 8l-8 8"/></svg>;
    case 'info':        return <svg {...common}><circle cx="12" cy="12" r="9"/><path d="M12 8v.01M11 12h1v5h1"/></svg>;
    case 'warn':        return <svg {...common}><path d="M12 3 2 21h20L12 3z"/><path d="M12 9v5M12 17v.01"/></svg>;
    case 'globe':       return <svg {...common}><circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3a14 14 0 0 1 0 18M12 3a14 14 0 0 0 0 18"/></svg>;
    case 'eye':         return <svg {...common}><path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/></svg>;
    case 'clock':       return <svg {...common}><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>;
    default:            return <svg {...common}><circle cx="12" cy="12" r="9"/></svg>;
  }
};

window.Icon = Icon;
