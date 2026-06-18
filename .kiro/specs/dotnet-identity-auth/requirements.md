# Requirements Document

## Introduction

Hệ thống xác thực và phân quyền backend (dotnet-identity-auth) được xây dựng trên ASP.NET Core (.NET 10) sử dụng ASP.NET Core Identity với PostgreSQL. Hệ thống cung cấp các tính năng: đăng ký tài khoản, đăng nhập/đăng xuất, đổi mật khẩu, lockout tài khoản sau 5 lần đăng nhập sai (khóa 15 phút), và phân quyền theo vai trò (Admin, User). Giao diện người dùng sử dụng HTML/CSS/JavaScript thuần, giao tiếp với backend qua REST API với JWT Bearer Token. PostgreSQL chạy trong Docker container.

---

## Glossary

- **AuthService**: Application service chứa toàn bộ business logic xác thực (đăng ký, đăng nhập, đổi mật khẩu, đăng xuất).
- **UserService**: Application service quản lý người dùng và phân quyền, dành cho Admin.
- **JwtService**: Service tạo và validate JWT Bearer Token.
- **AuthController**: ASP.NET Core API Controller tiếp nhận HTTP request xác thực.
- **UserController**: ASP.NET Core API Controller quản lý người dùng (Admin only).
- **ApplicationUser**: Domain model người dùng, kế thừa từ `IdentityUser` của ASP.NET Core Identity.
- **UserManager**: ASP.NET Core Identity component quản lý vòng đời người dùng (tạo, tìm kiếm, cập nhật).
- **SignInManager**: ASP.NET Core Identity component xử lý đăng nhập và lockout.
- **RoleManager**: ASP.NET Core Identity component quản lý vai trò (roles).
- **AccessFailedCount**: Số lần đăng nhập sai liên tiếp của một tài khoản.
- **LockoutEnd**: Thời điểm tài khoản được tự động mở khóa (null = không bị khóa).
- **SecurityStamp**: Giá trị ngẫu nhiên gắn với tài khoản, thay đổi khi mật khẩu hoặc thông tin bảo mật thay đổi.
- **JWT**: JSON Web Token — chuỗi token stateless dùng cho xác thực.
- **PBKDF2**: Thuật toán hash mật khẩu được ASP.NET Core Identity sử dụng mặc định.
- **Frontend**: Giao diện HTML/CSS/JavaScript thuần chạy trên trình duyệt.
- **API**: ASP.NET Core Web API backend.

---

## Requirements

### Requirement 1: Đăng ký tài khoản

**User Story:** As a visitor, I want to register a new account with username, email, and password, so that I can access the system with my own credentials.

#### Acceptance Criteria

1. WHEN a user submits a registration request with a valid username, email, and password, THE AuthService SHALL create a new ApplicationUser record in the database and assign the specified role (default: "User").
2. WHEN a user submits a registration request, THE AuthController SHALL return HTTP 201 Created with a success message upon successful registration.
3. IF the submitted email already exists in the database, THEN THE AuthService SHALL return an error result with the message "Email đã được sử dụng" and THE AuthController SHALL return HTTP 400 Bad Request.
4. IF the submitted password does not meet complexity requirements (minimum 8 characters, at least one digit, one uppercase letter, one lowercase letter), THEN THE AuthService SHALL reject the registration and return a descriptive validation error.
5. IF the submitted username is fewer than 3 characters or more than 50 characters, THEN THE AuthService SHALL reject the registration and return a validation error.
6. WHEN a new ApplicationUser is created, THE AuthService SHALL store the password as a PBKDF2 hash and SHALL NOT store the plaintext password anywhere in the database.
7. WHEN a new ApplicationUser is created, THE AuthService SHALL set the `CreatedAt` field to the current UTC time and `IsActive` to true.
8. WHERE the requested role does not exist in the database, THE AuthService SHALL create the role before assigning it to the new user.

---

### Requirement 2: Đăng nhập

**User Story:** As a registered user, I want to log in with my email and password, so that I can receive a JWT token to access protected resources.

