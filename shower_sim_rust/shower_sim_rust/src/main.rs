use glium::backend::glutin::SimpleWindowBuilder;
use glium::winit;
use glium::{implement_vertex, program, uniform, Surface};
use rand::rngs::StdRng;
use rand::{Rng, SeedableRng};
use std::collections::{HashMap, HashSet};
use std::env;
use std::sync::atomic::{AtomicU64, Ordering};
use std::thread;
use std::time::Instant;

const HEIGHT: f32 = 2.0;
const WIDTH: f32 = 1.0;
const DEPTH: f32 = 1.0;
const EMITTER_RADIUS: f32 = 0.05;
const GRAVITY: f32 = -9.81;
const CELL_SIZE: f32 = 0.015;
const COLLISION_DISTANCE: f32 = 0.012;
const PARTICLE_COUNT: usize = 100_000;
const FRAME_COUNT: usize = 600;
const DT: f32 = 1.0 / 60.0;
const DEFAULT_RENDER_STRIDE: usize = 100;

type Display = glium::backend::glutin::Display<glium::glutin::surface::WindowSurface>;

#[derive(Clone, Copy)]
struct ParticleVertex {
    position: [f32; 3],
    temperature: f32,
    mass: f32,
}

implement_vertex!(ParticleVertex, position, temperature, mass);

#[derive(Clone, Copy, Debug)]
enum VisualMode {
    Thermal,
    Mass,
}

impl VisualMode {
    fn uniform_value(self) -> i32 {
        match self {
            Self::Thermal => 0,
            Self::Mass => 1,
        }
    }

    fn label(self) -> &'static str {
        match self {
            Self::Thermal => "Thermal",
            Self::Mass => "Mass",
        }
    }
}

#[derive(Clone, Copy, Debug)]
struct RenderSettings {
    visual_mode: VisualMode,
    sample_stride: usize,
}

struct Renderer {
    window: winit::window::Window,
    display: Display,
    program: glium::Program,
}

impl Renderer {
    fn new(event_loop: &winit::event_loop::EventLoop<()>) -> Result<Self, String> {
        let (window, display) = SimpleWindowBuilder::new()
            .with_title("Shower Sim (OpenGL)")
            .with_inner_size(360, 720)
            .build(event_loop);

        let program = program!(
            &display,
            330 => {
                vertex: r#"
                    #version 330

                    in vec3 position;
                    in float temperature;
                    in float mass;

                    out vec3 v_color;

                    uniform vec3 world_size;
                    uniform int visual_mode;

                    void main() {
                        float x = (position.x / world_size.x) * 2.0 - 1.0;
                        float y = (position.y / world_size.y) * 2.0 - 1.0;
                        float z = (position.z / world_size.z) * 2.0 - 1.0;

                        // Slight perspective-like depth offset from z.
                        gl_Position = vec4(x, y, z * 0.15, 1.0);

                        // Use cubic-root mass scaling so merged droplets visibly grow while staying bounded.
                        float size = clamp(1.8 + pow(max(mass, 1.0), 0.3333) * 4.2, 1.8, 22.0);
                        gl_PointSize = size;

                        if (visual_mode == 0) {
                            // Thermal mode: multi-stop hot-to-cold gradient.
                            float t = clamp(temperature, 0.0, 1.0);

                            vec3 cold = vec3(0.08, 0.18, 1.0);
                            vec3 cool = vec3(0.1, 0.85, 1.0);
                            vec3 warm = vec3(1.0, 0.9, 0.15);
                            vec3 hot = vec3(1.0, 0.08, 0.08);

                            float u = t * 3.0;
                            if (u < 1.0) {
                                v_color = mix(cold, cool, u);
                            } else if (u < 2.0) {
                                v_color = mix(cool, warm, u - 1.0);
                            } else {
                                v_color = mix(warm, hot, u - 2.0);
                            }
                        } else {
                            // Mass mode: explicit distinct tiers.
                            if (mass < 1.5) {
                                v_color = vec3(0.12, 0.35, 1.0);   // Tier 1 (small)
                            } else if (mass < 3.0) {
                                v_color = vec3(0.08, 0.9, 0.35);   // Tier 2
                            } else if (mass < 6.0) {
                                v_color = vec3(1.0, 0.92, 0.1);    // Tier 3
                            } else if (mass < 10.0) {
                                v_color = vec3(1.0, 0.52, 0.08);   // Tier 4
                            } else {
                                v_color = vec3(0.92, 0.1, 0.86);   // Tier 5 (largest)
                            }
                        }
                    }
                "#,
                fragment: r#"
                    #version 330

                    in vec3 v_color;
                    out vec4 color;

                    void main() {
                        vec2 p = gl_PointCoord * 2.0 - 1.0;
                        float d = dot(p, p);
                        if (d > 1.0) {
                            discard;
                        }

                        float alpha = 1.0 - smoothstep(0.55, 1.0, sqrt(d));
                        color = vec4(v_color, alpha);
                    }
                "#
            }
        )
        .map_err(|e| format!("failed to build shader program: {e}"))?;

        Ok(Self {
            window,
            display,
            program,
        })
    }

