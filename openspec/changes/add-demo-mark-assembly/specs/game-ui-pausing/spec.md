## ADDED Requirements

### Requirement: Registered windows hold counted pause requests
A generic game state manager SHALL register window lifetimes and count active pause requests. Registration alone SHALL NOT pause. Each pause-enabled window SHALL hold at most one request while open. Gameplay SHALL remain paused until every active request is released. Unregistration SHALL release the window's outstanding request and remove listeners.

#### Scenario: Two windows close independently
- **WHEN** windows A and B open and A then closes
- **THEN** the pause count changes from 2 to 1 and gameplay remains paused
- **WHEN** B closes
- **THEN** the count becomes zero and gameplay may resume

#### Scenario: Duplicate notifications and destruction
- **WHEN** one registered window sends duplicate open notifications, then is destroyed and sends a late close notification
- **THEN** its contribution is counted once and released once, without negative counts or affecting another window's pause

### Requirement: Assembly and reward windows pause combat
Assembly and both beast-mark and buff choice windows SHALL automatically hold pause requests while visible. Pausing SHALL stop combat movement, physics, damage, attacks and cooldown progression while preserving UI input. The last request release SHALL restore the time multiplier saved before the first request.

#### Scenario: Reward selection
- **WHEN** a beast-mark or buff choice window opens
- **THEN** combat stops and the player can still confirm a reward
- **WHEN** confirmation closes that window while another request remains
- **THEN** gameplay remains paused

#### Scenario: Close while paused
- **WHEN** Tab, Esc or the assembly close button is used during pause
- **THEN** the interface closes and releases only its own request without producing a combat attack from the closing input

#### Scenario: Session exits
- **WHEN** the HUD is closed or the session exits with a pause-enabled window open
- **THEN** its requests and listeners are released and its Unity pause adapter leaves no stale pause in the next session
