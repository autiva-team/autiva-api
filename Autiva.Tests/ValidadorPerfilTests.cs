using Autiva.Application.Services;

namespace Autiva.Tests;

public class ValidadorPerfilTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validar_NombreVacio_RetornaErrorObligatorio(string? nombreInvalido)
    {
        // Act
        var errores = ValidadorPerfil.Validar(nombreInvalido!, "prueba@autiva.test");

        // Assert
        Assert.Contains(errores, e => e.Contains("nombre es obligatorio"));
    }

    [Fact]
    public void Validar_NombreMayorA120Caracteres_RetornaErrorLongitudMaxima()
    {
        // Arrange
        var nombre = new string('A', 121);

        // Act
        var errores = ValidadorPerfil.Validar(nombre, "prueba@autiva.test");

        // Assert
        Assert.Contains(errores, e => e.Contains("no puede superar los 120 caracteres"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validar_CorreoVacio_RetornaErrorObligatorio(string? correoInvalido)
    {
        // Act
        var errores = ValidadorPerfil.Validar("Prueba", correoInvalido!);

        // Assert
        Assert.Contains(errores, e => e.Contains("correo es obligatorio"));
    }

    [Fact]
    public void Validar_CorreoMayorA150Caracteres_RetornaErrorLongitudMaxima()
    {
        // Arrange
        var correo = new string('a', 145) + "@test.com";

        // Act
        var errores = ValidadorPerfil.Validar("Prueba", correo);

        // Assert
        Assert.Contains(errores, e => e.Contains("no puede superar los 150 caracteres"));
    }

    [Fact]
    public void Validar_CorreoFormatoInvalido_RetornaErrorFormato()
    {
        // Act
        var errores = ValidadorPerfil.Validar("Prueba", "correo-sin-formato");

        // Assert
        Assert.Contains(errores, e => e.Contains("formato válido"));
    }

    [Fact]
    public void Validar_NombreYCorreoValidos_RetornaListaVacia()
    {
        // Act
        var errores = ValidadorPerfil.Validar("Prueba HU030", "prueba.hu030@autiva.test");

        // Assert
        Assert.Empty(errores);
    }
}
