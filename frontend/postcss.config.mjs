// Tailwind CSS v4 is wired through its dedicated PostCSS plugin (slice #20). No tailwind.config.js
// is needed — v4 is configured from CSS via the @theme block in globals.css.
const config = {
  plugins: {
    "@tailwindcss/postcss": {},
  },
};

export default config;
