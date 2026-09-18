# Audio Uploader — Frontend

Frontend en React + TypeScript + Vite para subir archivos de audio (mp3 / m4a)
contra la **Audio Uploader API**, con validación paralela en el cliente usando
Web Workers.

## Cómo correrlo

```bash
npm install
cp .env.example .env   # y ajustá VITE_API_BASE_URL a tu backend
npm run dev
```

Correr los tests:

```bash
npm run test
```

## Qué resuelve cada requisito

- **Mostrar lista de audios subidos**: `ServerFileList` consulta `GET /api/audio`
  al cargar la página.
- **Actualizar la lista tras una nueva subida**: después de un `POST /api/audio`
  exitoso, `useAudioUpload` vuelve a pedir `GET /api/audio` automáticamente.
- **Validar el audio antes de enviarlo**: `src/lib/audioValidation.ts` revisa
  extensión, MIME y los *magic bytes* reales del archivo (cabecera ID3 / frame
  sync para mp3, box `ftyp` para m4a) para detectar archivos renombrados.
- **Bloquear/marcar archivos no válidos**: cada archivo inválido queda
  marcado en rojo con el motivo, y nunca se incluye en el `FormData` que se
  sube al servidor.
- **Programación paralela en el frontend**: `src/lib/workerPool.ts` mantiene
  un pool de Web Workers (`audioValidator.worker.ts`); cada archivo se valida
  en su propio hilo en paralelo, sin bloquear la UI, en vez de validarlos uno
  por uno en el hilo principal.
- **Flujo de subida funcionando**: `uploadValid()` sube con `axios`, todos los
  archivos válidos en un solo `multipart/form-data` (campo `files`), con
  barra de progreso real vía `onUploadProgress`.
- **Conservar pruebas anteriores**: `src/__tests__/audioValidation.test.ts`
  cubre la lógica pura de validación con Vitest; al agregar features nuevas,
  sumá tests ahí en vez de reemplazarlos.

## Estructura

```
src/
  lib/            # tipos, cliente axios, validación pura, utils
  workers/        # Web Worker de validación
  hooks/          # useAudioUpload: estado + orquestación
  components/     # UI (dropzone, listas, cards, botones)
  components/ui/  # kit de componentes propio estilo shadcn/ui
  __tests__/      # Vitest
```

## Diseño

Paleta oscura cálida (fondo casi negro, acento terracota, tipografía serif
para títulos) inspirada en la estética de Claude.ai, usando Tailwind con
tokens propios en `tailwind.config.js`.

## Próximos pasos sugeridos

- Detalle de audio (`GET /api/audio/{id}`) y estado del job en vivo
  (`GET /api/jobs/{jobId}/status`) con polling.
- Paginación real de `ServerFileList` (la API ya la soporta).
- Reintentos automáticos para archivos que fallan al subir.
