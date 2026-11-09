import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter, Route, Routes } from "react-router";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ProveedorSesion, RutaProtegida } from "./autenticacion/ContextoSesion";
import { Layout } from "./componentes/Layout";
import { Activos } from "./paginas/Activos";
import { Alertas } from "./paginas/Alertas";
import { Dashboard } from "./paginas/Dashboard";
import { DetalleEscaneo } from "./paginas/DetalleEscaneo";
import { Escaneos } from "./paginas/Escaneos";
import { EnMantenimiento } from "./paginas/EnMantenimiento";
import { Login } from "./paginas/Login";
import "./estilos.css";

const clienteConsultas = new QueryClient({
  defaultOptions: { queries: { staleTime: 5_000, refetchOnWindowFocus: true, retry: 1 } },
});

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryClientProvider client={clienteConsultas}>
      <BrowserRouter>
        <ProveedorSesion>
          <Routes>
            <Route path="/login" element={<Login />} />
            <Route element={<RutaProtegida><Layout /></RutaProtegida>}>
              <Route index element={<Dashboard />} />
              <Route path="activos" element={<Activos />} />
              <Route path="escaneos" element={<Escaneos />} />
              <Route path="escaneos/:id" element={<DetalleEscaneo />} />
              <Route path="alertas" element={<Alertas />} />
              <Route path="metricas" element={<EnMantenimiento titulo="Métricas" descripcion="Gráficas de hallazgos por severidad y categoría OWASP, evolución por escaneo y tiempos promedio." />} />
              <Route path="reportes" element={<EnMantenimiento titulo="Reportes" descripcion="Exportación de resultados de escaneo en PDF y CSV." />} />
              <Route path="auditoria" element={<EnMantenimiento titulo="Auditoría" descripcion="Registro de acciones: inicios de sesión, activos registrados y escaneos lanzados." />} />
              <Route path="usuarios" element={<EnMantenimiento titulo="Usuarios" descripcion="Alta, edición y desactivación de cuentas de clientes y administradores." />} />
              <Route path="configuracion" element={<EnMantenimiento titulo="Configuración" descripcion="Preferencias de notificación, perfil y parámetros de escaneo." />} />
              <Route path="*" element={<p>Página no encontrada.</p>} />
            </Route>
          </Routes>
        </ProveedorSesion>
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
);
