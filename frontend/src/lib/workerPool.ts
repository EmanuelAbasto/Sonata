import type { ValidationReply, ValidationRequest } from '../workers/audioValidator.worker';

type PendingTask = {
  resolve: (reply: ValidationReply) => void;
};

/**
 * Pool simple de Web Workers: reparte archivos entre varios hilos (workers)
 * para validarlos en paralelo real. El tamaño por defecto sigue el número
 * de núcleos lógicos del dispositivo (con un techo razonable).
 */
export class AudioValidationWorkerPool {
  private workers: Worker[] = [];
  private nextWorker = 0;
  private pending = new Map<string, PendingTask>();

  constructor(size: number = Math.min(4, Math.max(2, navigator.hardwareConcurrency || 2))) {
    for (let i = 0; i < size; i += 1) {
      const worker = new Worker(new URL('../workers/audioValidator.worker.ts', import.meta.url), {
        type: 'module',
      });
      worker.onmessage = (event: MessageEvent<ValidationReply>) => {
        const task = this.pending.get(event.data.taskId);
        if (task) {
          task.resolve(event.data);
          this.pending.delete(event.data.taskId);
        }
      };
      this.workers.push(worker);
    }
  }

  validate(file: File): Promise<ValidationReply> {
    const taskId = `${file.name}-${file.size}-${crypto.randomUUID()}`;
    const worker = this.workers[this.nextWorker];
    this.nextWorker = (this.nextWorker + 1) % this.workers.length;

    return new Promise((resolve) => {
      this.pending.set(taskId, { resolve });
      const request: ValidationRequest = { taskId, file };
      worker.postMessage(request);
    });
  }

  validateAll(files: File[]): Promise<ValidationReply[]> {
    return Promise.all(files.map((file) => this.validate(file)));
  }

  terminate() {
    this.workers.forEach((worker) => worker.terminate());
    this.pending.clear();
  }
}

let sharedPool: AudioValidationWorkerPool | null = null;

export function getAudioValidationPool(): AudioValidationWorkerPool {
  if (!sharedPool) {
    sharedPool = new AudioValidationWorkerPool();
  }
  return sharedPool;
}
