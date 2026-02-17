using ManejoArchivosF.Indexado;
using Microsoft.Win32;
using System.Collections.Generic;

namespace ManejoArchivosF.Indexado
{
    /// <summary>
    /// Interfaz para implementaciones de archivos indexados en diferentes formatos
    /// </summary>
    public interface IArchivoIndexado
    {
        /// <summary>
        /// Crea un nuevo archivo indexado con los registros proporcionados
        /// </summary>
        void CrearArchivo(string rutaArchivo, List<Registro> registros);

        /// <summary>
        /// Carga un archivo indexado existente
        /// </summary>
        void CargarArchivo(string rutaArchivo);

        /// <summary>
        /// Busca un registro por su ID usando el índice
        /// </summary>
        Registro? BuscarRegistro(string id);

        /// <summary>
        /// Inserta un nuevo registro en el archivo
        /// </summary>
        void InsertarRegistro(Registro registro);

        /// <summary>
        /// Modifica un registro existente
        /// </summary>
        void ModificarRegistro(string id, Registro nuevoRegistro);

        /// <summary>
        /// Elimina lógicamente un registro
        /// </summary>
        bool EliminarRegistro(string id);

        /// <summary>
        /// Lee todos los registros activos del archivo
        /// </summary>
        List<Registro> LeerTodosLosRegistros();

        /// <summary>
        /// Reorganiza el archivo eliminando físicamente los registros marcados como eliminados
        /// </summary>
        int Reorganizar();

        /// <summary>
        /// Obtiene una copia del índice actual
        /// </summary>
        Dictionary<string, EntradaIndice> ObtenerIndice();
    }
}
