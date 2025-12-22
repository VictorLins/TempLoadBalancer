# 🌐 DotNet-TCP-Balancer

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A high-performance, asynchronous **Layer 4 TCP Load Balancer** built with .NET 8. This solution efficiently distributes network traffic across multiple backend servers to ensure high availability and resource optimization.

## 📖 Table of Contents
* [Features](#-features)
* [Project Architecture](#️-project-architecture--structure)
* [Reactive Configuration](#-reactive-configuration--high-availability)
* [Getting Started](#-getting-started)
* [Configuration](#️-configuration)
* [Observability](#-observability--monitoring)
* [Testing & Simulation](#-testing--simulation)
* [Usage](#-usage)
* [License](#-license)

---

## ✨ Features

* **⚖️ Layer 4 Balancing**: Efficiently routes raw TCP traffic based on IP and port.
* **🔄 Multiple Strategies**: Supports **Round Robin**, **Least Connections**, and **Random** distribution algorithms.
* **🏥 Health Monitoring**: Proactively checks backend status and automatically removes unresponsive servers.
* **⚡ High Performance**: Fully asynchronous architecture using `System.Net.Sockets`.
* **📊 Live Status**: Exports real-time health and connection statistics to a JSON file.
* **🧪 Fully Tested**: Includes a comprehensive XUnit test suite for core logic and edge cases.

---

## 🏗️ Project Architecture & Structure

<p align="center">
  <img src="https://github.com/user-attachments/assets/27e59b1d-9419-4a36-b933-a8f97c55ac8f" alt="TcpLoadBalancer Architecture" width="850">
</p>

<details>
<summary><b>🔍 How it Works (Technical Deep Dive)</b></summary>

1. **Initialization**: The system loads its configuration from `appsettings.json`, defining the `ListenEndpoint` and the list of available `Backends`.
2. **Traffic Handling**: When a **Client** connects, the **Manager** evaluates the active connection counts and the selected **Strategy** to choose the best available server.
3. **Active Proxying**: Once a backend is selected, the engine establishes a bi-directional asynchronous stream, tunneling raw TCP traffic between the client and the server.
4. **Health Monitoring**: In the background, the **Health Monitor** pings each backend at a set interval. If a server goes down, it is flagged as "Unhealthy" and temporarily removed from the rotation.
5. **Observability**: All real-time data is periodically exported to `status.json`.

### 📂 Solution Projects
* **`TcpLoadBalancer`**: The heart of the application. Handles `System.Net.Sockets` logic and thread-safe state management.
* **`TcpLoadBalancer.Tests`**: A robust test suite using **XUnit** for strategy logic and failover simulations.
</details>

---

## ⚡ Reactive Configuration & High Availability

This load balancer leverages the **.NET Options Pattern** with `IOptionsMonitor<T>` to provide **Reactive Configuration Management**.

<details>
<summary><b>🟢 Dynamic Adjustments (Zero Downtime)</b></summary>

Changes in `appsettings.json` are detected via file-system watchers and injected into the running pipeline:
* **Elastic Backend Scaling**: You can scale the backend pool horizontally by adding or removing endpoints live.
* **Health Check Tuning**: Adjust `HealthCheckIntervalSeconds` on-the-fly to tune detection speed.
* **Idle Timeout Management**: Update `DefaultIdleTimeSeconds` at any time to optimize resource reclamation.
</details>

<details>
<summary><b>🔴 Architectural Constraints (Static Parameters)</b></summary>

To maintain system state integrity, certain parameters require a graceful restart:
* **Listen Endpoint**: Requires a **socket rebinding** operation at the OS level.
* **Balancing Strategy**: Locked upon initialization to prevent **"routing drift"** and ensure consistent session logic.
</details>

---

## 🚀 Getting Started

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Installation
```bash
git clone [https://github.com/yourusername/TcpLoadBalancer.git](https://github.com/yourusername/TcpLoadBalancer.git)
cd TcpLoadBalancer
dotnet restore
dotnet build
```
---

## ⚙️ Configuration

| Setting | Type | Reactive? | Description |
| :--- | :--- | :--- | :--- |
| **`Strategy`** | String | ❌ No | Algorithm: `RoundRobin`, `Random`, or `LeastConnections`. |
| **`ListenEndpoint`** | String | ❌ No | The IP/Port the load balancer listens on. |
| **`StatusFilePath`** | String | ❌ No | Path where the live status JSON is exported. |
| **`Backends`** | Array | ✅ Yes | List of backend objects (`Host` and `Port`). |
| **`HealthCheckIntervalSeconds`** | Int | ✅ Yes | Frequency of backend health verification. |
| **`DefaultIdleTimeoutSeconds`** | Int | ✅ Yes | Inactivity timeout before closing a connection. |

<details>
<summary><b>📄 View Example appsettings.json</b></summary>

```json
{
    "LoadBalancer": {
        "Strategy": "RoundRobin",
        "ListenEndpoint": "0.0.0.0:9000",
        "Backends": [
            { "Host": "127.0.0.1", "Port": 9101 },
            { "Host": "127.0.0.1", "Port": 9102 }
        ],
        "HealthCheckIntervalSeconds": 10,
        "DefaultIdleTimeoutSeconds": 600,
        "StatusFilePath": "status/loadbalancer-status.json"
    },
    "Serilog": {
        "MinimumLevel": "Debug",
        "WriteTo": [
            { "Name": "Console" },
            {
                "Name": "File",
                "Args": {
                    "path": "logs/loadbalancer-.log",
                    "rollingInterval": "Day"
                }
            }
        ]
    }
}
```
</details>
---

## 📊 Observability & Monitoring

The system maintains a real-time registry of backend states to ensure high availability and transparent traffic management.

<details>
<summary><b>👁️ View Sample Status Output (status.json)</b></summary>

The load balancer periodically updates this JSON file with the current health and traffic metrics for each backend.

```json
{
  "timestampUtc": "2025-12-22T23:16:19.0327807Z",
  "activeConnections": 16,
  "backends": [
    {
      "endpoint": "127.0.0.1:9101",
      "healthy": true,
      "activeConnections": 8
    },
    {
      "endpoint": "127.0.0.1:9102",
      "healthy": true,
      "activeConnections": 8
    }
  ]
}
```
</details>

## 🧪 Testing & Simulation

To verify the load balancer locally, you can use **Nmap (ncat)** to simulate backend servers and client traffic. This allows you to observe real-time routing and failover behavior.

<details>
<summary><b>🛠️ Step-by-Step Simulation Guide</b></summary>

### 1. Start Mock Backend Servers
Open two separate terminals and run the following commands to start listeners on different ports. These will act as your backend destinations:
```bash
# Terminal 1 (Backend A)
ncat -lk 9101

# Terminal 2 (Backend B)
ncat -lk 9102
```


### 2. Configure and Run the Load Balancer
Ensure your appsettings.json or configuration includes these backends, then start the project:
```bash
dotnet run --project TcpLoadBalancer
```
### 3. Simulate Client Traffic
Open a third terminal to simulate a client connecting to the Load Balancer (listening on port 9090). Any text you type here will be routed to one of your backends:
```bash
ncat 127.0.0.1 9090
```
</details>

## 💻 Usage

### Running the Load Balancer
To launch the application and start balancing traffic:
```bash
dotnet run --project TcpLoadBalancer
````

### Running the Tests
To execute the built-in XUnit tests:
```bash
# Standard test run
dotnet test

# Detailed test results (shows names of all 32 tests)
dotnet test --logger "console;verbosity=detailed"
````

## 📜 License
This project is licensed under the MIT License.
