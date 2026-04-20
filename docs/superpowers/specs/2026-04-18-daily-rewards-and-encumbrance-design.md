# Design: Daily Rewards & Encumbrance System

## 1. Goal
Fix the broken login reward system, implement a 12-month sequential login calendar, establish a master item catalog for weight management, and implement character encumbrance logic.

## 2. Data Architecture

### 2.1 Master Item Catalog (`items.json`)
A central repository for all game items to ensure consistent weights and values.
*   **Location**: `Darkness.Godot/assets/data/items.json`
*   **Schema**: List of `Item` objects.
*   **Seeder**: `ItemSeeder.cs` will populate the `items` collection in LiteDB.

### 2.2 Random Reward Pool (`random-rewards.json`)
A weighted list for the "Spin the Wheel" daily reward.
*   **Location**: `Darkness.Godot/assets/data/random-rewards.json`
*   **Schema**: `[ { "ItemName": "string", "Weight": int } ]`

### 2.3 Login Calendar (`login-calendar.json`)
A 12-month calendar where rewards are tied to the specific day of the month.
*   **Location**: `Darkness.Godot/assets/data/login-calendar.json`
*   **Schema**: `{ "1": ["ItemName", ...], "2": [...], ... "12": [...] }`
*   **Logic**: Uses `DateTime.Today.Day` as the index. If a player misses a day (e.g., Day 3), they miss that specific reward and receive the Day 4 reward on the 4th.

## 3. Core Engine Updates

### 3.1 Character Model (`Character.cs`)
*   **`TotalWeight`**: Computed property summing `(Item.Weight * Item.Quantity)` of all items in `Inventory`.
*   **`CarryCapacity`**: Computed property `Strength * 20`.
*   **`Inventory` Management**: Updated `ConsolidateInventory` to handle weights and stacking correctly.

### 3.2 Reward Service (`RewardService.cs`)
*   **`CheckDailyRewardAsync`**: 
    1.  Verifies if the user has logged in today (using `User.LastLogin`).
    2.  Fetches a random item from `random_rewards` (weighted selection).
    3.  Fetches the calendar item from `login_calendar` using `DateTime.Today.Month` and `Day`.
    4.  **Inventory Check**: Only adds items if `TotalWeight + NewItem.Weight <= CarryCapacity`.
    5.  **Persistence**: Correctly adds items to `Character.Inventory` and updates the `Character` and `User` collections in LiteDB.

## 4. Implementation Strategy

1.  **Model Updates**: Add Weight/Quantity fields to `Item.cs` and Capacity properties to `Character.cs`.
2.  **Seeding**: Implement `ItemSeeder` and `RewardSeeder`. Register them in `Global.cs`.
3.  **JSON Data**: Create initial `items.json`, `random-rewards.json`, and `login-calendar.json`.
4.  **Service Logic**: Rewrite `RewardService.cs` to query LiteDB and award items to the character.
5.  **UI Feedback**: Ensure `MainMenuScene.cs` displays the correct alerts for both rewards.

## 5. Success Criteria
*   Logging in awards exactly two items (1 random, 1 calendar).
*   Items appear in the inventory and persist across sessions.
*   The `TotalWeight` accurately reflects the inventory content.
*   Character is blocked from receiving rewards if they exceed `CarryCapacity`.
