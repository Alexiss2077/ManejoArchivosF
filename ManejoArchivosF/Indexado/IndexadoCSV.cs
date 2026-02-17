using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ManejoArchivosF.Indexado
{
    /// <summary>
    /// Organización Indexada en CSV.
    /// Encabezado: ID,Nombre,Edad,Email,Activo
    /// Índice separado: .idx.csv
    /// </summary>
    public class IndexadoCSV : IArchivoIndexado
    {
        private string rutaArchivoDatos = "";
        private string rutaArchivoIndice = "";
        private Dictionary<string, EntradaIndice> indice = new();
        private const char D = ',';
        private const char Q = '"';

        public void CrearArchivo(string rutaArchivo, List<Registro> registros)
        {
            rutaArchivoDatos = rutaArchivo;
            rutaArchivoIndice = rutaArchivo + ".idx.csv";
            indice.Clear();

            var ids = registros.Select(r => r.ID).Distinct().ToList();
            if (ids.Count != registros.Count) throw new Exception("Hay IDs duplicados en los registros.");

            using StreamWriter w = new(rutaArchivoDatos, false, Encoding.UTF8);
            w.WriteLine("ID,Nombre,Edad,Email,Activo");
            long n = 1;
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
            rutaArchivoIndice = rutaArchivo + ".idx.csv";
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
            if (!e.Activo) throw new Exception($"El registro {id} está eliminado.");
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
                .Skip(1)
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
                w.WriteLine("ID,Nombre,Edad,Email,Activo");
                long n = 1;
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

        //Privados
        private void GuardarIndice()
        {
            using StreamWriter w = new(rutaArchivoIndice, false, Encoding.UTF8);
            w.WriteLine("Clave,Posicion,Activo");
            foreach (var e in indice.Values)
                w.WriteLine($"{EscQ(e.Clave)},{e.Posicion},{(e.Activo ? "1" : "0")}");
        }

        private void CargarIndice()
        {
            indice.Clear();
            foreach (string linea in File.ReadAllLines(rutaArchivoIndice, Encoding.UTF8).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;
                var p = ParseLine(linea);
                if (p.Count >= 3)
                    indice[p[0]] = new EntradaIndice { Clave = p[0], Posicion = long.Parse(p[1]), Activo = p[2] == "1" };
            }
        }

        private Registro Parsear(string linea)
        {
            var p = ParseLine(linea);
            if (p.Count < 5) throw new FormatException($"Línea CSV inválida: {linea}");
            return new Registro
            {
                ID = p[0],
                Nombre = p[1],
                Edad = int.Parse(p[2]),
                Email = p[3],
                Activo = p[4] == "1"
            };
        }

        private string Formatear(Registro r) =>
            $"{EscQ(r.ID)},{EscQ(r.Nombre)},{r.Edad},{EscQ(r.Email)},{(r.Activo ? "1" : "0")}";

        private string EscQ(string t)
        {
            if (string.IsNullOrEmpty(t)) return t;
            if (t.Contains(D) || t.Contains(Q) || t.Contains('\n'))
                return $"{Q}{t.Replace(Q.ToString(), $"{Q}{Q}")}{Q}";
            return t;
        }

        private List<string> ParseLine(string linea)
        {
            var campos = new List<string>();
            var sb = new StringBuilder();
            bool inQ = false;
            for (int i = 0; i < linea.Length; i++)
            {
                char c = linea[i];
                if (c == Q)
                {
                    if (inQ && i + 1 < linea.Length && linea[i + 1] == Q) { sb.Append(Q); i++; }
                    else inQ = !inQ;
                }
                else if (c == D && !inQ) { campos.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            campos.Add(sb.ToString());
            return campos;
        }
    }
}