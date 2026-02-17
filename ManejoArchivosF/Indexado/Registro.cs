using System;

namespace ManejoArchivosF.Indexado
{
    /// <summary>
    /// Representa un registro de datos en el archivo indexado
    /// </summary>
    public class Registro
    {
        public string ID { get; set; } = "";
        public string Nombre { get; set; } = "";
        public int Edad { get; set; }
        public string Email { get; set; } = "";
        public bool Activo { get; set; } = true; // Indica si el registro está activo o eliminado lógicamente

        public override string ToString()
        {
            return $"ID: {ID}, Nombre: {Nombre}, Edad: {Edad}, Email: {Email}, Activo: {Activo}";
        }
    }
}