using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ManejoArchivosF.Indexado
{
    /// <summary>
    /// Organización Indexada en TXT.
    /// Formato datos:  ID|Nombre|Edad|Email|Activo
    /// Formato índice: Clave|Posicion|Activo  (.idx.txt)
    /// </summary>
    public class IndexadoTXT : IArchivoIndexado
    {
        private string rutaArchivoDatos = "";
        private string rutaArchivoIndice = "";
        private Dictionary<string, EntradaIndice> indice = new();
        private const char D = '|';

        public void CrearArchivo(string rutaArchivo, List<Registro> registros)
        {
            rutaArchivoDatos = rutaArchivo;
            rutaArchivoIndice = rutaArchivo + ".idx.txt";
            indice.Clear();

            var ids = registros.Select(r => r.ID).Distinct().ToList();
            if (ids.Count != registros.Count) throw new Exception("Hay IDs duplicados en los registros.");

            using StreamWriter w = new(rutaArchivoDatos, false, Encoding.UTF8);
            long n = 0;
            foreach (var r in registros)
            {
                w.WriteLine(Formatear(r));
                indice[r.ID] = new EntradaIndice { Clave = r.ID, Posicion = n++, Activo = true };
            }
            GuardarIndice();
        }

        public void CargarArchivo(string rutaArchivo)
        {
            rutaArchivoDatos = rutaArchivo;
            rutaArchivoIndice = rutaArchivo + ".idx.txt";
            if (!File.Exists(rutaArchivoDatos)) throw new FileNotFoundException("No se encuentra el archivo de datos.");
            if (!File.Exists(rutaArchivoIndice)) throw new FileNotFoundException("No se encuentra el archivo de índice.");
            CargarIndice();
        }

        public Registro? BuscarRegistro(string id)
        {
            if (!indice.TryGetValue(id, out var e) || !e.Activo) return null;
            var lineas = File.ReadAllLines(rutaArchivoDatos, Encoding.UTF8);
            if (e.Posicion >= lineas.Length) return null;
            var r = Parsear(lineas[e.Posicion]);
            return r.Activo ? r : null;
        }

        public void InsertarRegistro(Registro registro)
        {
            if (indice.ContainsKey(registro.ID)) throw new Exception($"Ya existe un registro con ID: {registro.ID}");
            long n = File.ReadLines(rutaArchivoDatos, Encoding.UTF8).Count();
            using StreamWriter w = new(rutaArchivoDatos, true, Encoding.UTF8);
            w.WriteLine(Formatear(registro));
            indice[registro.ID] = new EntradaIndice { Clave = registro.ID, Posicion = n, Activo = true };
            GuardarIndice();
        }

        public void ModificarRegistro(string id, Registro nuevo)
        {
            if (!indice.TryGetValue(id, out var e)) throw new Exception($"No existe un registro con ID: {id}");
            if (!e.Activo) throw new Exception($"El registro con ID {id} está eliminado.");
            nuevo.ID = id; nuevo.Activo = true;
            var lineas = File.ReadAllLines(rutaArchivoDatos, Encoding.UTF8);
            if (e.Posicion < lineas.Length) lineas[e.Posicion] = Formatear(nuevo);
            File.WriteAllLines(rutaArchivoDatos, lineas, Encoding.UTF8);
        }

        public bool EliminarRegistro(string id)
        {
            if (!indice.TryGetValue(id, out var e) || !e.Activo) return false;
            e.Activo = false;
            var lineas = File.ReadAllLines(rutaArchivoDatos, Encoding.UTF8);
            if (e.Posicion < lineas.Length)
            {
                var r = Parsear(lineas[e.Posicion]);
                r.Activo = false;
                lineas[e.Posicion] = Formatear(r);
            }
            File.WriteAllLines(rutaArchivoDatos, lineas, Encoding.UTF8);
            GuardarIndice();
            return true;
        }

        public List<Registro> LeerTodosLosRegistros()
        {
            return File.ReadAllLines(rutaArchivoDatos, Encoding.UTF8)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(Parsear)
                .Where(r => r.Activo)
                .ToList();
        }

        public int Reorganizar()
        {
            var activos = LeerTodosLosRegistros();
            int eliminados = indice.Count - activos.Count;
            string tmp = rutaArchivoDatos + ".tmp";
            indice.Clear();
            using (StreamWriter w = new(tmp, false, Encoding.UTF8))
            {
                long n = 0;
                foreach (var r in activos)
                {
                    w.WriteLine(Formatear(r));
                    indice[r.ID] = new EntradaIndice { Clave = r.ID, Posicion = n++, Activo = true };
                }
            }
            File.Delete(rutaArchivoDatos);
            File.Move(tmp, rutaArchivoDatos);
            GuardarIndice();
            return eliminados;
        }

        public Dictionary<string, EntradaIndice> ObtenerIndice() => new(indice);

        //Privado s
        private void GuardarIndice()
        {
            using StreamWriter w = new(rutaArchivoIndice, false, Encoding.UTF8);
            foreach (var e in indice.Values)
                w.WriteLine($"{Esc(e.Clave)}{D}{e.Posicion}{D}{(e.Activo ? "1" : "0")}");
        }

        private void CargarIndice()
        {
            indice.Clear();
            foreach (string linea in File.ReadAllLines(rutaArchivoIndice, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;
                var p = linea.Split(D);
                if (p.Length >= 3)
                    indice[Des(p[0])] = new EntradaIndice
                    {
                        Clave = Des(p[0]),
                        Posicion = long.Parse(p[1]),
                        Activo = p[2] == "1"
                    };
            }
        }

        private Registro Parsear(string linea)
        {
            var p = linea.Split(D);
            if (p.Length < 5) throw new FormatException($"Línea inválida: {linea}");
            return new Registro
            {
                ID = Des(p[0]),
                Nombre = Des(p[1]),
                Edad = int.Parse(p[2]),
                Email = Des(p[3]),
                Activo = p[4] == "1"
            };
        }

        private string Formatear(Registro r) =>
            $"{Esc(r.ID)}{D}{Esc(r.Nombre)}{D}{r.Edad}{D}{Esc(r.Email)}{D}{(r.Activo ? "1" : "0")}";

        private string Esc(string t) => t.Replace(D.ToString(), $"\\{D}");
        private string Des(string t) => t.Replace($"\\{D}", D.ToString());
    }
}