#### Acceptance Criteria

1. WHEN a user submits valid credentials (email and password), THE AuthService SHALL return a JWT access token, expiry time, and the user's roles.
2. WHEN a user submits valid credentials, THE AuthController SHALL return HTTP 200 OK with `{ accessToken, expiresAt, roles }`.
3. WHEN a login attempt succeeds, THE AuthService SHALL reset the user's `AccessFailedCount` to 0 and update `LastLoginAt` to the current UTC time.
4. IF the submitted email does not exist in the database or the account has `IsActive = false`, THEN THE AuthService SHALL return an error result and THE AuthController SHALL return HTTP 401 Unauthorized with the message "Email hoặc mật khẩu không đúng".
5. IF the submitted password is incorrect, THEN THE AuthService SHALL increment the user's `AccessFailedCount` by 1 and THE AuthController SHALL return HTTP 401 Unauthorized with the message "Email hoặc mật khẩu không đúng".
6. WHEN the user's `AccessFailedCount` reaches 5 consecutive failed attempts, THE AuthService SHALL set `LockoutEnd` to the current UTC time plus 15 minutes and THE AuthController SHALL return HTTP 423 Locked with `{ error, isLockedOut: true }`.
7. WHILE a user account has `LockoutEnd` greater than the current UTC time, THE AuthService SHALL reject all login attempts and THE AuthController SHALL return HTTP 423 Locked with `{ error, isLockedOut: true }`.
8. WHEN a locked account's `LockoutEnd` passes, THE AuthService SHALL allow login attempts to resume normally.

---

### Requirement 3: Đổi mật khẩu

**User Story:** As an authenticated user, I want to change my password by providing my current password and a new password, so that I can maintain account security.

#### Acceptance Criteria

1. WHEN an authenticated user submits a valid current password and a new password that meets complexity requirements, THE AuthService SHALL update the user's `PasswordHash` in the database.
2. WHEN a password change succeeds, THE AuthController SHALL return HTTP 200 OK with the message "Đổi mật khẩu thành công".
3. WHEN a password change succeeds, THE AuthService SHALL update the user's `SecurityStamp` to invalidate all previously issued JWT tokens.
4. IF the submitted current password does not match the stored hash, THEN THE AuthService SHALL return an error result and THE AuthController SHALL return HTTP 400 Bad Request with the message "Mật khẩu hiện tại không đúng".
5. IF the new password does not meet complexity requirements (minimum 8 characters, at least one digit, one uppercase letter, one lowercase letter), THEN THE AuthService SHALL reject the change and return a validation error.
6. IF the `ConfirmNewPassword` field does not match the `NewPassword` field, THEN THE AuthController SHALL return HTTP 400 Bad Request before calling the AuthService.
7. WHEN a request to change password is made without a valid JWT token, THE AuthController SHALL return HTTP 401 Unauthorized.

---

### Requirement 4: Đăng xuất

**User Story:** As an authenticated user, I want to log out, so that my session is ended and my token is no longer usable.

#### Acceptance Criteria

1. WHEN an authenticated user sends a logout request, THE AuthController SHALL return HTTP 200 OK with a success message.
2. WHEN a logout request is made without a valid JWT token, THE AuthController SHALL return HTTP 401 Unauthorized.
3. WHEN a user logs out, THE Frontend SHALL remove the JWT token from localStorage and redirect the user to the login page.

---

### Requirement 5: Phân quyền theo Role

**User Story:** As a system administrator, I want to manage user roles and protect endpoints by role, so that only authorized users can access sensitive operations.

#### Acceptance Criteria

