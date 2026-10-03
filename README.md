# 🛡️ FreightGuard

**Advanced Logistics Constraint Engine & Intelligent Fleet Management**

FreightGuard is a high-performance logistics solution designed to solve critical bottlenecks in road freight transportation: **speculative overbooking** and **inefficient cargo allocation**. 

By acting as a real-time validation gatekeeper, the system prevents carriers from double-booking vehicles across conflicting routes and optimizes load distribution using advanced heuristics.

## 🎯 The Problem
In third-party logistics (3PL) operations, carriers often "reserve" multiple loads for a single vehicle plate without actual capacity, leading to last-minute cancellations, missed loading windows, and expensive spot-market auctions to recover service levels (SLA). tesring.

## 🚀 Key Features

* **Anti-Overbooking Engine:** A robust validation layer that uses PostgreSQL pessimistic locking to prevent a single vehicle plate from being allocated to multiple conflicting loads simultaneously.
* **SLA & ETA Validation:** Real-time logistics feasibility checks. The system calculates the **Estimated Time of Arrival (ETA)** by crossing the vehicle's current position (Telemetria Mock) with target routes using the **OpenRouteService API**.
* **Volume & Weight Optimization:** Implements a custom **Knapsack-based heuristic** in C# to ensure cargo consolidation (Continuous Move) respects the physical limits (m³ and kg) of the vehicle's body.
* **Dynamic Attribute Matching:** Leverages PostgreSQL **JSONB** to store and validate specific requirements (e.g., Refrigeration, Hazmat, Armored Transport) without the overhead of complex relational joins.
* **Carrier Auction System:** Allows shippers to offer loads to a pre-approved base of carriers, managing bids and automated selection based on the lowest cost and confirmed availability.

## 🛠️ Tech Stack

* **Frontend:** React (Vite) + Tailwind CSS (Hosted on Azure Static Web Apps)
* **Backend:** .NET 8 Web API + Entity Framework Core (Hosted on Azure App Service)
* **Database:** PostgreSQL (Hosted on Neon.tech)
* **Routing & Geo:** OpenRouteService API + OpenStreetMap
* **Infrastructure:** Docker & GitHub Actions (CI/CD)

## 🏗️ System Architecture

The architecture is designed to be cloud-native and cost-effective, utilizing serverless components and managed services.

```mermaid
graph TD
    subgraph "Cloud Infrastructure (Azure / External)"
        A[Azure Static Web Apps<br/>React Frontend] -- "HTTPS" --> B
        
        subgraph "Application Layer"
            B[Azure App Service<br/>.NET 8 API] -- "EF Core" --> C[(Neon.tech<br/>PostgreSQL)]
            B -- "Internal Logic" --> D[Telemetria Mock Service]
        end
        
        subgraph "External Services"
            B -- "REST API" --> E[OpenRouteService API]
        end
    end

    User((Shipper / Carrier)) -- "UI Interaction" --> A
