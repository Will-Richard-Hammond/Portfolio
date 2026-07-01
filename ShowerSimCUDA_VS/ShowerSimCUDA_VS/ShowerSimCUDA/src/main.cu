#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <gl/GL.h>

#include <cuda_runtime.h>
#include <device_launch_parameters.h>

#include <algorithm>
#include <cmath>
#include <cstdio>
#include <cstdlib>
#include <iomanip>
#include <iostream>
#include <sstream>
#include <stdexcept>
#include <string>
#include <vector>

#pragma comment(lib, "opengl32.lib")

#ifndef VK_OEM_4
#define VK_OEM_4 0xDB
#endif
#ifndef VK_OEM_6
#define VK_OEM_6 0xDD
#endif
#ifndef VK_OEM_COMMA
#define VK_OEM_COMMA 0xBC
#endif
#ifndef VK_OEM_PERIOD
#define VK_OEM_PERIOD 0xBE
#endif
#ifndef VK_OEM_MINUS
#define VK_OEM_MINUS 0xBD
#endif
#ifndef VK_OEM_PLUS
#define VK_OEM_PLUS 0xBB
#endif

static constexpr float HEIGHT = 2.0f;
static constexpr float WIDTH = 1.0f;
static constexpr float DEPTH = 1.0f;
static constexpr float EMITTER_RADIUS = 0.05f;
static constexpr float GRAVITY = -9.81f;

static constexpr float CELL_SIZE = 0.015f;
static constexpr float COLLISION_DISTANCE = 0.012f;
static constexpr float COLLISION_DISTANCE_SQ = COLLISION_DISTANCE * COLLISION_DISTANCE;

static constexpr int GRID_X = static_cast<int>(WIDTH / CELL_SIZE) + 2;
static constexpr int GRID_Y = static_cast<int>(HEIGHT / CELL_SIZE) + 2;
static constexpr int GRID_Z = static_cast<int>(DEPTH / CELL_SIZE) + 2;
static constexpr int GRID_CELLS = GRID_X * GRID_Y * GRID_Z;

static constexpr float PI = 3.14159265358979323846f;

#define CUDA_CHECK(call)                                                                 \
    do {                                                                                 \
        cudaError_t err__ = (call);                                                      \
        if (err__ != cudaSuccess) {                                                      \
            std::ostringstream oss__;                                                    \
            oss__ << "CUDA error at " << __FILE__ << ":" << __LINE__ << " -> "      \
                  << cudaGetErrorString(err__);                                          \
            throw std::runtime_error(oss__.str());                                       \
        }                                                                                \
    } while (0)

struct SimulationConfig {
    int particleCount = 100000;
    int frameCount = 600;
    int blockSize = 256;
    float dt = 1.0f / 60.0f;
    float initialSpeed = 2.75f;
    float coolingFactor = 1.0f;
};

struct DeviceParticles {
    float* x = nullptr;
    float* y = nullptr;
    float* z = nullptr;
    float* vx = nullptr;
    float* vy = nullptr;
    float* vz = nullptr;
    float* mass = nullptr;
    float* temperature = nullptr;
    int* active = nullptr;
    int* sticky = nullptr;

    float* momentumX = nullptr;
    float* momentumY = nullptr;
    float* momentumZ = nullptr;
    float* weightedX = nullptr;
    float* weightedY = nullptr;
    float* weightedZ = nullptr;
    float* weightedTemperature = nullptr;

    int* cellHeads = nullptr;
    int* nextInCell = nullptr;
    int* mergeTarget = nullptr;

    unsigned long long* floorHits = nullptr;
    unsigned long long* merges = nullptr;
};

struct FrameStats {
    float physicsMs = 0.0f;
    float collisionMs = 0.0f;
    float thermalMs = 0.0f;
    float totalMs = 0.0f;
    unsigned long long floorHits = 0;
    unsigned long long merges = 0;
};

enum ColorMode { COLOR_THERMAL = 0, COLOR_MASS = 1 };

static ColorMode g_colorMode = COLOR_THERMAL;
static int g_sampleRate = 100;
static bool g_paused = false;
static bool g_resetRequested = false;
static float g_dtScale = 1.0f;

static HWND g_hwnd = nullptr;
static HDC g_hdc = nullptr;
static HGLRC g_glrc = nullptr;
static bool g_running = true;
static int g_windowWidth = 1024;
static int g_windowHeight = 768;

static float g_runtimeCoolingFactor = 2.25f;
static float g_runtimeInitialSpeed = 2.75f;

