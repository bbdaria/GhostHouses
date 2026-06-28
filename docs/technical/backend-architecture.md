# Backend Architecture Overview

This document explains how the backend of GhostHouses is structured and how the main layers interact.  
The goal is to make the codebase easy to understand and extend.

---

## 1. Controllers
Located in: `backend/Controllers/`

Controllers expose the API endpoints.  
They receive HTTP requests, validate basic input, and forward the real work to the Services layer.

Controllers **should not contain business logic**.  
Their job is only:
- Receive the request
- Call the correct service method
- Return the response

---

## 2. Services
Located in: `backend/Services/`

Services implement the application’s business logic.

Each service:
- Receives data from a controller
- Applies rules and validation
- Interacts with the database using the Data layer
- Returns a processed result

This is the “brain” of the system.

---

## 3. Data Layer
Located in: `backend/Data/`

Contains:
- `ApplicationDbContext` (EF Core context)
- Migrations
- Database configuration

The Data layer is responsible for:
- Mapping models to database tables
- Performing queries (through EF Core)
- Managing transactions

---

## 4. Models
Located in: `backend/Models/`

Models represent the database entities.  
Examples:
- `User`
- `Building`
- `Report`
- `Favorites`
- etc.

These classes define:
- Database fields
- Relationships (1-to-many, many-to-many)
- Validation rules (when needed)

---

## 5. Utilities
Located in: `backend/Utilities/`

Contains helper classes shared across the backend, such as:
- Token generation
- Password hashing
- Validation helpers
- Common reusable logic

---

## 6. Request Flow Summary
A typical request goes through these steps:

1. **Controller** receives request  
2. Controller calls a **Service**  
3. Service interacts with **DbContext**  
4. DbContext loads or writes **Models**  
5. Service returns a result  
6. Controller returns a response to the client

---

## 7. Benefits of This Architecture
- Clear separation of concerns  
- Easy to test each layer  
- Easy to add new features  
- Clean and maintainable long-term  
