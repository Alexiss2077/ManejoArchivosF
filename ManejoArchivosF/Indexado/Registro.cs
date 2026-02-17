namespace ManejoArchivosF.Indexado
{
    public class Registro
    {
        public string ID { get; set; } = "";
        public string Nombre { get; set; } = "";
        public int Edad { get; set; }
        public string Email { get; set; } = "";
        public bool Activo { get; set; } = true;
        public override string ToString() =>
            $"ID: {ID}, Nombre: {Nombre}, Edad: {Edad}, Email: {Email}, Activo: {Activo}";
    }
}