SimulationConfig parse_args(int argc, char** argv);
void allocate_particles(DeviceParticles& p, int count);
void free_particles(DeviceParticles& p);
float elapsed_ms(cudaEvent_t start, cudaEvent_t stop);
void launch_and_time(cudaEvent_t start, cudaEvent_t stop);
FrameStats simulate_frame(DeviceParticles& p, const SimulationConfig& cfg, int frame, float dtScale);
void process_window_messages();
void init_opengl_window(HINSTANCE instance);
void shutdown_opengl_window();
void render_frame_opengl(DeviceParticles& d, const SimulationConfig& cfg,
                         float* hostX, float* hostY, float* hostZ,
                         float* hostMass, float* hostTemp);

__device__ __forceinline__ unsigned int hash_u32(unsigned int x) {
    x ^= x >> 16;
    x *= 0x7feb352du;
    x ^= x >> 15;
    x *= 0x846ca68bu;
    x ^= x >> 16;
    return x;
}

__device__ __forceinline__ float rand01(unsigned int seed, unsigned int salt) {
    unsigned int h = hash_u32(seed ^ (salt * 0x9e3779b9u));
    return (h & 0x00FFFFFFu) / 16777216.0f;
}

__device__ __forceinline__ int clamp_int(int v, int lo, int hi) {
    return (v < lo) ? lo : ((v > hi) ? hi : v);
}

__device__ __forceinline__ int cell_index_from_position(float x, float y, float z) {
    int cx = clamp_int(static_cast<int>(floorf(x / CELL_SIZE)), 0, GRID_X - 1);
    int cy = clamp_int(static_cast<int>(floorf(y / CELL_SIZE)), 0, GRID_Y - 1);
    int cz = clamp_int(static_cast<int>(floorf(z / CELL_SIZE)), 0, GRID_Z - 1);
    return (cy * GRID_Z + cz) * GRID_X + cx;
}

__device__ void spawn_particle(DeviceParticles p, int i, unsigned int seed, float initialSpeed) {
    float r = EMITTER_RADIUS * sqrtf(rand01(seed, 1));
    float theta = 2.0f * PI * rand01(seed, 2);

    p.x[i] = WIDTH * 0.5f + r * cosf(theta);
    p.y[i] = HEIGHT;
    p.z[i] = DEPTH * 0.5f + r * sinf(theta);

    float coneAngle = 30.0f * PI / 180.0f;
    float tilt = coneAngle * rand01(seed, 3);
    float azimuth = 2.0f * PI * rand01(seed, 4);

    float horizontal = initialSpeed * sinf(tilt);
    p.vx[i] = horizontal * cosf(azimuth);
    p.vy[i] = -initialSpeed * cosf(tilt);
    p.vz[i] = horizontal * sinf(azimuth);

    p.mass[i] = 1.0f;
    p.temperature[i] = 1.0f;
    p.active[i] = 1;
    p.sticky[i] = 0;
}

__global__ void initialise_particles_kernel(DeviceParticles p, int count, unsigned int seed, float initialSpeed) {
    int i = blockIdx.x * blockDim.x + threadIdx.x;
    if (i >= count) return;

    spawn_particle(p, i, seed + i * 747796405u, initialSpeed);

    float age = 0.85f * rand01(seed + i, 99);
    float dt = 1.0f / 120.0f;
    int steps = static_cast<int>(age / dt);
    for (int s = 0; s < steps; ++s) {
        p.vy[i] += GRAVITY * dt;
        p.x[i] += p.vx[i] * dt;
        p.y[i] += p.vy[i] * dt;
        p.z[i] += p.vz[i] * dt;
        if (p.y[i] <= 0.0f) {
            spawn_particle(p, i, seed + i * 2654435761u + s, initialSpeed);
            break;
        }
        p.x[i] = fminf(fmaxf(p.x[i], 0.0f), WIDTH);
        p.z[i] = fminf(fmaxf(p.z[i], 0.0f), DEPTH);
    }
}

__device__ void handle_walls(DeviceParticles p, int i) {
    bool hitX = p.x[i] < 0.0f || p.x[i] > WIDTH;
    bool hitZ = p.z[i] < 0.0f || p.z[i] > DEPTH;
    if (!hitX && !hitZ) return;

    p.x[i] = fminf(fmaxf(p.x[i], 0.0f), WIDTH);
    p.z[i] = fminf(fmaxf(p.z[i], 0.0f), DEPTH);
    p.vx[i] = 0.0f;
    p.vz[i] = 0.0f;
    p.sticky[i] = 1;
}

