# Shadow Protocol - REST API Specification

Base URL: `/api/v1`

## 1. Authentication
- `POST /auth/register` - Create new operative account and initialize player profile.
- `POST /auth/login` - Authenticate and retrieve JWT bearer token.

## 2. Player Profile
- `GET /player/profile` - Retrieve current player vitals, level, currency, and position. (Requires `Authorization: Bearer <token>`)
- `PUT /player/profile` - Update player state and coordinates during gameplay checkpoints.

## 3. Inventory Synchronization
- `GET /inventory` - Fetch cloud inventory slot items.
- `POST /inventory/sync` - Bulk synchronize active inventory slots.

## 4. Cloud Saves
- `POST /save` - Upload serialized game state snapshot into designated slot ($0\dots9$).
- `GET /save/{slot_index}` - Download snapshot for offline resumption.

## 5. Leaderboard
- `POST /leaderboard/submit` - Submit mission score and survival metrics.
- `GET /leaderboard` - Fetch global top 50 survival rankings.
