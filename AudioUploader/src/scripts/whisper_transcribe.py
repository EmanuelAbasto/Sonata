#!/usr/bin/env python3
import argparse
import json
import os
import sys
import time
import pika
from faster_whisper import WhisperModel

def log_info(msg):
    print(f"[INFO] {msg}", file=sys.stderr)

def log_error(msg):
    print(f"[ERROR] {msg}", file=sys.stderr)


def build_rabbitmq_channel():
    """Conexión única para todo el batch (no una por archivo)."""
    host = os.getenv("RABBITMQ_HOST", "rabbitmq")
    port = int(os.getenv("RABBITMQ_PORT", "5672"))
    user = os.getenv("RABBITMQ_USER", "guest")
    password = os.getenv("RABBITMQ_PASSWORD", "guest")
    vhost = os.getenv("RABBITMQ_VHOST", "/")
    queue = os.getenv("RABBITMQ_TRANSCRIPTION_QUEUE", "transcription-results")

    credentials = pika.PlainCredentials(user, password)
    parameters = pika.ConnectionParameters(host=host, port=port, virtual_host=vhost, credentials=credentials)
    connection = pika.BlockingConnection(parameters)
    channel = connection.channel()
    channel.queue_declare(queue=queue, durable=True)
    return connection, channel, queue


def publish_result(channel, queue, payload):
    channel.basic_publish(
        exchange="",
        routing_key=queue,
        body=json.dumps(payload, ensure_ascii=False),
        properties=pika.BasicProperties(
            content_type="application/json",
            delivery_mode=2,  # persistente
        ),
    )


def main():
    default_model = os.getenv("WHISPER_MODEL", "large-v3-turbo")

    parser = argparse.ArgumentParser(description="Faster-Whisper transcription batch")
    parser.add_argument("--files", nargs="+", required=True, help="List of audio file paths")
    parser.add_argument("--job-ids", nargs="+", required=True, help="Job GUIDs, same order as --files")
    parser.add_argument("--model", default=default_model, help="Whisper model")
    parser.add_argument("--language", default="es", help="Language code")
    parser.add_argument("--device", default="cuda", choices=["cuda", "cpu"], help="Device")
    parser.add_argument("--compute-type", default="float16", choices=["float16", "int8", "float32"], help="Compute type")
    args = parser.parse_args()

    if len(args.files) != len(args.job_ids):
        log_error(f"Mismatch: {len(args.files)} files vs {len(args.job_ids)} job-ids")
        sys.exit(1)

    log_info(f"Device: {args.device.upper()}")
    log_info(f"Loading Faster-Whisper model '{args.model}'...")
    start_load = time.time()
    model = WhisperModel(args.model, device=args.device, compute_type=args.compute_type)
    log_info(f"Model loaded in {time.time() - start_load:.2f}s.")

    connection, channel, queue = build_rabbitmq_channel()
    log_info(f"Connected to RabbitMQ, publishing to queue '{queue}'")

    try:
        for idx, (file_path, job_id) in enumerate(zip(args.files, args.job_ids), 1):
            log_info(f"Processing file {idx}/{len(args.files)}: {os.path.basename(file_path)}")

            if not os.path.exists(file_path):
                publish_result(channel, queue, {
                    "jobId": job_id,
                    "text": "",
                    "language": None,
                    "confidence": None,
                    "durationMs": 0,
                    "success": False,
                    "error": f"File not found: {file_path}",
                })
                continue

            try:
                start_transcribe = time.time()
                segments, info = model.transcribe(
                    file_path,
                    language=args.language,
                    beam_size=5,
                    best_of=5,
                    vad_filter=True,
                )
                transcript = "".join([seg.text for seg in segments]).strip()
                duration_ms = int((time.time() - start_transcribe) * 1000)

                log_info(f"Transcription completed in {duration_ms}ms. {len(transcript)} characters.")

                # 🟢 Se publica INMEDIATAMENTE, apenas termina ESTE archivo — no se espera
                # a que el resto del batch termine para que el consumer se entere.
                publish_result(channel, queue, {
                    "jobId": job_id,
                    "text": transcript,
                    "language": info.language,
                    "confidence": info.language_probability,
                    "durationMs": duration_ms,
                    "success": True,
                    "error": None,
                })
            except Exception as e:
                log_error(f"Error transcribing {file_path}: {str(e)}")
                publish_result(channel, queue, {
                    "jobId": job_id,
                    "text": "",
                    "language": None,
                    "confidence": None,
                    "durationMs": 0,
                    "success": False,
                    "error": str(e),
                })
    finally:
        connection.close()

    log_info("Batch finished, all results published.")


if __name__ == "__main__":
    main()