__global__ void physics_kernel(DeviceParticles p, int count, float dt, float initialSpeed, unsigned int frameSeed) {
    int i = blockIdx.x * blockDim.x + threadIdx.x;
    if (i >= count) return;

    if (p.active[i] == 0) {
        spawn_particle(p, i, frameSeed + i * 1664525u, initialSpeed);
        return;
    }

    if (p.sticky[i]) {
        p.vx[i] = 0.0f;
        p.vz[i] = 0.0f;
    }

    p.vy[i] += GRAVITY * dt;
    p.x[i] += p.vx[i] * dt;
    p.y[i] += p.vy[i] * dt;
    p.z[i] += p.vz[i] * dt;

    if (p.y[i] <= 0.0f) {
        atomicAdd(p.floorHits, 1ull);
        spawn_particle(p, i, frameSeed + i * 22695477u, initialSpeed);
        return;
    }

    handle_walls(p, i);
}

__global__ void thermal_kernel(DeviceParticles p, int count, float dt, float coolingFactor) {
    int i = blockIdx.x * blockDim.x + threadIdx.x;
    if (i >= count || p.active[i] == 0) return;

    float m = fmaxf(p.mass[i], 1.0f);

    float fallProgress = 1.0f - fminf(fmaxf(p.y[i] / HEIGHT, 0.0f), 1.0f);
    float heightCoolingBoost = 1.0f + 3.0f * fallProgress * fallProgress;

    float cooling = coolingFactor * dt * heightCoolingBoost / m;
    p.temperature[i] = fmaxf(0.0f, p.temperature[i] - cooling);
}

__global__ void clear_grid_kernel(int* cellHeads, int cellCount) {
    int i = blockIdx.x * blockDim.x + threadIdx.x;
    if (i < cellCount) cellHeads[i] = -1;
}

__global__ void build_grid_kernel(DeviceParticles p, int count) {
    int i = blockIdx.x * blockDim.x + threadIdx.x;
    if (i >= count || p.active[i] == 0) {
        if (i < count) p.nextInCell[i] = -1;
        return;
    }

    int cell = cell_index_from_position(p.x[i], p.y[i], p.z[i]);
    int oldHead = atomicExch(&p.cellHeads[cell], i);
    p.nextInCell[i] = oldHead;
    p.mergeTarget[i] = -1;
}

__global__ void reset_collision_buffers_kernel(DeviceParticles p, int count) {
    int i = blockIdx.x * blockDim.x + threadIdx.x;
    if (i >= count) return;

    p.mergeTarget[i] = -1;
    p.momentumX[i] = p.mass[i] * p.vx[i];
    p.momentumY[i] = p.mass[i] * p.vy[i];
    p.momentumZ[i] = p.mass[i] * p.vz[i];
    p.weightedX[i] = p.mass[i] * p.x[i];
    p.weightedY[i] = p.mass[i] * p.y[i];
    p.weightedZ[i] = p.mass[i] * p.z[i];
    p.weightedTemperature[i] = p.mass[i] * p.temperature[i];
}

__global__ void find_collisions_kernel(DeviceParticles p, int count) {
    int i = blockIdx.x * blockDim.x + threadIdx.x;
    if (i >= count || p.active[i] == 0) return;

    int cx = clamp_int(static_cast<int>(floorf(p.x[i] / CELL_SIZE)), 0, GRID_X - 1);
    int cy = clamp_int(static_cast<int>(floorf(p.y[i] / CELL_SIZE)), 0, GRID_Y - 1);
    int cz = clamp_int(static_cast<int>(floorf(p.z[i] / CELL_SIZE)), 0, GRID_Z - 1);

    for (int oy = -1; oy <= 1; ++oy) {
        int ny = cy + oy;
        if (ny < 0 || ny >= GRID_Y) continue;
        for (int oz = -1; oz <= 1; ++oz) {
            int nz = cz + oz;
            if (nz < 0 || nz >= GRID_Z) continue;
            for (int ox = -1; ox <= 1; ++ox) {
                int nx = cx + ox;
                if (nx < 0 || nx >= GRID_X) continue;
                int cell = (ny * GRID_Z + nz) * GRID_X + nx;

                for (int j = p.cellHeads[cell]; j != -1; j = p.nextInCell[j]) {
                    if (j <= i || p.active[j] == 0) continue;

                    float dx = p.x[i] - p.x[j];
                    float dy = p.y[i] - p.y[j];
                    float dz = p.z[i] - p.z[j];
                    float distSq = dx * dx + dy * dy + dz * dz;

                    if (distSq <= COLLISION_DISTANCE_SQ) {
                        atomicCAS(&p.mergeTarget[j], -1, i);
                        return;
                    }
                }
            }
        }
    }
}

