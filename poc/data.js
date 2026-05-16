// Subly seed data (German subscriptions)
// Note: classified as text/babel so it shares scope with other Babel scripts.

const CATEGORIES = [
  { id: 'streaming',  label: 'Streaming',           color: 'var(--c-streaming)',  icon: 'play'      },
  { id: 'software',   label: 'Software & SaaS',     color: 'var(--c-software)',   icon: 'code'      },
  { id: 'insurance',  label: 'Versicherungen',      color: 'var(--c-insurance)',  icon: 'shield'    },
  { id: 'telecom',    label: 'Mobilfunk & Internet',color: 'var(--c-telecom)',    icon: 'signal'    },
  { id: 'energy',     label: 'Strom & Gas',         color: 'var(--c-energy)',     icon: 'bolt'      },
  { id: 'fitness',    label: 'Fitness',             color: 'var(--c-fitness)',    icon: 'heart'     },
  { id: 'news',       label: 'Zeitungen & Magazine',color: 'var(--c-news)',       icon: 'paper'     },
  { id: 'cloud',      label: 'Cloud & Hosting',     color: 'var(--c-cloud)',      icon: 'cloud'     },
  { id: 'membership', label: 'Mitgliedschaften',    color: 'var(--c-member)',     icon: 'badge'     },
];

const CAT_BY_ID = Object.fromEntries(CATEGORIES.map(c => [c.id, c]));

