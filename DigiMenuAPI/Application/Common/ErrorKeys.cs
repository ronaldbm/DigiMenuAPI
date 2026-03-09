namespace DigiMenuAPI.Application.Common
{
    /// <summary>
    /// Claves únicas de error para internacionalización (i18n).
    ///
    /// Estas keys se incluyen en el OperationResult junto al mensaje en español.
    /// El frontend las usa para traducir al idioma del usuario.
    /// Si el frontend no tiene traducción para una key, muestra el mensaje
    /// en español como fallback.
    ///
    /// Convención: ENTIDAD_DESCRIPCION en SNAKE_UPPER_CASE.
    ///
    /// Ejemplo de respuesta del API:
    /// {
    ///   "success": false,
    ///   "errorCode": "NotFound",
    ///   "errorKey": "CATEGORY_NOT_FOUND",
    ///   "message": "La categoría no fue encontrada."
    /// }
    /// </summary>
    public static class ErrorKeys
    {
        // ── Genéricos ─────────────────────────────────────────────────
        public const string UnexpectedError = "UNEXPECTED_ERROR";
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Forbidden = "FORBIDDEN";
        public const string ModuleRequired = "MODULE_REQUIRED";
        public const string IdMismatch = "ID_MISMATCH";

        // ── Auth ──────────────────────────────────────────────────────
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string AccountDisabled = "ACCOUNT_DISABLED";
        public const string CompanyDisabled = "COMPANY_DISABLED";
        public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
        public const string SlugAlreadyExists = "SLUG_ALREADY_EXISTS";
        public const string CannotAssignSuperAdmin = "CANNOT_ASSIGN_SUPER_ADMIN";
        public const string BranchRequiredForRole = "BRANCH_REQUIRED_FOR_ROLE";

        public const string WeakPassword = "WEAK_PASSWORD";
        public const string IncorrectPassword = "INCORRECT_PASSWORD";
        public const string MustChangePassword = "MUST_CHANGE_PASSWORD";
        public const string PasswordChangeFailed = "PASSWORD_CHANGE_FAILED";

        // ── Company ───────────────────────────────────────────────────
        public const string CompanyNotFound = "COMPANY_NOT_FOUND";

        // ── Branch ────────────────────────────────────────────────────
        public const string BranchNotFound = "BRANCH_NOT_FOUND";
        public const string BranchNotOwned = "BRANCH_NOT_OWNED";
        public const string BranchConfigMissing = "BRANCH_CONFIG_MISSING";

        // ── BranchInfo ────────────────────────────────────────────────
        public const string BranchInfoNotFound = "BRANCH_INFO_NOT_FOUND";
        public const string InvalidImageFormat = "INVALID_IMAGE_FORMAT";

        // ── BranchTheme ───────────────────────────────────────────────
        public const string BranchThemeNotFound = "BRANCH_THEME_NOT_FOUND";

        // ── BranchLocale ──────────────────────────────────────────────
        public const string BranchLocaleNotFound = "BRANCH_LOCALE_NOT_FOUND";

        // ── BranchSeo ─────────────────────────────────────────────────
        public const string BranchSeoNotFound = "BRANCH_SEO_NOT_FOUND";

        // ── BranchReservationForm ─────────────────────────────────────
        public const string ReservationFormNotFound = "RESERVATION_FORM_NOT_FOUND";
        public const string FormFieldRequiredButHidden = "FORM_FIELD_REQUIRED_BUT_HIDDEN";

        // ── Category ──────────────────────────────────────────────────
        public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
        public const string CategoryAlreadyExists = "CATEGORY_ALREADY_EXISTS";

        // ── Product ───────────────────────────────────────────────────
        public const string ProductNotFound = "PRODUCT_NOT_FOUND";
        public const string ProductAlreadyExists = "PRODUCT_ALREADY_EXISTS";

        // ── Tag ───────────────────────────────────────────────────────
        public const string TagNotFound = "TAG_NOT_FOUND";
        public const string TagNotOwned = "TAG_NOT_OWNED";

        // ── FooterLink ────────────────────────────────────────────────
        public const string FooterLinkNotFound = "FOOTER_LINK_NOT_FOUND";
        public const string FooterLinkNotOwned = "FOOTER_LINK_NOT_OWNED";

        // ── Reservation ───────────────────────────────────────────────
        public const string ReservationNotFound = "RESERVATION_NOT_FOUND";
        public const string ReservationNotOwned = "RESERVATION_NOT_OWNED";

        // ── Menu público ──────────────────────────────────────────────
        public const string MenuNotFound = "MENU_NOT_FOUND";
        public const string MenuUnavailable = "MENU_UNAVAILABLE";
    }
}