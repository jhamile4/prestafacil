import { CheckCircle2, XCircle } from 'lucide-react';
import type { Evaluacion } from '../api/types';
import { soles } from '../lib/format';

/** Semáforo de las reglas RN-04 a RN-07, con el motivo de cada una. */
export function EvaluacionPanel({ evaluacion }: { evaluacion: Evaluacion }) {
  // Qué parte del límite (40 % del ingreso neto) ocupa la cuota nueva
  const uso = evaluacion.limiteCuotas > 0 ? Math.min(1, evaluacion.cuotaNueva / evaluacion.limiteCuotas) : 1;
  const capacidadOk = evaluacion.reglas.find((r) => r.codigo === 'RN-04')?.cumple ?? true;

  return (
    <div className="space-y-4">
      <div className={`flex items-center gap-3 rounded-lg px-4 py-3 ${evaluacion.apto ? 'bg-emerald-50 text-emerald-800' : 'bg-red-50 text-red-800'}`}>
        {evaluacion.apto ? <CheckCircle2 className="size-6" /> : <XCircle className="size-6" />}
        <div>
          <p className="font-semibold">{evaluacion.apto ? 'Cliente apto' : 'No cumple la evaluación'}</p>
          <p className="text-sm opacity-80">
            Límite de cuotas: <span className="tabular font-semibold">{soles(evaluacion.limiteCuotas)}</span> (40 % del ingreso neto de {soles(evaluacion.ingresoNeto)})
          </p>
        </div>
      </div>

      <div>
        <div className="mb-1 flex justify-between text-xs text-stone-500">
          <span>Cuota nueva {soles(evaluacion.cuotaNueva)}</span>
          <span>Límite {soles(evaluacion.limiteCuotas)}</span>
        </div>
        <div className="h-2 rounded-full bg-stone-100" role="presentation">
          <div className={`h-2 rounded-full ${capacidadOk ? 'bg-marca-500' : 'bg-red-500'}`} style={{ width: `${uso * 100}%` }} />
        </div>
      </div>

      <ul className="space-y-2">
        {evaluacion.reglas.map((r) => (
          <li key={r.codigo} className="flex gap-3 rounded-lg border border-stone-100 px-3 py-2.5">
            {r.cumple ? <CheckCircle2 className="mt-0.5 size-5 shrink-0 text-emerald-600" aria-label="Cumple" /> : <XCircle className="mt-0.5 size-5 shrink-0 text-red-600" aria-label="No cumple" />}
            <div>
              <p className="text-sm font-medium text-stone-900">
                {r.descripcion} <span className="text-xs font-normal text-stone-400">{r.codigo}</span>
              </p>
              <p className="text-sm text-stone-600">{r.detalle}</p>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}
