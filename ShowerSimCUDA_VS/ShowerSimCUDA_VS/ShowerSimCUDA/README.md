# CUDA Shower Simulation, Headless Visual Studio Project

This is a CUDA/C++ implementation of the 600086 final lab shower simulation specification. Rendering is intentionally not implemented yet, so this project focuses on the GPU simulation pipeline and timing evidence.

## What is implemented

- 3D cuboid shower environment: 2.0m high, 1.0m wide, 1.0m deep.
- Circular shower emitter at the ceiling centre with 0.1m diameter.
- Random particle spawning within the emitter disc.
- Initial velocity within a 30 degree cone around the downward vertical axis.
- Gravity.
- Floor hit detection, immediate respawn, and cumulative floor-hit counter.
- Three selectable wall modes:
  - `sticky`, default
  - `reflective`
  - `toroidal`
- Particle cooling, where larger masses cool more slowly.
- GPU spatial grid collision detection.
- Merge response using mass, weighted position, weighted temperature, and momentum accumulation.
- Sticky-wall merge behaviour, forcing horizontal velocity to zero when a sticky particle is involved.
- CUDA event timing for physics, collision, thermal, and full frame time.

## Main kernels

- `physics_kernel`: one CUDA thread per particle for gravity, motion, boundary checks, and respawning floor hits.
- `thermal_kernel`: one CUDA thread per particle for temperature cooling.
- `clear_grid_kernel`, `build_grid_kernel`: spatial hash grid setup.
- `find_collisions_kernel`: one CUDA thread per particle checks neighbouring grid cells for possible merges.
- `apply_merges_kernel`: applies proposed merge events using atomics.
- `normalise_merged_particles_kernel`: converts accumulated momentum and weighted values back into particle velocity, position, and temperature.
- `respawn_inactive_after_merge_kernel`: keeps the benchmark particle count active after merges.

## Building in Visual Studio

1. Install Visual Studio 2022 with **Desktop development with C++**.
2. Install NVIDIA CUDA Toolkit. This project file references CUDA 12.6 build customisations.
3. Open `ShowerSimCUDA.sln`.
4. Select `x64` and `Release`.
5. Build and run.

If your CUDA Toolkit version is not 12.6, edit `ShowerSimCUDA.vcxproj` and replace:

```xml
<ImportGroup Label="ExtensionSettings">
  <Import Project="$(VCTargetsPath)\BuildCustomizations\CUDA 12.6.props" />
</ImportGroup>
```

and the matching `.targets` import near the bottom with your installed version, for example `CUDA 12.5.props` / `CUDA 12.5.targets`.

## Command-line options

```text
--particles N        Particle count, default 100000
--frames N           Frame count, default 600
--block N            CUDA block size, default 256
--speed F            Initial emitter speed, default 2.75
--cool F             Cooling factor, default 1.50
--dt F               Time step, default 0.0166667
--wall sticky|reflective|toroidal
--help
```

Example:

```bat
ShowerSimCUDA.exe --particles 100000 --frames 600 --block 256 --wall sticky
```

## Notes for the lab book

The current implementation is designed for simulation and timing only. It produces measurable GPU stage timings but does not yet include OpenGL visualisation. Rendering can be added later by mapping particle buffers into OpenGL VBOs or by copying a sampled subset back to the CPU for display.
