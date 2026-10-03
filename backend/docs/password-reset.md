# Password reset

## Setup

Apply the additive `AddPasswordResetTokens` migration using the existing EF tooling:

```powershell
$RepoRoot = 'E:/QuizAppMockProject'
dotnet ef database update --project "$RepoRoot/backend/Quizapp/Quizapp.Infrastructure" --startup-project "$RepoRoot/backend/Quizapp/Quizapp.Api"
```

Use a verified development database for local setup. Deployment migrations must use the normal release process.
The migration adds four nullable columns to `Users`; existing accounts need no backfill.

Set the complete frontend reset-page URL in the API environment:

```powershell
$env:PasswordReset__FrontendUrl = 'http://localhost:4200/reset-password'
```

Production requires an absolute HTTPS URL without credentials, query parameters or a fragment.
HTTP loopback URLs are allowed only in Development. An empty URL leaves other API features available,
but reset requests return `503` until configuration is complete. URLs are never built from request headers.

Reuse the existing `Smtp` settings: `Host`, `Port`, `EnableSsl`, `UserName`, `Password`, and `From`.
Supply credentials through environment variables (`Smtp__Password`) or the API's user-secret store;
never commit credentials. Use TLS for real email delivery. Tests replace the sender and do not deliver real mail.

## Reverse proxies and ingress

The API processes `X-Forwarded-For` and `X-Forwarded-Proto` before HTTPS redirection and rate limiting.
Only loopback proxies are trusted by default. For a reverse proxy or ingress, configure its actual
peer IP address as seen by the API, or its dedicated proxy network in CIDR notation:

```powershell
# Example only: replace with the address of your trusted proxy.
$env:ForwardedHeaders__KnownProxies__0 = '10.0.0.10'
# Alternatively, trust a dedicated ingress network:
$env:ForwardedHeaders__KnownIPNetworks__0 = '10.0.0.0/24'
$env:ForwardedHeaders__ForwardLimit = '1'
```

Use only the proxy addresses or networks you control. Do not trust all networks or enable the
unrestricted `ASPNETCORE_FORWARDEDHEADERS_ENABLED` shortcut. The proxy must overwrite incoming
forwarded headers or append the verified connecting client address. For multiple proxy hops, list
every trusted hop and set `ForwardLimit` to that positive hop count (the default is one).
Headers are processed from right to left and processing stops at an untrusted hop. Unknown peers'
headers are ignored, so direct clients cannot select their own rate-limit bucket.

The middleware uses ASP.NET Core's trusted proxy handling; see the
[deployment guidance](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0).

## Public endpoints

`POST /api/auth/forgot-password`

```json
{"email":"learner@example.com"}
```

A syntactically valid request returns `202` with the same message whether the account exists, is inactive,
or is unknown. This acknowledges the request, not successful delivery:

```json
{"message":"If an active account matches this email, you will receive a password reset link."}
```

The email contains `/reset-password#userId=<id>&token=<token>`. The fragment is processed by Angular;
it is not sent with the initial page request. The link is valid for 15 minutes and is usable once.
Only one link is current per account. A request within 60 seconds of the previous issuance does not send
another email or invalidate that link. A later issuance replaces it.

`POST /api/auth/reset-password`

```json
{
  "userId":"00000000-0000-0000-0000-000000000001",
  "token":"<token-from-email>",
  "newPassword":"<new-password>",
  "confirmNewPassword":"<same-new-password>"
}
```

Success returns `204`. Password policy is the same as registration. The frontend clears the URL fragment
and local session and returns to login. The backend rotates the security stamp, invalidating previous JWTs,
and queues an email notification without the password. Normal password changes and administrative changes
that rotate the security stamp also invalidate outstanding reset links.

Invalid, expired, superseded or used links return `400` with rule `InvalidPasswordResetLink`.
An unknown or inactive account produces the same error. Invalid DTO fields return `422` using the existing
validation response; malformed JSON or a malformed GUID can be rejected by ASP.NET Core with `400`.
Neither endpoint requires a bearer token.

## Delivery and limits

- Request limits per client IP (after trusted forwarded headers): 5 forgot-password requests and 10 reset-password requests per 15 minutes.
  Exceeding a limit returns `429` and `Retry-After`. Limits are local to one API process.
- The bounded queue holds 100 pending email jobs; a full request queue returns `503`.
- A background worker performs account lookup and email delivery. Request response time does not wait for SMTP.
- Queue contents are held in memory. A restart can lose pending mail; the user can request a fresh link.
- Delivery has a 30-second timeout. SMTP failures produce a warning without addresses, tokens, credentials,
  or exception details. Users may retry after the cooldown. Delivery failure never rolls back a completed reset.
- The database stores only a SHA-256 token hash, expiry, issuance time and the security stamp at issuance.
  Conditional SQL updates ensure only one concurrent request can consume a link.

## Verification

HTTP tests cover the email link, password change, replay rejection, old-session rejection, validation,
expiry, inactive/unknown accounts, replacement, rate limits, queue saturation and SMTP failure recovery.
SQL Server tests verify concurrent consumption, cooldown, replacement and expiry using isolated
`QuizappRelationTests_<guid>` databases. Set the test connection from the existing verified development
configuration as required by `AGENTS.md`; never run these tests against the application's database.

Frontend route tests verify form validation, public requests, response states and navigation back to login.
Run the full suites and build using the repository verification instructions and WSL frontend runtime.
