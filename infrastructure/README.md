# Audio Uploader - Infraestructura (Laboratory-7)

Este repositorio contiene la configuración Docker Compose para levantar todos los servicios necesarios para ejecutar la **API de Audio Uploader** con procesamiento completo (subida, compresión, transcripción y resumen) en una arquitectura monolítica con colas internas.

A diferencia de versiones anteriores, este sistema **no depende de RabbitMQ ni de workers externos**. Todo el procesamiento se realiza dentro del mismo contenedor utilizando `System.Threading.Channels`, `Task.WhenAll` y `SemaphoreSlim` para la concurrencia, y `BatchQueueService` para el procesamiento por lotes de transcripción y resumen. Esto simplifica el despliegue, reduce la latencia y mejora la eficiencia al eliminar la comunicación entre servicios.

La imagen de la aplicación está empaquetada y disponible en el Container Registry de GitLab. Al ejecutar `docker compose up`, la imagen se descargará automáticamente.

---

## Estructura del repositorio

```
infrastructure/
├── k6/
│   └── load-test.js          # Script de pruebas de carga (k6)
├── minio_data/               # Datos persistentes de MinIO (generado automáticamente)
├── .env                      # Variables de entorno (crear a partir de .env.example)
├── .env.example              # Ejemplo de variables de entorno
├── .gitignore
├── docker-compose.yml
└── README.md                 # Este documento
```

---

## Requisitos previos

- Docker (versión 20.10 o superior)
- Docker Compose (versión 2.0 o superior)
- Acceso a Internet (para descargar imágenes base y la imagen de la aplicación)
- **Acceso al Container Registry de GitLab** (requiere autenticación con token)
- **NVIDIA Container Toolkit** (si se usa GPU, para acelerar Whisper)
- **k6** (opcional, para ejecutar pruebas de carga)

---

## Importante: Autenticación en el Container Registry

Las imágenes de la aplicación están alojadas en el Container Registry de GitLab, que es privado. Para poder descargarlas, debes autenticarte con un **Personal Access Token** con permisos `read_registry`.

### Cómo obtener un token de acceso personal

1. Inicia sesión en GitLab.
2. Ve a tu perfil (icono de usuario en la esquina superior derecha) → **Settings** → **Access Tokens**.
3. En **"Token name"**, escribe `docker-registry` (o cualquier nombre descriptivo).
4. En **"Expiration date"**, elige una fecha futura (por ejemplo, 1 año).
5. En **"Select scopes"**, marca **`read_registry`** (solo lectura del registro).
6. Haz clic en **"Create personal access token"**.
7. **Copia el token** (aparece solo una vez) y guárdalo en un lugar seguro.

### Iniciar sesión en Docker

Ejecuta el siguiente comando, reemplazando `TU_USUARIO` por tu nombre de usuario de GitLab y `TU_TOKEN` por el token que copiaste:

```bash
echo "TU_TOKEN" | docker login registry.gitlab.com -u TU_USUARIO --password-stdin
```

Si el login es exitoso, verás `Login Succeeded`.

---

## Pasos para levantar el entorno

### 1. Clonar el repositorio y cambiar a la rama correcta

```bash
git clone https://gitlab.com/jala-university1/cohort-2/ES.CSPR-471.GA.T2.26.M1/SB/emanuel.abasto/infrastructure.git
cd infrastructure
git checkout feature/Laboratory-7
```

### 2. Configurar las variables de entorno

Copia el archivo de ejemplo y ajústalo según tus necesidades:

```bash
cp .env.example .env
```

Edita el archivo `.env` para configurar las credenciales de la base de datos, MinIO, y los parámetros de procesamiento (tamaño de lote, intervalo, concurrencia, etc.). Los valores por defecto son suficientes para pruebas locales, pero se recomienda revisarlos.

### 3. Levantar todos los servicios

Ejecuta el siguiente comando para iniciar todos los contenedores en segundo plano:

```bash
docker compose up -d
```

Esto levantará:

