using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Entidad base de la que heredan todas las entidades del sistema.
    ///
    /// Auditoría automática — el DbContext llena estos campos en SaveChanges:
    ///   CreatedAt      → UTC al momento de inserción. Nunca se modifica después.
    ///   ModifiedAt     → UTC al momento de última modificación. Null si nunca fue modificado.
    ///   CreatedUserId  → UserId del JWT al momento de inserción. Null en operaciones públicas.
    ///   ModifiedUserId → UserId del JWT al momento de última modificación. Null si nunca fue modificado.
    ///
    /// Los campos de usuario son nullable porque:
    ///   - El seed de datos no tiene usuario autenticado.
    ///   - Endpoints públicos (menú, reservas anónimas) no tienen JWT.
    ///   - El SuperAdmin puede operar sin CompanyId asociado.
    /// </summary>
    public abstract class BaseEntity
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Fecha UTC de creación. Asignada por DbContext en inserción.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Fecha UTC de última modificación. Null si nunca fue modificado.</summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>UserId que creó el registro. Null en contextos públicos o seed.</summary>
        public int? CreatedUserId { get; set; }

        /// <summary>UserId que realizó la última modificación. Null si nunca fue modificado.</summary>
        public int? ModifiedUserId { get; set; }
    }
}