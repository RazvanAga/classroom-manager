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

  common: {
    loading: "Se încarcă…",
    retry: "Reîncearcă",
    cancel: "Anulează",
  },
} as const;
