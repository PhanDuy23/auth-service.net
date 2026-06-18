# Implementation Plan: dotnet-identity-auth

## Overview

Triển khai hệ thống xác thực và phân quyền backend trên ASP.NET Core (.NET 10) với ASP.NET Core Identity, PostgreSQL (Docker), JWT Bearer Token và giao diện HTML/CSS/JavaScript thuần. Các task được sắp xếp theo thứ tự từ setup hạ tầng → domain models → services → controllers → frontend → tích hợp cuối.

---

## Tasks

- [ ] 1. Setup project structure và infrastructure
  - [ ] 1.1 Khởi tạo ASP.NET Core Web API project (.NET 10) và cấu hình solution
    - Tạo solution `auth_service.sln` và project `AuthService.Api`
    - Thêm các NuGet packages: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.EntityFrameworkCore.Tools`
    - Tạo cấu trúc thư mục: `Controllers/`, `Services/`, `Models/`, `DTOs/`, `Data/`, `Settings/`
    - _Requirements: 9.1, 9.3, 9.4_

  - [x] 1.2 Tạo `docker-compose.yml` và `Dockerfile`
    - Tạo `docker-compose.yml` với service `postgres:16-alpine` (port 5432, healthcheck) và service `api` (port 5000:8080)
    - Tạo `Dockerfile` multi-stage build cho ASP.NET Core
    - Cấu hình environment variables: `ConnectionStrings__DefaultConnection` dùng Docker Compose service name `postgres`
    - _Requirements: 9.1, 9.6_

  - [x] 1.3 Cấu hình `appsettings.json` và `JwtSettings`
    - Tạo class `JwtSettings` với properties: `SecretKey`, `Issuer`, `Audience`, `ExpiryHours`
    - Thêm section `JwtSettings`, `ConnectionStrings`, `IdentitySettings` vào `appsettings.json`
    - Đăng ký `JwtSettings` vào DI container qua `IOptions<JwtSettings>`
    - _Requirements: 9.3, 9.4, 9.5_

- [ ] 2. Domain models và Data layer
  - [ ] 2.1 Tạo `ApplicationUser` và `ApplicationDbContext`
    - Tạo class `ApplicationUser : IdentityUser` với custom fields: `FullName`, `CreatedAt`, `LastLoginAt`, `IsActive`
    - Tạo `ApplicationDbContext : IdentityDbContext<ApplicationUser>` với `OnModelCreating` override
    - _Requirements: 1.7, 2.3_

  - [ ] 2.2 Tạo DTOs
    - Tạo records: `RegisterDto`, `LoginDto`, `ChangePasswordDto`, `AssignRoleDto`
    - Tạo records: `AuthResult`, `UserInfoDto`
    - Thêm Data Annotations: `[Required]`, `[EmailAddress]`, `[MinLength(8)]`, `[Compare("NewPassword")]`
    - _Requirements: 1.1, 1.4, 1.5, 2.1, 3.1, 3.6_

  - [ ] 2.3 Cấu hình EF Core migrations và auto-apply khi startup
    - Chạy `dotnet ef migrations add InitialCreate` để tạo migration đầu tiên
    - Thêm code `app.MigrateDatabase()` (extension method) trong `Program.cs` để tự động apply migrations khi khởi động
    - _Requirements: 9.2_

  - [ ]* 2.4 Viết property test cho Email uniqueness (Property 6)
    - **Property 6: Email là unique trong toàn bộ hệ thống**
    - Dùng xUnit + FsCheck hoặc CsCheck: với bất kỳ 2 user có Id khác nhau, email phải khác nhau
    - **Validates: Requirements 1.3**

- [ ] 3. JwtService — tạo và validate JWT token
  - [ ] 3.1 Implement `IJwtService` và `JwtService`
    - Tạo interface `IJwtService` với methods `GenerateToken(ApplicationUser user, IList<string> roles)` và `ValidateToken(string token)`
    - Implement `JwtService`: tạo claims list (sub, email, jti, name, roles), ký bằng HMAC-SHA256, set expiry từ `JwtSettings.ExpiryHours`
    - Đăng ký `JwtService` vào DI container
    - _Requirements: 6.1, 6.2, 6.3_

  - [ ]* 3.2 Viết property test cho JWT claims (Property 3)
    - **Property 3: JWT token chứa đúng claims**
    - Với bất kỳ user và danh sách roles, `GenerateToken` phải trả về token có `sub = user.Id`, `email = user.Email`, và đủ role claims
    - **Validates: Requirements 6.1**

  - [ ]* 3.3 Viết property test cho JWT round-trip (Property 7)
    - **Property 7: JWT token round-trip — generate then validate**
    - Với bất kỳ token hợp lệ được tạo bởi `GenerateToken`, `ValidateToken` phải trả về `ClaimsPrincipal` với claims tương đương
    - **Validates: Requirements 6.2, 6.4**

- [ ] 4. Cấu hình ASP.NET Core Identity và JWT middleware trong `Program.cs`
  - [ ] 4.1 Đăng ký Identity, EF Core, JWT Authentication và Authorization
    - Cấu hình `AddIdentity<ApplicationUser, IdentityRole>` với password policy (RequireDigit, RequireLowercase, RequireUppercase, RequiredLength=8) và lockout policy (MaxFailedAccessAttempts=5, DefaultLockoutTimeSpan=15 phút)
    - Cấu hình `AddAuthentication().AddJwtBearer` với `TokenValidationParameters` (ValidateIssuer, ValidateAudience, ValidateLifetime, ValidateIssuerSigningKey, ClockSkew=Zero)
    - Cấu hình `AddAuthorization` với policies "AdminOnly" và "UserOrAdmin"
    - Thêm `UseAuthentication()` và `UseAuthorization()` vào middleware pipeline
    - _Requirements: 1.4, 2.6, 5.1, 5.2, 6.4, 6.5, 6.6_

- [ ] 5. AuthService — business logic xác thực
  - [ ] 5.1 Implement `IAuthService.RegisterAsync`
    - Kiểm tra email đã tồn tại (`FindByEmailAsync`), trả về lỗi "Email đã được sử dụng" nếu trùng
    - Tạo `ApplicationUser` mới với `CreatedAt = DateTime.UtcNow`, `IsActive = true`
    - Gọi `UserManager.CreateAsync(user, password)` để tạo user (Identity tự hash PBKDF2)
    - Kiểm tra role tồn tại (`RoleExistsAsync`), tạo nếu chưa có, gán role cho user
    - _Requirements: 1.1, 1.2, 1.3, 1.6, 1.7, 1.8_

  - [ ]* 5.2 Viết property test cho password không lưu plaintext (Property 1)
    - **Property 1: Mật khẩu không bao giờ được lưu dạng plaintext**
    - Với bất kỳ plaintext password hợp lệ, `user.PasswordHash` không được bằng plaintext và không xuất hiện trong DB
    - **Validates: Requirements 1.6**

  - [ ] 5.3 Implement `IAuthService.LoginAsync`
    - Tìm user theo email, kiểm tra `IsActive`; trả về 401 nếu không tìm thấy hoặc inactive
    - Kiểm tra lockout (`IsLockedOutAsync`); trả về lỗi lockout nếu đang bị khóa
    - Gọi `CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)`
    - Nếu thành công: reset `AccessFailedCount`, cập nhật `LastLoginAt`, lấy roles, tạo JWT token
    - Nếu thất bại: trả về lỗi phù hợp (Failed / LockedOut)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8_

  - [ ]* 5.4 Viết property test cho lockout threshold (Property 2)
    - **Property 2: Lockout được kích hoạt đúng ngưỡng**
    - Sau đúng 5 lần đăng nhập sai liên tiếp, `LockoutEnd > UtcNow` và login tiếp theo trả về `IsLockedOut = true`
    - **Validates: Requirements 2.6, 2.7**

  - [ ]* 5.5 Viết property test cho AccessFailedCount reset (Property 8)
    - **Property 8: AccessFailedCount reset sau đăng nhập thành công**
    - Với bất kỳ user có `AccessFailedCount > 0`, sau khi đăng nhập thành công, `AccessFailedCount` phải bằng 0
    - **Validates: Requirements 2.3**

  - [ ] 5.6 Implement `IAuthService.ChangePasswordAsync`
    - Tìm user theo `userId` (`FindByIdAsync`)
    - Gọi `UserManager.ChangePasswordAsync(user, currentPassword, newPassword)`
    - Nếu thành công: gọi `UpdateSecurityStampAsync(user)` để invalidate token cũ
    - Trả về lỗi phù hợp nếu mật khẩu hiện tại sai
    - _Requirements: 3.1, 3.3, 3.4, 3.5_

  - [ ]* 5.7 Viết property test cho SecurityStamp thay đổi sau đổi mật khẩu (Property 5)
    - **Property 5: Đổi mật khẩu thay đổi SecurityStamp**
    - Sau khi `ChangePasswordAsync` thành công, `SecurityStamp` phải khác giá trị trước đó
    - **Validates: Requirements 3.3**

  - [ ]* 5.8 Viết property test cho PasswordHash thay đổi sau đổi mật khẩu (Property 8 — requirements)
    - **Property 8 (requirements): Đổi mật khẩu cập nhật PasswordHash**
    - Sau khi đổi mật khẩu thành công, `PasswordHash` phải khác giá trị cũ và verify đúng với mật khẩu mới
    - **Validates: Requirements 3.1**

  - [ ] 5.9 Implement `IAuthService.LogoutAsync`
    - Trả về `AuthResult(true)` (stateless JWT — logout xử lý ở frontend)
    - _Requirements: 4.1_

- [ ] 6. UserService — quản lý người dùng (Admin)
  - [ ] 6.1 Implement `IUserService` và `UserService`
    - Tạo interface `IUserService` với methods: `GetAllUsersAsync`, `GetUserByIdAsync`, `AssignRoleAsync`, `RemoveRoleAsync`, `UnlockUserAsync`
    - Implement `GetAllUsersAsync`: lấy tất cả users, map sang `UserInfoDto` (bao gồm roles và `IsLockedOut`)
    - Implement `GetUserByIdAsync`: tìm user theo ID, trả về `UserInfoDto` hoặc null
    - _Requirements: 5.6, 5.7, 7.1, 7.2_

  - [ ] 6.2 Implement AssignRole, RemoveRole và UnlockUser trong UserService
    - `AssignRoleAsync`: kiểm tra role tồn tại (tạo nếu chưa có), gọi `AddToRoleAsync`
    - `RemoveRoleAsync`: gọi `RemoveFromRoleAsync` (cho phép xóa role cuối cùng)
    - `UnlockUserAsync`: set `LockoutEnd = null`, reset `AccessFailedCount = 0` qua `SetLockoutEndDateAsync` và `ResetAccessFailedCountAsync`
    - _Requirements: 5.3, 5.4, 5.5, 7.3, 7.4_

  - [ ]* 6.3 Viết property test cho assign/remove role là nghịch đảo (Property 9)
    - **Property 9: Gán role và xóa role là nghịch đảo nhau**
    - Với bất kỳ user và role hợp lệ, `AssignRole` rồi `RemoveRole` phải trả về tập roles giống trước khi gán
    - **Validates: Requirements 5.3, 5.4**

  - [ ]* 6.4 Viết property test cho unlock xóa trạng thái lockout (Property 10)
    - **Property 10: Mở khóa tài khoản xóa trạng thái lockout**
    - Sau `UnlockUserAsync`, `LockoutEnd` phải là null và `AccessFailedCount` phải bằng 0
    - **Validates: Requirements 5.5**

- [ ] 7. Checkpoint — Kiểm tra services và tests
  - Đảm bảo tất cả unit tests và property tests cho services pass
  - Kiểm tra DI registration đầy đủ (AuthService, UserService, JwtService)
  - Hỏi người dùng nếu có vấn đề phát sinh.

- [ ] 8. AuthController — HTTP endpoints xác thực
  - [ ] 8.1 Implement `AuthController` với các endpoints Register, Login, Logout, ChangePassword, GetCurrentUser
    - `POST /api/auth/register`: validate ModelState, gọi `AuthService.RegisterAsync`, trả về 201 hoặc 400
    - `POST /api/auth/login`: validate ModelState, gọi `AuthService.LoginAsync`, trả về 200/401/423
    - `POST /api/auth/logout` `[Authorize]`: gọi `AuthService.LogoutAsync`, trả về 200
    - `POST /api/auth/change-password` `[Authorize]`: validate ModelState (bao gồm ConfirmNewPassword), trích xuất userId từ Claims, gọi `AuthService.ChangePasswordAsync`, trả về 200/400
    - `GET /api/auth/me` `[Authorize]`: trả về thông tin user hiện tại từ Claims
    - _Requirements: 1.2, 2.2, 3.2, 3.6, 3.7, 4.1, 4.2_

  - [ ]* 8.2 Viết unit tests cho AuthController
    - Test Register: 201 khi thành công, 400 khi email trùng, 400 khi validation fail
    - Test Login: 200 khi thành công, 401 khi sai credentials, 423 khi bị lockout
    - Test ChangePassword: 200 khi thành công, 400 khi sai mật khẩu hiện tại, 401 khi không có token
    - _Requirements: 1.2, 2.2, 3.2, 3.4, 3.7_

  - [ ]* 8.3 Viết property test cho phân quyền Admin (Property 4)
    - **Property 4: Phân quyền đúng theo role**
    - Với bất kỳ request đến endpoint `[Authorize(Roles = "Admin")]` từ user không có role "Admin", API phải trả về HTTP 403
    - **Validates: Requirements 5.1**

- [ ] 9. UserController — HTTP endpoints quản lý người dùng (Admin)
  - [ ] 9.1 Implement `UserController` với `[Authorize(Roles = "Admin")]`
    - `GET /api/users`: gọi `UserService.GetAllUsersAsync`, trả về 200 với list `UserInfoDto`
    - `GET /api/users/{id}`: gọi `UserService.GetUserByIdAsync`, trả về 200 hoặc 404
    - `POST /api/users/{id}/roles`: gọi `UserService.AssignRoleAsync`, trả về 200 hoặc 400
    - `DELETE /api/users/{id}/roles/{role}`: gọi `UserService.RemoveRoleAsync`, trả về 200
    - `POST /api/users/{id}/unlock`: gọi `UserService.UnlockUserAsync`, trả về 200
    - _Requirements: 5.1, 5.3, 5.4, 5.5, 5.6, 5.7, 7.1, 7.2, 7.3, 7.4, 7.5_

  - [ ]* 9.2 Viết unit tests cho UserController
    - Test GetAllUsers: 200 với danh sách users, 403 khi không có role Admin
    - Test GetUserById: 200 khi tìm thấy, 404 khi không tìm thấy
    - Test AssignRole và RemoveRole: 200 khi thành công
    - Test UnlockUser: 200 khi thành công
    - _Requirements: 5.1, 5.6, 5.7, 7.2, 7.5_

- [ ] 10. Checkpoint — Kiểm tra API endpoints
  - Build project, đảm bảo không có compile errors
  - Chạy toàn bộ unit tests và property tests
  - Hỏi người dùng nếu có vấn đề phát sinh.

- [ ] 11. Frontend — HTML/CSS/JavaScript thuần
  - [ ] 11.1 Tạo trang đăng nhập (`login.html`) và script xử lý
    - Tạo `wwwroot/login.html` với form: email, password, submit button
    - Tạo `wwwroot/js/auth.js`: hàm `login(email, password)` gọi `POST /api/auth/login`
    - Khi thành công: lưu `accessToken`, `expiresAt`, `roles` vào `localStorage`, redirect đến `dashboard.html`
    - Khi HTTP 423: hiển thị "Tài khoản bị khóa tạm thời. Vui lòng thử lại sau 15 phút."
    - Khi HTTP 401: hiển thị thông báo lỗi
    - _Requirements: 8.1, 8.3, 8.4_

  - [ ] 11.2 Tạo trang đăng ký (`register.html`) và script xử lý
    - Tạo `wwwroot/register.html` với form: username, email, password, confirm password, fullName (optional)
    - Tạo `wwwroot/js/register.js`: hàm `register(...)` gọi `POST /api/auth/register`
    - Khi thành công: redirect đến `login.html`
    - Khi thất bại: hiển thị thông báo lỗi từ response
    - _Requirements: 8.2_

  - [ ] 11.3 Tạo `fetchWithAuth` utility và xử lý 401 tự động
    - Tạo `wwwroot/js/api.js` với hàm `fetchWithAuth(url, options)`: tự động thêm `Authorization: Bearer <token>` header
    - Nếu response trả về HTTP 401: xóa token khỏi `localStorage`, redirect về `login.html`
    - _Requirements: 8.5, 8.7_

  - [ ] 11.4 Tạo trang dashboard (`dashboard.html`) và trang đổi mật khẩu (`change-password.html`)
    - Tạo `wwwroot/dashboard.html`: hiển thị thông tin user, nút đăng xuất, link đổi mật khẩu
    - Tạo `wwwroot/change-password.html` với form: currentPassword, newPassword, confirmNewPassword
    - Tạo `wwwroot/js/change-password.js`: gọi `POST /api/auth/change-password` qua `fetchWithAuth`
    - Khi đổi mật khẩu thành công: `localStorage.clear()`, redirect về `login.html`
    - Xử lý đăng xuất: xóa token khỏi `localStorage`, redirect về `login.html`
    - _Requirements: 8.3, 8.6, 4.3_

  - [ ] 11.5 Cấu hình ASP.NET Core để serve static files
    - Thêm `UseStaticFiles()` và `UseDefaultFiles()` vào middleware pipeline trong `Program.cs`
    - Cấu hình fallback route cho SPA nếu cần
    - _Requirements: 8.1, 8.2_

- [ ] 12. Tích hợp và wiring cuối
  - [ ] 12.1 Đăng ký tất cả services vào DI container trong `Program.cs`
    - Đăng ký: `IAuthService → AuthService`, `IUserService → UserService`, `IJwtService → JwtService`
    - Đăng ký `ApplicationDbContext` với Npgsql connection string
    - Cấu hình CORS nếu cần cho frontend
    - _Requirements: 9.3, 9.4_

  - [ ] 12.2 Seed dữ liệu ban đầu (roles Admin và User)
    - Tạo `DatabaseSeeder` class: tạo roles "Admin" và "User" nếu chưa tồn tại khi startup
    - Tùy chọn: tạo tài khoản Admin mặc định từ environment variables
    - _Requirements: 5.8, 9.2_

  - [ ]* 12.3 Viết integration tests end-to-end
    - Test full flow: Register → Login → ChangePassword → Logout
    - Test lockout flow: 5 lần sai → bị khóa → Admin unlock → đăng nhập thành công
    - Test Admin flow: Login as Admin → GetAllUsers → AssignRole → RemoveRole
    - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.3, 5.4, 5.5_

- [ ] 13. Final Checkpoint — Kiểm tra toàn bộ hệ thống
  - Build toàn bộ project (`dotnet build`)
  - Chạy toàn bộ test suite (`dotnet test`)
  - Kiểm tra `docker-compose up` khởi động thành công, migrations được apply tự động
  - Hỏi người dùng nếu có vấn đề phát sinh.

---

## Notes

- Tasks đánh dấu `*` là optional, có thể bỏ qua để build MVP nhanh hơn
- Mỗi task tham chiếu requirements cụ thể để đảm bảo traceability
- Property tests dùng xUnit + FsCheck (hoặc CsCheck) để kiểm tra các invariants
- Unit tests dùng xUnit + Moq để mock `UserManager`, `SignInManager`, `JwtService`
- Checkpoints đảm bảo validate incremental progress trước khi tiếp tục
- Không hardcode secrets — dùng environment variables hoặc `appsettings.json` (không commit secret thật)
- Docker Compose service name `postgres` được dùng làm hostname thay vì `localhost`

---

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3"] },
    { "id": 1, "tasks": ["2.1", "2.2"] },
    { "id": 2, "tasks": ["2.3", "3.1"] },
    { "id": 3, "tasks": ["2.4", "3.2", "3.3", "4.1"] },
    { "id": 4, "tasks": ["5.1", "5.3", "5.6", "5.9"] },
    { "id": 5, "tasks": ["5.2", "5.4", "5.5", "5.7", "5.8", "6.1"] },
    { "id": 6, "tasks": ["6.2"] },
    { "id": 7, "tasks": ["6.3", "6.4", "8.1"] },
    { "id": 8, "tasks": ["8.2", "8.3", "9.1"] },
    { "id": 9, "tasks": ["9.2", "11.1", "11.2"] },
    { "id": 10, "tasks": ["11.3"] },
    { "id": 11, "tasks": ["11.4", "11.5"] },
    { "id": 12, "tasks": ["12.1"] },
    { "id": 13, "tasks": ["12.2"] },
    { "id": 14, "tasks": ["12.3"] }
  ]
}
```
