# Two-Factor Authentication (2FA)

## Overview
The system uses Two-Factor Authentication to add an additional security step during login.  
After entering their email and password, users must provide a temporary numeric code (OTP) that was sent to them.  
These codes are stored in memory, expire automatically, and must be validated before access is granted.

---

## TwoFactorService

### Responsibilities
- Generate 6-digit OTP codes.
- Store OTPs temporarily in an in-memory dictionary.
- Associate each OTP with a user ID and expiration time.
- Send OTP codes externally (e.g., email/SMS).
- Validate submitted OTPs.
- Remove expired codes during validation attempts.

---

## OTP Flow

1. User submits login credentials.  
2. The backend generates a 6-digit OTP for that user.  
3. The OTP is saved in memory with a 5-minute expiration.  
4. The code is sent externally.  
5. User enters the OTP into the verification page.  
6. The system checks:
   - Code exists  
   - Code matches  
   - Code has not expired  
7. If valid → login completes.  
8. If invalid → access is denied.

---

## Data Model

### Stored Fields
- **UserId** – identifies who the OTP belongs to.  
- **Code** – the generated 6-digit verification code.  
- **ExpiresAt** – the timestamp when the code becomes invalid.  

Codes are *not* stored in the database.

---

## Security Notes
- OTPs expire after 5 minutes.  
- OTPs are single-use.  
- Expired codes are automatically cleaned up.  
- No persistent storage is used.  
- Brute-force protection should be applied at a higher layer.

---

## Summary
`TwoFactorService` provides a simple and secure OTP mechanism using:
- In-memory temporary storage  
- Expiration handling  
- A straightforward validation flow  
- Integration hooks for external message delivery  

This ensures only verified users can complete the login process.

