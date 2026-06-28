# Logs

---

## Overview

The Logs module records every important action in the system.  
It provides full transparency and allows administrators to understand who changed what, and when.

Log entries are generated automatically by the backend through the AuditService.

---

## What Gets Logged

The system records events such as:

- Building created  
- Building updated  
- Building deleted  
- User role changed  
- Login attempts (optional)  
- External sync updates  
- Any significant backend action marked as auditable  

Each entry is stored with a timestamp and associated user (if available).

---

## Log List

The Logs page shows:
- Action type  
- Description of the event  
- Who performed the action  
- The date and time  
- Related building or user (when relevant)

Admins can filter logs to find activity related to a specific building or user.

---

## Log Details

Selecting a log entry reveals:
- Full description of the action  
- Old value and new value (when applicable)  
- User who performed the change  
- Link back to the related building or user  

This helps track how a building's information evolved over time.

---

## Integration With Buildings

Every time a building is modified:
- A new log entry is created  
- The entry includes what changed  
- Users can view a building’s history directly from its details page

This ensures accountability for field updates.

---

## External Sync Logs

When the external data synchronization worker updates the system:
- A snapshot is saved  
- Each building change is logged  
- Admins can review the sync history  

This allows the municipality to track differences between external data sources and the system’s local records.

---

## Permissions

Only **Admins** and **Editors** may access the Logs page.

- **Editors** can view logs  
- **Admins** can view full history, user-related logs, and sensitive entries  

Viewers do not have access to system logs.

---

## Log Format

A typical log entry includes:
- **Action** (e.g., “Updated Building Status”)  
- **Performed by** (user email or system)  
- **Date**  
- **Object type** (Building / User / System)  
- **Additional details**

---

## Conclusion

The Logs module ensures every update, deletion, and external sync is permanently tracked.  
This supports accountability, transparency, and safer municipal operations.

---
