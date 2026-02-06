# Keycloak Token Exchange (RFC 8693) - Important Notes

## The Scope Inheritance Problem

When performing token exchange in Keycloak, you **cannot add new scopes** that weren't in the original `subject_token`. This is a key principle of RFC 8693.

### Why Empty Tokens Occur

If your exchanged token has minimal claims like:
```json
{
  "exp": 1770376999,
  "iat": 1770376399,
  "jti": "...",
  "iss": "http://localhost:8080/realms/poc",
  "aud": "api",
  "typ": "Bearer",
  "azp": "api",
  "sid": "...",
  "scope": ""
}
```

This means the **original token** (`subject_token`) had no scopes or minimal claims.

## Solution Options

### Option 1: Ensure Original Token Has Scopes (Recommended)

Make sure the initial token request includes the scopes you need:

```http
POST /realms/poc/protocol/openid-connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password
&client_id=api
&client_secret=your-client-secret-here
&username=admin
&password=admin123
&scope=profile email
```

### Option 2: Configure Client Default Scopes in Keycloak

In Keycloak Admin Console:

1. Go to **Clients** ? Select your client (e.g., `api`)
2. Go to **Client scopes** tab
3. Add to **Default client scopes**:
   - `profile` - Includes name, username, preferred_username, given_name, family_name
   - `email` - Includes email, email_verified
   - `roles` - Includes realm_access and resource_access with user roles

Current configuration shows:
- `bff` client: Has `email` and `profile` as default scopes ?
- `api` client: Has NO default scopes ?

### Option 3: Use Token Exchange Mappers

Configure protocol mappers on the `bff` client to add claims during exchange, but this only works for certain claims and not for scopes from other clients.

## Token Exchange Rules

### ? What Token Exchange CAN Do:
- Change the audience (`aud` claim) to a different service
- Change the subject (`sub` claim) via impersonation (if permissions allow)
- Inherit all claims and scopes from the original token
- Reduce scopes (if supported by the authorization server)

### ? What Token Exchange CANNOT Do:
- Add new scopes that weren't in the original token
- Add user claims if they weren't in the original token
- Elevate privileges beyond what the original token had

## Testing Your Configuration

1. **Get initial token** and decode it at https://jwt.io
2. Check if it has:
   - `scope` claim with values like "profile email"
   - User claims: `preferred_username`, `email`, `name`, etc.
   - Role claims: `realm_access`, `resource_access`
3. **Perform token exchange** and decode the result
4. Verify the exchanged token has the same or fewer claims

## Common Issues

### Issue: "Invalid scopes: profile email"
**Cause**: Trying to add scopes during token exchange that weren't in the original token.
**Fix**: Remove the `scope` parameter from token exchange requests.

### Issue: Empty or minimal token claims
**Cause**: Original token had no scopes or was obtained via client_credentials without proper client scope configuration.
**Fix**: Add default client scopes to the client OR request scopes explicitly in the initial token request.

### Issue: Missing user information after exchange
**Cause**: The `api` client doesn't have `profile` and `email` in default scopes.
**Fix**: Configure the `api` client in Keycloak to include these scopes by default.

## Recommended Client Configuration

For all clients that will be used in token exchange workflows:

```json
{
  "clientId": "api",
  "defaultClientScopes": [
    "email",
    "profile",
    "roles"
  ],
  "optionalClientScopes": [
    "address",
    "phone",
    "offline_access"
  ]
}
```

## References

- [RFC 8693 - OAuth 2.0 Token Exchange](https://www.rfc-editor.org/rfc/rfc8693.html)
- [Keycloak Token Exchange Documentation](https://www.keycloak.org/docs/latest/securing_apps/#_token-exchange)
