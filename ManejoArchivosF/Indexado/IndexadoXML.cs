using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ManejoArchivosF.Indexado
{
    /// <summary>
    /// Implementación de archivo indexado usando formato XML
    /// </summary>
    public class IndexadoXML : IArchivoIndexado
    {
        private string rutaArchivoDatos = "";
        private string rutaArchivoIndice = "";
        private Dictionary<string, EntradaIndice> indice;

        public IndexadoXML()
        {
            indice = new Dictionary<string, EntradaIndice>();
        }

        public void CrearArchivo(string rutaArchivo, List<Registro> registros)
        {
            rutaArchivoDatos = rutaArchivo;
            rutaArchivoIndice = rutaArchivo + ".idx.xml";
            indice.Clear();

            // Verificar que no haya IDs duplicados
            var idsUnicos = registros.Select(r => r.ID).Distinct().ToList();
            if (idsUnicos.Count != registros.Count)
            {
                throw new Exception("Hay IDs duplicados en los registros");
            }

            // Crear documento XML
            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Registros")
            );

            XElement root = doc.Root!;
            long posicion = 0;

            foreach (var registro in registros)
            {
                XElement registroElement = new XElement("Registro",
                    new XElement("ID", registro.ID),
                    new XElement("Nombre", registro.Nombre),
                    new XElement("Edad", registro.Edad),
                    new XElement("Email", registro.Email),
                    new XElement("Activo", registro.Activo ? "1" : "0")
                );

                root.Add(registroElement);

                // Agregar entrada al índice (posición = índice del elemento en la colección)
                indice[registro.ID] = new EntradaIndice
                {
                    Clave = registro.ID,
                    Posicion = posicion,
                    Activo = true
                };

                posicion++;
            }

            // Guardar documento
            doc.Save(rutaArchivoDatos);

            // Guardar índice
            GuardarIndice();
        }

        public void CargarArchivo(string rutaArchivo)
        {
            rutaArchivoDatos = rutaArchivo;
            rutaArchivoIndice = rutaArchivo + ".idx.xml";

            if (!File.Exists(rutaArchivoDatos))
                throw new FileNotFoundException($"No se encuentra el archivo de datos: {rutaArchivoDatos}");

            if (!File.Exists(rutaArchivoIndice))
                throw new FileNotFoundException($"No se encuentra el archivo de índice: {rutaArchivoIndice}");

            // Cargar índice
            CargarIndice();
        }

        public Registro? BuscarRegistro(string id)
        {
            if (!indice.ContainsKey(id))
                return null;

            EntradaIndice entrada = indice[id];

            if (!entrada.Activo)
                return null;

            // Cargar documento XML
            XDocument doc = XDocument.Load(rutaArchivoDatos);
            XElement? root = doc.Root;

            if (root == null)
                return null;

            // Buscar el elemento en la posición indicada
            var elementos = root.Elements("Registro").ToList();

            if (entrada.Posicion >= elementos.Count)
                return null;

            XElement registroElement = elementos[(int)entrada.Posicion];
            Registro registro = ParsearElementoRegistro(registroElement);

            return registro.Activo ? registro : null;
        }

        public void InsertarRegistro(Registro registro)
        {
            if (indice.ContainsKey(registro.ID))
                throw new Exception($"Ya existe un registro con ID: {registro.ID}");

            // Cargar documento XML
            XDocument doc = XDocument.Load(rutaArchivoDatos);
            XElement? root = doc.Root;

            if (root == null)
                throw new Exception("El documento XML no tiene un elemento raíz válido");

            // Obtener la posición actual (número de elementos)
            long posicion = root.Elements("Registro").Count();

            // Crear nuevo elemento
            XElement nuevoRegistroElement = new XElement("Registro",
                new XElement("ID", registro.ID),
                new XElement("Nombre", registro.Nombre),
                new XElement("Edad", registro.Edad),
                new XElement("Email", registro.Email),
                new XElement("Activo", registro.Activo ? "1" : "0")
            );

            root.Add(nuevoRegistroElement);

            // Guardar documento
            doc.Save(rutaArchivoDatos);

            // Agregar entrada al índice
            indice[registro.ID] = new EntradaIndice
            {
                Clave = registro.ID,
                Posicion = posicion,
                Activo = true
            };

            // Guardar índice actualizado
            GuardarIndice();
        }

        public void ModificarRegistro(string id, Registro nuevoRegistro)
        {
            if (!indice.ContainsKey(id))
                throw new Exception($"No existe un registro con ID: {id}");

            EntradaIndice entrada = indice[id];

            if (!entrada.Activo)
                throw new Exception($"El registro con ID {id} está eliminado");

            nuevoRegistro.ID = id;
            nuevoRegistro.Activo = true;

            // Cargar documento XML
            XDocument doc = XDocument.Load(rutaArchivoDatos);
            XElement? root = doc.Root;

            if (root == null)
                return;

            // Buscar el elemento en la posición indicada
            var elementos = root.Elements("Registro").ToList();

            if (entrada.Posicion < elementos.Count)
            {
                XElement registroElement = elementos[(int)entrada.Posicion];

                // Actualizar valores
                registroElement.Element("ID")!.Value = nuevoRegistro.ID;
                registroElement.Element("Nombre")!.Value = nuevoRegistro.Nombre;
                registroElement.Element("Edad")!.Value = nuevoRegistro.Edad.ToString();
                registroElement.Element("Email")!.Value = nuevoRegistro.Email;
                registroElement.Element("Activo")!.Value = "1";
            }

            // Guardar documento
            doc.Save(rutaArchivoDatos);
        }

        public bool EliminarRegistro(string id)
        {
            if (!indice.ContainsKey(id))
                return false;

            EntradaIndice entrada = indice[id];

            if (!entrada.Activo)
                return false;

            // Marcar como inactivo en el índice
            entrada.Activo = false;

            // Cargar documento XML
            XDocument doc = XDocument.Load(rutaArchivoDatos);
            XElement? root = doc.Root;

            if (root == null)
                return false;

            // Buscar el elemento en la posición indicada
            var elementos = root.Elements("Registro").ToList();

            if (entrada.Posicion < elementos.Count)
            {
                XElement registroElement = elementos[(int)entrada.Posicion];
                registroElement.Element("Activo")!.Value = "0";
            }

            // Guardar documento
            doc.Save(rutaArchivoDatos);

            // Guardar índice actualizado
            GuardarIndice();

            return true;
        }

        public List<Registro> LeerTodosLosRegistros()
        {
            List<Registro> registros = new List<Registro>();

            // Cargar documento XML
            XDocument doc = XDocument.Load(rutaArchivoDatos);
            XElement? root = doc.Root;

            if (root == null)
                return registros;

            foreach (XElement registroElement in root.Elements("Registro"))
            {
                Registro registro = ParsearElementoRegistro(registroElement);

                if (registro.Activo)
                {
                    registros.Add(registro);
                }
            }

            return registros;
        }

        public int Reorganizar()
        {
            List<Registro> registrosActivos = LeerTodosLosRegistros();
            int registrosEliminados = indice.Count - registrosActivos.Count;

            // Crear nuevo documento XML
            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Registros")
            );

            XElement root = doc.Root!;
            indice.Clear();
            long posicion = 0;

            foreach (var registro in registrosActivos)
            {
                XElement registroElement = new XElement("Registro",
                    new XElement("ID", registro.ID),
                    new XElement("Nombre", registro.Nombre),
                    new XElement("Edad", registro.Edad),
                    new XElement("Email", registro.Email),
                    new XElement("Activo", "1")
                );

                root.Add(registroElement);

                indice[registro.ID] = new EntradaIndice
                {
                    Clave = registro.ID,
                    Posicion = posicion,
                    Activo = true
                };

                posicion++;
            }

            // Guardar documento
            doc.Save(rutaArchivoDatos);

            // Guardar índice reorganizado
            GuardarIndice();

            return registrosEliminados;
        }

        public Dictionary<string, EntradaIndice> ObtenerIndice()
        {
            return new Dictionary<string, EntradaIndice>(indice);
        }

        private void GuardarIndice()
        {
            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Indice")
            );

            XElement root = doc.Root!;

            foreach (var entrada in indice.Values)
            {
                XElement entradaElement = new XElement("Entrada",
                    new XElement("Clave", entrada.Clave),
                    new XElement("Posicion", entrada.Posicion),
                    new XElement("Activo", entrada.Activo ? "1" : "0")
                );

                root.Add(entradaElement);
            }

            doc.Save(rutaArchivoIndice);
        }

        private void CargarIndice()
        {
            indice.Clear();

            XDocument doc = XDocument.Load(rutaArchivoIndice);
            XElement? root = doc.Root;

            if (root == null)
                return;

            foreach (XElement entradaElement in root.Elements("Entrada"))
            {
                EntradaIndice entrada = new EntradaIndice
                {
                    Clave = entradaElement.Element("Clave")!.Value,
                    Posicion = long.Parse(entradaElement.Element("Posicion")!.Value),
                    Activo = entradaElement.Element("Activo")!.Value == "1"
                };

                indice[entrada.Clave] = entrada;
            }
        }

        private Registro ParsearElementoRegistro(XElement registroElement)
        {
            return new Registro
            {
                ID = registroElement.Element("ID")!.Value,
                Nombre = registroElement.Element("Nombre")!.Value,
                Edad = int.Parse(registroElement.Element("Edad")!.Value),
                Email = registroElement.Element("Email")!.Value,
                Activo = registroElement.Element("Activo")!.Value == "1"
            };
        }
    }
}
