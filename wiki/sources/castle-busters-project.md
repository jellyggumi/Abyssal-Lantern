# Castle Busters Project Source Summary

- **Project Path**: `/Users/jangyoung/Documents/Unity/portfolio/unknown-castle`
- **Platform**: Unity (Universal Render Pipeline - URP)
- **Genre**: 2D Physics-based Catapult & Real-time Combat Hybrid
- **Status**: Core Codebase Implemented & Compiled Successfully (0 errors, 0 warnings)

## Description Summary

Jang Young's portfolio project "unknown-castle" (Castle Busters) is a 2D physics-based catapult and real-time combat hybrid game. The project includes a fully destructible castle system using BFS structural integrity checks, drag-to-aim launching mechanics, unit state machines (Knight, Archer, Bomber), and juice elements like Hit-Stop and Screen Shake.

## Key Documents

- **Technical Specification & Blueprint**: [[wiki/reports/castle-busters-technical-spec]]
- **Implementation Log (2026-06-28)**: [[wiki/reports/castle-busters-implementation-log]]
- **Engineering Board**: [[wiki/reports/castle-busters-engineering-board]]

## Implementation Details (2026-06-28)

- **ScriptableObjects Integration**: Decoupled unit and block statistics into `UnitData` and `BlockData` ScriptableObjects.
- **BFS Structural Integrity Optimization**: Implemented end-of-frame batching and coroutine-based slicing (max 50 traversals per frame) in `CastleController` to prevent CPU spikes.
- **AI Trajectory Calibration**: Added random target offset based on `errorOffsetRange` to simulate human-like aiming error.
- **Juice & Polish**: Implemented `HitStopManager` (0.05s freeze) and `ScreenShakeManager` (camera local position shake) triggered on block destruction and bomber explosions.
- **Automated Testing**: Added unit tests for `HitStopManager` and `ScreenShakeManager` singletons in `GamePlayTests.cs`.
