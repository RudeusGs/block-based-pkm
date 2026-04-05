This folder contains recommended Redis configuration and a docker-compose file that enables persistence.

Files:
- redis.conf: Redis configuration enabling both RDB snapshot and AOF append-only file for durability.
- docker-compose.redis.yml: A docker-compose file that starts Redis and mounts a named volume `redis-data` to `/data` in the container so data survives container restarts.

Usage:
1. From repository root, run:
   docker compose -f redis/docker-compose.redis.yml up -d
2. Check logs:
   docker compose -f redis/docker-compose.redis.yml logs -f
3. If you need to change persistence settings, edit `redis/redis.conf`.
