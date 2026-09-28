using System.Security.Cryptography; // Importa clases para criptografía, como SHA256 
using System.Text; // Importa clases para codificación de texto 

namespace SistemaVeredas.Services // Define espacio de nombres para servicios 
{
    public static class PasswordService // Clase estática para gestionar contraseñas
    {
        // Método para generar un hash SHA256 de una contraseña 
        public static string HashPassword(string password)
        {
            // Valida que la contraseña no sea nula o vacía 
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("La contraseña no puede estar vacía.");

            // Crea una instancia de SHA256 para hash 
            using var sha256 = SHA256.Create();

            // Convierte la contraseña a un arreglo de bytes UTF8 
            var bytes = Encoding.UTF8.GetBytes(password);

            // Calcula el hash de los bytes 
            var hash = sha256.ComputeHash(bytes);

            // Convierte el hash a una cadena Base64 para almacenamiento o comparación
            return Convert.ToBase64String(hash);
        }

        // Método para verificar si una contraseña coincide con un hash almacenado
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            // Genera el hash de la contraseña de entrada 
            var hashOfInput = HashPassword(password);

            // Compara el hash calculado con el hash almacenado y devuelve true si son iguales
            return hashOfInput == hashedPassword;
        }
    }
}