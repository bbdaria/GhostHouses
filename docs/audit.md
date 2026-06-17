# Audit System

## Overview
The audit system records important changes made in the application, allowing the municipality to track who modified data, when the modification occurred, and what was changed.  
Every critical operation—such as creating, editing, or deleting buildings or users—is logged automatically.

---

## Components

### **AuditService**
Handles creation of audit log entries whenever an action needs to be recorded.

**Responsibilities**
- Create new `AuditEntry` records.
- Capture user identity, action type, and timestamp.
- Save audit data through the database context.
- Provide a centralized logging mechanism for all modules.

---

### **AuditEntry**
Represents a single audit log entry stored in the database.

**Fields**
- **Id** – unique identifier of the audit entry.  
- **UserId** – user who performed the action.  
- **Action** – description of what occurred.  
- **TargetId** – optional ID of the entity affected (e.g., BuildingId).  
- **Timestamp** – when the action occurred.  
- **Details** – optional additional information.

Audit entries form a historical record of system activity.

---

## When Auditing Occurs

The system generates audit logs during:
- Building creation  
- Building updates  
- Building deletion  
- User creation or role changes  
- External synchronization updates  
- Any administrative action requiring traceability  

Each log contains enough information to reconstruct what happened and who initiated it.

---

## Storage

All audit entries are stored in the database using the `AppDbContext`.  
Entries are appended and never modified, preserving a reliable, tamper-resistant history.

---

## Example Audit Flow

1. An Admin updates a building’s status.  
2. Controller calls `AuditService.LogAsync(...)`.  
3. AuditService creates a new `AuditEntry`.  
4. Entry is written to the database.  
5. Logs can later be viewed by authorized roles.

---

## Viewing Audit Logs

Audit data is exposed through the `/api/logs` endpoints, allowing Admins to:
- View all logs  
- Filter logs by user  
- Filter logs by target building  
- Sort chronologically  

This provides full visibility over system activity.

---

## Summary
The audit system ensures transparency and accountability.  
By recording every significant action, it provides a permanent activity history and supports internal oversight and debugging efforts.

