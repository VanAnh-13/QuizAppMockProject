#!/usr/bin/env bash
set -euo pipefail

# Run from WSL/Linux with Docker and curl available.
repo_root=$(cd "$(dirname "$0")/.." && pwd)
config=${1:-"$repo_root/frontend/nginx.conf"}
fixture=$(mktemp -d)
container=
cleanup() {
    if [[ -n "$container" ]]; then
        docker rm -f "$container" >/dev/null
    fi
    rm -rf "$fixture"
}
trap cleanup EXIT

chmod 755 "$fixture"
assets=(favicon.svg logo.png font.woff2 styles.css main-ABCDEFGH.js)
for asset in "${assets[@]}"; do
    printf 'cache fixture\n' > "$fixture/$asset"
done

container=$(docker run --rm -d -p 127.0.0.1::80 \
    --add-host quizapp-api:127.0.0.1 \
    -v "$config:/etc/nginx/conf.d/default.conf:ro" \
    -v "$fixture:/usr/share/nginx/html:ro" nginx:1.27-alpine)
docker exec "$container" nginx -t
address=$(docker port "$container" 80/tcp)
url="http://$address"
curl --retry 10 --retry-connrefused --retry-delay 1 --fail --silent --show-error \
    "$url/favicon.svg" >/dev/null

for asset in "${assets[@]}"; do
    headers=$(curl --fail --silent --show-error -D - -o /dev/null "$url/$asset" | tr -d '\r')
    grep -qi '^Cache-Control: no-cache$' <<< "$headers"
    if grep -Eqi 'immutable|^Expires:' <<< "$headers"; then
        echo "Unexpected long-lived caching for $asset" >&2
        exit 1
    fi
    echo "PASS: $asset requires revalidation"
done

etag=$(curl --fail --silent --show-error -I "$url/favicon.svg" | tr -d '\r' | sed -n 's/^ETag: //p')
[[ -n "$etag" ]]
status=$(curl --silent --show-error -o /dev/null -w '%{http_code}' \
    -H "If-None-Match: $etag" "$url/favicon.svg")
[[ "$status" == 304 ]]
echo 'PASS: unchanged favicon returns 304 after conditional revalidation'

printf 'updated favicon after deployment\n' > "$fixture/favicon.svg"
status=$(curl --silent --show-error -o "$fixture/response" -w '%{http_code}' \
    -H "If-None-Match: $etag" "$url/favicon.svg")
[[ "$status" == 200 ]]
grep -qx 'updated favicon after deployment' "$fixture/response"
echo 'PASS: changed favicon returns the new content at the same URL'
