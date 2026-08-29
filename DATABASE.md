# Shadow Protocol - Database & Caching Specification

## 1. Relational Database Schema (PostgreSQL)

```
       +-----------------------+
       |         users         |
       +-----------------------+
       | id (UUID, PK)         |
       | username (VARCHAR)    |
       | email (VARCHAR)       |
       | hashed_password (STR) |
       +-----------+-----------+
                   | 1:1
       +-----------v-----------+
       |        players        |
       +-----------------------+
       | id (UUID, PK)         |
       | user_id (FK -> users) |
       | callsign (VARCHAR)    |
       | level, exp, currency  |
       | health, stamina, armor|
       | pos_x, pos_y, pos_z   |
       +-----+-----------+-----+
             | 1:N       | 1:N
+------------v-----+   +-v-------------------+
| inventory_items  |   |     save_games      |
+------------------+   +---------------------+
| id (UUID, PK)    |   | id (UUID, PK)       |
| player_id (FK)   |   | player_id (FK)      |
| item_id (VARCHAR)|   | slot_index (INT)    |
| quantity (INT)   |   | snapshot_json (TEXT)|
+------------------+   +---------------------+
```

## 2. Redis Caching Strategy

- **Session Tokens**: Active JWT revocation and session validation (`session:<user_id>`).
- **Leaderboards**: Redis Sorted Sets (`ZADD leaderboards <score> <player_callsign>`) for high-throughput $O(\log(N))$ top-50 queries.
- **Rate Limiting**: Sliding window token bucket for REST endpoints to prevent brute-force attacks.
