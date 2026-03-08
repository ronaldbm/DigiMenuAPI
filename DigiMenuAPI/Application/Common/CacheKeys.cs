namespace DigiMenuAPI.Application.Common
{
    /// <summary>
    /// Centraliza todos los nombres y patrones de claves/tags de cache del sistema.
    ///
    /// Convención de tags de OutputCache (invalidación por tenant):
    ///   menu-branch:{branchId}   → invalida el menú público de una Branch específica.
    ///                              Usar cuando cambian: Setting, FooterLinks, BranchProducts.
    ///   menu-company:{companyId} → invalida todos los menús de una Company.
    ///                              Usar cuando cambian: Categories, Products, Tags (catálogo global).
    ///
    /// Convención de claves de IMemoryCache:
    ///   module:{companyId}:{moduleCode} → resultado de verificación de módulo activo.
    /// </summary>
    public static class CacheKeys
    {
        // ── OutputCache tags ──────────────────────────────────────────

        /// <summary>
        /// Tag del menú público de una Branch específica.
        /// Invalida solo esa Branch — no afecta otras Branches ni otras Companies.
        /// </summary>
        public static string MenuBranch(int branchId)
            => $"menu-branch:{branchId}";

        /// <summary>
        /// Tag del catálogo global de una Company.
        /// Una Company puede tener múltiples Branches — este tag invalida todas.
        /// Usar cuando cambia algo que afecta a todas las Branches: Category, Product, Tag.
        /// </summary>
        public static string MenuCompany(int companyId)
            => $"menu-company:{companyId}";

        // ── IMemoryCache keys ─────────────────────────────────────────

        /// <summary>
        /// Clave para el resultado de verificación de módulo activo por empresa.
        /// Usado por ModuleGuard para evitar queries repetidas al verificar módulos.
        /// </summary>
        public static string Module(int companyId, string moduleCode)
            => $"module:{companyId}:{moduleCode}";
    }
}