# Rust CPU Shower Simulation

This is the first implementation for the final lab: a Rust CPU version of the high-performance shower particle simulation.

## What it includes

- 3D shower cubicle: 1.0m width, 1.0m depth, 2.0m height
- Circular shower-head emitter with a 0.1m diameter
- Random particle spawning inside the emitter
- Randomized initial velocity within a 30 degree cone from the downward vertical axis
- Gravity-driven particle motion
- Floor respawn with cumulative floor-hit counter
- Three wall modes implemented in code:
  - Reflective
  - Toroidal/wrapping
  - Sticky/gliding
- Particle merging using mass addition and conservation of momentum
- Sticky-mode collision constraint, where merged wall particles keep horizontal velocity at zero
- Cooling based on mass, where larger particles cool more slowly
- CPU parallelism using safe Rust concurrency:
  - 2 physics threads
  - 2 collision detection threads
  - 2 thermodynamics threads
- Synchronisation primitives:
  - `AtomicU64` for cumulative floor hits
  - `Arc<Mutex<Vec<MergeEvent>>>` for collecting collision events safely
  - scoped threads and mutable slice partitioning for safe parallel updates

## Build and run

```bash
cargo run --release
```

The default particle count is set to 100,000 and the simulation runs for 600 frames.
You can tune constants at the top of `src/main.rs`:

```rust
const PARTICLE_COUNT: usize = 100_000;
const FRAME_COUNT: usize = 600;
const DT: f32 = 1.0 / 60.0;
```

Wall mode is set in `main()`:

```rust
wall_mode: WallMode::Sticky,
```

Change it to:

```rust
WallMode::Reflective
WallMode::Toroidal
WallMode::Sticky
```

## Notes for lab book

This version focuses on the simulation backend first. OpenGL rendering should be added as a separate layer that reads a sampled subset of the particle vector, for example one particle out of every 100, so rendering does not dominate the benchmark.

For the CPU design section, explain that physics, collision detection, and thermodynamics are split into separate stages. Each stage uses two worker threads, meeting the requirement for at least two dedicated threads per functional area. Rust prevents shared mutable access to the particle array by requiring the physics and thermal stages to operate on non-overlapping mutable slices. Collision detection uses a read-only particle snapshot and shared event collection protected by a mutex.
