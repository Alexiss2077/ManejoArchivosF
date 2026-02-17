using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ManejoArchivosF.Indexado
{
    /// <summary>
    /// Implementación de archivo indexado usando formato CSV (Comma Separated Values)
    /// Incluye encabezados en la primera línea
    /// </summary>
    public class IndexadoCSV : IArchivoIndexado
    {
        private string rutaArchivoDatos = "";
        private string rutaArchivoIndice = "";
        private Dictionary<string, EntradaIndice> indice;
        private const char DELIMITADOR = ',';
        private const char COMILLA = '"';

        public IndexadoCSV()
        {
            indice = new Dictionary<string, EntradaIndice>();
        }

        public void CrearArchivo(string rutaArchivo, List<Registro> registros)
        {
            rutaArchivoDatos = rutaArchivo;
            rutaArchivoIndice = rutaArchivo + ".idx.csv";
            indice.Clear();

            // Verificar que no haya IDs duplicados
            var idsUnicos = registros.Select(r => r.ID).Distinct().ToList();
            if (idsUnicos.Count != registros.Count)
            {
                throw new Exception("Hay IDs duplicados en los registros");
            }

            // Crear archivo de datos con encabezados
            using (StreamWriter writer = new StreamWriter(rutaArchivoDatos, false, Encoding.UTF8))
            {
                // Escribir encabezados
                writer.WriteLine("ID,Nombre,Edad,Email,Activo");

                long lineNumber = 1; // Línea 0 es el encabezado

                foreach (var registro in registros)
                {
                    string linea = FormatearRegistroCSV(registro);
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
            rutaArchivoIndice = rutaArchivo + ".idx.csv";

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
            Registro registro = ParsearLineaCSV(linea);

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
                string linea = FormatearRegistroCSV(registro);
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
                lineas[entrada.Posicion] = FormatearRegistroCSV(nuevoRegistro);
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
                Registro registro = ParsearLineaCSV(lineas[entrada.Posicion]);
                registro.Activo = false;
                lineas[entrada.Posicion] = FormatearRegistroCSV(registro);
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

            // Saltar la primera línea (encabezados)
            for (int i = 1; i < lineas.Length; i++)
            {
                string linea = lineas[i];

                if (string.IsNullOrWhiteSpace(linea))
                    continue;

                Registro registro = ParsearLineaCSV(linea);

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
                // Escribir encabezados
                writer.WriteLine("ID,Nombre,Edad,Email,Activo");

                long lineNumber = 1;

                foreach (var registro in registrosActivos)
                {
                    string linea = FormatearRegistroCSV(registro);
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
                // Escribir encabezados
                writer.WriteLine("Clave,Posicion,Activo");

                foreach (var entrada in indice.Values)
                {
                    string linea = $"{EscaparCSV(entrada.Clave)},{entrada.Posicion},{(entrada.Activo ? "1" : "0")}";
                    writer.WriteLine(linea);
                }
            }
        }

        private void CargarIndice()
        {
            indice.Clear();

            string[] lineas = File.ReadAllLines(rutaArchivoIndice, Encoding.UTF8);

            // Saltar la primera línea (encabezados)
            for (int i = 1; i < lineas.Length; i++)
            {
                string linea = lineas[i];

                if (string.IsNullOrWhiteSpace(linea))
                    continue;

                List<string> partes = ParsearLineaCSVCompleta(linea);

                if (partes.Count >= 3)
                {
                    EntradaIndice entrada = new EntradaIndice
                    {
                        Clave = partes[0],
                        Posicion = long.Parse(partes[1]),
                        Activo = partes[2] == "1"
                    };

                    indice[entrada.Clave] = entrada;
                }
            }
        }

        private Registro ParsearLineaCSV(string linea)
        {
            List<string> partes = ParsearLineaCSVCompleta(linea);

            if (partes.Count < 5)
                throw new FormatException($"Formato de línea CSV inválido: {linea}");

            return new Registro
            {
                ID = partes[0],
                Nombre = partes[1],
                Edad = int.Parse(partes[2]),
                Email = partes[3],
                Activo = partes[4] == "1"
            };
        }

        private List<string> ParsearLineaCSVCompleta(string linea)
        {
            List<string> campos = new List<string>();
            StringBuilder campoActual = new StringBuilder();
            bool dentroDeComillas = false;

            for (int i = 0; i < linea.Length; i++)
            {
                char c = linea[i];

                if (c == COMILLA)
                {
                    // Verificar si es una comilla de escape
                    if (dentroDeComillas && i + 1 < linea.Length && linea[i + 1] == COMILLA)
                    {
                        campoActual.Append(COMILLA);
                        i++; // Saltar la siguiente comilla
                    }
                    else
                    {
                        dentroDeComillas = !dentroDeComillas;
                    }
                }
                else if (c == DELIMITADOR && !dentroDeComillas)
                {
                    campos.Add(campoActual.ToString());
                    campoActual.Clear();
                }
                else
                {
                    campoActual.Append(c);
                }
            }

            // Agregar el último campo
            campos.Add(campoActual.ToString());

            return campos;
        }

        private string FormatearRegistroCSV(Registro registro)
        {
            return $"{EscaparCSV(registro.ID)},{EscaparCSV(registro.Nombre)},{registro.Edad}," +
                   $"{EscaparCSV(registro.Email)},{(registro.Activo ? "1" : "0")}";
        }

        private string EscaparCSV(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return texto;

            // Si contiene coma, comilla o salto de línea, encerrar en comillas
            if (texto.Contains(DELIMITADOR) || texto.Contains(COMILLA) || texto.Contains('\n') || texto.Contains('\r'))
            {
                // Escapar comillas duplicándolas
                texto = texto.Replace(COMILLA.ToString(), $"{COMILLA}{COMILLA}");
                return $"{COMILLA}{texto}{COMILLA}";
            }

            return texto;
        }
    }
}
