import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Production is a static export served by Nginx — no Node runtime (design.md §8.2).
  output: "export",

  // Dev only: proxy /api to the local API so cookies stay same-origin. In production the
  // reverse proxy (Nginx, slice #17) serves the static app and the API under one origin,
  // so this rewrite is intentionally ignored by `output: export`.
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: "http://localhost:5080/api/:path*",
      },
    ];
  },
};

export default nextConfig;
