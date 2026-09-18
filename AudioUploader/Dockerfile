# ============================================
# Variables Globales
# ============================================
ARG DOTNET_VERSION=10.0-preview
ARG CUDA_VERSION=12.8.0
ARG UBUNTU_VERSION=ubuntu22.04
ARG HF_TOKEN

# ============================================
# Build Stage
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY src/AudioUploader.Domain/*.csproj AudioUploader.Domain/
COPY src/AudioUploader.Application/*.csproj AudioUploader.Application/
COPY src/AudioUploader.Infrastructure/*.csproj AudioUploader.Infrastructure/
COPY src/AudioUploader.Web/*.csproj AudioUploader.Web/

RUN dotnet restore AudioUploader.Web/AudioUploader.Web.csproj

COPY src/ .
RUN dotnet publish AudioUploader.Web/AudioUploader.Web.csproj \
    -c Release -o /app/publish --no-restore

# ============================================
# Runtime Stage (CUDA + .NET Runtime)
# ============================================
FROM nvidia/cuda:${CUDA_VERSION}-runtime-${UBUNTU_VERSION} AS runtime

RUN groupadd -r appuser && useradd -r -m -g appuser appuser

WORKDIR /app

COPY src/scripts/requirements.txt /app/scripts/requirements.txt

RUN apt-get update && apt-get install -y --no-install-recommends \
        curl \
        python3 python3-pip python3-venv \
        ffmpeg \
    && curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin \
        --runtime aspnetcore \
        --channel 10.0 \
        --install-dir /usr/share/dotnet \
    && pip3 install --no-cache-dir -r /app/scripts/requirements.txt \
    && apt-get purge -y curl \
    && apt-get autoremove -y \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/* /root/.cache/pip

ENV PATH="/usr/share/dotnet:${PATH}"

# ============================================
# Configuración del usuario, scripts y entrypoint
# ============================================
COPY src/scripts/ /app/scripts/
RUN chmod +x /app/scripts/*.py

COPY src/scripts/entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

RUN mkdir -p /home/appuser/.cache/whisper && \
    chown -R appuser:appuser /home/appuser/.cache

COPY --from=build /app/publish /app
RUN chown -R appuser:appuser /app

ENV HOME=/home/appuser
ENV XDG_CACHE_HOME=/home/appuser/.cache

USER appuser

EXPOSE 5247

# ============================================
# ENTRADA PRINCIPAL (Esto ejecuta el entrypoint)
# ============================================
ENTRYPOINT ["/app/entrypoint.sh"]