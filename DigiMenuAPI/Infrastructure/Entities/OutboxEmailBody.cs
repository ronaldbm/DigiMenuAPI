using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Cuerpo HTML del correo. Separado de OutboxEmail para que los
    /// queries del processor no arrastren megas de HTML innecesariamente.
    ///
    /// Se carga únicamente en el momento del envío mediante JOIN explícito.
    /// Relación 1:1 con OutboxEmail — shared primary key.
    /// </summary>
    public class OutboxEmailBody
    {
        /// <summary>Mismo Id que OutboxEmail — shared primary key.</summary>
        public int OutboxEmailId { get; set; }
        public OutboxEmail OutboxEmail { get; set; } = null!;

        /// <summary>
        /// HTML completo renderizado en el momento del encolado.
        /// Refleja exactamente lo que se enviará/envió al destinatario.
        /// </summary>
        [Required]
        public string HtmlBody { get; set; } = null!;
    }
}