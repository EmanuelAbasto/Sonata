import type { HubConnection } from '@microsoft/signalr';
import { createJobHubConnection, JOB_HUB_EVENTS, type JobHubEvent, type JobHubPayload } from './signalr';

type Listener = (event: JobHubEvent, payload: JobHubPayload) => void;

/**
 * Una sola conexión SignalR compartida por toda la app. Permite que varias
 * partes de la UI (sidebar, página de detalle, notificaciones globales) se
 * "unan" al grupo de un job sin abrir una conexión de WebSocket por cada
 * componente, y sin duplicar el JoinJobGroup si dos componentes escuchan el
 * mismo job a la vez (usa conteo de referencias).
 */
class JobHubManager {
  private connection: HubConnection | null = null;
  private startPromise: Promise<void> | null = null;
  private refCounts = new Map<string, number>();
  private jobListeners = new Map<string, Set<Listener>>();
  private globalListeners = new Set<Listener>();

  private ensureConnection(): HubConnection {
    if (!this.connection) {
      this.connection = createJobHubConnection();
      JOB_HUB_EVENTS.forEach((eventName) => {
        this.connection!.on(eventName, (payload: JobHubPayload) => this.dispatch(eventName, payload));
      });
    }
    return this.connection;
  }

  private dispatch(event: JobHubEvent, payload: JobHubPayload) {
    this.jobListeners.get(payload.jobId)?.forEach((listener) => listener(event, payload));
    this.globalListeners.forEach((listener) => listener(event, payload));
  }

  private start(): Promise<void> {
    const connection = this.ensureConnection();
    if (connection.state === 'Connected') return Promise.resolve();
    if (!this.startPromise) {
      this.startPromise = connection.start().catch((error) => {
        this.startPromise = null;
        throw error;
      });
    }
    return this.startPromise;
  }

  /** Se une al grupo del job y escucha sus eventos. Devuelve la función para salir. */
  async subscribeJob(jobId: string, listener: Listener): Promise<() => void> {
    const listeners = this.jobListeners.get(jobId) ?? new Set<Listener>();
    listeners.add(listener);
    this.jobListeners.set(jobId, listeners);

    const previousCount = this.refCounts.get(jobId) ?? 0;
    this.refCounts.set(jobId, previousCount + 1);

    try {
      await this.start();
      if (previousCount === 0) {
        await this.connection!.invoke('JoinJobGroup', jobId);
      }
    } catch (error) {
      console.error(`No se pudo unir al grupo del job ${jobId}`, error);
    }

    return () => {
      this.jobListeners.get(jobId)?.delete(listener);
      const remaining = (this.refCounts.get(jobId) ?? 1) - 1;
      if (remaining <= 0) {
        this.refCounts.delete(jobId);
        this.jobListeners.delete(jobId);
        this.connection?.invoke('LeaveJobGroup', jobId).catch(() => undefined);
      } else {
        this.refCounts.set(jobId, remaining);
      }
    };
  }

  /** Escucha todos los eventos de todos los jobs a los que ESTA app ya esté unida. */
  subscribeAll(listener: Listener): () => void {
    this.globalListeners.add(listener);
    this.start().catch(() => undefined);
    return () => this.globalListeners.delete(listener);
  }
}

export const jobHubManager = new JobHubManager();
