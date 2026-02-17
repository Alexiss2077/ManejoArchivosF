using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ManejoArchivosF.AccesoDirecto
{
    /// <summary>
    /// Organización de Acceso Directo: se puede saltar directamente a cualquier registro por su ID
    /// sin recorrer el archivo de forma secuencial.
    /// Caso de uso: Catálogo de productos de ferretería acceso por código de artículo.
    /// </summary>
    public class Directo
    {
        public string ArchivoActual { get; private set; } = string.Empty;

        public class Registro
        {
            public int Id { get; set; }
            public string Contenido { get; set; } = string.Empty;
        }

        //CONFIGURAR GRID
        public void ConfigurarDataGridViews(DataGridView dgvDatos, DataGridView dgvPropiedades)
        {
            dgvDatos.AllowUserToAddRows = true;
            dgvDatos.AllowUserToDeleteRows = true;
            dgvDatos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDatos.Columns.Clear();
            dgvDatos.Columns.Add("Id", "Código de producto");
            dgvDatos.Columns.Add("Contenido", "Descripción | Precio | Stock");

            dgvPropiedades.AllowUserToAddRows = false;
            dgvPropiedades.AllowUserToDeleteRows = false;
            dgvPropiedades.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPropiedades.Columns.Clear();
            dgvPropiedades.Columns.Add("Propiedad", "Propiedad");
            dgvPropiedades.Columns.Add("Valor", "Valor");
        }

        //OBTENER REGISTROS DESDE GRI
        public List<Registro> ObtenerRegistrosDesdeGrid(DataGridView grid)
        {
            List<Registro> registros = new();

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;

                if (!int.TryParse(row.Cells[0].Value?.ToString(), out int id) || id <= 0)
                    continue;

                string contenido = row.Cells[1].Value?.ToString()?.Trim() ?? string.Empty;
                registros.Add(new Registro { Id = id, Contenido = contenido });
            }

            return registros.OrderBy(r => r.Id).ToList();
        }

        //CARGAR REGISTROS EN GRID
        public void CargarRegistrosEnGrid(IEnumerable<Registro> registros, DataGridView grid)
        {
            grid.Rows.Clear();
            foreach (Registro registro in registros.OrderBy(r => r.Id))
                grid.Rows.Add(registro.Id, registro.Contenido);
        }

        //CSV HELPERS
        private string EscapeCsv(string value)
        {
            string escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        private string LimpiarComillasCsv(string value)
        {
            string trimmed = value.Trim();
            if (trimmed.StartsWith('"') && trimmed.EndsWith('"') && trimmed.Length >= 2)
                trimmed = trimmed[1..^1].Replace("\"\"", "\"");
            return trimmed;
        }

        private string[] ParseCsvLine(string line)
        {
            List<string> values = new();
            StringBuilder current = new();
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '"') { inQuotes = !inQuotes; current.Append(c); continue; }
                if (c == ',' && !inQuotes) { values.Add(current.ToString()); current.Clear(); continue; }
                current.Append(c);
            }
            values.Add(current.ToString());
            return values.ToArray();
        }

        //   //////////////////////LECTURA GENERAL
        public List<Registro> LeerRegistros(string rutaArchivo)
        {
            string extension = Path.GetExtension(rutaArchivo).ToLowerInvariant();
            return extension switch
            {
                ".txt" => LeerTxt(rutaArchivo),
                ".csv" => LeerCsv(rutaArchivo),
                ".json" => LeerJson(rutaArchivo),
                ".xml" => LeerXml(rutaArchivo),
                _ => throw new InvalidOperationException("Formato no soportado. Use TXT, CSV, JSON o XML.")
            };
        }

        // GUARDADO GENERAL
        public void GuardarRegistros(string rutaArchivo, List<Registro> registros)
        {
            string extension = Path.GetExtension(rutaArchivo).ToLowerInvariant();
            switch (extension)
            {
                case ".txt": GuardarTxt(rutaArchivo, registros); break;
                case ".csv": GuardarCsv(rutaArchivo, registros); break;
                case ".json": GuardarJson(rutaArchivo, registros); break;
                case ".xml": GuardarXml(rutaArchivo, registros); break;
                default: throw new InvalidOperationException("Formato no soportado.");
            }
        }

        //          TXT 
        private List<Registro> LeerTxt(string rutaArchivo)
        {
            List<Registro> registros = new();
            if (!File.Exists(rutaArchivo)) return registros;

            foreach (string line in File.ReadAllLines(rutaArchivo, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] partes = line.Split('|', 2);
                if (partes.Length < 2 || !int.TryParse(partes[0], out int id) || id <= 0) continue;
                registros.Add(new Registro { Id = id, Contenido = partes[1] });
            }
            return registros;
        }

        private void GuardarTxt(string rutaArchivo, List<Registro> registros)
        {
            File.WriteAllLines(rutaArchivo,
                registros.OrderBy(r => r.Id).Select(r => $"{r.Id}|{r.Contenido}"),
                Encoding.UTF8);
        }

        // CSV
        private List<Registro> LeerCsv(string rutaArchivo)
        {
            List<Registro> registros = new();
            if (!File.Exists(rutaArchivo)) return registros;

            foreach (string linea in File.ReadAllLines(rutaArchivo, Encoding.UTF8).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;
                string[] campos = ParseCsvLine(linea);
                if (campos.Length < 2) continue;
                if (!int.TryParse(LimpiarComillasCsv(campos[0]), out int id) || id <= 0) continue;
                registros.Add(new Registro { Id = id, Contenido = LimpiarComillasCsv(campos[1]) });
            }
            return registros;
        }

        private void GuardarCsv(string rutaArchivo, List<Registro> registros)
        {
            List<string> lineas = new() { "Id,Contenido" };
            foreach (Registro r in registros.OrderBy(r => r.Id))
                lineas.Add($"{r.Id},{EscapeCsv(r.Contenido)}");
            File.WriteAllLines(rutaArchivo, lineas, Encoding.UTF8);
        }

        //             JSON
        private List<Registro> LeerJson(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo)) return new();
            string json = File.ReadAllText(rutaArchivo, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) return new();
            return JsonSerializer.Deserialize<List<Registro>>(json)?.Where(r => r.Id > 0).ToList() ?? new();
        }

        private void GuardarJson(string rutaArchivo, List<Registro> registros)
        {
            File.WriteAllText(rutaArchivo,
                JsonSerializer.Serialize(registros.OrderBy(r => r.Id), new JsonSerializerOptions { WriteIndented = true }),
                Encoding.UTF8);
        }

        //             XML
        private List<Registro> LeerXml(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo)) return new();
            XDocument doc = XDocument.Load(rutaArchivo);
            return (doc.Root?.Elements("Registro")
                .Select(x => new Registro
                {
                    Id = (int?)x.Element("Id") ?? 0,
                    Contenido = (string?)x.Element("Contenido") ?? string.Empty
                })
                .Where(r => r.Id > 0) ?? Enumerable.Empty<Registro>()).ToList();
        }

        private void GuardarXml(string rutaArchivo, List<Registro> registros)
        {
            new XDocument(
                new XElement("Registros",
                    registros.OrderBy(r => r.Id).Select(r =>
                        new XElement("Registro",
                            new XElement("Id", r.Id),
                            new XElement("Contenido", r.Contenido))))).Save(rutaArchivo);
        }

        //   CREAR
        public void CrearArchivo(string rutaArchivo, DataGridView dgvDatos)
        {
            List<Registro> registros = ObtenerRegistrosDesdeGrid(dgvDatos);
            GuardarRegistros(rutaArchivo, registros);
            ArchivoActual = rutaArchivo;
            MessageBox.Show("Catálogo creado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        //    ABRIR
        public void AbrirArchivo(string rutaArchivo, DataGridView dgvDatos)
        {
            ArchivoActual = rutaArchivo;
            CargarRegistrosEnGrid(LeerRegistros(rutaArchivo), dgvDatos);
            MessageBox.Show("Catálogo cargado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        //        MODIFICAR 
        public void ModificarArchivo(DataGridView dgvDatos)
        {
            if (string.IsNullOrWhiteSpace(ArchivoActual) || !File.Exists(ArchivoActual))
            {
                MessageBox.Show("Primero abra o cree un catálogo.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GuardarRegistros(ArchivoActual, ObtenerRegistrosDesdeGrid(dgvDatos));
            MessageBox.Show("Catálogo actualizado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        ///ELIMINAR FILA 
        public void EliminarContenido(DataGridView dgvDatos)
        {
            if (dgvDatos.CurrentRow == null || dgvDatos.CurrentRow.IsNewRow)
            {
                MessageBox.Show("Seleccione una fila para eliminar.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            dgvDatos.Rows.RemoveAt(dgvDatos.CurrentRow.Index);
            ModificarArchivo(dgvDatos);
        }

        //              ELIMINAR ARCHIVO
        public void EliminarArchivo(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo))
            {
                MessageBox.Show("El archivo no existe.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            File.Delete(rutaArchivo);
            ArchivoActual = string.Empty;
            MessageBox.Show("Catálogo eliminado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        //COPIAR 
        public void CopiarArchivo(string rutaOrigen, string rutaDestino)
        {
            File.Copy(rutaOrigen, rutaDestino, true);
            MessageBox.Show("Catálogo copiado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        //        MOVER 
        public void MoverArchivo(string rutaOrigen, string rutaDestino)
        {
            File.Move(rutaOrigen, rutaDestino, true);
            ArchivoActual = rutaDestino;
            MessageBox.Show("Catálogo movido exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        //    PROPIEDADES 
        public void VerPropiedades(string rutaArchivo, DataGridView dgvPropiedades)
        {
            FileInfo info = new(rutaArchivo);
            dgvPropiedades.Rows.Clear();
            dgvPropiedades.Rows.Add("Tamaño", info.Length + " bytes");
            dgvPropiedades.Rows.Add("Nombre", info.Name);
            dgvPropiedades.Rows.Add("Fecha de creación", info.CreationTime.ToString());
            dgvPropiedades.Rows.Add("Extensión", info.Extension);
            dgvPropiedades.Rows.Add("Último acceso", info.LastAccessTime.ToString());
            dgvPropiedades.Rows.Add("Última modificación", info.LastWriteTime.ToString());
            dgvPropiedades.Rows.Add("Atributos", info.Attributes.ToString());
            dgvPropiedades.Rows.Add("Ubicación", info.FullName);
            dgvPropiedades.Rows.Add("Carpeta contenedora", info.DirectoryName);

            // Mostrar cantidad de registros (acceso directo: contar sin procesar todo)
            int total = LeerRegistros(rutaArchivo).Count;
            dgvPropiedades.Rows.Add("Total de productos", total.ToString());
        }
    }
}