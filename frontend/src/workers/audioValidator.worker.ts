/// <reference lib="webworker" />
import { validateAudioFile } from '../lib/audioValidation';

export interface ValidationRequest {
  taskId: string;
  file: File;
}

export interface ValidationReply {
  taskId: string;
  valid: boolean;
  reason?: string;
}

self.onmessage = async (event: MessageEvent<ValidationRequest>) => {
  const { taskId, file } = event.data;

  try {
    const result = await validateAudioFile(file);
    const reply: ValidationReply = { taskId, valid: result.valid, reason: result.reason };
    (self as unknown as Worker).postMessage(reply);
  } catch (error) {
    const reply: ValidationReply = {
      taskId,
      valid: false,
      reason: error instanceof Error ? error.message : 'Error inesperado validando el archivo.',
    };
    (self as unknown as Worker).postMessage(reply);
  }
};
