# Balance sheet — castle-war precision pass

- run-id: 20260809-castle-war-stage1
- owner: game-designer lane
- status: implemented; focused precision, probability, and current-roster role gates passed
- scope: aim fidelity, difficulty ramp, comeback fairness, stage scale

```yaml
balance:
  launch:
    max_drag_distance: 4.2
    force_multiplier: 6.0
    min_velocity: 3.0
    max_velocity: 25.2
    preview_samples: 150
    preview_step_seconds: 0.02
    preview_horizon_seconds: 3.0
  difficulty:
    ramp_turns: 15
    wind_cap_start: 2.0
    wind_cap_end: 6.5
    ai_error_start: 2.5
    ai_error_end: 0.8
    storm_chance_start: 0.02
    storm_chance_end: 0.15
  last_stand:
    danger_hp_fraction: 0.35
    player_damage_multiplier: 2.2
    player_radius_multiplier: 1.5
    player_speed_multiplier: 1.3
    ai_damage_multiplier: 1.6
    ai_radius_multiplier: 1.25
    ai_speed_multiplier: 1.15
    single_hit_damage_cap: 140
  stage_scale:
    stage_1:
      launch_apron_abs_x: 14.5
      wind_cap: 6.5
      camera_world_width: 39.0
      obstacle_cap: 6
      mutation_turns: 3
    stage_2:
      launch_apron_abs_x: 13.5
      wind_cap: 6.2
      camera_world_width: 36.3
      obstacle_cap: 4
      mutation_turns: 2
    stage_3:
      launch_apron_abs_x: 18.5
      wall_height_blocks: 4
      wind_cap: 7.2
      camera_world_width: 47.0
      obstacle_cap: 7
      mutation_turns: 4
  deployment:
    supply:
      max: 24.0
      start: 8.0
      regen_per_second: 0.7
      kill_bonus: 2.0
      block_bonus: 0.5
    cards:
      knight: { cost: 5, cooldown: 2.5, unlock_turn: 0, cap_group: body }
      archer: { cost: 6, cooldown: 3.5, unlock_turn: 1, cap_group: body }
      cannon: { cost: 12, cooldown: 12.0, unlock_turn: 3, cap_group: battery }
      barrel: { cost: 4, cooldown: 5.0, unlock_turn: 2, cap_group: hazard }
    caps:
      body: 6      # knight + archer SHARE this cap
      battery: 2
      hazard: 3
    zone:
      min_abs_x: 0.5
      max_abs_x: 12.5
      min_y: 0.0
      max_y: 8.0
      enemy_overlap_radius: 0.45
    cannon:
      max_hp: 140.0
      range: 13.0
      reload_seconds: 3.2
      shell_damage: 42.0
      shell_splash_radius: 1.5
      muzzle_height: 0.55
      arc_apex_bonus: 2.5
```

Current shipped combat-role measurements are pinned separately in
`../qa/current-roster-balance-gate.md`. That gate intentionally reports no
45–55% match win-rate claim: it measures counterconditions, DPS/supply,
survivability/supply, siege TTK, hazard radius/fuse, reversal ceiling, and
side-swap parity without inventing a full-match AI decision model.

## Decisions

- **Aim preview is a contract, not decoration.** It now uses the same 0.02 s semi-implicit integration order as runtime physics. Wind is applied only while the projectile remains inside the configured wind radius, and launched-unit mass matches the runtime mass reduction.
- **The opening is forgiving; the endgame is readable, not random.** Fifteen turns move wind from 2.0 to 6.5, AI error from 2.5 to 0.8, and storm probability from 2% to 15%. `SmoothStep` keeps both endpoints stable instead of introducing an abrupt mid-session spike.
- **Comeback power and chain reactions preserve counterplay.** Last Stand still caps each buffed hit at 140. Independently, a core that starts a turn at its pristine 150 HP can lose at most 140 core health during that turn, after shield absorption. Barrel chains and gate clones can therefore collapse the fortress but must leave at least 10 core HP for one answer; later turns and already-wounded cores remain fully lethal.
- **The campaign rises on fortress height, not on wind.** Wall height is now 2 → 3 → 4 across the sequential unlock order; Stage3 previously inherited Stage1's 2-block wall, the one stage value carried over without a stated reason, which left the final unlock as the softest fortress in the game. Wind deliberately stays non-monotonic (Stage2 6.2 < Stage1 6.5 < Stage3 7.2) because it is derived from throw distance — flight time scales with range, so wind pressure must too — and obstacle cap plus mutation cadence stay each board's pacing identity rather than a difficulty dial. Pinned by `StageProgressionShapeTests`.
- **Scale changes alter tactics, not only camera zoom.** Stage 2 compresses range and accelerates obstacle mutation; Stage 3 widens range, raises the wind ceiling, opens the center, and slows mutation. Stage 1 remains the fixed baseline.
- **Creation is gated by supply, not by turn count alone.** Deploy runs during BOTH turns, so the enemy turn stops being dead air, but 0.7/s regen against a 5–12 supply price sets the action floor at roughly one deploy per 7–17 s. The 24 cap forbids banking an alpha strike; the 8 start guarantees turn 1 can deploy so the mechanic teaches itself immediately. Full model: `deployment-economy.md`.
- **The Cannon is an installation, never a projectile.** It is deploy-only at 140 HP and 13.1 sustained dps (26.2 at the 2-battery cap). Two unanswered batteries erase a 150 HP core + 50 shield in ≈7.6 s — deliberately inside one turn, because leaving a battery alive should lose the game. The counterplay is the existing volley: one direct hit removes a battery.

## Probability boundaries

- Item-drop thirds are half-open buckets: values below $1/3$ produce Sword, $[1/3, 2/3)$ Shield, and $[2/3, 1)$ Boots. Exact-boundary tests defend the contract.
- Storm chance is clamped by the difficulty ramp to $[0.02, 0.15]$; no additional hidden roll modifier is allowed.
- Deploy conditions are evaluated most-permanent-first (Locked → FieldCap → Cooldown → Supply → Zone), so a multi-blocked deploy always names the condition the player must actually solve.

## Verification targets

1. First preview sample equals $p_0 + (v_0 + g\,\Delta t)\,\Delta t$ at $\Delta t=0.02$.
2. Full draw reaches 25.2 velocity while sub-threshold drags do not launch.
3. Item-drop exact thirds select the documented buckets.
4. Last Stand never produces a hit above 140 damage.
5. Multiple same-turn hits cannot reduce a turn-start-pristine core below 10 HP; the next turn can consume its shield and defeat it.
6. Stage 1/2/3 retain distinct range, wind, obstacle, and mutation profiles.
7. Deploy cost/cooldown/unlock/cap table matches `DeploymentRules` exactly; Knight and Archer share one body cap.
8. Supply clamps at 0 and 24; regen, kill bonus, and block bonus are additive and cannot overfill.
9. The deploy zone rejects the center line, the enemy half, out-of-band y, and both launch rings.