1. WHEN a request is made to an Admin-only endpoint by a user who does not have the "Admin" role, THE API SHALL return HTTP 403 Forbidden.
2. WHEN a request is made to any protected endpoint without a valid JWT token, THE API SHALL return HTTP 401 Unauthorized.
3. WHEN an Admin sends a request to assign a role to a user, THE UserService SHALL add the specified role to the user's role assignments in the database.
4. WHEN an Admin sends a request to remove a role from a user, THE UserService SHALL remove the specified role from the user's role assignments in the database.
5. WHEN an Admin sends a request to unlock a locked user account, THE UserService SHALL set `LockoutEnd` to null and reset `AccessFailedCount` to 0 for that user.
6. WHEN an Admin requests the list of all users, THE UserController SHALL return HTTP 200 OK with a list of `UserInfoDto` objects containing id, username, email, fullName, roles, isLockedOut, and createdAt.
7. WHEN an Admin requests a specific user by ID, THE UserController SHALL return HTTP 200 OK with the `UserInfoDto` for that user, or HTTP 404 Not Found if the user does not exist.
8. THE System SHALL support at minimum two roles: "Admin" and "User".

---

### Requirement 6: JWT Token Generation và Validation

**User Story:** As a developer, I want the system to generate and validate JWT tokens correctly, so that stateless authentication is secure and reliable.

#### Acceptance Criteria

1. WHEN THE JwtService generates a token for a user, THE JwtService SHALL include the following claims: `sub` (user ID), `email`, `jti` (unique token ID), `name` (username), and one `ClaimTypes.Role` entry for each role assigned to the user.
2. WHEN THE JwtService generates a token, THE JwtService SHALL sign the token using HMAC-SHA256 with a secret key of at least 32 bytes.
3. WHEN THE JwtService generates a token, THE JwtService SHALL set the expiry (`exp`) to the current UTC time plus the configured `ExpiryHours` (default: 24 hours).
4. WHEN the JWT middleware validates an incoming token, THE API SHALL verify the issuer, audience, token lifetime, and signing key signature.
5. IF an incoming JWT token is expired, has an invalid signature, or is malformed, THEN THE API SHALL return HTTP 401 Unauthorized automatically via the JWT middleware.
6. THE JwtService SHALL set `ClockSkew` to zero so that token expiry is enforced exactly without time drift tolerance.

---

### Requirement 7: Quản lý người dùng (Admin)

**User Story:** As an administrator, I want to view and manage all user accounts, so that I can maintain system health and handle policy violations.

#### Acceptance Criteria

1. WHEN an Admin requests the full user list, THE UserController SHALL return all users with their profile information and current lock status.
2. WHEN an Admin requests a user by ID that does not exist, THE UserController SHALL return HTTP 404 Not Found.
3. WHEN an Admin assigns a role that does not exist in the system, THE UserService SHALL create the role before assigning it.
4. WHEN an Admin removes the last role from a user, THE UserService SHALL complete the operation and the user SHALL have no roles assigned.
5. THE UserController SHALL require the "Admin" role for all endpoints under `/api/users`.

---

### Requirement 8: Giao diện Frontend

**User Story:** As a user, I want to interact with the authentication system through a web browser using plain HTML/CSS/JavaScript, so that I can register, log in, change my password, and access protected pages.

#### Acceptance Criteria

1. WHEN a user visits the login page, THE Frontend SHALL display a form with email and password input fields and a submit button.
2. WHEN a user visits the registration page, THE Frontend SHALL display a form with username, email, password, confirm password, and optional full name fields.
3. WHEN a login request succeeds, THE Frontend SHALL store the `accessToken`, `expiresAt`, and `roles` in `localStorage` and redirect the user to the dashboard page.
4. WHEN a login request returns HTTP 423 Locked, THE Frontend SHALL display the message "Tài khoản bị khóa tạm thời. Vui lòng thử lại sau 15 phút."
5. WHEN making any API request that requires authentication, THE Frontend SHALL include the `Authorization: Bearer <token>` header using the token stored in `localStorage`.
6. WHEN a password change request succeeds, THE Frontend SHALL clear all items from `localStorage` and redirect the user to the login page.
7. WHEN an API request returns HTTP 401 Unauthorized, THE Frontend SHALL clear the token from `localStorage` and redirect the user to the login page.

---

