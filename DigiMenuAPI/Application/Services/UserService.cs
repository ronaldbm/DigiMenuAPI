using AutoMapper;
using AppCore.Application.Common;
using AppCore.Application.DTOs.Email;
using AppCore.Application.Interfaces;
using AppCore.Application.Utils;
using AppCore.Domain.Entities;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly IEmailQueueService _emailQueue;
        private readonly IMapper _mapper;
        private readonly IConfiguration _config;

        private string AppUrl => _config["Email:AppUrl"] ?? "https://app.digimenu.cr";

        public UserService(
            ApplicationDbContext context,
            ITenantService tenantService,
            IEmailQueueService emailQueue,
            IMapper mapper,
            IConfiguration config)
        {
            _context = context;
            _tenantService = tenantService;
            _emailQueue = emailQueue;
            _mapper = mapper;
            _config = config;
        }

        // ── GET ALL ───────────────────────────────────────────────────
        public async Task<OperationResult<List<AppUserSummaryDto>>> GetAll()
        {
            var companyId = _tenantService.GetCompanyId();
            var callerRole = _tenantService.GetUserRole();
            var callerBranchId = _tenantService.TryGetBranchId();

            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.CompanyId == companyId && !u.IsDeleted);

            // BranchAdmin solo ve usuarios de su propia Branch
            if (UserRoles.NeedsBranch(callerRole) && callerBranchId.HasValue)
                query = query.Where(u => u.BranchId == callerBranchId.Value);

            var users = await query
                .Include(u => u.Branch)
                .OrderBy(u => u.FullName)
                .Select(u => new AppUserSummaryDto(
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.IsActive,
                    u.BranchId,
                    u.Branch != null ? u.Branch.Name : null))
                .ToListAsync();

            return OperationResult<List<AppUserSummaryDto>>.Ok(users);
        }

        // ── GET BY ID ─────────────────────────────────────────────────
        public async Task<OperationResult<AppUserReadDto>> GetById(int userId)
        {
            var user = await ResolveUserForCallerAsync(userId);
            if (user is null)
                return OperationResult<AppUserReadDto>.NotFound(
                    "Usuario no encontrado.",
                    ErrorKeys.UserNotFound);

            return OperationResult<AppUserReadDto>.Ok(_mapper.Map<AppUserReadDto>(user));
        }

        // ── CREATE ────────────────────────────────────────────────────
        public async Task<OperationResult<AppUserReadDto>> Create(AppUserCreateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();
            var callerRole = _tenantService.GetUserRole();
            var callerId = _tenantService.GetUserId();
            var email = dto.Email.Trim().ToLower();

            // Validar jerarquía de roles
            if (!UserRoles.CanAssign(callerRole, dto.Role))
                return OperationResult<AppUserReadDto>.Forbidden(
                    "No tienes permiso para asignar este rol.",
                    ErrorKeys.CannotAssignSuperAdmin);

            // BranchAdmin solo puede crear usuarios en su propia Branch
            if (UserRoles.NeedsBranch(callerRole))
            {
                var callerBranchId = _tenantService.GetBranchId();
                if (dto.BranchId != callerBranchId)
                    return OperationResult<AppUserReadDto>.Forbidden(
                        "Solo puedes crear usuarios en tu propia sucursal.",
                        ErrorKeys.Forbidden);
            }

            // BranchAdmin y Staff deben tener Branch asignada
            if (UserRoles.NeedsBranch(dto.Role) && dto.BranchId is null)
                return OperationResult<AppUserReadDto>.ValidationError(
                    "BranchAdmin y Staff deben estar asignados a una sucursal.",
                    ErrorKeys.BranchRequiredForRole);

            // Validar que el email no esté en uso
            if (await _context.Users.IgnoreQueryFilters()
                    .AnyAsync(u => u.Email == email))
                return OperationResult<AppUserReadDto>.Conflict(
                    "El email ya está registrado.",
                    ErrorKeys.EmailAlreadyExists);

            // Validar que la Branch pertenece a la empresa
            if (dto.BranchId.HasValue)
            {
                var branchBelongs = await _context.Branches
                    .AnyAsync(b => b.Id == dto.BranchId.Value && b.CompanyId == companyId);

                if (!branchBelongs)
                    return OperationResult<AppUserReadDto>.NotFound(
                        "La sucursal indicada no pertenece a tu empresa.",
                        ErrorKeys.BranchNotFound);
            }

            // Validar límite de usuarios del plan
            var company = await _context.Companies
                .AsNoTracking()
                .FirstAsync(c => c.Id == companyId);

            if (company.MaxUsers != -1)
            {
                var currentCount = await _context.Users
                    .CountAsync(u => u.CompanyId == companyId && !u.IsDeleted);

                if (currentCount >= company.MaxUsers)
                    return OperationResult<AppUserReadDto>.Conflict(
                        $"Tu plan permite un máximo de {company.MaxUsers} usuarios.",
                        ErrorKeys.UserLimitReached);
            }

            // Generar contraseña temporal
            var temporaryPassword = PasswordValidator.GenerateTemporary();

            var user = new AppUser
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
                Role = dto.Role,
                CompanyId = companyId,
                BranchId = dto.BranchId,
                IsActive = true,
                MustChangePassword = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Encolar email con contraseña temporal
            await _emailQueue.QueueTemporaryPasswordAsync(new TemporaryPasswordEmailDto(
                ToEmail: email,
                FullName: dto.FullName.Trim(),
                CompanyName: company.Name,
                TemporaryPassword: temporaryPassword,
                LoginUrl: $"{AppUrl}/login"
            ), companyId);

            // Recargar con relaciones para mapear CompanyName y BranchName
            var created = await _context.Users
                .Include(u => u.Company)
                .Include(u => u.Branch)
                .FirstAsync(u => u.Id == user.Id);

            return OperationResult<AppUserReadDto>.Ok(_mapper.Map<AppUserReadDto>(created));
        }

        // ── UPDATE ────────────────────────────────────────────────────
        public async Task<OperationResult<AppUserReadDto>> Update(AppUserUpdateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var user = await ResolveUserForCallerAsync(dto.Id);
            if (user is null)
                return OperationResult<AppUserReadDto>.NotFound(
                    "Usuario no encontrado.",
                    ErrorKeys.UserNotFound);

            // Validar email si cambió
            var newEmail = dto.Email.Trim().ToLower();
            if (newEmail != user.Email)
            {
                var emailInUse = await _context.Users
                    .IgnoreQueryFilters()
                    .AnyAsync(u => u.Email == newEmail && u.Id != dto.Id);

                if (emailInUse)
                    return OperationResult<AppUserReadDto>.Conflict(
                        "El email ya está en uso por otro usuario.",
                        ErrorKeys.EmailAlreadyExists);
            }

            // Validar nueva Branch si cambió
            if (dto.BranchId.HasValue)
            {
                var branchBelongs = await _context.Branches
                    .AnyAsync(b => b.Id == dto.BranchId.Value && b.CompanyId == companyId);

                if (!branchBelongs)
                    return OperationResult<AppUserReadDto>.NotFound(
                        "La sucursal indicada no pertenece a tu empresa.",
                        ErrorKeys.BranchNotFound);
            }

            // Rol sigue requiriendo Branch
            if (UserRoles.NeedsBranch(user.Role) && dto.BranchId is null)
                return OperationResult<AppUserReadDto>.ValidationError(
                    "Este rol requiere una sucursal asignada.",
                    ErrorKeys.BranchRequiredForRole);

            user.FullName = dto.FullName.Trim();
            user.Email = newEmail;
            user.BranchId = dto.BranchId;
            await _context.SaveChangesAsync();

            // Recargar con relaciones
            await _context.Entry(user).Reference(u => u.Company).LoadAsync();
            await _context.Entry(user).Reference(u => u.Branch).LoadAsync();

            return OperationResult<AppUserReadDto>.Ok(_mapper.Map<AppUserReadDto>(user));
        }

        // ── TOGGLE ACTIVE ─────────────────────────────────────────────
        public async Task<OperationResult<bool>> ToggleActive(int userId)
        {
            var callerId = _tenantService.GetUserId();

            // Un usuario no puede desactivarse a sí mismo
            if (userId == callerId)
                return OperationResult<bool>.ValidationError(
                    "No puedes activar o desactivar tu propia cuenta.",
                    ErrorKeys.CannotModifySelf);

            var user = await ResolveUserForCallerAsync(userId);
            if (user is null)
                return OperationResult<bool>.NotFound(
                    "Usuario no encontrado.",
                    ErrorKeys.UserNotFound);

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Ok(true);
        }

        // ── DELETE ────────────────────────────────────────────────────
        public async Task<OperationResult<bool>> Delete(int userId)
        {
            var callerId = _tenantService.GetUserId();

            // Un usuario no puede eliminarse a sí mismo
            if (userId == callerId)
                return OperationResult<bool>.ValidationError(
                    "No puedes eliminar tu propia cuenta.",
                    ErrorKeys.CannotModifySelf);

            var user = await ResolveUserForCallerAsync(userId);
            if (user is null)
                return OperationResult<bool>.NotFound(
                    "Usuario no encontrado.",
                    ErrorKeys.UserNotFound);

            user.IsDeleted = true;
            user.IsActive = false;
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Ok(true);
        }

        // ── RESET PASSWORD ────────────────────────────────────────────
        public async Task<OperationResult<bool>> ResetPassword(int userId)
        {
            var companyId = _tenantService.GetCompanyId();
            var callerId = _tenantService.GetUserId();

            // Un usuario no puede resetear su propia contraseña desde aquí
            // Para eso existe AuthService.ChangePassword / ForgotPassword
            if (userId == callerId)
                return OperationResult<bool>.ValidationError(
                    "Para cambiar tu propia contraseña usa el formulario de cambio de contraseña.",
                    ErrorKeys.CannotModifySelf);

            var user = await ResolveUserForCallerAsync(userId);
            if (user is null)
                return OperationResult<bool>.NotFound(
                    "Usuario no encontrado.",
                    ErrorKeys.UserNotFound);

            var temporaryPassword = PasswordValidator.GenerateTemporary();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
            user.MustChangePassword = true;
            await _context.SaveChangesAsync();

            // Cargar empresa para el email
            var company = await _context.Companies
                .AsNoTracking()
                .FirstAsync(c => c.Id == companyId);

            await _emailQueue.QueueTemporaryPasswordAsync(new TemporaryPasswordEmailDto(
                ToEmail: user.Email,
                FullName: user.FullName,
                CompanyName: company.Name,
                TemporaryPassword: temporaryPassword,
                LoginUrl: $"{AppUrl}/login"
            ), companyId);

            return OperationResult<bool>.Ok(true);
        }

        // ── HELPER PRIVADO ────────────────────────────────────────────

        /// <summary>
        /// Resuelve un usuario validando aislamiento multiempresa y restricciones de rol:
        ///   - CompanyAdmin: cualquier usuario de su empresa
        ///   - BranchAdmin:  solo usuarios de su propia Branch
        /// Devuelve null si no existe o el caller no tiene acceso.
        /// </summary>
        private async Task<AppUser?> ResolveUserForCallerAsync(int userId)
        {
            var companyId = _tenantService.GetCompanyId();
            var callerRole = _tenantService.GetUserRole();
            var callerBranchId = _tenantService.TryGetBranchId();

            var query = _context.Users
                .Where(u =>
                    u.Id == userId &&
                    u.CompanyId == companyId &&
                    !u.IsDeleted);

            // BranchAdmin solo puede ver/modificar usuarios de su Branch
            if (UserRoles.NeedsBranch(callerRole) && callerBranchId.HasValue)
                query = query.Where(u => u.BranchId == callerBranchId.Value);

            return await query.FirstOrDefaultAsync();
        }
    }
}