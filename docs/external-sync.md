# External Data Synchronization

## Overview
The system periodically synchronizes building data from an external governmental system.  
This synchronization ensures that the internal database always reflects the most up-to-date information about abandoned or hazardous buildings.

The synchronization process runs automatically in the background and updates local records without requiring manual intervention.

---

## Components

### **ExternalSyncWorker**
A hosted background service responsible for:

- Triggering synchronization at fixed intervals.
- Fetching building snapshots from the external system.
- Logging errors and failures.
- Delegating processing to `ExternalDataService`.

**Responsibilities**
- Runs on application startup.
- Executes sync every X minutes (configured in `appsettings.json`).
- Ensures only one sync runs at a time.
- Writes audit entries for each synchronization run.

---

### **ExternalDataService**
Handles all logic related to external building data.

**Responsibilities**
- Connecting to the external API / data source.
- Fetching building snapshots.
- Validating received data.
- Comparing snapshots to existing database records.
- Creating, updating, or marking buildings as inactive according to external changes.
- Recording synchronization status and errors.

---

## Data Model

### **ExternalSystemSnapshot**
Represents a single building record received from the external system.

**Fields**
- `ExternalId`: Unique identifier from the external source.  
- `Status`: Building status (active, demolished, etc.).  
- `Address`: Human-readable address.  
- `LastUpdated`: Timestamp of update by the external authority.

Snapshots are not stored permanently; they are used to update local `Building` records.

---

## Synchronization Flow

1. **Worker wakes up** based on the configured interval.  
2. **ExternalDataService fetches snapshots** from the external API.  
3. **For each snapshot**:
   - If the building does not exist → **create new building entry**.
   - If it exists → **update status, address, or metadata**.
   - If a building is missing from the new snapshot → **mark as inactive**.
4. **Write audit logs** documenting how many records were added/updated/disabled.
5. **Errors are logged** and do not stop the next scheduled sync.

---

## Error Handling

- All exceptions during sync are written to logs.
- Audit entries record:
  - Sync start and end time.
  - Number of successful updates.
  - Number of failures.
- The worker automatically retries at the next interval.

---

## Configuration

The sync interval is defined in `appsettings.json`:

```json
"ExternalSync": {
  "IntervalMinutes": 15,
  "ApiEndpoint": "https://example.gov.il/buildings"
}
