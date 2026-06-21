// Centralized Romanian UI copy (slice #20). The convention for this frontend is English code /
// Romanian UI: identifiers, comments and types stay English while every user-visible string lives
// here in one place. Components import `ro` and never inline literal Romanian text.

export const ro = {
  appName: "Clasa Mea",

  auth: {
    title: "Clasa Mea",
    subtitle: "Autentifică-te ca să-ți gestionezi clasele.",
    email: "Email",
    password: "Parolă",
    signIn: "Intră în cont",
    signingIn: "Se intră…",
    signOut: "Ieși din cont",
    signingOut: "Se iese…",
    or: "sau",
    tryDemo: "Încearcă demonstrația",
    startingDemo: "Se pornește demonstrația…",
    demoHint: "Intră într-o clasă de probă, deja populată — fără cont.",
    loginFailed: "Autentificare eșuată.",
  },

  classes: {
    title: "Clasele tale",
    signedInAs: "Conectat ca",
    newClassPlaceholder: "Nume clasă nouă",
    add: "Adaugă",
    adding: "Se adaugă…",
    empty: "Încă nu ai nicio clasă — creează prima mai sus.",
    open: "Deschide clasa",
    archive: "Arhivează",
    archiving: "Se arhivează…",
    archiveConfirm: "Arhivezi această clasă? Va dispărea din listă, dar istoricul se păstrează.",
    students: (n: number) => (n === 1 ? "1 elev" : `${n} elevi`),
    roleOwner: "Proprietar",
    roleCollaborator: "Colaborator",
  },

  nav: {
    dashboard: "Tablou",
    shop: "Magazin",
    groups: "Grupuri",
    reports: "Rapoarte",
    settings: "Setări",
    kiosk: "Mod chioșc",
    enteringKiosk: "Se intră…",
    backToClasses: "Toate clasele",
  },

  kiosk: {
    badge: "Mod chioșc",
    heading: "Mod chioșc activ",
    description: "Elevii își pot vedea magazinul și avatarul. Acțiunile profesorului sunt blocate.",
    exit: "Ieși din chioșc",
    exiting: "Se iese…",
    pinPlaceholder: "PIN",
    wrongPin: "PIN incorect.",
  },

  sections: {
    // Placeholder copy for the five section pages; later slices replace each page's body.
    placeholder: (name: string) => `„${name}” se construiește într-o felie următoare.`,
  },

  dashboard: {
    multiSelect: "Selectează mai mulți",
    multiSelectDone: "Gata",
    selected: (n: number) => (n === 1 ? "1 elev selectat" : `${n} elevi selectați`),
    applyToSelected: "Acordă celor selectați",
    wholeClass: "Toată clasa",
    emptyRoster: "Încă niciun elev în această clasă.",
    emptyRosterHint: "Adaugă elevi din Setări.",
  },

  leaderboard: {
    title: "Clasament",
    subtitle: "După total câștigat",
    empty: "Încă fără activitate.",
  },

  award: {
    available: "disponibile",
    notePlaceholder: "Notă opțională…",
    givePositive: "Acordă",
    giveNegative: "Scade",
    noBehaviors: "Niciun comportament definit. Adaugă-le din Setări.",
    applyingTo: (n: number) => (n === 1 ? "1 elev" : `${n} elevi`),
    saving: "Se aplică…",
  },

  recent: {
    title: "Activitate recentă",
    empty: "Nicio activitate încă.",
    undo: "Anulează",
    undoing: "Se anulează…",
    undone: "Anulat",
    purchase: "Cumpărare din magazin",
    adjustment: "Ajustare manuală",
    batch: (n: number) => (n === 1 ? "1 elev" : `${n} elevi`),
  },

  shop: {
    pickStudent: "Alege un elev pentru a-i deschide magazinul.",
    back: "Înapoi la elevi",
    available: "disponibile",
    preview: "Avatarul lui",
    equipped: "Echipat",
    equip: "Echipează",
    equipping: "Se echipează…",
    buy: "Cumpără",
    buying: "Se cumpără…",
    insufficient: "Insuficient",
    emptyRoster: "Încă niciun elev în această clasă.",
    // Slot group headings (the locked DiceBear style's always-on layers).
    slots: {
      Hair: "Păr",
      HairColor: "Culoare păr",
      SkinColor: "Ten",
      Eyes: "Ochi",
      Mouth: "Gură",
    },
    rarity: {
      Common: "Comun",
      Rare: "Rar",
      Epic: "Epic",
      Legendary: "Legendar",
    },
  },

  common: {
    loading: "Se încarcă…",
    retry: "Reîncearcă",
    cancel: "Anulează",
  },
} as const;