    fn draw(&self, vertices: &[ParticleVertex], settings: RenderSettings) -> Result<(), String> {
        let mut target = self.display.draw();
        target.clear_color_and_depth((0.0, 0.0, 0.0, 1.0), 1.0);

        if !vertices.is_empty() {
            let vertex_buffer =
                glium::VertexBuffer::new(&self.display, vertices).map_err(|e| format!("vertex buffer error: {e}"))?;

            let uniforms = uniform! {
                world_size: [WIDTH, HEIGHT, DEPTH],
                visual_mode: settings.visual_mode.uniform_value(),
            };

            let params = glium::DrawParameters {
                blend: glium::Blend::alpha_blending(),
                depth: glium::Depth {
                    test: glium::draw_parameters::DepthTest::IfLess,
                    write: false,
                    ..Default::default()
                },
                ..Default::default()
            };

            target
                .draw(
                    &vertex_buffer,
                    glium::index::NoIndices(glium::index::PrimitiveType::Points),
                    &self.program,
                    &uniforms,
                    &params,
                )
                .map_err(|e| format!("draw error: {e}"))?;
        }

        target.finish().map_err(|e| format!("swap error: {e}"))
    }
}

#[derive(Clone, Copy, Debug, Default)]
struct Vec3 {
    x: f32,
    y: f32,
    z: f32,
}

impl Vec3 {
    fn new(x: f32, y: f32, z: f32) -> Self {
        Self { x, y, z }
    }

    fn distance_squared(self, other: Vec3) -> f32 {
        let dx = self.x - other.x;
        let dy = self.y - other.y;
        let dz = self.z - other.z;
        dx * dx + dy * dy + dz * dz
    }
}

#[derive(Clone, Copy, Debug)]
struct Particle {
    position: Vec3,
    velocity: Vec3,
    mass: f32,
    temperature: f32,
    active: bool,
    sticky_wall: bool,
}

#[derive(Clone, Debug)]
struct SimulationConfig {
    initial_speed: f32,
    cooling_factor: f32,
}

#[derive(Debug, Default)]
struct FrameStats {
    frame: usize,
    physics_ms: f64,
    collision_ms: f64,
    thermal_ms: f64,
    total_ms: f64,
    merges: usize,
    floor_hits: u64,
}

#[derive(Clone, Copy, Debug)]
struct MergeEvent {
    keep: usize,
    remove: usize,
}

