# Buildings

---

## Overview

The Buildings module is the main part of the system.  
It allows municipal employees to search, view, update, and manage abandoned buildings.  
Access to editing and deleting records depends on the user’s role.

Users with the **Viewer** role may only view building information,  
while **Editors** and **Admins** are allowed to update or remove records.

---

## Features

### **Search & Filtering**
Users can:
- Search buildings by address, city, or keywords  
- Filter by status (Abandoned, UnderInspection, Demolished, Occupied, Unknown)  
- Sort results by creation date or last update

This helps employees quickly find the relevant abandoned building they need to handle.

---

### **Building List**
The main buildings page displays:
- Address  
- City  
- Status  
- Last updated date  
- Assigned inspector (optional)  

Clicking a building opens the detailed view.

---

## Building Details

The building profile page includes:

- **Full address**  
- **Coordinates (if available)**  
- **Current status**  
- **Description / notes**  
- **Last updated by**  
- **History log shortcut**

If the user has permission, they will also see **Edit** and **Delete** buttons.

---

## Editing Buildings

Users with **Editor** or **Admin** roles can update:

- Status  
- Description / notes  
- Address fields  
- Any additional attributes defined in the model  

Every change is:
- Saved in the database  
- Logged using the Audit system  
- Visible in the Logs page

---

## Adding New Buildings

Only **Admins** and **Editors** may create new buildings.  
The “Add Building” form includes fields for address, status, and optional notes.

Once saved:
- The building becomes immediately searchable  
- A log entry is created for tracking  

---

## Deleting Buildings

Only **Admins** can delete buildings.  
This action:
- Removes the building from listings  
- Writes an audit log entry  
- Updates the “last actions” section in logs

Deletion requires confirmation to prevent mistakes.

---

## Building Statuses

Each building has a status defined in the backend:

- **Unknown**  
- **Abandoned**  
- **UnderInspection**  
- **Demolished**  
- **Occupied**

Statuses are displayed with clear labels to help employees understand the building situation quickly.

---

## Logs Integration

Every building action is linked to the Logs module:

- Creation  
- Updates  
- Deletion  
- External sync changes  

From the building details page, users can open the full history of that building.

---

## Permissions Summary

| Role | View | Edit | Add | Delete |
|------|------|------|------|--------|
| Viewer | ✔ | ✖ | ✖ | ✖ |
| Editor | ✔ | ✔ | ✔ | ✖ |
| Admin | ✔ | ✔ | ✔ | ✔ |

---

## Conclusion

The Buildings module centralizes all abandoned-building information,  
providing a clear workflow for municipal employees and ensuring every update is tracked through logs.

---
