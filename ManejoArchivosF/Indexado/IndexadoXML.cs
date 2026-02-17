using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ManejoArchivosF.Indexado
{
    /// <summary>
    /// Organización Indexada en XML.
    /// Archivo de datos XML + índice separado (.idx.xml).
    /// La "posición" es el índice del nodo dentro del elemento raíz.
    /// </summary>
    public class IndexadoXML : IArchivoIndexado
    {
        private string rutaArchivoDatos = "";
        private string rutaArchivoIndice = "";
        private Dictionary<string, EntradaIndice> indice = new();

        public void CrearArchivo(string rutaArchivo, List<Registro> registros)
        {
            rutaArchivoDatos = rutaArchivo;
            rutaArchivoIndice = rutaArchivo + ".idx.xml";
            indice.Clear();

            var ids = registros.Select(r => r.ID).Distinct().ToList();
            if (ids.Count != registros.Count) throw new Exception("Hay IDs duplicados en los registros.");

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("Registros"));
            long pos = 0;
            foreach (var r in registros)
            {
                doc.Root!.Add(ElementoDeRegistro(r));
                indice[r.ID] = new EntradaIndice { Clave = r.ID, Posicion = pos++, Activo = true };
            }
            doc.Save(rutaArchivoDatos);
            GuardarIndice();
        }

        public void CargarArchivo(string rutaArchivo)
        {
            rutaArchivoDatos = rutaArchivo;
            rutaArchivoIndice = rutaArchivo + ".idx.xml";
            if (!File.Exists(rutaArchivoDatos)) throw new FileNotFoundException("No se encuentra el archivo de datos.");
            if (!File.Exists(rutaArchivoIndice)) throw new FileNotFoundException("No se encuentra el archivo de índice.");
            CargarIndice();
        }

        public Registro? BuscarRegistro(string id)
        {
            if (!indice.TryGetValue(id, out var e) || !e.Activo) return null;
            var elementos = XDocument.Load(rutaArchivoDatos).Root!.Elements("Registro").ToList();
            if (e.Posicion >= elementos.Count) return null;
            var r = RegistroDeElemento(elementos[(int)e.Posicion]);
            return r.Activo ? r : null;
        }

        public void InsertarRegistro(Registro registro)
        {
            if (indice.ContainsKey(registro.ID)) throw new Exception($"Ya existe un registro con ID: {registro.ID}");
            var doc = XDocument.Load(rutaArchivoDatos);
            long pos = doc.Root!.Elements("Registro").Count();
            doc.Root.Add(ElementoDeRegistro(registro));
            doc.Save(rutaArchivoDatos);
            indice[registro.ID] = new EntradaIndice { Clave = registro.ID, Posicion = pos, Activo = true };
            GuardarIndice();
        }

        public void ModificarRegistro(string id, Registro nuevo)
        {
            if (!indice.TryGetValue(id, out var e)) throw new Exception($"No existe un registro con ID: {id}");
            if (!e.Activo) throw new Exception($"El registro {id} está eliminado.");
            nuevo.ID = id; nuevo.Activo = true;
            var doc = XDocument.Load(rutaArchivoDatos);
            var elementos = doc.Root!.Elements("Registro").ToList();
            if (e.Posicion < elementos.Count)
            {
                var el = elementos[(int)e.Posicion];
                el.Element("ID")!.Value = nuevo.ID;
                el.Element("Nombre")!.Value = nuevo.Nombre;
                el.Element("Edad")!.Value = nuevo.Edad.ToString();
                el.Element("Email")!.Value = nuevo.Email;
                el.Element("Activo")!.Value = "1";
            }
            doc.Save(rutaArchivoDatos);
        }

        public bool EliminarRegistro(string id)
        {
            if (!indice.TryGetValue(id, out var e) || !e.Activo) return false;
            e.Activo = false;
            var doc = XDocument.Load(rutaArchivoDatos);
            var elementos = doc.Root!.Elements("Registro").ToList();
            if (e.Posicion < elementos.Count)
                elementos[(int)e.Posicion].Element("Activo")!.Value = "0";
            doc.Save(rutaArchivoDatos);
            GuardarIndice();
            return true;
        }

        public List<Registro> LeerTodosLosRegistros()
        {
            return XDocument.Load(rutaArchivoDatos).Root!
                .Elements("Registro")
                .Select(RegistroDeElemento)
                .Where(r => r.Activo)
                .ToList();
        }

        public int Reorganizar()
        {
            var activos = LeerTodosLosRegistros();
            int eliminados = indice.Count - activos.Count;
            indice.Clear();
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("Registros"));
            long pos = 0;
            foreach (var r in activos)
            {
                doc.Root!.Add(ElementoDeRegistro(r));
                indice[r.ID] = new EntradaIndice { Clave = r.ID, Posicion = pos++, Activo = true };
            }
            doc.Save(rutaArchivoDatos);
            GuardarIndice();
            return eliminados;
        }

        public Dictionary<string, EntradaIndice> ObtenerIndice() => new(indice);

        // ── Privados
        private void GuardarIndice()
        {
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("Indice"));
            foreach (var e in indice.Values)
                doc.Root!.Add(new XElement("Entrada",
                    new XElement("Clave", e.Clave),
                    new XElement("Posicion", e.Posicion),
                    new XElement("Activo", e.Activo ? "1" : "0")));
            doc.Save(rutaArchivoIndice);
        }

        private void CargarIndice()
        {
            indice.Clear();
            foreach (var el in XDocument.Load(rutaArchivoIndice).Root!.Elements("Entrada"))
                indice[el.Element("Clave")!.Value] = new EntradaIndice
                {
                    Clave = el.Element("Clave")!.Value,
                    Posicion = long.Parse(el.Element("Posicion")!.Value),
                    Activo = el.Element("Activo")!.Value == "1"
                };
        }

        private static XElement ElementoDeRegistro(Registro r) =>
            new("Registro",
                new XElement("ID", r.ID),
                new XElement("Nombre", r.Nombre),
                new XElement("Edad", r.Edad),
                new XElement("Email", r.Email),
                new XElement("Activo", r.Activo ? "1" : "0"));

        private static Registro RegistroDeElemento(XElement el) => new()
        {
            ID = el.Element("ID")!.Value,
            Nombre = el.Element("Nombre")!.Value,
            Edad = int.Parse(el.Element("Edad")!.Value),
            Email = el.Element("Email")!.Value,
            Activo = el.Element("Activo")!.Value == "1"
        };
    }
}