fn spawn_particle(rng: &mut StdRng, config: &SimulationConfig) -> Particle {
    // Uniform sample inside the circular shower head.
    let r = EMITTER_RADIUS * rng.gen::<f32>().sqrt();
    let theta = rng.gen_range(0.0..std::f32::consts::TAU);

    let x = WIDTH * 0.5 + r * theta.cos();
    let z = DEPTH * 0.5 + r * theta.sin();

    // Randomized direction within a 30 degree cone around downward vertical.
    let cone_angle = 30.0_f32.to_radians();
    let tilt = rng.gen_range(0.0..cone_angle);
    let azimuth = rng.gen_range(0.0..std::f32::consts::TAU);

    let horizontal = config.initial_speed * tilt.sin();
    let vy = -config.initial_speed * tilt.cos();
    let vx = horizontal * azimuth.cos();
    let vz = horizontal * azimuth.sin();

    Particle {
        position: Vec3::new(x, HEIGHT, z),
        velocity: Vec3::new(vx, vy, vz),
        mass: 1.0,
        temperature: 1.0,
        active: true,
        sticky_wall: false,
    }
}

fn advance_particle(particle: &mut Particle, dt: f32) -> bool {
    if particle.sticky_wall {
        particle.velocity.x = 0.0;
        particle.velocity.z = 0.0;
    }

    particle.velocity.y += GRAVITY * dt;

    particle.position.x += particle.velocity.x * dt;
    particle.position.y += particle.velocity.y * dt;
    particle.position.z += particle.velocity.z * dt;

    if particle.position.y <= 0.0 {
        return false;
    }

    handle_walls(particle);
    true
}

fn estimated_flight_time(config: &SimulationConfig) -> f32 {
    let cone_angle = 30.0_f32.to_radians();
    let min_downward_speed = (config.initial_speed * cone_angle.cos()).max(0.1);
    let a = 0.5 * GRAVITY;
    let b = -min_downward_speed;
    let c = HEIGHT;
    let discriminant = b * b - 4.0 * a * c;

    if discriminant <= 0.0 {
        return DT;
    }

    let sqrt_discriminant = discriminant.sqrt();
    let t0 = (-b + sqrt_discriminant) / (2.0 * a);
    let t1 = (-b - sqrt_discriminant) / (2.0 * a);

    t0.max(t1).max(DT)
}

fn make_particles(count: usize, config: &SimulationConfig) -> Vec<Particle> {
    let mut rng = StdRng::seed_from_u64(42);
    let max_age = estimated_flight_time(config);

    (0..count)
        .map(|_| {
            let mut particle = spawn_particle(&mut rng, config);
            let age = rng.gen_range(0.0..max_age);
            let steps = (age / DT).floor() as usize;
            let remainder = age - steps as f32 * DT;

            for _ in 0..steps {
                if !advance_particle(&mut particle, DT) {
                    return spawn_particle(&mut rng, config);
                }
            }

            if remainder > 0.0 && !advance_particle(&mut particle, remainder) {
                return spawn_particle(&mut rng, config);
            }

            particle
        })
        .collect()
}

fn update_physics_chunk(
    chunk: &mut [Particle],
    chunk_start_index: usize,
    dt: f32,
    config: &SimulationConfig,
    floor_hits: &AtomicU64,
) {
    let mut rng = StdRng::seed_from_u64(1000 + chunk_start_index as u64);

    for particle in chunk.iter_mut() {
        if !particle.active {
            *particle = spawn_particle(&mut rng, config);
            continue;
        }

        if !advance_particle(particle, dt) {
            floor_hits.fetch_add(1, Ordering::Relaxed);
            *particle = spawn_particle(&mut rng, config);
        }
    }
}

fn handle_walls(particle: &mut Particle) {
    let hit_x_wall = particle.position.x < 0.0 || particle.position.x > WIDTH;
    let hit_z_wall = particle.position.z < 0.0 || particle.position.z > DEPTH;

    if hit_x_wall || hit_z_wall {
        particle.position.x = particle.position.x.clamp(0.0, WIDTH);
        particle.position.z = particle.position.z.clamp(0.0, DEPTH);
        particle.velocity.x = 0.0;
        particle.velocity.z = 0.0;
        particle.sticky_wall = true;
    }
}

