Monster AI & UI System for Unity
This repository provides a modular and extensible Monster AI and UI system for Unity, featuring:

Monster AI with pathfinding, avoidance, melee and ranged attacks, and loot drops.
Monster UI for displaying health, name, and level on mouse hover.
Monster Stats as ScriptableObjects for easy configuration and reuse.
Features.

1. Monster AI (MonsterAI2D.cs)
Movement & Pathfinding:
Monsters move toward the player, avoid obstacles and other monsters, and rotate smoothly to face their target.
Attack Logic:
Melee: Attacks when within close range.
Ranged/Magic: Shoots projectiles at the player when within attack range and line of sight, with controllable fire rate.
As the monsters too will attack and move to increase the chance that you may mis-shot them.

https://github.com/user-attachments/assets/c0fef3e5-55c9-4789-bb2c-0cf79ae70954

Note: Soon there will be an update to control the range of the moving while attacking the player depending the monster type
Loot Drops:
Drops items on death, with drop chances based on item rarity (common to legendary).
Health Management:
Takes damage from bullets and dies when health reaches zero.
Performance:
Uses efficient checks and can be extended with pooling and optimization strategies.


3. Monster UI (MonsterUI.cs)
Dynamic UI:
Shows monster health, name, and level when the mouse hovers over the monster.
Smooth Health Bar:
Health bar smoothly interpolates to reflect damage.
UI Reset:
UI resets when the mouse exits the monster.

4. Monster Stats (MonsterStats.cs)
ScriptableObject:
All monster stats are defined in a ScriptableObject for easy editing and reuse.
Configurable:
Health, level, attack damage, attack range, cooldowns, movement, and more.
Monster Types:
Supports Melee, Ranged, and Magic types.

![Screenshot (5)](https://github.com/user-attachments/assets/3ec4c1a6-594f-47dc-b22e-9f84292f179c)

Loot Drop System
Drop Probability:
Each monster can drop items from a list, with drop chances based on rarity:
Common: 60%
Uncommon: 20%
Rare: 12%
Epic: 6%
Legendary: 2%
Multiple Drops:
Monsters can drop up to a configurable maximum number of items.

UI Integration
Health Bar:
Uses Unity UI Image or Slider to display health.
Name & Level:
Uses TextMeshPro for crisp, flexible text rendering.
Usage
Create MonsterStats assets for each monster type in your project.
Attach MonsterAI2D and MonsterUI to your monster prefabs.
Assign references in the Inspector (player, firePoint, MonsterStats, UI elements).
Configure loot drops by assigning possible drop items and their rarities.
Customize UI as needed for your game’s look and feel.
Example Inspector Setup
MonsterAI2D

player: Reference to the player transform.
firePoint: Transform where projectiles spawn.
monsterStats: Reference to the MonsterStats asset.
possibleDrops: List of Items assets.
MonsterUI

healthBar: UI Image or Slider.
nameText: TextMeshProUGUI for monster name.
levelText: TextMeshProUGUI for monster level.
Extending
Add new monster types by extending MonsterStats.MonsterType.
Add new loot items by creating new Items ScriptableObjects.
Customize AI by overriding or extending methods in MonsterAI2D.
Example: Adding a New Monster
Create a new MonsterStats asset in the Project window.
Fill in the stats (name, health, attack, etc.).
Create a new prefab and attach MonsterAI2D and MonsterUI.
Assign the MonsterStats asset and UI references.
Add possible drop items to the possibleDrops list.
Requirements
Unity 2021.3 or newer recommended
TextMeshPro package for UI text
