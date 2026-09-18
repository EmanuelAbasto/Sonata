"""
Genera resúmenes de textos usando Ollama y publica cada resultado a RabbitMQ
apenas termina (no espera al resto del batch).
"""
import argparse
import json
import sys
import os
import time
import requests
import pika

def log_info(msg):
    print(f"[INFO] {msg}", file=sys.stderr)

def log_warn(msg):
    print(f"[WARN] {msg}", file=sys.stderr)

def log_error(msg):
    print(f"[ERROR] {msg}", file=sys.stderr)


def build_rabbitmq_channel():
    host = os.getenv("RABBITMQ_HOST", "rabbitmq")
    port = int(os.getenv("RABBITMQ_PORT", "5672"))
    user = os.getenv("RABBITMQ_USER", "guest")
    password = os.getenv("RABBITMQ_PASSWORD", "guest")
    vhost = os.getenv("RABBITMQ_VHOST", "/")
    queue = os.getenv("RABBITMQ_SUMMARY_QUEUE", "summary-results")

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
        properties=pika.BasicProperties(content_type="application/json", delivery_mode=2),
    )


def main():
    parser = argparse.ArgumentParser(description="Generate summaries using Ollama")
    parser.add_argument("--texts", required=True, help="Path to file with texts (one per line)")
    parser.add_argument("--job-ids", nargs="+", required=True, help="Job GUIDs, same order as the lines in --texts")
    parser.add_argument("--model", default="llama3.2", help="Ollama model name")
    parser.add_argument("--max-length", type=int, default=300, help="Maximum summary length in characters")
    parser.add_argument("--min-length", type=int, default=50, help="Minimum summary length in characters")
    default_ollama_url = os.getenv("OLLAMA_URL", "http://localhost:11434")
    parser.add_argument("--ollama-url", default=default_ollama_url, help="Ollama API URL")
    args = parser.parse_args()

    if not os.path.exists(args.texts):
        log_error(f"Texts file not found: {args.texts}")
        sys.exit(1)

    with open(args.texts, "r", encoding="utf-8") as f:
        texts = [line.strip() for line in f if line.strip()]

    if len(texts) != len(args.job_ids):
        log_error(f"Mismatch: {len(texts)} texts vs {len(args.job_ids)} job-ids")
        sys.exit(1)

    try:
        response = requests.get(f"{args.ollama_url}/api/tags", timeout=10)
        if response.status_code != 200:
            log_error(f"Ollama not running at {args.ollama_url}")
            sys.exit(1)
        models = [m["name"] for m in response.json().get("models", [])]
        if args.model not in models:
            log_warn(f"Model '{args.model}' is not installed. It will be pulled on first use.")
    except Exception as e:
        log_error(f"Error connecting to Ollama: {e}")
        sys.exit(1)

    connection, channel, queue = build_rabbitmq_channel()
    log_info(f"Connected to RabbitMQ, publishing to queue '{queue}'")

    try:
        for idx, (text, job_id) in enumerate(zip(texts, args.job_ids)):
            try:
                original_text = text
                if len(text) > 4000:
                    text = text[:4000]
                    log_warn(f"Text truncated to 4000 characters for item {idx+1}")

                log_info(f"Generating summary {idx+1}/{len(texts)}...")
                start_summary = time.time()

                prompt = (
                    f"Resume el siguiente texto en español en un resumen claro y conciso, "
                    f"incluyendo los puntos principales. El resumen debe tener entre {args.min_length} "
                    f"y {args.max_length} caracteres aproximadamente. "
                    f"**Devuelve ÚNICAMENTE el resumen, sin introducciones ni frases adicionales.**\n\n"
                    f"Texto:\n{text}\n\n"
                    f"Resumen:"
                )

                payload = {
                    "model": args.model,
                    "prompt": prompt,
                    "stream": False,
                    "options": {"temperature": 0.3, "num_predict": 256},
                }

                response = requests.post(f"{args.ollama_url}/api/generate", json=payload, timeout=120)
                if response.status_code != 200:
                    raise Exception(f"Ollama API error: {response.text}")

                result = response.json()
                summary_text = result.get("response", "").strip()

                prefixes = [
                    "Aquí te presento un resumen:",
                    "Aquí tienes un resumen:",
                    "Resumen:",
                    "Este es el resumen:",
                    "A continuación, el resumen:",
                ]
                for prefix in prefixes:
                    if summary_text.startswith(prefix):
                        summary_text = summary_text[len(prefix):].strip()
                        break

                if len(summary_text) > args.max_length:
                    summary_text = summary_text[:args.max_length]

                if len(summary_text) < args.min_length and len(original_text) > 100:
                    log_warn(f"Summary too short ({len(summary_text)} chars) for item {idx+1}")

                duration_ms = int((time.time() - start_summary) * 1000)
                log_info(f"Summary generated in {duration_ms}ms: {summary_text[:50]}...")

                # 🟢 Se publica apenas termina ESTE resumen.
                publish_result(channel, queue, {
                    "jobId": job_id,
                    "summary": summary_text,
                    "durationMs": duration_ms,
                    "success": True,
                    "error": None,
                })

            except Exception as e:
                log_error(f"Error in summary {idx}: {str(e)}")
                publish_result(channel, queue, {
                    "jobId": job_id,
                    "summary": "",
                    "durationMs": 0,
                    "success": False,
                    "error": str(e),
                })
    finally:
        connection.close()

    log_info("Batch finished, all results published.")


if __name__ == "__main__":
    main()