fn update_thermal_chunk(chunk: &mut [Particle], dt: f32, cooling_factor: f32) {
    for particle in chunk.iter_mut().filter(|p| p.active) {
        // Use direct cooling so droplets visibly transition to cold by floor impact.
        let cooling = cooling_factor * dt;
        particle.temperature = (particle.temperature - cooling).max(0.0);
    }
}

fn grid_key(position: Vec3) -> (i32, i32, i32) {
    (
        (position.x / CELL_SIZE).floor() as i32,
        (position.y / CELL_SIZE).floor() as i32,
        (position.z / CELL_SIZE).floor() as i32,
    )
}

fn build_spatial_grid(particles: &[Particle]) -> HashMap<(i32, i32, i32), Vec<usize>> {
    let mut grid = HashMap::new();

    for (i, particle) in particles.iter().enumerate() {
        if particle.active {
            grid.entry(grid_key(particle.position)).or_insert_with(Vec::new).push(i);
        }
    }

    grid
}

fn detect_collisions_for_cells(
    particles: &[Particle],
    grid: &HashMap<(i32, i32, i32), Vec<usize>>,
    cell_keys: &[(i32, i32, i32)],
    claimed: &mut [bool],
) -> Vec<MergeEvent> {
    let mut events = Vec::new();
    let max_dist_sq = COLLISION_DISTANCE * COLLISION_DISTANCE;

    for &key in cell_keys {
        let Some(indices) = grid.get(&key) else {
            continue;
        };

        for &i in indices {
            let p0 = particles[i];
            if !p0.active || claimed[i] {
                continue;
            }

            let mut matched = false;

            for dx in -1..=1 {
                for dy in -1..=1 {
                    for dz in -1..=1 {
                        if dx < 0 || (dx == 0 && dy < 0) || (dx == 0 && dy == 0 && dz < 0) {
                            continue;
                        }

                        let nkey = (key.0 + dx, key.1 + dy, key.2 + dz);
                        let Some(neighbours) = grid.get(&nkey) else {
                            continue;
                        };

                        for &j in neighbours {
                            if key == nkey && j <= i {
                                continue;
                            }

                            if claimed[j] {
                                continue;
                            }

                            let p1 = particles[j];
                            if !p1.active {
                                continue;
                            }

                            if p0.position.distance_squared(p1.position) <= max_dist_sq {
                                claimed[i] = true;
                                claimed[j] = true;
                                events.push(MergeEvent { keep: i, remove: j });
                                matched = true;
                                break;
                            }
                        }

                        if matched {
                            break;
                        }
                    }

                    if matched {
                        break;
                    }
                }

                if matched {
                    break;
                }
            }
        }
    }

    events
}

fn apply_merge_events(particles: &mut [Particle], events: Vec<MergeEvent>) -> usize {
    let mut consumed = HashSet::new();
    let mut merge_count = 0;

    for event in events {
        if event.keep == event.remove {
            continue;
        }

        if consumed.contains(&event.keep) || consumed.contains(&event.remove) {
            continue;
        }

        if !particles[event.keep].active || !particles[event.remove].active {
            continue;
        }

        let p0 = particles[event.keep];
        let p1 = particles[event.remove];
        let new_mass = p0.mass + p1.mass;

        let mut new_velocity = Vec3::new(
            (p0.mass * p0.velocity.x + p1.mass * p1.velocity.x) / new_mass,
            (p0.mass * p0.velocity.y + p1.mass * p1.velocity.y) / new_mass,
            (p0.mass * p0.velocity.z + p1.mass * p1.velocity.z) / new_mass,
        );

        let sticky_collision = p0.sticky_wall || p1.sticky_wall;
        if sticky_collision {
            new_velocity.x = 0.0;
            new_velocity.z = 0.0;
        }

        particles[event.keep] = Particle {
            position: Vec3::new(
                (p0.position.x * p0.mass + p1.position.x * p1.mass) / new_mass,
                (p0.position.y * p0.mass + p1.position.y * p1.mass) / new_mass,
                (p0.position.z * p0.mass + p1.position.z * p1.mass) / new_mass,
            ),
            velocity: new_velocity,
            mass: new_mass,
            temperature: (p0.temperature * p0.mass + p1.temperature * p1.mass) / new_mass,
            active: true,
            sticky_wall: sticky_collision,
        };

        particles[event.remove].active = false;
        consumed.insert(event.remove);
        merge_count += 1;
    }

    merge_count
}

