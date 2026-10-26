-- AchievHub DB monitoring queries (post OptimizeDbStorageAndIndexes)

-- Size trend
SELECT relname,
       pg_size_pretty(pg_total_relation_size(c.oid)) AS total,
       s.n_live_tup AS est_rows
FROM pg_class c
JOIN pg_stat_user_tables s ON s.relid = c.oid
WHERE c.relnamespace = 'public'::regnamespace
ORDER BY pg_total_relation_size(c.oid) DESC;

-- Confirm hashes (expect ~0 full URLs after backfill)
SELECT COUNT(*) FILTER (WHERE "ImageUrlLock" LIKE 'http%') AS lock_urls,
       COUNT(*) FILTER (WHERE "ImageUrlUnlock" LIKE 'http%') AS unlock_urls,
       ROUND(AVG(LENGTH("ImageUrlLock"))::numeric, 1) AS avg_lock_len,
       ROUND(AVG(LENGTH("ImageUrlUnlock"))::numeric, 1) AS avg_unlock_len
FROM achievements;

-- Null-key sanity (expect 0)
SELECT COUNT(*) FILTER (WHERE "ApiName" IS NULL OR btrim("ApiName") = '') AS bad_apiname
FROM achievements;
SELECT COUNT(*) FILTER (WHERE "GameSteamId" IS NULL OR btrim("GameSteamId") = '') AS bad_steam_id
FROM games;

-- Index health (after ANALYZE + traffic)
SELECT indexrelname, idx_scan, pg_size_pretty(pg_relation_size(indexrelid)) AS size
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
ORDER BY idx_scan ASC, pg_relation_size(indexrelid) DESC;

-- Slow queries (requires CREATE EXTENSION IF NOT EXISTS pg_stat_statements;)
-- SELECT mean_exec_time, calls, LEFT(query, 120)
-- FROM pg_stat_statements
-- ORDER BY mean_exec_time * calls DESC
-- LIMIT 20;
