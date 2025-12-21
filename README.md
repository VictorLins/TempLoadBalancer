# TCP Load Balancer (L4) – .NET Study Case

## Overview

This repository contains a lightweight **TCP Layer-4 Load Balancer** implemented in **C# and .NET**.

The project is intentionally scoped as a **technical study case**, demonstrating how scalability challenges were historically addressed before modern cloud-native tooling (managed load balancers, containers, service meshes).

The goal is **not** to build a production-grade load balancer, but to demonstrate sound engineering decisions, TCP fundamentals, and clean architecture suitable for a senior-level take-home exercise.

---

## Why This Project Exists

In the late 1990s and early 2000s, scalability was often achieved by:

- Accepting incoming TCP connections
- Routing traffic to backend servers
- Tracking health and connection counts
- Forwarding raw TCP streams efficiently

This project recreates that approach using modern .NET while staying faithful to the constraints and mindset of that era.

---

## Key Characteristics

- Layer-4 (TCP) load balancing
- Round-robin backend selection
- Health-aware routing
- Active connection tracking per backend
- Correct handling of half-closed TCP connections
- Graceful shutdown via cancellation tokens
- Structured logging with Serilog
- Optional status file for lightweight observability

---

## Solution Structure

TcpLoadBalancerSolution
├── TcpLoadBalancer
│ ├── Configuration
│ │ └── LoadBalancerOptions.cs
│ ├── Models
│ │ └── BackendStatus.cs
│ ├── Networking
│ │ ├── TcpListenerService.cs
│ │ └── ConnectionHandler.cs
│ ├── Services
│ │ ├── BackendSelector.cs
│ │ └── StatusWriter.cs
│ ├── appsettings.json
│ └── Program.cs
│
└── TcpLoadBalancer.Tests
└── BackendSelectorTests.cs


---

## Project Types

### TcpLoadBalancer

- **Type:** .NET Console Application
- **Purpose:** Acts as a TCP proxy and load balancer.

**Responsibilities:**
- Listen for incoming TCP connections
- Select a healthy backend endpoint
- Forward traffic bidirectionally
- Track active connections
- Handle graceful shutdown

---

### TcpLoadBalancer.Tests

- **Type:** xUnit Test Project
- **Purpose:** Validate deterministic logic.

**What is tested:**
- Backend selection logic
- Round-robin behaviour
- State transitions

Low-level TCP and OS socket behaviour are intentionally excluded.

---

## Configuration

All runtime configuration is externalised via `appsettings.json`.

### Example

```json
{
  "LoadBalancer": {
    "ListenEndpoint": "127.0.0.1:9000",
    "Backends": [
      "127.0.0.1:9101",
      "127.0.0.1:9102"
    ],
    "StatusFilePath": "status.json"
  }
}
