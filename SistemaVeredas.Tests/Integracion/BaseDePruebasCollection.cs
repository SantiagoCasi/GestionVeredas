namespace SistemaVeredas.Tests.Integracion
{
    // Todas las pruebas de integración comparten la base: van en una sola colección y corren de a una.
    [CollectionDefinition("Base de pruebas")]
    public class BaseDePruebasCollection : ICollectionFixture<AppFactory> { }
}
