# HappyBoxx Authentication and Account Requirements

## 1. Identity Provider and Ownership

- Use Microsoft Entra External ID with an external tenant for the initial customer and staff sign-in experience. Both groups use personal email addresses and are application users, not HappyBoxx workforce identities.
- Register separate applications for the customer and staff portals. Use Entra-hosted sign-in and sign-up flows with OpenID Connect Authorization Code flow and PKCE.
- Entra owns passwords, email verification, OTP generation and delivery, OTP validation, and password reset. HappyBoxx MUST NOT store passwords or OTPs.
- HappyBoxx owns customer profiles, account lifecycle state, staff invitations, application roles, warehouse permissions, and business authorization.
- A personal email address or successful sign-in MUST NOT grant staff privileges. APIs MUST map a validated Entra identity to an active HappyBoxx account and enforce its permissions server-side.

## 2. Sign-in and Email Verification

- All users sign in with email address and password, followed by a six-digit, single-use OTP sent to that email address.
- Require successful OTP verification before completing sign-in. Entra MUST enforce OTP expiry, resend limits, and failed-attempt protections.
- During initial registration, verify ownership of the email address through Entra's email OTP step. HappyBoxx MUST NOT mark the corresponding account Active or grant any protected access until the Entra flow completes successfully and the backend verifies the provider-issued identity and email-verification result.
- Do not trust a client-submitted `emailVerified` value. An incomplete sign-up, an invalid or expired OTP, or a failed Entra callback MUST leave the HappyBoxx account without access.
- Use email OTP as the required second verification method. Do not enable SMS or other separately billed MFA add-ons. Email OTP depends on the security of the user's mailbox and is less phishing-resistant than a passkey.
- Use Entra for password reset. Never log credentials, OTPs, access tokens, or refresh tokens.

## 3. Customer Registration

- Customers can self-register through the customer portal.
- Entra collects and verifies the email address and manages the password. HappyBoxx collects and stores the physical address, phone number, and other HappyBoxx profile details required by the business.
- A customer account is not Active until email verification is complete. The API MUST reject protected customer operations for unverified, disabled, or otherwise inactive accounts.
- Initial release supports standard customer accounts only. Wholesale registration, approval workflows, wholesale pricing, and wholesale-only permissions are deferred.
- Keep customer lifecycle state extensible so a future verified wholesale customer can enter `PendingApproval` and remain unable to order until an authorized administrator approves the account. Do not implement wholesale approval in the initial release.

## 4. Staff Invitation and Provisioning

- There MUST NOT be public staff registration.
- An authorized HappyBoxx administrator creates a single-use, expiring invitation bound to a specific email address and assigns the intended HappyBoxx role.
- The invitee completes Entra registration, sets a password, and verifies the invited email address using OTP. The invite MUST NOT activate access until the backend validates the Entra identity and confirms that it matches the invitation.
- Invitation links carry the single-use token in the URL fragment, not the query string. The portal MUST remove the fragment from browser history immediately and retain the token only in session storage while the invitee completes Entra sign-up.
- Reject expired, revoked, already-used, email-mismatched, and uninvited staff registrations. No staff permissions are effective before account activation.
- Administrators can revoke invitations, disable staff accounts, and change role assignments. Disabling an account MUST prevent subsequent API access.
- Establish the initial administrator by manually creating the designated identity in Entra External ID, then applying a reviewed one-time application-database seed for that identity's stable Entra identifier and the `Admin` role. The seed MUST NOT key on email alone, must not bypass the email-verification gate, and MUST NOT be exposed through a public endpoint.

## 5. HappyBoxx Roles and Authorization

Role assignment is owned by HappyBoxx. Deny permissions by default and enforce each permission in the APIs, not only in the UI or gateway.

| Role | Allowed access | Explicitly prohibited |
|---|---|---|
| `Admin` | Manage staff invitations and roles, customer accounts, catalog, orders, and inventory, subject to audit logging. | None within the HappyBoxx application; platform/Entra tenant administration remains separately privileged. |
| `Order Creator` | View the product, customer, and stock availability information needed to create orders; create, view, edit, and cancel only orders they created. | Create or modify products/categories, adjust or receive stock, change stock levels or reorder settings, manage users, or assign roles. |

- Order creation MUST NOT indirectly mutate stock. Stock reservation or adjustment requires a separately authorized workflow.
- Staff and customer permissions MUST be tested at service/API boundaries, including attempts to call endpoints directly without the required role.

## 6. Data, Tokens, and Cost

