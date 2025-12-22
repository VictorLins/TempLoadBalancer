# 🌐 TcpLoadBalancer

A high-performance, asynchronous Layer 4 Load Balancer built with **.NET 8**. This solution efficiently distributes TCP traffic across multiple backend servers, ensuring high availability and fault tolerance for your network services. 🚀

## ✨ Features

* **⚖️ Layer 4 Load Balancing**: Efficiently routes raw TCP traffic based on IP and port.
* **🔄 Distribution Algorithms**: Supports **Round Robin** and **Least Connections** to optimize resource usage.
* **🏥 Health Monitoring**: Periodically checks backend status and automatically removes unresponsive servers from the pool.
* **⏱️ Configurable Timeouts**: Custom settings for connection establishment and idle sessions.
* **🛡️ Robust Error Handling**: Gracefully manages connection drops and backend failures.
* **🧪 Fully Tested**: Includes a comprehensive XUnit test suite covering core logic and edge cases.

## 🏗️ Project Structure

The solution is divided into two main projects:

* **`TcpLoadBalancer`**: The core engine containing the socket handling, balancing logic, and server management.
* **`TcpLoadBalancer.Tests`**: Detailed unit and integration tests to ensure system reliability.

## 🚀 Getting Started

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer.

### Installation

1. **Clone the repository**
   ```bash
   git clone [https://github.com/yourusername/TcpLoadBalancer.git](https://github.com/yourusername/TcpLoadBalancer.git)
   cd TcpLoadBalancer