### Requirement 9: Hạ tầng và Cấu hình

**User Story:** As a developer, I want the system to run with PostgreSQL in Docker and be configurable via environment variables, so that the application is portable and easy to deploy.

#### Acceptance Criteria

1. THE System SHALL use PostgreSQL 16 running in a Docker container as the primary data store.
2. WHEN the application starts, THE System SHALL apply all pending Entity Framework Core migrations to the PostgreSQL database automatically.
3. THE System SHALL read the JWT secret key, issuer, audience, and expiry hours from the `JwtSettings` configuration section (environment variables or `appsettings.json`).
4. THE System SHALL read the database connection string from the `ConnectionStrings:DefaultConnection` configuration key.
5. THE System SHALL NOT hardcode any secrets (JWT secret key, database password) in source code.
6. WHERE the application is running in a Docker Compose environment, THE System SHALL use the Docker Compose service name as the PostgreSQL hostname instead of `localhost`.

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Mật khẩu không bao giờ được lưu dạng plaintext

*For any* valid registration request with a plaintext password, the value stored in `ApplicationUser.PasswordHash` SHALL NOT equal the plaintext password, and the plaintext password SHALL NOT appear anywhere in the database.

**Validates: Requirements 1.6**

---

### Property 2: Email là unique trong toàn bộ hệ thống

*For any* two ApplicationUser records in the database with different IDs, their email addresses SHALL be different.

**Validates: Requirements 1.3**

---

### Property 3: Lockout được kích hoạt đúng ngưỡng

*For any* user account, after exactly 5 consecutive failed login attempts, `LockoutEnd` SHALL be set to a value greater than the current UTC time, and any subsequent login attempt SHALL return `IsLockedOut = true`.

**Validates: Requirements 2.6, 2.7**

---

### Property 4: AccessFailedCount reset sau đăng nhập thành công

*For any* user account with a non-zero `AccessFailedCount`, after a successful login, `AccessFailedCount` SHALL equal 0.

**Validates: Requirements 2.3**

---

### Property 5: JWT token chứa đúng claims

*For any* user and any list of roles assigned to that user, the JWT token generated by THE JwtService SHALL contain `sub` equal to the user's ID, `email` equal to the user's email, and one `ClaimTypes.Role` claim for each role in the list.

**Validates: Requirements 6.1**

---

### Property 6: JWT token round-trip — generate then validate

*For any* valid JWT token generated by THE JwtService, validating that token using the same secret key, issuer, and audience SHALL succeed and return a `ClaimsPrincipal` with claims equivalent to those used during generation.

**Validates: Requirements 6.2, 6.4**

---

### Property 7: Đổi mật khẩu thay đổi SecurityStamp

*For any* user account, after a successful password change, the value of `SecurityStamp` SHALL differ from its value before the change.

**Validates: Requirements 3.3**

---

### Property 8: Đổi mật khẩu cập nhật PasswordHash

*For any* user account, after a successful password change with a new password, the value of `PasswordHash` SHALL differ from its value before the change, and the new hash SHALL verify correctly against the new plaintext password.

**Validates: Requirements 3.1**

---

### Property 9: Phân quyền Admin — non-Admin bị từ chối

*For any* request to an endpoint decorated with `[Authorize(Roles = "Admin")]` made by a user whose JWT token does not contain the "Admin" role claim, THE API SHALL return HTTP 403 Forbidden.

**Validates: Requirements 5.1**

---

### Property 10: Gán role và xóa role là nghịch đảo nhau

*For any* user and any valid role, assigning the role then removing the role SHALL result in the user having the same set of roles as before the assignment.

**Validates: Requirements 5.3, 5.4**

---

### Property 11: Mở khóa tài khoản xóa trạng thái lockout

*For any* locked user account (where `LockoutEnd > UtcNow`), after an Admin unlock operation, `LockoutEnd` SHALL be null and `AccessFailedCount` SHALL equal 0, and a subsequent login attempt with correct credentials SHALL succeed.

**Validates: Requirements 5.5**
