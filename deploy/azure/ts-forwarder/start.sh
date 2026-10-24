#!/bin/sh
# Tailscale in userspace mode (no /dev/net/tun in Azure Container Apps) plus a forwarder: what connects to
# localhost:PORT in this pod, for each PORT in FORWARD_PORTS, is dialled through the tailnet to FORWARD_TO_HOST:PORT.
# In userspace mode nothing else can address a 100.x host directly, so `tailscale nc` does the dialling. One sidecar,
# one tailnet node, every port the app needs (CL19).
set -eu
: "${FORWARD_TO_HOST:?FORWARD_TO_HOST is required: the tailnet IP or name of the PC that runs the browser and Mongo}"
: "${FORWARD_PORTS:=9222,27017}"

# Azure Container Apps runs on Kubernetes and hands every container KUBERNETES_SERVICE_HOST, so containerboot would look
# for a service account and die ("error initializing kube client"). This pod is no Kubernetes client: drop the hint.
unset KUBERNETES_SERVICE_HOST KUBERNETES_SERVICE_PORT KUBERNETES_PORT
export TS_KUBE_SECRET=

/usr/local/bin/containerboot &
boot=$!

until tailscale status >/dev/null 2>&1; do sleep 1; done

# Loopback only: the pod's other containers reach it, the ingress never does. One `tailscale nc` per connection.
for port in $(echo "$FORWARD_PORTS" | tr ',' ' '); do
  echo "ts-forwarder: tailnet up; localhost:${port} -> ${FORWARD_TO_HOST}:${port}" >&2
  socat -d TCP-LISTEN:"${port}",bind=127.0.0.1,fork,reuseaddr EXEC:"tailscale nc ${FORWARD_TO_HOST} ${port}" &
done

# A forwarder with `fork` outlives any one failed dial, so the container lives as long as Tailscale does; when
# containerboot exits, so does this script, and Azure restarts the replica. (busybox sh: no `wait -n`.)
wait "$boot"
