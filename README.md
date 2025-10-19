# Web Proxy

> **A distributed HTTP proxy service for mediating access to Data Warehouse nodes**

## 📋 Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Architecture](#architecture)
- [Technologies Used](#technologies-used)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [API Endpoints](#api-endpoints)

---

## 🎯 Overview

This project implements a **transparent HTTP proxy** that intermediates connections between clients and distributed Data Warehouse (DW) nodes. The proxy handles semi-structured data through standard HTTP protocols (GET, PUT, POST).

### Core Concept

**Clients should NOT know the real data source** → The proxy becomes the single point of access, abstracting the underlying distributed architecture.

---

## ✨ Key Features

### Proxy Implementation

- ✅ **HTTP Proxy Server** built with C# and ASP.NET Core
- ✅ **Middleware Pipeline** for request handling and routing
- ✅ **HTTP Methods Support**: GET, PUT, POST
- ✅ **Ocelot API Gateway** integration for advanced routing
- ✅ **Load Balancing** with Round-Robin strategy
- ✅ **Response Caching** for improved performance

### Distributed Architecture

- ✅ **Multiple Data Warehouse Nodes** simulating distributed data storage
- ✅ **RabbitMQ Message Broker** for database synchronization between APIs
- ✅ **MongoDB Replication** across multiple instances
- ✅ **Docker Containerization** for easy deployment
- ✅ **Health Checks** and automatic service recovery

### Data Format Support

- JSON endpoints for data retrieval and manipulation
- Semi-structured data handling
- Extensible format support

---

## 🏗️ Architecture

```
┌──────────┐
│  Client  │
└────┬─────┘
     │
     ▼
┌─────────────────┐
│  Proxy (Ocelot) │  ← Load Balancing + Caching
└────┬────────────┘
     │
     ├─────────────┬─────────────┐
     ▼             ▼             ▼
┌─────────┐   ┌─────────┐   ┌──────────┐
│ Web API │   │ Web API │   │ RabbitMQ │
│    #1   │   │    #2   │   │  Broker  │
└────┬────┘   └────┬────┘   └────┬─────┘
     │             │              │
     ▼             ▼              │
┌─────────┐   ┌─────────┐        │
│ MongoDB │   │ MongoDB │◄───────┘
│    #1   │   │    #2   │   (Sync)
└─────────┘   └─────────┘
```

### Components

1. **Proxy Layer (Ocelot)**

   - Routes incoming requests to backend services
   - Implements load balancing (Round-Robin)
   - Caches responses for frequently accessed data

2. **Web API Services**

   - Two identical API instances for high availability
   - Each connected to its own MongoDB instance
   - Processes HTTP requests (GET, PUT, POST)

3. **RabbitMQ Message Broker**

   - Synchronizes data between MongoDB instances
   - Fanout exchange pattern for broadcasting updates
   - Ensures eventual consistency across databases

4. **MongoDB Databases**
   - Separate database instances for each API
   - Stores employee and warehouse data
   - Synchronized via RabbitMQ messaging

---

## 🛠️ Technologies Used

| Technology         | Purpose                          |
| ------------------ | -------------------------------- |
| **C# / .NET 8**    | Core application framework       |
| **ASP.NET Core**   | Web API and Middleware           |
| **Ocelot**         | API Gateway and proxy routing    |
| **RabbitMQ**       | Message broker for database sync |
| **MongoDB**        | NoSQL database for data storage  |
| **Docker**         | Containerization platform        |
| **Docker Compose** | Multi-container orchestration    |

---

## 📁 Project Structure

```
smart_proxy_app/
├── Server/
│   ├── Controllers/
│   ├── Models/
│   ├── Services/
│   ├── Middleware/
│   └── Dockerfile
├── Proxy/
│   ├── ocelot.json
│   └── Configuration/
├── docker-compose.yml
├── .env
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop) (v20.10+)
- [Docker Compose](https://docs.docker.com/compose/) (v2.0+)
- .NET 8 SDK (for local development)

### Installation

1. **Clone the repository**

   ```bash
   git clone <repository-url>
   cd smart_proxy_app
   ```

2. **Configure environment variables**

   Create a `.env` file in the project root:

   ```env
   RABBITMQ_USER=admin
   RABBITMQ_PASS=your_secure_password
   ```

3. **Build and start services**

   ```
   docker-compose up --build
   ```

4. **Verify services are running**

   The following services will be available:

   - Web API #1: `http://localhost:8080`
   - Web API #2: `http://localhost:8081`
   - RabbitMQ Management: `http://localhost:15672`
   - MongoDB #1: `localhost:27017`
   - MongoDB #2: `localhost:27018`

### Health Checks

RabbitMQ includes automatic health monitoring:

- **Interval**: 5 seconds
- **Timeout**: 10 seconds
- **Retries**: 10 attempts
- **Start Period**: 30 seconds

APIs will wait for RabbitMQ to be healthy before starting.

---

## ⚙️ Configuration

### RabbitMQ Configuration

Both Web APIs are configured with identical RabbitMQ settings:

```yaml
Exchange:
  Name: WebApiExchange
  Type: fanout
  Durable: true
  AutoDelete: false

Queue:
  API #1: WebApiQueue1
  API #2: WebApiQueue2
  Durable: true
  Exclusive: false
  AutoDelete: false
```

### MongoDB Configuration

Each API instance connects to its own MongoDB:

- **API #1**: `mongo1:27017/EmployeeDb1`
- **API #2**: `mongo2:27017/EmployeeDb2`

Data is synchronized between databases using RabbitMQ fanout exchange pattern.

### Ocelot Proxy Configuration

The proxy implements:

- **Load Balancing**: Round-Robin strategy across backend services
- **Caching**: Response caching for improved performance
- **Routing**: Dynamic route configuration for multiple endpoints

---

## 📡 API Endpoints

### Example Endpoints

| Method | Endpoint                    | Description                           |
| ------ | --------------------------- | ------------------------------------- |
| GET    | `/api/employee/all`         | Retrieve all employees in JSON format |
| GET    | `/api/employee/{id}`        | Retrieve a specific employee by ID    |
| POST   | `/api/employee/add`         | Create a new employee record          |
| PUT    | `/api/employee/update/{id}` | Update or create an employee (upsert) |
| DELETE | `/api/employee/delete/{id}` | Delete an employee record             |

### Response Caching

Frequently accessed endpoints are cached by the proxy to reduce backend load and improve response times.

---

## 🔄 Data Synchronization Flow

1. **Client** sends a POST/PUT request to the **Proxy**
2. **Proxy** routes the request to one of the Web APIs (Round-Robin)
3. **Web API** processes the request and updates its MongoDB
4. **Web API** publishes a message to **RabbitMQ** (fanout exchange)
5. **Both Web APIs** receive the message from their respective queues
6. **Second API** updates its MongoDB to maintain consistency
7. **Result**: Both databases are synchronized

---

## 🐳 Docker Services

### Service Overview

| Service    | Container Name | Ports       | Description                       |
| ---------- | -------------- | ----------- | --------------------------------- |
| RabbitMQ   | `rabbitmq`     | 5672, 15672 | Message broker with management UI |
| Web API #1 | `web_api_1`    | 8080        | First API instance                |
| Web API #2 | `web_api_2`    | 8081        | Second API instance               |
| MongoDB #1 | `mongo1`       | 27017       | First database instance           |
| MongoDB #2 | `mongo2`       | 27018       | Second database instance          |

### Persistent Volumes

Data is persisted across container restarts:

- `mongo_data1` & `mongo_data2`: Database files
- `mongo_config1` & `mongo_config2`: MongoDB configuration
- `rabbitmq_data`: RabbitMQ messages and configuration

---

## 🔮 Future Enhancements

- [ ] XML format support for data endpoints
- [ ] PUSH method implementation for real-time updates
- [ ] Advanced caching strategies (Redis integration)
- [ ] Authentication and authorization middleware

---

**Built with ❤️ using C#, Docker, and modern distributed systems architecture**
