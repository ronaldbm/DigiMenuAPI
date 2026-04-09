using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Auth
{
    /// <summary>
    /// Body del endpoint POST /api/auth/impersonate/exchange.
    /// DigiMenuWeb envía el token que recibió en el URL fragment.
    /// </summary>
    public record ImpersonationExchangeDto(
        [Required] string Token
    );
}