__global__ void apply_merges_kernel(DeviceParticles p, int count) {
    int j = blockIdx.x * blockDim.x + threadIdx.x;
    if (j >= count) return;

    int keep = p.mergeTarget[j];
    if (keep < 0 || keep >= count || keep == j) return;
    if (p.active[j] == 0 || p.active[keep] == 0) return;

    if (atomicCAS(&p.active[j], 1, 0) != 1) return;

    float mj = p.mass[j];
    atomicAdd(&p.mass[keep], mj);
    atomicAdd(&p.momentumX[keep], mj * p.vx[j]);
    atomicAdd(&p.momentumY[keep], mj * p.vy[j]);
    atomicAdd(&p.momentumZ[keep], mj * p.vz[j]);
    atomicAdd(&p.weightedX[keep], mj * p.x[j]);
    atomicAdd(&p.weightedY[keep], mj * p.y[j]);
    atomicAdd(&p.weightedZ[keep], mj * p.z[j]);
    atomicAdd(&p.weightedTemperature[keep], mj * p.temperature[j]);

    if (p.sticky[j] || p.sticky[keep]) {
        atomicExch(&p.sticky[keep], 1);
    }

    atomicAdd(p.merges, 1ull);
}

__global__ void normalise_merged_particles_kernel(DeviceParticles p, int count) {
    int i = blockIdx.x * blockDim.x + threadIdx.x;
    if (i >= count || p.active[i] == 0) return;

    float m = fmaxf(p.mass[i], 1.0e-6f);
    p.x[i] = p.weightedX[i] / m;
    p.y[i] = p.weightedY[i] / m;
    p.z[i] = p.weightedZ[i] / m;
    p.temperature[i] = fminf(1.0f, fmaxf(0.0f, p.weightedTemperature[i] / m));

    p.vy[i] = p.momentumY[i] / m;
    if (p.sticky[i]) {
        p.vx[i] = 0.0f;
        p.vz[i] = 0.0f;
        p.x[i] = fminf(fmaxf(p.x[i], 0.0f), WIDTH);
        p.z[i] = fminf(fmaxf(p.z[i], 0.0f), DEPTH);
    } else {
        p.vx[i] = p.momentumX[i] / m;
        p.vz[i] = p.momentumZ[i] / m;
    }
}

__global__ void respawn_inactive_after_merge_kernel(DeviceParticles p, int count, unsigned int frameSeed, float initialSpeed) {
    int i = blockIdx.x * blockDim.x + threadIdx.x;
    if (i >= count || p.active[i] != 0) return;

    spawn_particle(p, i, frameSeed + i * 1103515245u, initialSpeed);
}

void allocate_particles(DeviceParticles& p, int count) {
    CUDA_CHECK(cudaMalloc(&p.x, count * sizeof(float)));
    CUDA_CHECK(cudaMalloc(&p.y, count * sizeof(float)));
    CUDA_CHECK(cudaMalloc(&p.z, count * sizeof(float)));
    CUDA_CHECK(cudaMalloc(&p.vx, count * sizeof(float)));
    CUDA_CHECK(cudaMalloc(&p.vy, count * sizeof(float)));
    CUDA_CHECK(cudaMalloc(&p.vz, count * sizeof(float)));
    CUDA_CHECK(cudaMalloc(&p.mass, count * sizeof(float)));
    CUDA_CHECK(cudaMalloc(&p.temperature, count * sizeof(float)));
    CUDA_CHECK(cudaMalloc(&p.active, count * sizeof(int)));
    CUDA_CHECK(cudaMalloc(&p.sticky, count * sizeof(int)));

    CUDA_CHECK(cudaMalloc(&p.momentumX, count * sizeof(float)));
    CUDA_CHECK(cudaMalloc(&p.momentumY, count * sizeof(float)));
    CUDA_CHECK(cudaMalloc(&p.momentumZ, count * sizeof(float)));
    CUDA_CHECK(cudaMalloc(&p.weightedX, count * sizeof(float)));
    CUDA_CHECK(cudaMalloc(&p.weightedY, count * sizeof(float)));
    CUDA_CHECK(cudaMalloc(&p.weightedZ, count * sizeof(float)));
    CUDA_CHECK(cudaMalloc(&p.weightedTemperature, count * sizeof(float)));

    CUDA_CHECK(cudaMalloc(&p.cellHeads, GRID_CELLS * sizeof(int)));
    CUDA_CHECK(cudaMalloc(&p.nextInCell, count * sizeof(int)));
    CUDA_CHECK(cudaMalloc(&p.mergeTarget, count * sizeof(int)));
    CUDA_CHECK(cudaMalloc(&p.floorHits, sizeof(unsigned long long)));
    CUDA_CHECK(cudaMalloc(&p.merges, sizeof(unsigned long long)));
    CUDA_CHECK(cudaMemset(p.floorHits, 0, sizeof(unsigned long long)));
    CUDA_CHECK(cudaMemset(p.merges, 0, sizeof(unsigned long long)));
}

