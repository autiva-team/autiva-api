using Autiva.Application.Services;

namespace Autiva.Tests;

public class ValidadorContrasenaTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validar_ContrasenaVacia_RetornaErrorObligatoria(string? contrasenaInvalida)
    {
        // Act
        var errores = ValidadorContrasena.Validar(contrasenaInvalida!);

        // Assert
        Assert.Contains(errores, e => e.Contains("obligatoria"));
    }

    [Fact]
    public void Validar_ContrasenaMenorA8Caracteres_RetornaErrorLongitudMinima()
    {
        // Arrange
        var contrasena = "Abc123!";

        // Act
        var errores = ValidadorContrasena.Validar(contrasena);

        // Assert
        Assert.Contains(errores, e => e.Contains("al menos 8 caracteres"));
    }

    [Fact]
    public void Validar_ContrasenaMayorA72Caracteres_RetornaErrorLongitudMaxima()
    {
        // Arrange
        var contrasena = new string('A', 73);

        // Act
        var errores = ValidadorContrasena.Validar(contrasena);

        // Assert
        Assert.Contains(errores, e => e.Contains("no puede superar los 72 caracteres"));
    }

    [Fact]
    public void Validar_ContrasenaValida_RetornaListaVacia()
    {
        // Arrange
        var contrasena = "Prueba123!";

        // Act
        var errores = ValidadorContrasena.Validar(contrasena);

        // Assert
        Assert.Empty(errores);
    }
}
