SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @AccountId UNIQUEIDENTIFIER = TRY_CONVERT(UNIQUEIDENTIFIER, N'<GENERATE-A-GUID-V7>');
DECLARE @ExternalSubject NVARCHAR(200) = N'<VERIFIED-ENTRA-SUBJECT-CLAIM>';
DECLARE @Email NVARCHAR(320) = N'<VERIFIED-EMAIL-ADDRESS>';
DECLARE @DisplayName NVARCHAR(200) = N'<ADMIN-DISPLAY-NAME>';

IF @AccountId IS NULL
    THROW 51000, 'Replace AccountId with a GUID v7 before running this seed.', 1;

IF @ExternalSubject LIKE N'<%'
    THROW 51001, 'Replace ExternalSubject with the verified Entra subject claim.', 1;

IF @Email LIKE N'<%'
    THROW 51002, 'Replace Email with the verified Entra email address.', 1;

IF EXISTS (
    SELECT 1
    FROM identity.AccessAccounts
    WHERE Kind = 1 AND Role = N'Admin' AND Status = 1
)
    THROW 51003, 'An active Admin already exists. Do not run this bootstrap seed again.', 1;

IF EXISTS (
    SELECT 1
    FROM identity.AccessAccounts
    WHERE ExternalSubject = @ExternalSubject OR NormalizedEmail = UPPER(LTRIM(RTRIM(@Email)))
)
    THROW 51004, 'This Entra identity or email already has an account.', 1;

INSERT INTO identity.AccessAccounts
    (Id, ExternalSubject, Email, NormalizedEmail, DisplayName, Kind, Role, Status, CreatedAtUtc, DisabledAtUtc)
VALUES
    (@AccountId, @ExternalSubject, @Email, UPPER(LTRIM(RTRIM(@Email))), @DisplayName, 1, N'Admin', 1, SYSUTCDATETIME(), NULL);

COMMIT TRANSACTION;