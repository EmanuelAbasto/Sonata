// Validación de archivos de audio. Es un módulo puro (sin DOM ni React) para
// poder importarlo tanto desde el Web Worker como desde los tests unitarios.

export const ALLOWED_EXTENSIONS = ['mp3', 'm4a'] as const;
export type AllowedExtension = (typeof ALLOWED_EXTENSIONS)[number];

export const ALLOWED_MIME_TYPES = [
  'audio/mpeg',
  'audio/mp3',
  'audio/m4a',
  'audio/x-m4a',
  'audio/mp4',
  'audio/aac',
] as const;

export const MAX_FILE_SIZE_BYTES = 200 * 1024 * 1024; // 200 MB, ajustable

export interface ValidationResult {
  valid: boolean;
  /** Motivo legible para mostrar al usuario cuando valid = false */
  reason?: string;
}

export function getExtension(fileName: string): string {
  const parts = fileName.toLowerCase().split('.');
  return parts.length > 1 ? (parts.pop() as string) : '';
}

export function hasAllowedExtension(fileName: string): boolean {
  const ext = getExtension(fileName);
  return (ALLOWED_EXTENSIONS as readonly string[]).includes(ext);
}

export function hasAllowedMimeType(mimeType: string): boolean {
  if (!mimeType) return true; // algunos navegadores no reportan mimeType; no bloqueamos solo por esto
  return (ALLOWED_MIME_TYPES as readonly string[]).includes(mimeType.toLowerCase());
}

/**
 * Revisa la "firma" binaria (magic bytes) del archivo para confirmar que el
 * contenido real es mp3 o m4a/mp4, y no un archivo renombrado con otra extensión.
 */
export function detectFormatFromBytes(bytes: Uint8Array): AllowedExtension | null {
  if (bytes.length < 4) return null;

  // MP3 con cabecera ID3 (49 44 33 -> "ID3")
  if (bytes[0] === 0x49 && bytes[1] === 0x44 && bytes[2] === 0x33) {
    return 'mp3';
  }

  // MP3 sin ID3: frame sync de 11 bits en 0xFFE0 (cubre MPEG 1/2, capas 1/2/3)
  if (bytes[0] === 0xff && (bytes[1] & 0xe0) === 0xe0) {
    return 'mp3';
  }

  // M4A / MP4: bytes 4-7 deben ser "ftyp"
  if (bytes.length >= 12) {
    const ftyp = String.fromCharCode(bytes[4], bytes[5], bytes[6], bytes[7]);
    if (ftyp === 'ftyp') {
      const brand = String.fromCharCode(bytes[8], bytes[9], bytes[10], bytes[11]).trim();
      const m4aBrands = ['M4A', 'M4B', 'mp42', 'mp41', 'isom', 'iso2', 'M4V'];
      if (m4aBrands.some((b) => brand.startsWith(b))) {
        return 'm4a';
      }
    }
  }

  return null;
}

interface FileLike {
  name: string;
  size: number;
  type: string;
}

export function validateMetadata(file: FileLike): ValidationResult {
  if (!hasAllowedExtension(file.name)) {
    return {
      valid: false,
      reason: `Extensión no permitida. Solo se aceptan ${ALLOWED_EXTENSIONS.join(' y ')}.`,
    };
  }

  if (!hasAllowedMimeType(file.type)) {
    return {
      valid: false,
      reason: `Tipo de archivo no permitido (${file.type || 'desconocido'}).`,
    };
  }

  if (file.size <= 0) {
    return { valid: false, reason: 'El archivo está vacío.' };
  }

  if (file.size > MAX_FILE_SIZE_BYTES) {
    return {
      valid: false,
      reason: `El archivo supera el máximo permitido (${Math.round(
        MAX_FILE_SIZE_BYTES / (1024 * 1024),
      )} MB).`,
    };
  }

  return { valid: true };
}

/**
 * Valida un archivo combinando metadata (extensión/MIME/tamaño) con una
 * verificación del contenido real (magic bytes) leyendo solo los primeros
 * bytes, para detectar archivos renombrados o corruptos.
 */
export async function validateAudioFile(
  file: File | { name: string; size: number; type: string; arrayBuffer: () => Promise<ArrayBuffer> },
): Promise<ValidationResult> {
  const metadataResult = validateMetadata(file);
  if (!metadataResult.valid) return metadataResult;

  const headerSlice =
    'slice' in file && typeof (file as File).slice === 'function'
      ? (file as File).slice(0, 16)
      : file;

  const buffer =
    'arrayBuffer' in headerSlice
      ? await (headerSlice as Blob).arrayBuffer()
      : await file.arrayBuffer();

  const detected = detectFormatFromBytes(new Uint8Array(buffer));
  const declaredExt = getExtension(file.name) as AllowedExtension;

  if (!detected) {
    return {
      valid: false,
      reason: 'El contenido del archivo no coincide con un audio mp3 o m4a válido.',
    };
  }

  if (detected !== declaredExt) {
    return {
      valid: false,
      reason: `El archivo dice ser .${declaredExt} pero su contenido parece .${detected}.`,
    };
  }

  return { valid: true };
}
