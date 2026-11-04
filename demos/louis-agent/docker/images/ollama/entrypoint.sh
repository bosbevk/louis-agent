#!/bin/bash
set -e

# Start Ollama in background
echo "Starting Ollama server..."
ollama serve &
OLLAMA_PID=$!

# Wait for Ollama to be ready
echo "Waiting for Ollama to be ready..."
for i in {1..30}; do
  if curl -s http://localhost:11434/api/tags > /dev/null 2>&1; then
    echo "Ollama is ready!"
    break
  fi
  echo "Waiting... ($i/30)"
  sleep 1
done

# Pull the profile's model (config/.env.ollama) if not already present; OLLAMA_MODEL overrides it
OLLAMA_MODEL="${OLLAMA_MODEL:-$LLM_MODEL}"
if [ -n "$OLLAMA_MODEL" ]; then
  echo "Checking for model: $OLLAMA_MODEL"

  # Check if model exists
  if curl -s http://localhost:11434/api/tags | grep -q "\"name\":\"$OLLAMA_MODEL\""; then
    echo "Model $OLLAMA_MODEL already exists"
  else
    echo "Pulling model: $OLLAMA_MODEL"
    ollama pull "$OLLAMA_MODEL"
    echo "Model $OLLAMA_MODEL pulled successfully"
  fi
fi

echo "Ollama is ready!"

# Keep the process running
wait $OLLAMA_PID
