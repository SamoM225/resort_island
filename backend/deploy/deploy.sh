#!/bin/sh
# Deploys a backend image into the compose stack in this directory, on the server:
#   sh deploy.sh ghcr.io/<owner>/<repo>-backend@sha256:...
# Any failing step exits non-zero, so whoever runs it (CI over SSH or a person) sees the failure.
set -eu

cd "$(dirname "$0")"

image="${1:?usage: deploy.sh <image>}"

if [ ! -f .env ]; then
    echo "Missing $(pwd)/.env — create it from server.env.example first." >&2
    exit 1
fi

# Takes precedence over the value in .env for every compose command below.
export BACKEND_IMAGE="$image"

echo "==> Pulling ${image}"
docker compose pull app

echo "==> Starting database"
docker compose up -d --wait db

echo "==> Running migrations and seeding initial data"
docker compose run --rm --no-deps app php artisan migrate --force --seed

echo "==> Starting new app container"
docker compose up -d --wait --wait-timeout 120 --remove-orphans app

echo "==> Health check"
docker compose exec -T app curl --silent --show-error --fail http://localhost:8080/up > /dev/null

# Remember the deployed image so manual `docker compose ...` commands on the server keep working.
if grep -q '^BACKEND_IMAGE=' .env; then
    sed -i.bak "s|^BACKEND_IMAGE=.*|BACKEND_IMAGE=${image}|" .env && rm -f .env.bak
else
    printf '\nBACKEND_IMAGE=%s\n' "$image" >> .env
fi

docker image prune --force > /dev/null
echo "==> Deployed ${image}"