fn run_physics_stage(
    particles: &mut [Particle],
    dt: f32,
    config: &SimulationConfig,
    floor_hits: &AtomicU64,
) {
    let mid = particles.len() / 2;
    let (left, right) = particles.split_at_mut(mid);

    thread::scope(|scope| {
        scope.spawn(|| update_physics_chunk(left, 0, dt, config, floor_hits));
        scope.spawn(|| update_physics_chunk(right, mid, dt, config, floor_hits));
    });
}

fn run_collision_stage(particles: &mut [Particle]) -> usize {
    let snapshot = particles.to_vec();
    let grid = build_spatial_grid(&snapshot);
    let keys: Vec<_> = grid.keys().copied().collect();
    let mut claimed = vec![false; snapshot.len()];
    let events = detect_collisions_for_cells(&snapshot, &grid, &keys, &mut claimed);

    apply_merge_events(particles, events)
}

fn run_thermal_stage(particles: &mut [Particle], dt: f32, cooling_factor: f32) {
    let mid = particles.len() / 2;
    let (left, right) = particles.split_at_mut(mid);

    thread::scope(|scope| {
        scope.spawn(|| update_thermal_chunk(left, dt, cooling_factor));
        scope.spawn(|| update_thermal_chunk(right, dt, cooling_factor));
    });
}

fn simulate_frame(
    particles: &mut [Particle],
    frame: usize,
    dt: f32,
    config: &SimulationConfig,
    floor_hits: &AtomicU64,
) -> FrameStats {
    let frame_start = Instant::now();

    let start = Instant::now();
    run_physics_stage(particles, dt, config, floor_hits);
    let physics_ms = start.elapsed().as_secs_f64() * 1000.0;

    let start = Instant::now();
    let merges = run_collision_stage(particles);
    let collision_ms = start.elapsed().as_secs_f64() * 1000.0;

    let start = Instant::now();
    run_thermal_stage(particles, dt, config.cooling_factor);
    let thermal_ms = start.elapsed().as_secs_f64() * 1000.0;

    FrameStats {
        frame,
        physics_ms,
        collision_ms,
        thermal_ms,
        total_ms: frame_start.elapsed().as_secs_f64() * 1000.0,
        merges,
        floor_hits: floor_hits.load(Ordering::Relaxed),
    }
}

fn active_count(particles: &[Particle]) -> usize {
    particles.iter().filter(|p| p.active).count()
}

fn print_report(stats: &[FrameStats], particles: &[Particle]) {
    let avg_total = stats.iter().map(|s| s.total_ms).sum::<f64>() / stats.len() as f64;
    let avg_physics = stats.iter().map(|s| s.physics_ms).sum::<f64>() / stats.len() as f64;
    let avg_collision = stats.iter().map(|s| s.collision_ms).sum::<f64>() / stats.len() as f64;
    let avg_thermal = stats.iter().map(|s| s.thermal_ms).sum::<f64>() / stats.len() as f64;
    let total_merges: usize = stats.iter().map(|s| s.merges).sum();
    let final_floor_hits = stats.last().map(|s| s.floor_hits).unwrap_or(0);
    let average_temperature = particles
        .iter()
        .filter(|p| p.active)
        .map(|p| p.temperature)
        .sum::<f32>()
        / active_count(particles).max(1) as f32;

    println!("\n--- Rust CPU Shower Simulation Report ---");
    println!("particles allocated:       {}", particles.len());
    println!("particles active:          {}", active_count(particles));
    println!("frames simulated:          {}", stats.len());
    println!("average frame time:        {:.3} ms", avg_total);
    println!("average physics stage:     {:.3} ms", avg_physics);
    println!("average collision stage:   {:.3} ms", avg_collision);
    println!("average thermal stage:     {:.3} ms", avg_thermal);
    println!("total merge events:        {}", total_merges);
    println!("cumulative floor hits:     {}", final_floor_hits);
    println!("average temperature:       {:.3}", average_temperature);
}

