namespace ManejoArchivosF.Indexado
{
    public interface IArchivoIndexado
    {
        void CrearArchivo(string rutaArchivo, List<Registro> registros);
        void CargarArchivo(string rutaArchivo);
        Registro? BuscarRegistro(string id);
        void InsertarRegistro(Registro registro);
        void ModificarRegistro(string id, Registro nuevoRegistro);
        bool EliminarRegistro(string id);
        List<Registro> LeerTodosLosRegistros();
        int Reorganizar();
        Dictionary<string, EntradaIndice> ObtenerIndice();
    }
}