- Use Entra-issued identities and validated tokens. Identify an account using the provider and stable subject identifier; do not use email as the sole permanent identity key.
- HappyBoxx Identity is the sole owner of application roles and account status. Gateway and business APIs MUST resolve the current role from Identity for each authorization decision; Entra role claims MUST NOT grant HappyBoxx access.
- Store HappyBoxx profile data, lifecycle state, invitation metadata, and role assignments in application storage. Do not store passwords, OTPs, or Entra client secrets there.
- Browser applications MUST NOT contain client secrets or put tokens in `localStorage`. Validate token issuer, audience, signature, expiry, and required application account state at protected APIs.
- Use a dedicated Entra External ID test tenant, separate from production. Microsoft currently offers a 30-day external-tenant trial that does not require an Azure subscription; continued use requires billing configuration. The app and APIs run locally and do not need to be hosted in Azure for authentication testing.
- For real end-to-end tests, register `http://localhost:5173/`, `http://localhost:5173/register`, and `http://localhost:5173/accept-invitation` as SPA redirect URLs in the test tenant and use test identities/inboxes. Do not use production user data. OTP email is sent to the actual test mailbox.
- The admin SPA reads public client configuration from `VITE_ENTRA_CLIENT_ID`, `VITE_ENTRA_AUTHORITY`, `VITE_ENTRA_API_SCOPE`, and optional `VITE_ENTRA_REDIRECT_URI`. The API services read `Authentication__Authority` and `Authentication__Audience` from environment configuration. Client IDs, authority, and scope are public identifiers; client secrets MUST NOT be added to Vite configuration.
- The API access token must target the HappyBoxx API audience and contain the stable `sub` and verified `email` claims emitted by the configured Entra user flow. Identity maps `sub` to an active application account and contributes the database-owned role; client-submitted identity or role values are never trusted.
- Bootstrap the first Admin only after they complete Entra sign-up and email OTP verification. Use [bootstrap-first-admin.example.sql](../scripts/sql/bootstrap-first-admin.example.sql) once, replacing every placeholder and supplying a GUID v7. Never expose a public first-Admin bootstrap endpoint.
- Automated UI and API tests MUST also run without Entra by substituting a fake identity provider or test authentication handler. These test doubles MUST be limited to test execution and MUST NOT be available as an authentication path in deployed environments.
- Entra External ID uses monthly-active-user billing and provides a free allowance. Link an Azure subscription for ongoing usage and monitor billing; usage beyond the free allowance may incur charges. Do not enable SMS MFA add-ons.
- Email OTP is delivered by Entra. No HappyBoxx SMTP service is required for authentication OTPs.
- Staff invitation links are emailed by HappyBoxx through Gmail SMTP from `danielvo00.au@gmail.com`. Configure an App Password only after enabling Google 2-Step Verification; keep it in .NET user-secrets for local testing and a deployment secret store in production. Configure `Email:PublicBaseUrl` to the deployed portal origin; the sender fails closed if it is missing outside Development.
- For local Gmail SMTP testing, create an App Password in the Google account security settings (requires 2-Step Verification), then run `dotnet user-secrets set "Email:Password" "<app-password>" --project src/Services/Identity/HappyBoxx.Identity.Api` directly in your terminal. Do not send the App Password in chat or commit it. Override `Email:Mode=GmailSmtp` and `Email:PublicBaseUrl=http://localhost:5173/` in user-secrets to enable local sending; Development otherwise keeps copy-link mode.
- Development email delivery is disabled and returns a copyable single-use invitation link instead. No invitation link is returned to the Admin after email delivery succeeds.

## 7. Local and End-to-End Testing

- Unit and API integration tests MUST cover email-verification gating, account states, invitations, and authorization using test identities; they MUST NOT depend on an Entra tenant or send real email.
- Local end-to-end tests of Entra-hosted password and email OTP flows MUST use the dedicated test tenant and real test inboxes. These tests verify the provider integration, not just HappyBoxx authorization.
- A successful local or mocked test MUST NOT be treated as proof that Entra user-flow configuration, email delivery, or token claims are correct. Run an Entra smoke test before release.
- Never place production client secrets or production user accounts in local test configuration.

## 8. Acceptance Criteria

- An unverified customer cannot access protected customer operations or place orders.
- A staff user cannot register or gain staff access without a valid invitation bound to their verified email.
- A verified invited staff member receives only the role assigned by an administrator.
- An `Order Creator` cannot change inventory through either the UI or direct API calls; an `Admin` can perform the authorized inventory operations.
- Disabled accounts and revoked invitations cannot be used to obtain or retain HappyBoxx access.
- Authentication tests cover valid and invalid OTP completion, email mismatch, invitation expiry/revocation/reuse, disabled accounts, and role-based API denial.

## 9. Microsoft References

- [Create an External ID tenant](https://learn.microsoft.com/en-us/entra/external-id/customers/how-to-create-external-tenant-portal)
- [External ID authentication methods](https://learn.microsoft.com/en-us/entra/external-id/customers/concept-authentication-methods-customers)
- [Create customer sign-up and sign-in flows](https://learn.microsoft.com/en-us/entra/external-id/customers/how-to-user-flow-sign-up-sign-in-customers)
- [Configure customer MFA](https://learn.microsoft.com/en-us/entra/external-id/customers/how-to-multifactor-authentication-customers)
- [Workforce and external tenant configurations](https://learn.microsoft.com/en-us/entra/external-id/tenant-configurations)
- [External ID pricing](https://learn.microsoft.com/en-us/entra/external-id/external-identities-pricing)