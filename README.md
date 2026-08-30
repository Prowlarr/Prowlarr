# Prowlarr

## Installation

This fork is designed to be a drop-in replacement for existing Prowlarr docker installations. Simply replace your Prowlarr docker image with `ghcr.io/actuallyevan/prowlarr:latest`

Sample docker compose:
```docker
prowlarr:
    image: ghcr.io/actuallyevan/prowlarr:latest
    container_name: prowlarr
    environment:
      - PUID=1000
      - PGID=1000
      - TZ=Etc/UTC
      # Configure caching behavior
      - CACHE_TTL_MINS=10
      - CACHE_MAX_SIZE_MB=100
    volumes:
      - /path/to/prowlarr/data:/config
      # Additional volume mounts as needed
    ports:
      - 9696:9696
    restart: unless-stopped
```

## Why this fork?

This fork aims to improve certain aspects of Prowlarr to make it work better with remote "infinite" library setups (Debrid/Usenet streaming, etc). This fork will be kept up-to-date with Prowlarr stable releases and you should be able to swap back and forth between them if needed.

## Cache indexer query responses

| Env Var           | Default | Description                                                                                                                                         |
|-------------------|---------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| CACHE_TTL_MINS    | 10      | How long a particular query response should be cached for. Should be <15 mins to ensure RSS queries get fresh data but you can go higher if needed. |
| CACHE_MAX_SIZE_MB | 100     | Maximum size of cache in memory before old records are cleaned up. Higher values will use more memory.                                              |


Debrid/Usenet mounting tools cause a lot of repeated queries to the indexer that waste time and API queries. In particular, the workflow for most Usenet streaming setups is:
- Arrs search for an item
- Arr grabs the item
- Tools check whether the nzb is streamable
- If not streamable, mark the download as failed which triggers another search
- Repeat

On analyzing my Prowlarr database for duplicate queries, I found that nearly 30% of queries were duplicated within 10 minutes and having a cache would have saved thousands of queries to my indexers and also drastically speed up the search/import process.

Generally, if you're using any of the streaming clients, this fork will give you much better search performance. Run this SQL query against your Prowlarr database if you want to check how beneficial caching would be for your setup:

```sql
WITH enriched AS (
    SELECT
        IndexerId,
        json_extract(Data, '$.season') AS season,
        json_extract(Data, '$.query') AS query,
        json_extract(Data, '$.categories') AS categories,
        json_extract(Data, '$.queryType') AS queryType,
        json_extract(Data, '$.tvdbId') AS tvdbId,
        json_extract(Data, '$.tmdbId') AS tmdbId,
        json_extract(Data, '$.imdbId') AS imdbId,
        CAST(strftime('%s', date) / 600 AS INTEGER) AS window_id
    FROM History
    WHERE date >= datetime('now', '-90 days') AND (EventType = 2 OR EventType = 3)
    ),
    grouped AS (
SELECT
    COUNT(*) AS total_calls,
    COUNT(*) - 1 AS duplicate_calls
FROM enriched
GROUP BY
    IndexerId, season, query, categories, queryType, tvdbId, tmdbId, imdbId, window_id
    )
SELECT
    SUM(total_calls) AS total_requests,
    SUM(duplicate_calls) AS total_duplicate_calls,
    100.0 * SUM(duplicate_calls) / SUM(total_calls) AS duplicate_percent
FROM grouped;
```

## Contributing

Feel free to open issues or pull requests for any changes you'd like to see.
