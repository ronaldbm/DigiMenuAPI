namespace AppCore.Application.Common
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
        public const string InvalidResetToken = "INVALID_RESET_TOKEN";
        public const string PasswordChangeFailed = "PASSWORD_CHANGE_FAILED";

        // ── AppUser ───────────────────────────────────────────────────
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string UserLimitReached = "USER_LIMIT_REACHED";
        public const string CannotModifySelf = "CANNOT_MODIFY_SELF";

        // ── Company ───────────────────────────────────────────────────
        public const string CompanyNotFound = "COMPANY_NOT_FOUND";

        // ── Branch ────────────────────────────────────────────────────
        public const string BranchNotFound = "BRANCH_NOT_FOUND";
        public const string BranchNotOwned = "BRANCH_NOT_OWNED";
        public const string BranchConfigMissing = "BRANCH_CONFIG_MISSING";
        public const string BranchLimitReached = "BRANCH_LIMIT_REACHED";
        public const string BranchSlugAlreadyExists = "BRANCH_SLUG_ALREADY_EXISTS";
        public const string BranchInactive = "BRANCH_INACTIVE";
        public const string BranchHasActiveUsers = "BRANCH_HAS_ACTIVE_USERS";

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

        // ── Schedule ──────────────────────────────────────────────────
        public const string ScheduleNotFound = "SCHEDULE_NOT_FOUND";
        public const string InvalidScheduleDay = "INVALID_SCHEDULE_DAY";
        public const string ScheduleOpenTimeRequired = "SCHEDULE_OPEN_TIME_REQUIRED";
        public const string ScheduleCloseTimeRequired = "SCHEDULE_CLOSE_TIME_REQUIRED";
        public const string ScheduleCloseBeforeOpen = "SCHEDULE_CLOSE_BEFORE_OPEN";

        // ── SpecialDay ────────────────────────────────────────────────
        public const string SpecialDayNotFound = "SPECIAL_DAY_NOT_FOUND";
        public const string SpecialDayPastDate = "SPECIAL_DAY_PAST_DATE";
        public const string SpecialDayDuplicate = "SPECIAL_DAY_DUPLICATE";
        public const string SpecialDayHoursRequired = "SPECIAL_DAY_HOURS_REQUIRED";
        public const string SpecialDayCloseBeforeOpen = "SPECIAL_DAY_CLOSE_BEFORE_OPEN";

        // ── Reservation ───────────────────────────────────────────────
        public const string ReservationBranchClosed = "RESERVATION_BRANCH_CLOSED";
        public const string ReservationOutsideHours = "RESERVATION_OUTSIDE_HOURS";
        public const string ReservationTooCloseToClosing = "RESERVATION_TOO_CLOSE_TO_CLOSING";
        public const string ReservationCapacityExceeded = "RESERVATION_CAPACITY_EXCEEDED";
        public const string ReservationNotFound = "RESERVATION_NOT_FOUND";
        public const string ReservationNotOwned = "RESERVATION_NOT_OWNED";

        // ── Menu público ──────────────────────────────────────────────
        public const string MenuNotFound = "MENU_NOT_FOUND";
        public const string MenuUnavailable = "MENU_UNAVAILABLE";
    }
}
