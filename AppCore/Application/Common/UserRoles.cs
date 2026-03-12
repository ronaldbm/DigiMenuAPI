namespace AppCore.Application.Common
{
    /// <summary>
    /// Constantes de roles del sistema.
    ///
    /// Jerarquía:
    ///   SuperAdmin        (255) → acceso total a la plataforma. Sin Company ni Branch.
    ///   SuperAdminCompany (254) → acceso total a una Company específica. Sin Branch.
    ///   CompanyAdmin        (1) → gestiona su Company y todas sus Branches. Sin Branch.
    ///   BranchAdmin         (2) → gestiona solo su Branch asignada.
    ///   Staff               (3) → acceso operativo limitado a su Branch.
    ///
    /// Jerarquía de creación:
    ///   SuperAdmin        → puede crear SuperAdminCompany y CompanyAdmin
    ///   SuperAdminCompany → puede crear CompanyAdmin dentro de su Company
    ///   CompanyAdmin      → puede crear BranchAdmin
    ///   BranchAdmin       → puede crear Staff
    ///
    /// Regla de BranchId:
    ///   BranchId = null  → SuperAdmin, SuperAdminCompany, CompanyAdmin
    ///   BranchId requerido → BranchAdmin, Staff
    /// </summary>
    public static class UserRoles
    {
        public const byte SuperAdmin = 255;
        public const byte SuperAdminCompany = 254;
        public const byte CompanyAdmin = 1;
        public const byte BranchAdmin = 2;
        public const byte Staff = 3;

        /// <summary>
        /// Roles que requieren BranchId asignado.
        /// </summary>
        public static readonly IReadOnlySet<byte> RequireBranch =
            new HashSet<byte> { BranchAdmin, Staff };

        /// <summary>
        /// Roles con acceso de plataforma (sin restricción de Company).
        /// </summary>
        public static readonly IReadOnlySet<byte> PlatformLevel =
            new HashSet<byte> { SuperAdmin, SuperAdminCompany };

        /// <summary>
        /// Devuelve true si el rol tiene acceso de plataforma (SuperAdmin o SuperAdminCompany).
        /// </summary>
        public static bool IsPlatformLevel(byte role)
            => PlatformLevel.Contains(role);

        /// <summary>
        /// Devuelve true si el rol requiere BranchId asignado.
        /// </summary>
        public static bool NeedsBranch(byte role)
            => RequireBranch.Contains(role);

        /// <summary>
        /// Devuelve true si el rol creador puede asignar el rol destino.
        ///
        /// Regla: solo puedes crear roles de menor jerarquía que el tuyo.
        ///   SuperAdmin        → puede crear cualquier rol menor
        ///   SuperAdminCompany → puede crear CompanyAdmin, BranchAdmin, Staff
        ///   CompanyAdmin      → puede crear BranchAdmin, Staff
        ///   BranchAdmin       → puede crear Staff
        ///   Staff             → no puede crear usuarios
        /// </summary>
        public static bool CanAssign(byte creatorRole, byte targetRole)
            => creatorRole switch
            {
                SuperAdmin => targetRole != SuperAdmin,
                SuperAdminCompany => targetRole is CompanyAdmin or BranchAdmin or Staff,
                CompanyAdmin => targetRole is BranchAdmin or Staff,
                BranchAdmin => targetRole == Staff,
                _ => false
            };
    }
}
