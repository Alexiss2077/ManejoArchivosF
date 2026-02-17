namespace ManejoArchivosF.Indexado
{
    public class EntradaIndice
    {
        public string Clave { get; set; } = "";
        public long Posicion { get; set; }
        public bool Activo { get; set; } = true;
        public override string ToString() => $"Clave: {Clave}, Posición: {Posicion}, Activo: {Activo}";
    }
}