void free_particles(DeviceParticles& p) {
    cudaFree(p.x); cudaFree(p.y); cudaFree(p.z);
    cudaFree(p.vx); cudaFree(p.vy); cudaFree(p.vz);
    cudaFree(p.mass); cudaFree(p.temperature);
    cudaFree(p.active); cudaFree(p.sticky);
    cudaFree(p.momentumX); cudaFree(p.momentumY); cudaFree(p.momentumZ);
    cudaFree(p.weightedX); cudaFree(p.weightedY); cudaFree(p.weightedZ); cudaFree(p.weightedTemperature);
    cudaFree(p.cellHeads); cudaFree(p.nextInCell); cudaFree(p.mergeTarget);
    cudaFree(p.floorHits); cudaFree(p.merges);
    p = DeviceParticles{};
}

float elapsed_ms(cudaEvent_t start, cudaEvent_t stop) {
    float ms = 0.0f;
    CUDA_CHECK(cudaEventElapsedTime(&ms, start, stop));
    return ms;
}

void launch_and_time(cudaEvent_t start, cudaEvent_t stop) {
    cudaError_t err = cudaGetLastError();
    CUDA_CHECK(err);
    CUDA_CHECK(cudaEventRecord(stop));
    CUDA_CHECK(cudaEventSynchronize(stop));
    err = cudaGetLastError();
    CUDA_CHECK(err);
}

std::string wall_mode_name(int) {
    return "Sticky";
}

void print_usage() {
    std::cout
        << "CUDA Shower Simulation (visual)\n"
        << "Controls:\n"
        << "  1: Thermal mode\n"
        << "  2: Mass mode\n"
        << "  Q: Quit simulation\n"
        << "  -: Render fewer particles\n"
        << "  +: Render more particles\n"
        << "  Space: Pause/resume simulation\n"
        << "  R: Reset particles, frame counter, stats, and floor-hit counter\n"
        << "  Up Arrow: Increase simulation speed\n"
        << "  Down Arrow: Decrease simulation speed\n"
        << "  [: Decrease cooling factor\n"
        << "  ]: Increase cooling factor\n"
        << "  ,: Decrease emitter initial speed\n"
        << "  .: Increase emitter initial speed\n"
        << "Options:\n"
        << "  --particles N\n"
        << "  --frames N\n"
        << "  --block N\n"
        << "  --speed F\n"
        << "  --cool F\n"
        << "  --dt F\n"
        << "  --help\n";
}

SimulationConfig parse_args(int argc, char** argv) {
    SimulationConfig cfg{};
    for (int i = 1; i < argc; ++i) {
        std::string arg = argv[i];
        auto need_value = [&](const char* name) -> const char* {
            if (i + 1 >= argc) throw std::runtime_error(std::string("Missing value for ") + name);
            return argv[++i];
        };

        if (arg == "--particles") cfg.particleCount = std::max(1, std::atoi(need_value("--particles")));
        else if (arg == "--frames") cfg.frameCount = std::max(1, std::atoi(need_value("--frames")));
        else if (arg == "--block") cfg.blockSize = std::max(32, std::atoi(need_value("--block")));
        else if (arg == "--speed") cfg.initialSpeed = std::atof(need_value("--speed"));
        else if (arg == "--cool") cfg.coolingFactor = std::atof(need_value("--cool"));
        else if (arg == "--dt") cfg.dt = std::atof(need_value("--dt"));
        else if (arg == "--help" || arg == "-h") {
            print_usage();
            std::exit(0);
        } else {
            throw std::runtime_error("Unknown argument: " + arg);
        }
    }
    return cfg;
}

inline void temperature_to_rgb(float t, float& r, float& g, float& b) {
    float clamped = fminf(1.0f, fmaxf(0.0f, t));
    if (clamped < 0.5f) {
        float u = clamped / 0.5f;
        r = u;
        g = u;
        b = 1.0f - u;
    } else {
        float u = (clamped - 0.5f) / 0.5f;
        r = 1.0f;
        g = 1.0f - u;
        b = 0.0f;
    }
}

