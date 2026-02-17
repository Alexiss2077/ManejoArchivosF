using System;

namespace ManejoArchivosF.Indexado
{
    /// <summary>
    /// Representa una entrada en el índice del archivo
    /// </summary>
    public class EntradaIndice
    {
        public string Clave { get; set; } = ""; // ID del registro
        public long Posicion { get; set; }       // Posición del registro en el archivo de datos (número de línea para TXT/CSV, o nodo para XML)
        public bool Activo { get; set; } = true; // Indica si el registro está activo

        public override string ToString()
        {
            return $"Clave: {Clave}, Posición: {Posicion}, Activo: {Activo}";
        }
    }
}
