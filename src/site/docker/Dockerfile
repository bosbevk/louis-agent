FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "src/site/louis-agent.site-api/louis-agent.site-api.csproj" -c Release -o /app/publish

# The framework's site's host (F124) needs only the runtime: the guide reads docs and sends mail, it runs no build.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish/ .
# The docs the guide reads (F109.3), laid out as the repo lays them so their links hold: the framework's, each demo's
# and the packs', with the repo's own front page; the films are pictures the guide never reads, and 20 MB.
COPY --from=build /src/README.md /src/ROADMAP.md /src/TODO.md /app/docs/
COPY --from=build /src/src/docs/ /app/docs/src/docs/
COPY --from=build /src/demos/louis-agent/docs/ /app/docs/demos/louis-agent/docs/
COPY --from=build /src/demos/puls/docs/ /app/docs/demos/puls/docs/
COPY --from=build /src/packs/docs/ /app/docs/packs/docs/
RUN rm -rf /app/docs/src/docs/films
ENV DOCS_ROOT=/app/docs
# The price and rate tables the usage ledger reads, the site's own copies, so the image needs no share mounted.
COPY --from=build /src/src/site/config/prices.json /src/src/site/config/rates.json /app/config/
ENV CONFIG_DIRECTORY=/app/config
ENV PRICES_FILE=/app/config/prices.json
ENV RATES_FILE=/app/config/rates.json
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s CMD curl -fs http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "louis-agent.site-api.dll"]
