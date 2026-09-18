import { describe, expect, it } from 'vitest';
import {
  detectFormatFromBytes,
  getExtension,
  hasAllowedExtension,
  hasAllowedMimeType,
  validateAudioFile,
  validateMetadata,
} from '@/lib/audioValidation';

function makeFile(name: string, type: string, bytes: number[]): File {
  const blob = new Blob([new Uint8Array(bytes)], { type });
  return new File([blob], name, { type });
}

const MP3_ID3_HEADER = [0x49, 0x44, 0x33, 0x03, 0, 0, 0, 0, 0, 0, 0, 0];
const MP3_FRAME_HEADER = [0xff, 0xfb, 0x90, 0x00, 0, 0, 0, 0, 0, 0, 0, 0];
const M4A_HEADER = [
  0, 0, 0, 0x20, 0x66, 0x74, 0x79, 0x70, 0x4d, 0x34, 0x41, 0x20, 0, 0, 0, 0,
]; // ....ftypM4A ....
const PNG_HEADER = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

describe('getExtension / hasAllowedExtension', () => {
  it('extrae la extensión en minúsculas', () => {
    expect(getExtension('Cancion.MP3')).toBe('mp3');
    expect(getExtension('nota.de.voz.m4a')).toBe('m4a');
  });

  it('rechaza extensiones distintas a mp3/m4a', () => {
    expect(hasAllowedExtension('audio.wav')).toBe(false);
    expect(hasAllowedExtension('audio.mp3')).toBe(true);
    expect(hasAllowedExtension('audio.m4a')).toBe(true);
  });
});

describe('hasAllowedMimeType', () => {
  it('acepta los MIME conocidos de mp3/m4a', () => {
    expect(hasAllowedMimeType('audio/mpeg')).toBe(true);
    expect(hasAllowedMimeType('audio/x-m4a')).toBe(true);
  });

  it('rechaza otros MIME declarados', () => {
    expect(hasAllowedMimeType('video/mp4')).toBe(false);
  });
});

describe('detectFormatFromBytes', () => {
  it('detecta mp3 por cabecera ID3', () => {
    expect(detectFormatFromBytes(new Uint8Array(MP3_ID3_HEADER))).toBe('mp3');
  });

  it('detecta mp3 por frame sync sin ID3', () => {
    expect(detectFormatFromBytes(new Uint8Array(MP3_FRAME_HEADER))).toBe('mp3');
  });

  it('detecta m4a por el box ftyp', () => {
    expect(detectFormatFromBytes(new Uint8Array(M4A_HEADER))).toBe('m4a');
  });

  it('devuelve null para contenido que no es audio soportado', () => {
    expect(detectFormatFromBytes(new Uint8Array(PNG_HEADER))).toBeNull();
  });
});

describe('validateMetadata', () => {
  it('rechaza archivos vacíos', () => {
    const file = makeFile('vacio.mp3', 'audio/mpeg', []);
    expect(validateMetadata(file).valid).toBe(false);
  });

  it('rechaza extensión no permitida', () => {
    const file = makeFile('audio.wav', 'audio/wav', [1, 2, 3]);
    expect(validateMetadata(file).valid).toBe(false);
  });
});

describe('validateAudioFile (extensión + magic bytes)', () => {
  it('acepta un mp3 real con extensión correcta', async () => {
    const file = makeFile('cancion.mp3', 'audio/mpeg', MP3_ID3_HEADER);
    await expect(validateAudioFile(file)).resolves.toEqual({ valid: true });
  });

  it('acepta un m4a real con extensión correcta', async () => {
    const file = makeFile('nota.m4a', 'audio/mp4', M4A_HEADER);
    await expect(validateAudioFile(file)).resolves.toEqual({ valid: true });
  });

  it('bloquea un archivo renombrado (png disfrazado de mp3)', async () => {
    const file = makeFile('falso.mp3', 'audio/mpeg', PNG_HEADER);
    const result = await validateAudioFile(file);
    expect(result.valid).toBe(false);
    expect(result.reason).toBeTruthy();
  });

  it('bloquea un m4a real pero guardado con extensión .mp3', async () => {
    const file = makeFile('cruzado.mp3', 'audio/mpeg', M4A_HEADER);
    const result = await validateAudioFile(file);
    expect(result.valid).toBe(false);
  });
});