inline void mass_to_rgb(float m, float& r, float& g, float& b) {
    if (m < 1.5f)       { r = 0.0f; g = 0.0f; b = 1.0f; }
    else if (m < 3.0f)  { r = 0.0f; g = 1.0f; b = 0.0f; }
    else if (m < 6.0f)  { r = 1.0f; g = 1.0f; b = 0.0f; }
    else if (m < 10.0f) { r = 1.0f; g = 0.55f; b = 0.0f; }
    else                { r = 1.0f; g = 0.0f; b = 1.0f; }
}

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam) {
    switch (uMsg) {
    case WM_CLOSE:
        g_running = false;
        DestroyWindow(hwnd);
        return 0;
    case WM_DESTROY:
        g_running = false;
        PostQuitMessage(0);
        return 0;
    case WM_SIZE:
        g_windowWidth = LOWORD(lParam);
        g_windowHeight = HIWORD(lParam);
        return 0;
    case WM_KEYDOWN:
        switch (wParam) {
        case '1': g_colorMode = COLOR_THERMAL; break;
        case '2': g_colorMode = COLOR_MASS; break;
        case 'Q':
        case 'q':
            g_running = false;
            DestroyWindow(hwnd);
            break;
        case VK_SPACE: g_paused = !g_paused; break;
        case 'R': g_resetRequested = true; break;
        case VK_UP: g_dtScale *= 1.1f; break;
        case VK_DOWN: g_dtScale = std::max(1.0e-6f, g_dtScale / 1.1f); break;
        case VK_OEM_4: g_runtimeCoolingFactor *= 0.9f; break;
        case VK_OEM_6: g_runtimeCoolingFactor *= 1.1f; break;
        case VK_OEM_COMMA: g_runtimeInitialSpeed = std::max(0.0f, g_runtimeInitialSpeed - 0.1f); break;
        case VK_OEM_PERIOD: g_runtimeInitialSpeed += 0.1f; break;
        case VK_OEM_MINUS: g_sampleRate = std::min(1000000, g_sampleRate * 2); break;
        case VK_OEM_PLUS: g_sampleRate = std::max(1, g_sampleRate / 2); break;
        case VK_ESCAPE:
            g_running = false;
            DestroyWindow(hwnd);
            break;
        default:
            break;
        }
        return 0;
    default:
        return DefWindowProc(hwnd, uMsg, wParam, lParam);
    }
}

void process_window_messages() {
    MSG msg{};
    while (PeekMessage(&msg, nullptr, 0, 0, PM_REMOVE)) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }
}

void init_opengl_window(HINSTANCE instance) {
    WNDCLASS wc{};
    wc.lpfnWndProc = WindowProc;
    wc.hInstance = instance;
    wc.lpszClassName = L"ShowerSimCUDAWindowClass";
    wc.style = CS_OWNDC;

    if (!RegisterClass(&wc)) {
        throw std::runtime_error("Failed to register window class");
    }

    g_hwnd = CreateWindowEx(
        0,
        wc.lpszClassName,
        L"ShowerSimCUDA - Visualization",
        WS_OVERLAPPEDWINDOW | WS_VISIBLE,
        CW_USEDEFAULT, CW_USEDEFAULT,
        g_windowWidth, g_windowHeight,
        nullptr, nullptr, instance, nullptr);

    if (!g_hwnd) {
        throw std::runtime_error("Failed to create window");
    }

    g_hdc = GetDC(g_hwnd);

    PIXELFORMATDESCRIPTOR pfd{};
    pfd.nSize = sizeof(PIXELFORMATDESCRIPTOR);
    pfd.nVersion = 1;
    pfd.dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER;
    pfd.iPixelType = PFD_TYPE_RGBA;
    pfd.cColorBits = 24;
    pfd.cDepthBits = 24;
    pfd.iLayerType = PFD_MAIN_PLANE;

    int pixelFormat = ChoosePixelFormat(g_hdc, &pfd);
    if (pixelFormat == 0) throw std::runtime_error("ChoosePixelFormat failed");
    if (!SetPixelFormat(g_hdc, pixelFormat, &pfd)) throw std::runtime_error("SetPixelFormat failed");

    g_glrc = wglCreateContext(g_hdc);
    if (!g_glrc) throw std::runtime_error("wglCreateContext failed");
    if (!wglMakeCurrent(g_hdc, g_glrc)) throw std::runtime_error("wglMakeCurrent failed");

    glDisable(GL_DEPTH_TEST);
    glPointSize(2.0f);
}

void shutdown_opengl_window() {
    if (g_glrc) {
        wglMakeCurrent(nullptr, nullptr);
        wglDeleteContext(g_glrc);
        g_glrc = nullptr;
    }
    if (g_hdc && g_hwnd) {
        ReleaseDC(g_hwnd, g_hdc);
        g_hdc = nullptr;
    }
    if (g_hwnd) {
        DestroyWindow(g_hwnd);
        g_hwnd = nullptr;
    }
}