- **PostgreSQL** (base de datos)
- **MinIO** (almacenamiento de objetos)
- **Ollama** (servicio para resúmenes con modelos locales)
- **Ollama-init** (inicializador que descarga el modelo `llama3.2` una sola vez)
- **App** (Audio Uploader - aplicación monolítica con todos los servicios)
- **Frontend** (interfaz web de usuario)

La primera vez que se ejecute, Docker descargará las imágenes necesarias (puede tomar varios minutos). Si la descarga falla por problemas de autenticación, verifica el paso de login.

### 4. Verificar que todo esté corriendo

```bash
docker compose ps
```

Todos los servicios deberían mostrar estado `Up` y `healthy`. Verás los contenedores:
- `audio-uploader-postgres`
- `audio-uploader-minio`
- `audio-uploader-ollama`
- `audio-uploader-app`
- `audio-uploader-frontend`

---

## Servicios y puertos

Una vez levantado el entorno, los servicios estarán disponibles en los siguientes puertos (desde el host donde corre Docker):

| Servicio | Puerto (host) | URL / Comando de acceso | Credenciales (por defecto) |
| :--- | :--- | :--- | :--- |
| API (Swagger) | `5247` | `http://<IP_HOST>:5247/swagger` | Sin autenticación |
| API (Endpoint) | `5247` | `http://<IP_HOST>:5247/api/audio` | Sin autenticación |
| **Métricas (Dashboard)** | `5247` | `http://<IP_HOST>:5247/dashboard.html` | Sin autenticación |
| **Métricas (JSON)** | `5247` | `http://<IP_HOST>:5247/api/metrics` | Sin autenticación |
| **Frontend** | `80` | `http://<IP_HOST>:80` | Sin autenticación |
| MinIO Console | `9001` | `http://<IP_HOST>:9001` | `minioadmin` / `minioadmin` |
| MinIO API | `9000` | Usado internamente | `minioadmin` / `minioadmin` |
| Ollama API | `11434` | `http://<IP_HOST>:11434` | Sin autenticación |
| PostgreSQL | `5432` | `psql -h <IP_HOST> -p 5432 -U postgres` | `postgres` / `postgres123` |

Nota: Reemplaza `<IP_HOST>` por la dirección IP de la máquina donde corre Docker (ej. `192.168.1.9`). Si estás ejecutando localmente, usa `localhost`.

---

## Prueba rápida de la API

Puedes probar la API subiendo uno o varios archivos de audio. A continuación se muestran ejemplos para Windows (PowerShell) y Linux/macOS.

### Windows (PowerShell)

```powershell
# Subir un archivo
curl.exe -X POST http://localhost:5247/api/audio -F "files=@C:\ruta\a\tu\audio.mp3"

# Subir múltiples archivos
curl.exe -X POST http://localhost:5247/api/audio `
  -F "files=@C:\ruta\a\audio1.mp3" `
  -F "files=@C:\ruta\a\audio2.mp3"
```

### Linux / macOS

```bash
# Subir un archivo
curl -X POST http://localhost:5247/api/audio -F "files=@/ruta/a/tu/audio.mp3"

# Subir múltiples archivos
curl -X POST http://localhost:5247/api/audio \
  -F "files=@/ruta/a/audio1.mp3" \
  -F "files=@/ruta/a/audio2.mp3"
```

### Respuesta esperada

```json
{
  "statusCode": 200,
  "response": [
    {
      "jobId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "audioId": 1,
      "fileName": "audio.mp3",
      "status": "Pending"
    }
  ],
  "error": null
}
```

El estado `Pending` indica que el archivo se ha recibido y está en cola para ser procesado (subida a MinIO, compresión, transcripción y resumen).

---

## Pruebas de carga con k6

El repositorio incluye un script de pruebas de carga en la carpeta `k6/load-test.js`. Este script simula múltiples usuarios subiendo archivos de audio y consultando el estado de sus jobs, permitiendo medir el rendimiento de la API bajo carga.

### Requisitos para ejecutar las pruebas