fn print_interactive_report(
    frames_simulated: usize,
    total_frame_ms: f64,
    total_physics_ms: f64,
    total_collision_ms: f64,
    total_thermal_ms: f64,
    total_merges: usize,
    floor_hits: u64,
    particles: &[Particle],
) {
    let frames = frames_simulated.max(1) as f64;
    let average_temperature = particles
        .iter()
        .filter(|p| p.active)
        .map(|p| p.temperature)
        .sum::<f32>()
        / active_count(particles).max(1) as f32;

    println!("\n--- Rust CPU Shower Simulation Report ---");
    println!("particles allocated:       {}", particles.len());
    println!("particles active:          {}", active_count(particles));
    println!("frames simulated:          {}", frames_simulated);
    println!("average frame time:        {:.3} ms", total_frame_ms / frames);
    println!("average physics stage:     {:.3} ms", total_physics_ms / frames);
    println!("average collision stage:   {:.3} ms", total_collision_ms / frames);
    println!("average thermal stage:     {:.3} ms", total_thermal_ms / frames);
    println!("total merge events:        {}", total_merges);
    println!("cumulative floor hits:     {}", floor_hits);
    println!("average temperature:       {:.3}", average_temperature);
}

fn build_render_vertices(particles: &[Particle], sample_stride: usize, out: &mut Vec<ParticleVertex>) {
    out.clear();
    let stride = sample_stride.max(1);
    out.reserve(particles.len().saturating_div(stride).max(1));

    // Stable subsampling by index avoids visual popping caused by selecting every Nth active
    // particle from a changing active set each frame.
    for particle in particles.iter().step_by(stride) {
        if !particle.active {
            continue;
        }

        out.push(ParticleVertex {
            position: [particle.position.x, particle.position.y, particle.position.z],
            temperature: particle.temperature,
            mass: particle.mass,
        });
    }
}

fn update_window_title(
    window: &winit::window::Window,
    settings: RenderSettings,
    paused: bool,
    dt_scale: f32,
    config: &SimulationConfig,
    active_particles: usize,
) {
    let paused_text = if paused { "Paused" } else { "Running" };
    window.set_title(&format!(
        "Shower Sim (OpenGL) | {} | Particles: {} | Mode: {} | Render 1/{} | dt x{:.2} | cool {:.2} | speed {:.2}",
        paused_text,
        active_particles,
        settings.visual_mode.label(),
        settings.sample_stride,
        dt_scale,
        config.cooling_factor,
        config.initial_speed
    ));
}

fn run_headless(config: &SimulationConfig) {
    println!("Starting Rust CPU shower simulation...");
    println!("Config: {:?}", config);
    println!("Using 2 physics threads, 1 collision thread, and 2 thermodynamics threads per frame.");

    let floor_hits = AtomicU64::new(0);
    let mut particles = make_particles(PARTICLE_COUNT, config);
    let mut stats = Vec::with_capacity(FRAME_COUNT);

    for frame in 0..FRAME_COUNT {
        let frame_stats = simulate_frame(&mut particles, frame, DT, config, &floor_hits);

        if frame % 60 == 0 {
            println!(
                "frame {:>4}: total={:>8.3}ms physics={:>7.3}ms collision={:>7.3}ms thermal={:>7.3}ms merges={:>5} floor_hits={}",
                frame_stats.frame,
                frame_stats.total_ms,
                frame_stats.physics_ms,
                frame_stats.collision_ms,
                frame_stats.thermal_ms,
                frame_stats.merges,
                frame_stats.floor_hits
            );
        }

        stats.push(frame_stats);
    }

    print_report(&stats, &particles);
}

