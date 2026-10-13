## ADDED Requirements

### Requirement: Ownership and equipment are separate
The game SHALL retain acquired beast marks for the current run and allow only owned marks to be equipped. Reward selection SHALL NOT directly equip a mark. Unequipping or replacing SHALL retain ownership, revival SHALL retain both ownership and equipment, and restarting SHALL clear both.

#### Scenario: Acquire and manually equip
- **WHEN** the player confirms a new beast mark reward
- **THEN** the owned collection gains the mark while all four equipped slots remain unchanged
- **AND** the player can later equip it through the assembly interface

#### Scenario: Move to an occupied slot
- **WHEN** an owned mark equipped in the skill slot is moved to an occupied normal attack slot
- **THEN** the skill slot becomes empty and the normal attack slot contains the moved mark in one final equipment change
- **AND** ownership of both marks is retained

### Requirement: Duplicate rewards convert to currency
Reward choices SHALL continue to display owned marks. Selecting one SHALL award configured positive currency exactly once, without adding a duplicate, upgrading or changing equipment.

#### Scenario: Confirm a duplicate
- **WHEN** a player confirms an owned mark and the same confirmation is attempted again
- **THEN** only the first confirmation awards the displayed currency
- **AND** equipment and ownership remain unchanged

### Requirement: Assembly and HUD show actual equipment
The assembly interface SHALL offer owned marks, four target slots, previews and explicit equip / replace / move / unequip operations. The HUD SHALL show empty or corresponding equipped icons for normal attack, skill, ultimate and attribute slots. A HUD button and semantic Tab input SHALL open the same interface; Tab, Esc and the close button SHALL close it.

#### Scenario: Modal access
- **WHEN** a reward or completion interface is open
- **THEN** the assembly button is disabled and Tab does not open the assembly interface

#### Scenario: Unequip updates both views
- **WHEN** an equipped mark is unequipped
- **THEN** the assembly slot and HUD both show empty while the mark remains in the owned list
