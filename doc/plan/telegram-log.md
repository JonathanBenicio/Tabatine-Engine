# Telegram Logs Channel - Strategy

This document outlines the transition from a single hardcoded environment variable (`Telegram__ChatId`) to a flexible, database-driven system for receiving "System Logs" via Telegram.

## Concept

Instead of configuring a single chat ID in the deployment environment, we use the `perfis` table to manage multiple recipients. This allows for:
- Multiple administrators receiving logs.
- Dedicated "Group Chats" for logs without redeploying.
- Easy toggling of notifications via the database.

## Architecture

We are adding a `receive_logs` boolean column to the `perfis` table.

- **`receive_logs = true`**: This profile (user or group) will receive all system notifications (broadcasts).
- **`receive_logs = false`**: (Default) This profile only receives direct messages relevant to its own actions (e.g., auth linking).

### How to designate a Log Channel

Since we don't have a Management UI yet, the process for designating a log channel is:

1. **Add the Bot to a Group** (or interact with it directly).
2. **Link the account** following the standard Bot flow (`/start TOKEN`).
3. **Set the flag** manually in the database for that `chat_id`:
   ```sql
   UPDATE perfis SET receive_logs = true WHERE telegram_chat_id = <CHAT_ID>;
   ```

## Workflow Transition

1. **Schema Update**: Adds the `receive_logs` column.
2. **Code Update**: `TelegramNotificationService` now fetches all chats with `receive_logs = true`.
3. **Environment Cleanup**: The `Telegram__ChatId` environment variable is removed from CI/CD and production environments.

## Benefits

- **Scalability**: Want another group to see logs? Just add the bot and set the flag.
- **Security**: Chat IDs are kept in the database, not exposed in CI/CD logs or environment variables.
- **Flexibility**: Different levels of logs could be implemented in the future using this same pattern.