fn run_with_opengl(config: &SimulationConfig) -> Result<(), String> {
    use winit::event::ElementState;
    use winit::keyboard::{KeyCode, PhysicalKey};

    let event_loop = winit::event_loop::EventLoop::builder()
        .build()
        .map_err(|e| format!("failed to create event loop: {e}"))?;

    let renderer = Renderer::new(&event_loop)?;
    let floor_hits = AtomicU64::new(0);
    let mut particles = make_particles(PARTICLE_COUNT, config);
    let mut vertices = Vec::with_capacity(PARTICLE_COUNT);
    let mut frame = 0usize;
    let mut total_frame_ms = 0.0_f64;
    let mut total_physics_ms = 0.0_f64;
    let mut total_collision_ms = 0.0_f64;
    let mut total_thermal_ms = 0.0_f64;
    let mut total_merges = 0usize;
    let mut runtime_config = config.clone();
    let mut paused = false;
    let mut dt_scale = 1.0_f32;
    let mut settings = RenderSettings {
        visual_mode: VisualMode::Thermal,
        sample_stride: DEFAULT_RENDER_STRIDE,
    };

    update_window_title(
        &renderer.window,
        settings,
        paused,
        dt_scale,
        &runtime_config,
        active_count(&particles),
    );

    println!("Starting Rust CPU shower simulation with OpenGL rendering...");
    println!("Config: {:?}", config);
    println!(
        "Controls: 1=Thermal 2=Mass -=fewer points +=more points Space=pause R=reset Q=quit Up/Down=sim speed [/]=cooling ,/.=emitter speed"
    );

    #[allow(deprecated)]
    event_loop
        .run(move |event, event_loop| {
            event_loop.set_control_flow(winit::event_loop::ControlFlow::Poll);

            match event {
                winit::event::Event::WindowEvent { event, .. } => match event {
                    winit::event::WindowEvent::CloseRequested => {
                        event_loop.exit();
                    }
                    winit::event::WindowEvent::KeyboardInput { event, .. } => {
                        if event.state == ElementState::Pressed {
                            let mut changed = false;
                            match event.physical_key {
                                PhysicalKey::Code(KeyCode::Digit1) => {
                                    settings.visual_mode = VisualMode::Thermal;
                                    changed = true;
                                }
                                PhysicalKey::Code(KeyCode::Digit2) => {
                                    settings.visual_mode = VisualMode::Mass;
                                    changed = true;
                                }
                                PhysicalKey::Code(KeyCode::Minus)
                                | PhysicalKey::Code(KeyCode::NumpadSubtract) => {
                                    settings.sample_stride =
                                        (settings.sample_stride.saturating_mul(2)).clamp(1, 8192);
                                    changed = true;
                                }
                                PhysicalKey::Code(KeyCode::Equal)
                                | PhysicalKey::Code(KeyCode::NumpadAdd) => {
                                    settings.sample_stride = (settings.sample_stride / 2).max(1);
                                    changed = true;
                                }
                                PhysicalKey::Code(KeyCode::Space) => {
                                    paused = !paused;
                                    changed = true;
                                }
                                PhysicalKey::Code(KeyCode::KeyR) => {
                                    particles = make_particles(PARTICLE_COUNT, &runtime_config);
                                    frame = 0;
                                    total_frame_ms = 0.0;
                                    total_physics_ms = 0.0;
                                    total_collision_ms = 0.0;
                                    total_thermal_ms = 0.0;
                                    total_merges = 0;
                                    floor_hits.store(0, Ordering::Relaxed);
                                    changed = true;
                                }
                                PhysicalKey::Code(KeyCode::KeyQ) => {
                                    print_interactive_report(
                                        frame,
                                        total_frame_ms,
                                        total_physics_ms,
                                        total_collision_ms,
                                        total_thermal_ms,
                                        total_merges,
                                        floor_hits.load(Ordering::Relaxed),
                                        &particles,
                                    );
                                    event_loop.exit();
                                    return;
                                }
                                PhysicalKey::Code(KeyCode::ArrowUp) => {
                                    dt_scale = (dt_scale * 1.25).clamp(0.1, 5.0);
                                    changed = true;
                                }
                                PhysicalKey::Code(KeyCode::ArrowDown) => {
                                    dt_scale = (dt_scale * 0.8).clamp(0.1, 5.0);
                                    changed = true;
                                }
                                PhysicalKey::Code(KeyCode::BracketRight) => {
                                    runtime_config.cooling_factor =
                                        (runtime_config.cooling_factor + 0.1).clamp(0.1, 10.0);
                                    changed = true;
                                }
                                PhysicalKey::Code(KeyCode::BracketLeft) => {
                                    runtime_config.cooling_factor =
                                        (runtime_config.cooling_factor - 0.1).clamp(0.1, 10.0);
                                    changed = true;
                                }
                                PhysicalKey::Code(KeyCode::Period) => {
                                    runtime_config.initial_speed =
                                        (runtime_config.initial_speed + 0.1).clamp(0.5, 10.0);
                                    changed = true;
                                }
                                PhysicalKey::Code(KeyCode::Comma) => {
                                    runtime_config.initial_speed =
                                        (runtime_config.initial_speed - 0.1).clamp(0.5, 10.0);
                                    changed = true;
                                }
                                _ => {}
                            }

                            if changed {
                                update_window_title(
                                    &renderer.window,
                                    settings,
                                    paused,
                                    dt_scale,
                                    &runtime_config,
                                    active_count(&particles),
                                );
                                println!(
                                    "visual: mode={} render_stride=1/{} paused={} dt_scale={:.2} cooling={:.2} initial_speed={:.2}",
                                    settings.visual_mode.label(),
                                    settings.sample_stride,
                                    paused,
                                    dt_scale,
                                    runtime_config.cooling_factor,
                                    runtime_config.initial_speed
                                );
                            }
                        }
                    }
                    winit::event::WindowEvent::RedrawRequested => {
                        if !paused {
                            let frame_stats =
                                simulate_frame(&mut particles, frame, DT * dt_scale, &runtime_config, &floor_hits);

                            if frame % 60 == 0 {
                                println!(
                                    "frame {:>4}: total={:>8.3}ms physics={:>7.3}ms collision={:>7.3}ms thermal={:>7.3}ms merges={:>5} floor_hits={}",
                                    frame_stats.frame,
                                    frame_stats.total_ms,
                                    frame_stats.physics_ms,
                                    frame_stats.collision_ms,
                                    frame_stats.thermal_ms,
                                    frame_stats.merges,
                                    frame_stats.floor_hits
                                );
                            }

                            total_frame_ms += frame_stats.total_ms;
                            total_physics_ms += frame_stats.physics_ms;
                            total_collision_ms += frame_stats.collision_ms;
                            total_thermal_ms += frame_stats.thermal_ms;
                            total_merges += frame_stats.merges;
                            frame += 1;
                        }

                        build_render_vertices(&particles, settings.sample_stride, &mut vertices);
                        if let Err(err) = renderer.draw(&vertices, settings) {
                            eprintln!("render error: {err}");
                            event_loop.exit();
                            return;
                        }
                    }
                    _ => {}
                },
                winit::event::Event::AboutToWait => {
                    renderer.window.request_redraw();
                }
                _ => {}
            }
        })
        .map_err(|e| format!("event loop error: {e}"))
}

fn main() {
    let mut headless = false;

    for arg in env::args().skip(1) {
        if arg == "--headless" {
            headless = true;
            continue;
        }

        eprintln!(
            "Ignoring unknown argument '{arg}'. Use --headless."
        );
    }

    let config = SimulationConfig {
        initial_speed: 2.75,
        // Tune so baseline droplets cool close to zero before reaching the floor.
        cooling_factor: 1.50,
    };

    if headless {
        run_headless(&config);
        return;
    }

    if let Err(err) = run_with_opengl(&config) {
        eprintln!("OpenGL renderer unavailable ({err}), running headless mode.");
        run_headless(&config);
    }
}
