# Fix: "Standard Token Exchange" Checkbox Not Enabled in Keycloak UI

## Problem
After running `docker-compose up`, the BFF client in Keycloak Admin UI shows "Standard Token Exchange" as **unchecked** even though `oauth2.token.exchange.grant.enabled` is set to `"true"` in the JSON configuration.

## Root Cause
Keycloak's UI requires **two things** for the "Standard Token Exchange" checkbox to show as enabled:

1. ? The attribute: `"oauth2.token.exchange.grant.enabled": "true"` (already present)
2. ? The flow properties: `oauth2DeviceAuthorizationGrantEnabled` and `oidcCibaGrantEnabled` (missing)

Without the flow properties, Keycloak's UI cannot determine which OAuth2 flows are enabled and defaults all checkboxes to OFF.

## Solution

### Manual Fix (Recommended)

Edit `.keycloak/realms/poc-realm.json` and find the BFF client section:

**BEFORE** (Current - Missing properties):
```json
{
  "clientId": "bff",
  "enabled": true,
  "clientAuthenticatorType": "client-secret",
  "secret": "your-client-secret-here",
  "publicClient": false,
  "protocol": "openid-connect",
  "redirectUris": [...],
  "webOrigins": [...],
  "standardFlowEnabled": true,
  "implicitFlowEnabled": false,
  "directAccessGrantsEnabled": true,
  "serviceAccountsEnabled": true,
  "authorizationServicesEnabled": false,
  "attributes": {
    "access.token.lifespan": "600",
    "client.secret.creation.time": "1234567890",
    "oauth2.token.exchange.grant.enabled": "true"
  },
  ...
}
```

**AFTER** (Fixed - Add these two lines):
```json
{
  "clientId": "bff",
  "enabled": true,
  "clientAuthenticatorType": "client-secret",
  "secret": "your-client-secret-here",
  "publicClient": false,
  "protocol": "openid-connect",
  "redirectUris": [...],
  "webOrigins": [...],
  "standardFlowEnabled": true,
  "implicitFlowEnabled": false,
  "directAccessGrantsEnabled": true,
  "serviceAccountsEnabled": true,
  "oauth2DeviceAuthorizationGrantEnabled": false,    // ? ADD THIS LINE
  "oidcCibaGrantEnabled": false,                      // ? ADD THIS LINE
  "authorizationServicesEnabled": false,
  "attributes": {
    "access.token.lifespan": "600",
    "client.secret.creation.time": "1234567890",
    "oauth2.token.exchange.grant.enabled": "true"
  },
  ...
}
```

**Key Points:**
- Add both lines **after** `serviceAccountsEnabled`
- Add them **before** `authorizationServicesEnabled`
- Use lowercase `false` (not `"false"` or `False`)
- Maintain the JSON structure and commas

### Automated Fix (PowerShell Script)

Run this PowerShell script:

```powershell
$jsonPath = "D:\github\poc\.keycloak\realms\poc-realm.json"
$json = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Find BFF client and add missing properties
foreach ($client in $json.clients) {
    if ($client.clientId -eq "bff") {
        $client | Add-Member -NotePropertyName "oauth2DeviceAuthorizationGrantEnabled" -NotePropertyValue $false -Force
        $client | Add-Member -NotePropertyName "oidcCibaGrantEnabled" -NotePropertyValue $false -Force
        break
    }
}

# Save
$json | ConvertTo-Json -Depth 100 | Set-Content $jsonPath -Encoding UTF8
Write-Host "? BFF client updated - Standard Token Exchange will now show as enabled in UI"
```

## Verification Steps

### 1. Apply the Fix
Edit `.keycloak/realms/poc-realm.json` as shown above.

### 2. Restart Keycloak
```bash
cd D:\github\poc
docker-compose down
docker-compose up -d
```

### 3. Check Keycloak Admin UI
1. Open: http://localhost:8080/admin
2. Navigate to: **Clients** ? **bff** ? **Capability config**
3. Verify: **"Standard Token Exchange"** checkbox should now be **? ON**

### 4. Test Token Exchange
Run the tests in `samples/Poc.Yarp/Test.Yarp.http`:
- ? Step 1: Get BFF token
- ? Step 3: Exchange to API token
- ? Step 4: Use exchanged token

## Understanding the Configuration

### OAuth2 Flow Properties

| Property | Purpose | BFF Value |
|----------|---------|-----------|
| `standardFlowEnabled` | Authorization Code Flow | `true` ? |
| `implicitFlowEnabled` | Implicit Flow (deprecated) | `false` ? |
| `directAccessGrantsEnabled` | Password Grant / Resource Owner | `true` ? |
| `serviceAccountsEnabled` | Client Credentials Grant | `true` ? |
| `oauth2DeviceAuthorizationGrantEnabled` | Device Authorization Grant | `false` ? |
| `oidcCibaGrantEnabled` | Client Initiated Backchannel Authentication | `false` ? |

### Token Exchange Attribute

The attribute `oauth2.token.exchange.grant.enabled` enables RFC 8693 Token Exchange:
```json
"attributes": {
  "oauth2.token.exchange.grant.enabled": "true"
}
```

This allows the BFF client to exchange tokens using:
```http
POST /realms/poc/protocol/openid-connect/token
grant_type=urn:ietf:params:oauth:grant-type:token-exchange
&client_id=bff
&subject_token={{bffToken}}
&audience=api
```

## Why This Happens

Keycloak's Admin UI uses **two sources** to determine checkbox states:

1. **Flow Properties** (top-level JSON properties)
   - Used to enable/disable specific OAuth2 flows
   - Control what the client **can** do

2. **Attributes** (nested in the `attributes` object)
   - Fine-grained configuration for enabled flows
   - Control **how** the client does it

When flow properties are missing, the UI **cannot determine** which flows are enabled, so it shows all as unchecked - even if the underlying functionality works!

## Related Files

- Configuration: `.keycloak/realms/poc-realm.json`
- Fix Script: `.keycloak/fix-token-exchange-ui.ps1`
- Test File: `samples/Poc.Yarp/Test.Yarp.http`
- Documentation:
  - `samples/Poc.Yarp/TOKEN-EXCHANGE-FIX.md`
  - `samples/Poc.Yarp/SOLUTION-SUMMARY.md`

## References

- [Keycloak Client Configuration](https://www.keycloak.org/docs/latest/server_admin/#_clients)
- [RFC 8693 - OAuth 2.0 Token Exchange](https://www.rfc-editor.org/rfc/rfc8693.html)
- [Keycloak Token Exchange Documentation](https://www.keycloak.org/docs/latest/securing_apps/#_token-exchange)

---

**Status**: ?? Requires manual edit to `poc-realm.json`  
**Impact**: UI only - token exchange functionality works regardless  
**Priority**: Low (cosmetic issue, does not affect functionality)