void render_frame_opengl(DeviceParticles& d, const SimulationConfig& cfg,
                         float* hostX, float* hostY, float* hostZ,
                         float* hostMass, float* hostTemp) {
    CUDA_CHECK(cudaMemcpy(hostX, d.x, cfg.particleCount * sizeof(float), cudaMemcpyDeviceToHost));
    CUDA_CHECK(cudaMemcpy(hostY, d.y, cfg.particleCount * sizeof(float), cudaMemcpyDeviceToHost));
    CUDA_CHECK(cudaMemcpy(hostZ, d.z, cfg.particleCount * sizeof(float), cudaMemcpyDeviceToHost));
    CUDA_CHECK(cudaMemcpy(hostMass, d.mass, cfg.particleCount * sizeof(float), cudaMemcpyDeviceToHost));
    CUDA_CHECK(cudaMemcpy(hostTemp, d.temperature, cfg.particleCount * sizeof(float), cudaMemcpyDeviceToHost));

    glViewport(0, 0, g_windowWidth, g_windowHeight);
    glClearColor(0.05f, 0.05f, 0.07f, 1.0f);
    glClear(GL_COLOR_BUFFER_BIT);

    glMatrixMode(GL_PROJECTION);
    glLoadIdentity();
    glOrtho(0.0, WIDTH, 0.0, HEIGHT, -1.0, 1.0);

    glMatrixMode(GL_MODELVIEW);
    glLoadIdentity();

    for (int i = 0; i < cfg.particleCount; i += g_sampleRate) {
        float r, g, b;
        if (g_colorMode == COLOR_THERMAL) temperature_to_rgb(hostTemp[i], r, g, b);
        else mass_to_rgb(hostMass[i], r, g, b);

        float pointSize = 2.0f + 1.5f * sqrtf(fmaxf(hostMass[i], 1.0f));
        pointSize = fminf(pointSize, 12.0f);

        glPointSize(pointSize);
        glBegin(GL_POINTS);
        glColor3f(r, g, b);
        glVertex2f(hostX[i], hostY[i]);
        glEnd();
    }

    SwapBuffers(g_hdc);
}

FrameStats simulate_frame(DeviceParticles& p, const SimulationConfig& cfg, int frame, float dtScale) {
    FrameStats stats{};
    int particleBlocks = (cfg.particleCount + cfg.blockSize - 1) / cfg.blockSize;
    int gridBlocks = (GRID_CELLS + cfg.blockSize - 1) / cfg.blockSize;

    cudaEvent_t frameStart, frameStop, start, stop;
    CUDA_CHECK(cudaEventCreate(&frameStart));
    CUDA_CHECK(cudaEventCreate(&frameStop));
    CUDA_CHECK(cudaEventCreate(&start));
    CUDA_CHECK(cudaEventCreate(&stop));

    CUDA_CHECK(cudaEventRecord(frameStart));

    CUDA_CHECK(cudaEventRecord(start));
    physics_kernel<<<particleBlocks, cfg.blockSize>>>(p, cfg.particleCount, cfg.dt * dtScale, cfg.initialSpeed, 1234u + frame * 9781u);
    launch_and_time(start, stop);
    stats.physicsMs = elapsed_ms(start, stop);

    CUDA_CHECK(cudaEventRecord(start));
    clear_grid_kernel<<<gridBlocks, cfg.blockSize>>>(p.cellHeads, GRID_CELLS);
    reset_collision_buffers_kernel<<<particleBlocks, cfg.blockSize>>>(p, cfg.particleCount);
    build_grid_kernel<<<particleBlocks, cfg.blockSize>>>(p, cfg.particleCount);
    find_collisions_kernel<<<particleBlocks, cfg.blockSize>>>(p, cfg.particleCount);
    apply_merges_kernel<<<particleBlocks, cfg.blockSize>>>(p, cfg.particleCount);
    normalise_merged_particles_kernel<<<particleBlocks, cfg.blockSize>>>(p, cfg.particleCount);
    respawn_inactive_after_merge_kernel<<<particleBlocks, cfg.blockSize>>>(p, cfg.particleCount, 99173u + frame * 2654435761u, cfg.initialSpeed);
    launch_and_time(start, stop);
    stats.collisionMs = elapsed_ms(start, stop);

    CUDA_CHECK(cudaEventRecord(start));
    thermal_kernel<<<particleBlocks, cfg.blockSize>>>(p, cfg.particleCount, cfg.dt * dtScale, cfg.coolingFactor);
    launch_and_time(start, stop);
    stats.thermalMs = elapsed_ms(start, stop);

    CUDA_CHECK(cudaEventRecord(frameStop));
    CUDA_CHECK(cudaEventSynchronize(frameStop));
    stats.totalMs = elapsed_ms(frameStart, frameStop);

    CUDA_CHECK(cudaMemcpy(&stats.floorHits, p.floorHits, sizeof(unsigned long long), cudaMemcpyDeviceToHost));
    CUDA_CHECK(cudaMemcpy(&stats.merges, p.merges, sizeof(unsigned long long), cudaMemcpyDeviceToHost));

    CUDA_CHECK(cudaEventDestroy(frameStart));
    CUDA_CHECK(cudaEventDestroy(frameStop));
    CUDA_CHECK(cudaEventDestroy(start));
    CUDA_CHECK(cudaEventDestroy(stop));

    return stats;
}

