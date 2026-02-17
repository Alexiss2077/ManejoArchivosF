using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ManejoArchivosF.Indexado
{
    /// <summary>
    /// Implementación de archivo indexado usando formato TXT (texto plano con delimitadores)
    /// Formato de datos: ID|Nombre|Edad|Email|Activo
    /// Formato de índice: Clave|Posicion|Activo
    /// </summary>
    public class IndexadoTXT : IArchivoIndexado
    {
        private string rutaArchivoDatos = "";
        private string rutaArchivoIndice = "";
        private Dictionary<string, EntradaIndice> indice;
        private const char DELIMITADOR = '|';

        public IndexadoTXT()
        {
            indice = new Dictionary<string, EntradaIndice>();
        }

        public void CrearArchivo(string rutaArchivo, List<Registro> registros)
        {
            rutaArchivoDatos = rutaArchivo;
            rutaArchivoIndice = rutaArchivo + ".idx.txt";
            indice.Clear();

            // Verificar que no haya IDs duplicados
            var idsUnicos = registros.Select(r => r.ID).Distinct().ToList();
            if (idsUnicos.Count != registros.Count)
            {
                throw new Exception("Hay IDs duplicados en los registros");
            }

            // Crear archivo de datos
            using (StreamWriter writer = new StreamWriter(rutaArchivoDatos, false, Encoding.UTF8))
            {
                long lineNumber = 0;

                foreach (var registro in registros)
                {
                    // Escribir registro: ID|Nombre|Edad|Email|Activo
                    string linea = $"{EscaparTexto(registro.ID)}{DELIMITADOR}" +
                                 $"{EscaparTexto(registro.Nombre)}{DELIMITADOR}" +
                                 $"{registro.Edad}{DELIMITADOR}" +
                                 $"{EscaparTexto(registro.Email)}{DELIMITADOR}" +
                                 $"{(registro.Activo ? "1" : "0")}";
                    writer.WriteLine(linea);

                    // Agregar entrada al índice
                    indice[registro.ID] = new EntradaIndice
                    {
                        Clave = registro.ID,
                        Posicion = lineNumber,
                        Activo = true
                    };

                    lineNumber++;
                }
            }

            // Guardar índice
            GuardarIndice();
        }

        public void CargarArchivo(string rutaArchivo)
        {
            rutaArchivoDatos = rutaArchivo;
            rutaArchivoIndice = rutaArchivo + ".idx.txt";

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

            // Leer el registro desde la línea indicada
            string[] lineas = File.ReadAllLines(rutaArchivoDatos, Encoding.UTF8);

            if (entrada.Posicion >= lineas.Length)
                return null;

            string linea = lineas[entrada.Posicion];
            Registro registro = ParsearLineaRegistro(linea);

            return registro.Activo ? registro : null;
        }

        public void InsertarRegistro(Registro registro)
        {
            if (indice.ContainsKey(registro.ID))
                throw new Exception($"Ya existe un registro con ID: {registro.ID}");

            // Obtener el número de líneas actual
            long lineNumber = File.ReadLines(rutaArchivoDatos, Encoding.UTF8).Count();

            // Agregar al final del archivo
            using (StreamWriter writer = new StreamWriter(rutaArchivoDatos, true, Encoding.UTF8))
            {
                string linea = $"{EscaparTexto(registro.ID)}{DELIMITADOR}" +
                             $"{EscaparTexto(registro.Nombre)}{DELIMITADOR}" +
                             $"{registro.Edad}{DELIMITADOR}" +
                             $"{EscaparTexto(registro.Email)}{DELIMITADOR}" +
                             $"{(registro.Activo ? "1" : "0")}";
                writer.WriteLine(linea);
            }

            // Agregar entrada al índice
            indice[registro.ID] = new EntradaIndice
            {
                Clave = registro.ID,
                Posicion = lineNumber,
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

            // Leer todas las líneas
            string[] lineas = File.ReadAllLines(rutaArchivoDatos, Encoding.UTF8);

            // Modificar la línea específica
            if (entrada.Posicion < lineas.Length)
            {
                string nuevaLinea = $"{EscaparTexto(nuevoRegistro.ID)}{DELIMITADOR}" +
                                  $"{EscaparTexto(nuevoRegistro.Nombre)}{DELIMITADOR}" +
                                  $"{nuevoRegistro.Edad}{DELIMITADOR}" +
                                  $"{EscaparTexto(nuevoRegistro.Email)}{DELIMITADOR}" +
                                  $"{(nuevoRegistro.Activo ? "1" : "0")}";
                lineas[entrada.Posicion] = nuevaLinea;
            }

            // Reescribir el archivo
            File.WriteAllLines(rutaArchivoDatos, lineas, Encoding.UTF8);
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

            // Marcar como inactivo en el archivo
            string[] lineas = File.ReadAllLines(rutaArchivoDatos, Encoding.UTF8);

            if (entrada.Posicion < lineas.Length)
            {
                Registro registro = ParsearLineaRegistro(lineas[entrada.Posicion]);
                registro.Activo = false;

                string nuevaLinea = $"{EscaparTexto(registro.ID)}{DELIMITADOR}" +
                                  $"{EscaparTexto(registro.Nombre)}{DELIMITADOR}" +
                                  $"{registro.Edad}{DELIMITADOR}" +
                                  $"{EscaparTexto(registro.Email)}{DELIMITADOR}0";
                lineas[entrada.Posicion] = nuevaLinea;
            }

            File.WriteAllLines(rutaArchivoDatos, lineas, Encoding.UTF8);

            // Guardar índice actualizado
            GuardarIndice();

            return true;
        }

        public List<Registro> LeerTodosLosRegistros()
        {
            List<Registro> registros = new List<Registro>();

            string[] lineas = File.ReadAllLines(rutaArchivoDatos, Encoding.UTF8);

            foreach (string linea in lineas)
            {
                if (string.IsNullOrWhiteSpace(linea))
                    continue;

                Registro registro = ParsearLineaRegistro(linea);

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

            // Crear archivo temporal
            string archivoTemp = rutaArchivoDatos + ".tmp";

            indice.Clear();

            using (StreamWriter writer = new StreamWriter(archivoTemp, false, Encoding.UTF8))
            {
                long lineNumber = 0;

                foreach (var registro in registrosActivos)
                {
                    string linea = $"{EscaparTexto(registro.ID)}{DELIMITADOR}" +
                                 $"{EscaparTexto(registro.Nombre)}{DELIMITADOR}" +
                                 $"{registro.Edad}{DELIMITADOR}" +
                                 $"{EscaparTexto(registro.Email)}{DELIMITADOR}1";
                    writer.WriteLine(linea);

                    indice[registro.ID] = new EntradaIndice
                    {
                        Clave = registro.ID,
                        Posicion = lineNumber,
                        Activo = true
                    };

                    lineNumber++;
                }
            }

            // Reemplazar archivo original
            File.Delete(rutaArchivoDatos);
            File.Move(archivoTemp, rutaArchivoDatos);

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
            using (StreamWriter writer = new StreamWriter(rutaArchivoIndice, false, Encoding.UTF8))
            {
                foreach (var entrada in indice.Values)
                {
                    // Formato: Clave|Posicion|Activo
                    string linea = $"{EscaparTexto(entrada.Clave)}{DELIMITADOR}" +
                                 $"{entrada.Posicion}{DELIMITADOR}" +
                                 $"{(entrada.Activo ? "1" : "0")}";
                    writer.WriteLine(linea);
                }
            }
        }

        private void CargarIndice()
        {
            indice.Clear();

            string[] lineas = File.ReadAllLines(rutaArchivoIndice, Encoding.UTF8);

            foreach (string linea in lineas)
            {
                if (string.IsNullOrWhiteSpace(linea))
                    continue;

                string[] partes = linea.Split(DELIMITADOR);

                if (partes.Length >= 3)
                {
                    EntradaIndice entrada = new EntradaIndice
                    {
                        Clave = DesescaparTexto(partes[0]),
                        Posicion = long.Parse(partes[1]),
                        Activo = partes[2] == "1"
                    };

                    indice[entrada.Clave] = entrada;
                }
            }
        }

        private Registro ParsearLineaRegistro(string linea)
        {
            string[] partes = linea.Split(DELIMITADOR);

            if (partes.Length < 5)
                throw new FormatException($"Formato de línea inválido: {linea}");

            return new Registro
            {
                ID = DesescaparTexto(partes[0]),
                Nombre = DesescaparTexto(partes[1]),
                Edad = int.Parse(partes[2]),
                Email = DesescaparTexto(partes[3]),
                Activo = partes[4] == "1"
            };
        }

        private string EscaparTexto(string texto)
        {
            // Escapar el delimitador si aparece en el texto
            return texto.Replace(DELIMITADOR.ToString(), $"\\{DELIMITADOR}");
        }

        private string DesescaparTexto(string texto)
        {
            // Desescapar el delimitador
            return texto.Replace($"\\{DELIMITADOR}", DELIMITADOR.ToString());
        }
    }
}
