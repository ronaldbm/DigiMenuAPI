using AppCore.Application.Common;
using FluentAssertions;

namespace AppCore.UnitTests.Application.Common;

[Trait("Category", "Unit")]
public sealed class OperationResultTests
{
    // ── Ok ───────────────────────────────────────────────────────────────

    [Fact]
    public void Ok_WithData_SetsSuccessTrue()
    {
        var result = OperationResult<string>.Ok("hello");

        result.Success.Should().BeTrue();
        result.Data.Should().Be("hello");
        result.Message.Should().BeNull();
        result.ErrorCode.Should().Be(OperationResultError.General);
        result.ErrorKey.Should().BeNull();
    }

    [Fact]
    public void Ok_WithDataAndMessage_SetsMessage()
    {
        var result = OperationResult<int>.Ok(42, "operación exitosa");

        result.Success.Should().BeTrue();
        result.Data.Should().Be(42);
        result.Message.Should().Be("operación exitosa");
    }

    [Fact]
    public void Ok_WithNullData_StillSucceeds()
    {
        var result = OperationResult<string?>.Ok(null);

        result.Success.Should().BeTrue();
        result.Data.Should().BeNull();
    }

    // ── Fail genérico ─────────────────────────────────────────────────────

    [Fact]
    public void Fail_SetsSuccessFalse_WithGeneralErrorCode()
    {
        var result = OperationResult<string>.Fail("algo salió mal");

        result.Success.Should().BeFalse();
        result.Message.Should().Be("algo salió mal");
        result.ErrorCode.Should().Be(OperationResultError.General);
        result.ErrorKey.Should().BeNull();
        result.Data.Should().BeNull();
    }

    [Fact]
    public void Fail_WithErrorCodeAndKey_SetsAllFields()
    {
        var result = OperationResult<string>.Fail(
            "no encontrado",
            OperationResultError.NotFound,
            "CATEGORY_NOT_FOUND");

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be("CATEGORY_NOT_FOUND");
        result.Message.Should().Be("no encontrado");
    }

    // ── NotFound ──────────────────────────────────────────────────────────

    [Fact]
    public void NotFound_SetsNotFoundErrorCode_AndErrorKey()
    {
        var result = OperationResult<object>.NotFound(
            "Categoría no encontrada.",
            ErrorKeys.CategoryNotFound);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.CategoryNotFound);
        result.Message.Should().Be("Categoría no encontrada.");
    }

    // ── Forbidden ─────────────────────────────────────────────────────────

    [Fact]
    public void Forbidden_SetsForbiddenErrorCode_AndErrorKey()
    {
        var result = OperationResult<bool>.Forbidden(
            "Sin permiso.",
            ErrorKeys.Forbidden);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Forbidden);
        result.ErrorKey.Should().Be(ErrorKeys.Forbidden);
    }

    // ── Conflict ──────────────────────────────────────────────────────────

    [Fact]
    public void Conflict_SetsConflictErrorCode_AndErrorKey()
    {
        var result = OperationResult<bool>.Conflict(
            "Ya existe.",
            ErrorKeys.CategoryAlreadyExists);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Conflict);
        result.ErrorKey.Should().Be(ErrorKeys.CategoryAlreadyExists);
    }

    // ── ValidationError ───────────────────────────────────────────────────

    [Fact]
    public void ValidationError_SetsValidationErrorCode_AndErrorKey()
    {
        var result = OperationResult<bool>.ValidationError(
            "Campo inválido.",
            ErrorKeys.ValidationFailed);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.ValidationFailed);
    }

    // ── ModuleRequired ────────────────────────────────────────────────────

    [Fact]
    public void ModuleRequired_SetsModuleRequiredErrorCode()
    {
        var result = OperationResult<bool>.ModuleRequired(ModuleCodes.Reservations);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.ModuleRequired);
        result.ErrorKey.Should().Be(ErrorKeys.ModuleRequired);
        result.Message.Should().Contain(ModuleCodes.Reservations);
    }

    [Fact]
    public void ModuleRequired_IncludesModuleCodeInMessage()
    {
        var result = OperationResult<object>.ModuleRequired(ModuleCodes.AccountManagement);

        result.Message.Should().Contain(ModuleCodes.AccountManagement);
    }

    // ── Inmutabilidad del tipo ─────────────────────────────────────────────

    [Fact]
    public void Ok_AndFail_AreDistinctInstances()
    {
        var ok   = OperationResult<int>.Ok(1);
        var fail = OperationResult<int>.Fail("error");

        ok.Success.Should().BeTrue();
        fail.Success.Should().BeFalse();
    }
}