// price = amount in EUR for the cycle (monthly or yearly)
// Today = 16. Mai 2026
const SUBSCRIPTIONS = [
  // Streaming
  { id:'netflix',     name:'Netflix Standard',     vendor:'Netflix',         category:'streaming',  price:17.99, cycle:'monthly', nextPayment:'2026-05-22', startedAt:'2019-03-14', logo:{bg:'#E50914', initials:'N'}, paymentMethod:'Visa •• 4421', autoRenew:true,  status:'active', usage:'high',   priceHistory:[12.99,13.99,17.99], tags:['privat','4K'],  contract:{minTerm:'monatlich', notice:'jederzeit'}, lastUsed:'2026-05-15' },
  { id:'spotify',     name:'Spotify Family',       vendor:'Spotify',         category:'streaming',  price:17.99, cycle:'monthly', nextPayment:'2026-05-18', startedAt:'2018-08-02', logo:{bg:'#1DB954', initials:'S'}, paymentMethod:'PayPal',      autoRenew:true,  status:'active', usage:'high',   priceHistory:[14.99,17.99],       tags:['familie'],     contract:{minTerm:'monatlich', notice:'jederzeit'}, lastUsed:'2026-05-16' },
  { id:'disney',      name:'Disney+ Standard',     vendor:'Disney+',         category:'streaming',  price:89.90, cycle:'yearly',  nextPayment:'2026-11-04', startedAt:'2021-11-04', logo:{bg:'#1A2A6E', initials:'D+'},paymentMethod:'PayPal',      autoRenew:true,  status:'active', usage:'medium', priceHistory:[69.99,89.90],       tags:['jahresabo'],   contract:{minTerm:'12 Monate', notice:'1 Monat'},   lastUsed:'2026-04-21' },
  { id:'prime',       name:'Amazon Prime',         vendor:'Amazon',          category:'streaming',  price:89.90, cycle:'yearly',  nextPayment:'2026-08-12', startedAt:'2017-06-01', logo:{bg:'#FF9900', initials:'a'}, paymentMethod:'Amex •• 1009',autoRenew:true,  status:'active', usage:'high',   priceHistory:[69.00,89.90],       tags:['versand'],     contract:{minTerm:'12 Monate', notice:'jederzeit'}, lastUsed:'2026-05-14' },
  { id:'dazn',        name:'DAZN Unlimited',       vendor:'DAZN',            category:'streaming',  price:44.99, cycle:'monthly', nextPayment:'2026-06-02', startedAt:'2023-08-12', logo:{bg:'#F8FF13', fg:'#000', initials:'D'}, paymentMethod:'Visa •• 4421', autoRenew:true, status:'active', usage:'low',    priceHistory:[29.99,34.99,44.99], tags:['sport'],       contract:{minTerm:'12 Monate', notice:'1 Monat'},   lastUsed:'2026-03-02' },

  // Software & SaaS
  { id:'chatgpt',     name:'ChatGPT Plus',         vendor:'OpenAI',          category:'software',   price:22.00, cycle:'monthly', nextPayment:'2026-05-19', startedAt:'2023-04-08', logo:{bg:'#10A37F', initials:'AI'},paymentMethod:'Visa •• 4421', autoRenew:true,  status:'active', usage:'high',   priceHistory:[20.00,22.00],       tags:['produktivität'],contract:{minTerm:'monatlich', notice:'jederzeit'}, lastUsed:'2026-05-16' },
  { id:'adobe',       name:'Creative Cloud',       vendor:'Adobe',           category:'software',   price:65.16, cycle:'monthly', nextPayment:'2026-05-28', startedAt:'2020-01-12', logo:{bg:'#FA0F00', initials:'Ai'},paymentMethod:'Mastercard •• 0044', autoRenew:true, status:'active', usage:'low',   priceHistory:[59.49,65.16],       tags:['design'],      contract:{minTerm:'12 Monate', notice:'1 Monat'},   lastUsed:'2026-02-08' },
  { id:'notion',      name:'Notion Plus',          vendor:'Notion',          category:'software',   price:9.50,  cycle:'monthly', nextPayment:'2026-06-04', startedAt:'2022-02-01', logo:{bg:'#000', initials:'N'},   paymentMethod:'PayPal',     autoRenew:true,  status:'active', usage:'high',   priceHistory:[8.00,9.50],        tags:['notizen'],     contract:{minTerm:'monatlich', notice:'jederzeit'}, lastUsed:'2026-05-15' },
  { id:'github',      name:'GitHub Pro',           vendor:'GitHub',          category:'software',   price:4.00,  cycle:'monthly', nextPayment:'2026-06-01', startedAt:'2019-09-12', logo:{bg:'#181717', initials:'Gh'},paymentMethod:'PayPal',     autoRenew:true,  status:'active', usage:'high',   priceHistory:[4.00],              tags:['dev'],         contract:{minTerm:'monatlich', notice:'jederzeit'}, lastUsed:'2026-05-16' },
  { id:'1password',   name:'1Password Family',     vendor:'1Password',       category:'software',   price:60.00, cycle:'yearly',  nextPayment:'2026-12-02', startedAt:'2020-12-02', logo:{bg:'#0572EC', initials:'1P'},paymentMethod:'Visa •• 4421', autoRenew:true, status:'active', usage:'high',   priceHistory:[55.20,60.00],      tags:['sicherheit'],  contract:{minTerm:'12 Monate', notice:'1 Monat'},   lastUsed:'2026-05-16' },

  // Cloud
  { id:'icloud',      name:'iCloud+ 200 GB',       vendor:'Apple',           category:'cloud',      price:2.99,  cycle:'monthly', nextPayment:'2026-05-24', startedAt:'2018-11-04', logo:{bg:'#000', initials:''},  paymentMethod:'Apple Pay',   autoRenew:true,  status:'active', usage:'high',   priceHistory:[2.99],              tags:['backup'],      contract:{minTerm:'monatlich', notice:'jederzeit'}, lastUsed:'2026-05-16' },
  { id:'dropbox',     name:'Dropbox Plus',         vendor:'Dropbox',         category:'cloud',      price:11.99, cycle:'monthly', nextPayment:'2026-06-08', startedAt:'2016-04-22', logo:{bg:'#0061FF', initials:'Db'},paymentMethod:'PayPal',     autoRenew:true,  status:'active', usage:'low',    priceHistory:[9.99,11.99],       tags:['storage'],     contract:{minTerm:'monatlich', notice:'jederzeit'}, lastUsed:'2025-12-19' },

  // Telecom
  { id:'1u1',         name:'1&1 DSL 100',          vendor:'1&1',             category:'telecom',    price:39.99, cycle:'monthly', nextPayment:'2026-06-01', startedAt:'2022-06-01', logo:{bg:'#003D8F', initials:'1&1'}, paymentMethod:'Lastschrift', autoRenew:true, status:'active', usage:'high', priceHistory:[34.99,39.99],     tags:['internet'],    contract:{minTerm:'24 Monate, bis 06/2026', notice:'3 Monate'}, lastUsed:'2026-05-16' },
  { id:'o2',          name:'O₂ Free Unlimited M',  vendor:'O₂',              category:'telecom',    price:29.99, cycle:'monthly', nextPayment:'2026-05-30', startedAt:'2024-05-30', logo:{bg:'#0019A5', initials:'O₂'},paymentMethod:'Lastschrift', autoRenew:true, status:'active', usage:'high', priceHistory:[29.99],         tags:['mobilfunk'],   contract:{minTerm:'24 Monate, bis 05/2026', notice:'1 Monat'}, lastUsed:'2026-05-16' },

  // Energy
  { id:'vattenfall',  name:'Vattenfall Strom',     vendor:'Vattenfall',      category:'energy',     price:89.00, cycle:'monthly', nextPayment:'2026-06-01', startedAt:'2021-01-01', logo:{bg:'#FCE300', fg:'#000', initials:'V'}, paymentMethod:'Lastschrift', autoRenew:true, status:'active', usage:'high', priceHistory:[64.00,78.00,89.00], tags:['abschlag'], contract:{minTerm:'12 Monate', notice:'6 Wochen'}, lastUsed:'2026-05-16' },
  { id:'stadtwerke',  name:'Stadtwerke Gas',       vendor:'Stadtwerke',      category:'energy',     price:64.00, cycle:'monthly', nextPayment:'2026-06-01', startedAt:'2020-08-01', logo:{bg:'#EA580C', initials:'SW'},paymentMethod:'Lastschrift', autoRenew:true,  status:'active', usage:'medium',priceHistory:[44.00,54.00,64.00],tags:['abschlag'],    contract:{minTerm:'12 Monate', notice:'6 Wochen'},  lastUsed:'2026-05-16' },

  // Insurance
  { id:'allianz',     name:'Hausratversicherung',  vendor:'Allianz',         category:'insurance',  price:12.50, cycle:'monthly', nextPayment:'2026-06-01', startedAt:'2019-04-01', logo:{bg:'#0033A1', initials:'A'}, paymentMethod:'Lastschrift', autoRenew:true,  status:'active', usage:'low',    priceHistory:[12.50],            tags:['wichtig'],     contract:{minTerm:'12 Monate', notice:'3 Monate'},  lastUsed:'—' },
  { id:'huk',         name:'KFZ-Haftpflicht',      vendor:'HUK24',           category:'insurance',  price:540.00,cycle:'yearly',  nextPayment:'2027-01-01', startedAt:'2018-01-01', logo:{bg:'#FFCC00', fg:'#000', initials:'HUK'}, paymentMethod:'Lastschrift', autoRenew:true, status:'active', usage:'high', priceHistory:[480.00,540.00], tags:['pflicht'],     contract:{minTerm:'12 Monate', notice:'1 Monat'},  lastUsed:'—' },
  { id:'gothaer',     name:'Privathaftpflicht',    vendor:'Gothaer',         category:'insurance',  price:5.90,  cycle:'monthly', nextPayment:'2026-06-15', startedAt:'2017-06-15', logo:{bg:'#003F8B', initials:'G'}, paymentMethod:'Lastschrift', autoRenew:true,  status:'active', usage:'low',    priceHistory:[5.90],             tags:['wichtig'],     contract:{minTerm:'12 Monate', notice:'3 Monate'}, lastUsed:'—' },

  // Fitness
  { id:'mcfit',       name:'McFit Standard',       vendor:'McFit',           category:'fitness',    price:24.90, cycle:'monthly', nextPayment:'2026-06-05', startedAt:'2024-06-05', logo:{bg:'#E20613', initials:'Mc'},paymentMethod:'Lastschrift', autoRenew:true,  status:'active', usage:'low',    priceHistory:[24.90],            tags:['ungenutzt'],   contract:{minTerm:'12 Monate, bis 06/2026', notice:'1 Monat'}, lastUsed:'2026-01-12' },

  // News
  { id:'faz',         name:'FAZ Digital',          vendor:'FAZ',             category:'news',       price:49.90, cycle:'monthly', nextPayment:'2026-05-26', startedAt:'2022-09-12', logo:{bg:'#001F3F', initials:'F'},paymentMethod:'PayPal',       autoRenew:true,  status:'active', usage:'medium', priceHistory:[44.90,49.90],     tags:['lesen'],       contract:{minTerm:'monatlich', notice:'jederzeit'}, lastUsed:'2026-05-13' },

  // Membership
  { id:'adac',        name:'ADAC Premium',         vendor:'ADAC',            category:'membership', price:139.00,cycle:'yearly',  nextPayment:'2027-01-01', startedAt:'2015-01-01', logo:{bg:'#FFCB00', fg:'#1A1A1A', initials:'A'}, paymentMethod:'Lastschrift', autoRenew:true, status:'active', usage:'medium', priceHistory:[119.00,139.00], tags:['mobilität'],   contract:{minTerm:'12 Monate', notice:'3 Monate'},  lastUsed:'2025-08-04' },
];