int main(int argc, char** argv) {
    try {
        SimulationConfig cfg = parse_args(argc, argv);
        g_runtimeCoolingFactor = cfg.coolingFactor;
        g_runtimeInitialSpeed = cfg.initialSpeed;

        int device = 0;
        CUDA_CHECK(cudaSetDevice(device));
        cudaDeviceProp prop{};
        CUDA_CHECK(cudaGetDeviceProperties(&prop, device));

        std::cout << "--- CUDA Shower Simulation (visual) ---\n";
        std::cout << "GPU: " << prop.name << "\n";
        std::cout << "Particles: " << cfg.particleCount << "\n";
        std::cout << "Frames: unlimited (press Q to quit)\n";
        std::cout << "Block size: " << cfg.blockSize << "\n";
        std::cout << "Wall mode: Sticky\n";

        DeviceParticles particles{};
        allocate_particles(particles, cfg.particleCount);

        int particleBlocks = (cfg.particleCount + cfg.blockSize - 1) / cfg.blockSize;
        initialise_particles_kernel<<<particleBlocks, cfg.blockSize>>>(particles, cfg.particleCount, 42u, cfg.initialSpeed);
        CUDA_CHECK(cudaGetLastError());
        CUDA_CHECK(cudaDeviceSynchronize());

        init_opengl_window(GetModuleHandle(nullptr));

        float* hostX = nullptr;
        float* hostY = nullptr;
        float* hostZ = nullptr;
        float* hostMass = nullptr;
        float* hostTemp = nullptr;
        CUDA_CHECK(cudaHostAlloc(&hostX, cfg.particleCount * sizeof(float), cudaHostAllocDefault));
        CUDA_CHECK(cudaHostAlloc(&hostY, cfg.particleCount * sizeof(float), cudaHostAllocDefault));
        CUDA_CHECK(cudaHostAlloc(&hostZ, cfg.particleCount * sizeof(float), cudaHostAllocDefault));
        CUDA_CHECK(cudaHostAlloc(&hostMass, cfg.particleCount * sizeof(float), cudaHostAllocDefault));
        CUDA_CHECK(cudaHostAlloc(&hostTemp, cfg.particleCount * sizeof(float), cudaHostAllocDefault));

        std::vector<FrameStats> frameStats;
        frameStats.reserve(std::max(1, cfg.frameCount));

        int frame = 0;
        while (g_running) {
            process_window_messages();

            SimulationConfig runtimeCfg = cfg;
            runtimeCfg.coolingFactor = g_runtimeCoolingFactor;
            runtimeCfg.initialSpeed = g_runtimeInitialSpeed;

            if (g_resetRequested) {
                initialise_particles_kernel<<<particleBlocks, cfg.blockSize>>>(particles, cfg.particleCount, 42u, runtimeCfg.initialSpeed);
                CUDA_CHECK(cudaGetLastError());
                CUDA_CHECK(cudaDeviceSynchronize());
                CUDA_CHECK(cudaMemset(particles.floorHits, 0, sizeof(unsigned long long)));
                CUDA_CHECK(cudaMemset(particles.merges, 0, sizeof(unsigned long long)));
                frameStats.clear();
                frame = 0;
                g_resetRequested = false;
            }

            if (!g_paused) {
                FrameStats stats = simulate_frame(particles, runtimeCfg, frame, g_dtScale);
                frameStats.push_back(stats);
                ++frame;

                if (frame % 60 == 0) {
                    std::cout << "frame " << std::setw(5) << frame
                              << ": total=" << std::setw(8) << std::fixed << std::setprecision(3) << stats.totalMs << "ms"
                              << " physics=" << std::setw(8) << stats.physicsMs << "ms"
                              << " collision=" << std::setw(8) << stats.collisionMs << "ms"
                              << " thermal=" << std::setw(8) << stats.thermalMs << "ms"
                              << " particles=" << cfg.particleCount
                              << " merges=" << stats.merges
                              << " floor_hits=" << stats.floorHits
                              << "\n";
                }
            }

            render_frame_opengl(particles, runtimeCfg, hostX, hostY, hostZ, hostMass, hostTemp);
        }

        CUDA_CHECK(cudaFreeHost(hostX));
        CUDA_CHECK(cudaFreeHost(hostY));
        CUDA_CHECK(cudaFreeHost(hostZ));
        CUDA_CHECK(cudaFreeHost(hostMass));
        CUDA_CHECK(cudaFreeHost(hostTemp));

        shutdown_opengl_window();
        free_particles(particles);
        CUDA_CHECK(cudaDeviceReset());
        return 0;
    } catch (const std::exception& ex) {
        std::cerr << "Error: " << ex.what() << "\n";
        return 1;
    }
}
