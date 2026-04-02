using AppCore.Application.Common;
using DigiMenuAPI.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.UnitTests.Controllers;

/// <summary>
/// Tests de BaseController.HandleResult — verifica que el mapeo de
/// OperationResult a HTTP status codes sea exhaustivo y correcto.
/// Se usa una subclase concreta TestableController para exponer HandleResult.
/// </summary>
[Trait("Category", "Unit")]
public sealed class BaseControllerTests
{
    private readonly TestableController _controller;

    public BaseControllerTests()
    {
        _controller = new TestableController();
        // Configurar HttpContext mínimo para que ControllerBase funcione
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // ── Éxito ─────────────────────────────────────────────────────────────

    [Fact]
    public void HandleResult_SuccessWithData_Returns200Ok()
    {
        var result = OperationResult<string>.Ok("test data");

        var actionResult = _controller.Expose(result);

        actionResult.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public void HandleResult_SuccessWithNullData_Returns204NoContent()
    {
        var result = OperationResult<string?>.Ok(null);

        var actionResult = _controller.Expose(result);

        actionResult.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public void HandleResult_SuccessBoolTrue_Returns200WithData()
    {
        var result = OperationResult<bool>.Ok(true);

        var actionResult = _controller.Expose(result);

        actionResult.Should().BeOfType<OkObjectResult>();
    }

    // ── Null result ───────────────────────────────────────────────────────

    [Fact]
    public void HandleResult_NullResult_Returns404NotFound()
    {
        var actionResult = _controller.Expose<string>(null!);

        actionResult.Should().BeOfType<NotFoundObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    // ── Errores ───────────────────────────────────────────────────────────

    [Fact]
    public void HandleResult_GeneralError_Returns400BadRequest()
    {
        var result = OperationResult<bool>.Fail("error genérico");

        var actionResult = _controller.Expose(result);

        actionResult.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void HandleResult_NotFound_Returns404()
    {
        var result = OperationResult<bool>.NotFound("no encontrado", ErrorKeys.CategoryNotFound);

        var actionResult = _controller.Expose(result);

        actionResult.Should().BeOfType<NotFoundObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void HandleResult_Forbidden_Returns403()
    {
        var result = OperationResult<bool>.Forbidden("sin permiso", ErrorKeys.Forbidden);

        var actionResult = _controller.Expose(result);

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void HandleResult_ModuleRequired_Returns403()
    {
        var result = OperationResult<bool>.ModuleRequired(ModuleCodes.Reservations);

        var actionResult = _controller.Expose(result);

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void HandleResult_Conflict_Returns409()
    {
        var result = OperationResult<bool>.Conflict("ya existe", ErrorKeys.CategoryAlreadyExists);

        var actionResult = _controller.Expose(result);

        actionResult.Should().BeOfType<ConflictObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void HandleResult_ValidationError_Returns422()
    {
        var result = OperationResult<bool>.ValidationError("campo inválido", ErrorKeys.ValidationFailed);

        var actionResult = _controller.Expose(result);

        actionResult.Should().BeOfType<UnprocessableEntityObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    // ── Verificar que el body de error contiene los campos correctos ───────

    [Fact]
    public void HandleResult_ErrorBody_ContainsErrorKeyAndMessage()
    {
        var result = OperationResult<bool>.NotFound(
            "Categoría no encontrada.",
            ErrorKeys.CategoryNotFound);

        var actionResult = _controller.Expose(result) as NotFoundObjectResult;
        var body = actionResult!.Value!;

        // El body es un tipo anónimo — verificar via reflexión
        var bodyType = body.GetType();
        bodyType.GetProperty("ErrorKey")!.GetValue(body)
            .Should().Be(ErrorKeys.CategoryNotFound);
        bodyType.GetProperty("Message")!.GetValue(body)
            .Should().Be("Categoría no encontrada.");
        bodyType.GetProperty("Success")!.GetValue(body)
            .Should().Be(false);
    }
}

/// <summary>Subclase concreta que expone el método protegido HandleResult para tests.</summary>
public sealed class TestableController : BaseController
{
    public ActionResult Expose<T>(OperationResult<T> result) => HandleResult(result);
}