// ---- Helpers
const TODAY = new Date('2026-05-16T12:00:00');

function monthlyEquivalent(s) {
  return s.cycle === 'yearly' ? s.price / 12 : s.price;
}
function yearlyEquivalent(s) {
  return s.cycle === 'yearly' ? s.price : s.price * 12;
}
function daysBetween(a, b) {
  return Math.round((new Date(b) - new Date(a)) / 86400000);
}
function daysUntil(dateStr) {
  return daysBetween(TODAY, dateStr);
}
function fmtEUR(n, { cents = true } = {}) {
  return new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR', minimumFractionDigits: cents ? 2 : 0, maximumFractionDigits: cents ? 2 : 0 }).format(n);
}
function fmtNum(n, digits = 2) {
  return new Intl.NumberFormat('de-DE', { minimumFractionDigits: digits, maximumFractionDigits: digits }).format(n);
}
function fmtDate(d, opts = { day: '2-digit', month: 'short', year: 'numeric' }) {
  return new Intl.DateTimeFormat('de-DE', opts).format(new Date(d));
}
function fmtDateShort(d) {
  return new Intl.DateTimeFormat('de-DE', { day:'2-digit', month:'2-digit' }).format(new Date(d));
}

// monthly cost history for last 12 months (simulated from price history & startedAt)
function simulatedMonthly(months = 12) {
  // produce array of {label, total} per month going back `months` months from today
  const out = [];
  for (let i = months - 1; i >= 0; i--) {
    const d = new Date(TODAY.getFullYear(), TODAY.getMonth() - i, 1);
    let total = 0;
    SUBSCRIPTIONS.forEach(s => {
      const start = new Date(s.startedAt);
      if (start > d) return;
      // approximate: latest priceHistory used for last 6 mo, prior price for older
      let p = s.price;
      if (s.priceHistory && s.priceHistory.length >= 2 && i > 5) p = s.priceHistory[s.priceHistory.length - 2];
      if (s.priceHistory && s.priceHistory.length >= 3 && i > 11) p = s.priceHistory[0];
      total += s.cycle === 'yearly' ? (p / 12) : p;
    });
    out.push({
      label: new Intl.DateTimeFormat('de-DE', { month: 'short' }).format(d),
      year: d.getFullYear(),
      monthIdx: d.getMonth(),
      total: Math.round(total * 100) / 100,
    });
  }
  return out;
}

Object.assign(window, {
  CATEGORIES, CAT_BY_ID, SUBSCRIPTIONS, TODAY,
  monthlyEquivalent, yearlyEquivalent, daysBetween, daysUntil,
  fmtEUR, fmtNum, fmtDate, fmtDateShort, simulatedMonthly,
});
