import { Link } from "react-router";

interface Props {
  titulo: string;
  descripcion: string;
}

/** Módulos previstos pero aún no disponibles en el prototipo. */
export function EnMantenimiento({ titulo, descripcion }: Props) {
  return (
    <section className="mantenimiento">
      <svg viewBox="0 0 24 24" width="56" height="56" aria-hidden>
        <path
          d="M14.7 6.3a4 4 0 0 0-5.4 5.4L3 18l3 3 6.3-6.3a4 4 0 0 0 5.4-5.4l-2.5 2.5-2.4-.6-.6-2.4z"
          fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round"
        />
      </svg>
      <h1>{titulo}</h1>
      <span className="etiqueta-mantenimiento">En mantenimiento</span>
      <p className="tenue">{descripcion}</p>
      <p className="tenue pequeno">Este módulo estará disponible en una próxima versión de ÉGIDA.</p>
      <Link to="/" className="boton">Volver al panel</Link>
    </section>
  );
}
