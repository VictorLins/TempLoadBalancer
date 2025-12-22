# 🌐 DotNet-TCP-Balancer

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A high-performance, asynchronous **Layer 4 TCP Load Balancer** built with .NET 8. This solution efficiently distributes network traffic across multiple backend servers to ensure high availability and resource optimization.


## ✨ Features

* **⚖️ Layer 4 Balancing**: Efficiently routes raw TCP traffic based on IP and port.
* **🔄 Multiple Strategies**: Supports **Round Robin**, **Least Connections**, and **Random** distribution algorithms.
* **🏥 Health Monitoring**: Proactively checks backend status and automatically removes unresponsive servers from the pool.
* **⚡ High Performance**: Fully asynchronous architecture using `System.Net.Sockets` for low-latency proxying.
* **📊 Live Status**: Exports real-time health and connection statistics to a JSON file.
* **🛡️ Resilient**: Gracefully handles connection drops, backend failures, and timeouts.
* **🧪 Fully Tested**: Includes a comprehensive XUnit test suite for core logic and edge cases.

## 🏗️ Project Architecture & Structure

To understand how the load balancer operates, here is a high-level overview of the request flow and the system components:

<p align="center">
  <img src="https://github.com/user-attachments/assets/27e59b1d-9419-4a36-b933-a8f97c55ac8f" alt="TcpLoadBalancer Architecture" width="850">
</p>

### 🔄 How it Works
1.  **Initialization**: The system loads its configuration from `appsettings.json`, defining the `ListenEndpoint` and the list of available `Backends`.
2.  **Traffic Handling**: When a **Client** connects, the **Manager** evaluates the active connection counts and the selected **Strategy** (Round Robin, Least Connections, etc.) to choose the best available server.
3.  **Active Proxying**: Once a backend is selected, the engine establishes a bi-directional asynchronous stream, tunneling raw TCP traffic between the client and the server.
4.  **Health Monitoring**: In the background, the **Health Monitor** pings each backend at a set interval. If a server goes down, it is flagged as "Unhealthy" and temporarily removed from the rotation until it passes a check again.
5.  **Observability**: All real-time data, including current connection counts and health status, is periodically exported to `status.json`.

### 📂 Solution Projects
The codebase is split into two focused areas:
* **`TcpLoadBalancer`**: The heart of the application. It handles the low-level `System.Net.Sockets` logic, the thread-safe state management of backends, and the implementation of the balancing algorithms.
* **`TcpLoadBalancer.Tests`**: A robust test suite using **XUnit**. It includes unit tests for the strategy logic and integration tests that simulate backend failures to verify the auto-failover capabilities.

---

## 🚀 Getting Started

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Installation
1.  **Clone the repository**
    ```bash
    git clone [https://github.com/yourusername/TcpLoadBalancer.git](https://github.com/yourusername/TcpLoadBalancer.git)
    cd TcpLoadBalancer
    ```
2.  **Restore and Build**
    ```bash
    dotnet restore
    dotnet build
    ```

---

## ⚙️ Configuration

Configure the application via the `LoadBalancer` section in `appsettings.json`.

| Setting | Description | Example |
| :--- | :--- | :--- |
| **`Strategy`** | The algorithm used (`RoundRobin`, `Random`, or `LeastConnections`) | `Random` |
| **`ListenEndpoint`** | The IP and Port the load balancer listens on | `0.0.0.0:9000` |
| **`Backends`** | Array of backend objects (`Host` and `Port`) | See below |
| **`HealthCheckIntervalSeconds`** | Frequency of backend health verification | `10` |
| **`StatusFilePath`** | Path where the live status JSON is exported | `status/status.json` |

### Example `appsettings.json`
```json
{
  "LoadBalancer": {
    "Strategy": "Random",
    "ListenEndpoint": "0.0.0.0:9000",
    "Backends": [
      { "Host": "127.0.0.1", "Port": 9101 },
      { "Host": "127.0.0.1", "Port": 9102 }
    ],
    "HealthCheckIntervalSeconds": 10,
    "StatusFilePath": "status/loadbalancer-status.json"
  }
}
```

## 🧪 Testing & Simulation

To verify the load balancer in a local environment, you can use **Nmap (ncat)** to simulate backend servers and client traffic.

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

## 🛠️ Usage

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
