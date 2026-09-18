#!/bin/bash
set -e

echo "============================================================"
echo "🚀 Iniciando Entrypoint - Verificando modelo Faster-Whisper..."
echo "Modelo solicitado: ${WHISPER_MODEL}"
echo "============================================================"

# Usamos python3 -u y leemos la variable de entorno DENTRO del script Python con os.environ
python3 -u -c "
import os
import sys
from faster_whisper import WhisperModel

# Leer la variable de entorno directamente desde Python
model_name = os.environ.get('WHISPER_MODEL', 'small')
print(f'[ENTRYPOINT] Descargando/precargando modelo: {model_name}...')

# Carga en CPU para descargar los pesos a la caché del usuario (volumen persistente)
model = WhisperModel(model_name, device='cpu', compute_type='float32')
print(f'[ENTRYPOINT] Modelo {model_name} cargado y cacheado exitosamente en CPU.')
" || echo "⚠️  Error en la precarga. El modelo se descargará bajo demanda durante la transcripción."

echo "✅ Entrypoint completado. Iniciando aplicación .NET..."
exec dotnet AudioUploader.Web.dll