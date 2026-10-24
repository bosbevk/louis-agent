# The sidecar beside puls-app on Azure Container Apps (CL19): Tailscale in userspace mode plus a TCP forwarder, so the
# app container in the same pod reaches the home PC's browser and Mongo at localhost (deploy/azure/README.md).
# Environment: TS_AUTHKEY (a secret; reusable and ephemeral, so a dead replica's node vanishes), TS_USERSPACE=true,
# TS_STATE_DIR, TS_EXTRA_ARGS (--hostname=puls-app), FORWARD_TO_HOST (the PC's Tailscale IP), FORWARD_PORTS (9222,27017).
# Built from this folder: deploy.ps1 build, or
#   az acr build -r acrpulsza -t ts-forwarder:TAG deploy/azure/ts-forwarder
FROM tailscale/tailscale:stable
RUN apk add --no-cache socat
COPY start.sh /usr/local/bin/ts-forwarder
RUN chmod +x /usr/local/bin/ts-forwarder
ENTRYPOINT ["/usr/local/bin/ts-forwarder"]
