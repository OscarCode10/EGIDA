import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// En desarrollo, /api y /hubs van a la API local; en Docker los atiende nginx (mismo origen, sin CORS).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": "http://localhost:5080",
      "/hubs": { target: "http://localhost:5080", ws: true },
    },
  },
});