- Tener **k6** instalado ([Instrucciones de instalación](https://grafana.com/docs/k6/latest/set-up/install-k6/))
- La aplicación debe estar corriendo (ejecutar `docker compose up -d` primero)
- El archivo de prueba debe existir en la ruta especificada

### Ejecutar la prueba de carga

Desde la raíz del repositorio (carpeta `infrastructure/`), ejecuta:

```bash
k6 run -e BASE_URL="http://localhost:5247" \
       -e TEST_FILE="C:/Users/EMANUEL/Postman/files/Aladino y la lampara maravillosa.mp3" \
       -e WAIT_TIME=2 \
       -e BATCH_SIZE=10 \
       k6/load-test.js
```

**Explicación de las variables de entorno:**

| Variable | Descripción | Ejemplo |
| :--- | :--- | :--- |
| `BASE_URL` | URL base de la API (debe coincidir con el puerto expuesto) | `http://localhost:5247` (o la IP del host donde corre Docker) |
| `TEST_FILE` | Ruta absoluta al archivo de audio que se usará para las subidas | `C:/Users/EMANUEL/Postman/files/Aladino y la lampara maravillosa.mp3` |
| `WAIT_TIME` | Tiempo de espera (en segundos) entre la subida y la consulta de estado | `2` |
| `BATCH_SIZE` | Tamaño de lote para las consultas de listado | `10` |

**Nota:** Si la aplicación corre en una máquina remota, reemplaza `localhost` por la IP de esa máquina (ej. `http://192.168.1.9:5247`).

### Interpretación de los resultados

Al finalizar la prueba, k6 mostrará un resumen con métricas como:

- **Iteraciones completadas:** número total de flujos completados (subida + estado + listado).
- **Tasa de error:** porcentaje de peticiones fallidas (debe ser 0% en un sistema saludable).
- **P95 http_req_duration:** percentil 95 de la latencia de las peticiones HTTP (debe cumplir con el umbral definido en el script).
- **P95 upload_duration:** tiempo de subida a la API (incluye procesamiento inicial).

Puedes ajustar los parámetros de la prueba (número de VUs, duración, etc.) modificando el script `load-test.js` en la sección `options.stages`.

---

## Verificar el procesamiento completo

El sistema procesa automáticamente los audios subidos en el siguiente flujo:

1. **Subida a MinIO** (en paralelo con la compresión)
2. **Compresión a AAC** con FFmpeg (en paralelo con la subida)
3. **Transcripción** con Whisper (modelo `large-v3-turbo` o el configurado) en lotes de hasta 20 archivos o cada 120 segundos.
4. **Resumen** con Ollama (modelo `llama3.2`) en lotes similares.

### Logs de la aplicación

Para ver los logs en tiempo real:

```bash
docker compose logs -f app
```

Deberías ver mensajes como:

```
info: AudioUploader.Application.Services.AudioService[0]
      Temporary file saved at /tmp/audio_uploads/... for job X
info: AudioUploader.Infrastructure.Background.JobOrchestrator[0]
      Starting background processing for job X
info: AudioUploader.Infrastructure.Background.JobOrchestrator[0]
      Upload to MinIO completed for job X
info: AudioUploader.Infrastructure.Background.JobOrchestrator[0]
      Compression completed for job X
info: AudioUploader.Infrastructure.Background.BatchQueueService[0]
      Job X added to transcription batch (accumulated: N)
info: AudioUploader.Infrastructure.Background.BatchQueueService[0]
      Processing transcription batch of N jobs
info: AudioUploader.Infrastructure.Processing.WhisperTranscriptionService[0]
      [Whisper stderr] Device: CUDA
info: AudioUploader.Infrastructure.Processing.WhisperTranscriptionService[0]
      [Whisper stderr] Model loaded in 4.2s.
info: AudioUploader.Infrastructure.Processing.WhisperTranscriptionService[0]
      Transcription completed in 12.3s for file X.
info: AudioUploader.Infrastructure.Background.BatchQueueService[0]
      Job X completed successfully
```

### Verificar en PostgreSQL

Conéctate a la base de datos y consulta el estado de los Jobs:

```bash
docker exec -it audio-uploader-postgres psql -U postgres -d audiouploader
```

Luego ejecuta:

```sql
SELECT "Id", "Status", "OriginalFileId", "LightFileId", "TranscriptId" FROM jobs ORDER BY "Id" DESC LIMIT 10;
SELECT COUNT(*), "Status" FROM jobs GROUP BY "Status";
```

### Verificar en MinIO

Accede a la consola de MinIO (`http://<IP_HOST>:9001`) y navega al bucket `audio-uploads`. Deberías ver:
- Una carpeta `uploads/` con los archivos originales.
- Una carpeta `compressed/` con los archivos comprimidos `.aac`.

---

## Panel de métricas

La aplicación incluye un dashboard HTML que muestra las métricas de procesamiento en tiempo real. Puedes acceder a él desde:

```
http://<IP_HOST>:5247/dashboard.html
```

Este dashboard muestra:

- **Tiempos promedio** por etapa (subida, compresión, transcripción, resumen, total).
- **Distribución de tiempos** en gráficos de barras y dispersión.
- **Detalle por job** con tiempos individuales.
- **Información de batches** (tamaño, duración, promedios).

También puedes obtener las métricas en formato JSON para integrarlas con otras herramientas:

```
http://<IP_HOST>:5247/api/metrics?count=50
```

---

## Variables de entorno importantes

El comportamiento del sistema se puede ajustar mediante las siguientes variables en el archivo `.env`:

| Variable | Descripción | Valor por defecto |
| :--- | :--- | :--- |
| `BATCH_SIZE` | Número de jobs por lote de transcripción/resumen | `20` |
| `BATCH_INTERVAL_SECONDS` | Tiempo máximo de espera para llenar un lote | `120` |
| `MAX_CONCURRENT_COMPRESSIONS` | Número máximo de compresiones en paralelo | `8` |
| `WHISPER_MODEL` | Modelo de Whisper a utilizar | `large-v3-turbo` |
| `WHISPER_DEVICE` | Dispositivo para ejecutar Whisper (`cuda` o `cpu`) | `cuda` |
| `WHISPER_COMPUTE_TYPE` | Tipo de precisión para Whisper | `float16` |
| `SUMMARY_MODEL` | Modelo de Ollama para resúmenes | `llama3.2` |
| `OLLAMA_URL` | URL del servicio Ollama | `http://ollama:11434` |
| `HF_TOKEN` | Token de Hugging Face para descargar modelos | (opcional) |

---

## Detener los servicios

Para detener todos los contenedores:

```bash
docker compose down
```

Si también quieres eliminar los volúmenes (datos persistentes):

```bash
docker compose down -v
```

---

## Notas importantes

- **Autenticación en el registro:** Si el repositorio es privado, es obligatorio hacer `docker login` con un token de acceso con alcance `read_registry`. Sin esto, las imágenes no se descargarán.
- **GPU:** El sistema está configurado para usar GPU (NVIDIA) si está disponible. Asegúrate de tener el `nvidia-container-toolkit` instalado y configurado en Docker.
- **Modelos:** La primera vez que se ejecute la aplicación, se descargarán los modelos de Whisper y Ollama (si no están precargados). Esto puede tomar varios minutos.
- **Rendimiento:** El sistema está optimizado para procesar grandes volúmenes de archivos en background. Los parámetros de batch y concurrencia se pueden ajustar según el hardware disponible.
- **Dashboard:** El dashboard HTML se genera estáticamente y se sirve desde la aplicación. No requiere autenticación para entornos de desarrollo.
- **Acceso al Frontend:** La interfaz web está disponible en `http://<IP_HOST>:80` (o `http://localhost:80` si ejecutas localmente).

---

## Soporte

Si encuentras algún problema, verifica los logs de los contenedores:

```bash
docker compose logs -f
```

Para ver los logs de un servicio específico:

```bash
docker compose logs app
docker compose logs postgres
docker compose logs minio
docker compose logs ollama
docker compose logs frontend
```

Si el problema persiste, asegúrate de haber realizado correctamente el `docker login` y de estar en la rama `feature/Laboratory-6`.
```