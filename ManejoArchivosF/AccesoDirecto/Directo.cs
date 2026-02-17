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
    public class Directo
    {
        public string ArchivoActual { get; private set; } = string.Empty;

        public class Registro
        {
            public int Id { get; set; }
            public string Contenido { get; set; } = string.Empty;
        }

        // ==================== CONFIGURAR GRID ====================
        public void ConfigurarDataGridViews(DataGridView dgvDatos, DataGridView dgvPropiedades)
        {
            dgvDatos.AllowUserToAddRows = true;
            dgvDatos.AllowUserToDeleteRows = true;
            dgvDatos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDatos.Columns.Clear();
            dgvDatos.Columns.Add("Id", "Id de registro");
            dgvDatos.Columns.Add("Contenido", "Contenido");

            dgvPropiedades.AllowUserToAddRows = false;
            dgvPropiedades.AllowUserToDeleteRows = false;
            dgvPropiedades.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPropiedades.Columns.Clear();
            dgvPropiedades.Columns.Add("Propiedad", "Propiedad");
            dgvPropiedades.Columns.Add("Valor", "Valor");
        }

        // ==================== OBTENER REGISTROS DESDE GRID ====================
        public List<Registro> ObtenerRegistrosDesdeGrid(DataGridView grid)
        {
            List<Registro> registros = new();

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                if (!int.TryParse(row.Cells[0].Value?.ToString(), out int id) || id <= 0)
                    continue;

                string contenido = row.Cells[1].Value?.ToString()?.Trim() ?? string.Empty;

                registros.Add(new Registro
                {
                    Id = id,
                    Contenido = contenido
                });
            }

            return registros.OrderBy(r => r.Id).ToList();
        }

        // ==================== CARGAR REGISTROS EN GRID ====================
        public void CargarRegistrosEnGrid(IEnumerable<Registro> registros, DataGridView grid)
        {
            grid.Rows.Clear();

            foreach (Registro registro in registros.OrderBy(r => r.Id))
            {
                grid.Rows.Add(registro.Id, registro.Contenido);
            }
        }

        // ==================== CSV HELPERS ====================
        private string EscapeCsv(string value)
        {
            string escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        private string LimpiarComillasCsv(string value)
        {
            string trimmed = value.Trim();

            if (trimmed.StartsWith('"') && trimmed.EndsWith('"') && trimmed.Length >= 2)
            {
                trimmed = trimmed[1..^1].Replace("\"\"", "\"");
            }

            return trimmed;
        }

        private string[] ParseCsvLine(string line)
        {
            List<string> values = new();
            StringBuilder current = new();
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    current.Append(c);
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            values.Add(current.ToString());
            return values.ToArray();
        }

        // ==================== LECTURA GENERAL ====================
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

        // ==================== GUARDADO GENERAL ====================
        public void GuardarRegistros(string rutaArchivo, List<Registro> registros)
        {
            string extension = Path.GetExtension(rutaArchivo).ToLowerInvariant();

            switch (extension)
            {
                case ".txt":
                    GuardarTxt(rutaArchivo, registros);
                    break;

                case ".csv":
                    GuardarCsv(rutaArchivo, registros);
                    break;

                case ".json":
                    GuardarJson(rutaArchivo, registros);
                    break;

                case ".xml":
                    GuardarXml(rutaArchivo, registros);
                    break;

                default:
                    throw new InvalidOperationException("Formato no soportado. Use TXT, CSV, JSON o XML.");
            }
        }

        // ==================== TXT ====================
        private List<Registro> LeerTxt(string rutaArchivo)
        {
            List<Registro> registros = new();

            if (!File.Exists(rutaArchivo))
                return registros;

            foreach (string line in File.ReadAllLines(rutaArchivo, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] partes = line.Split('|', 2);

                if (partes.Length < 2 || !int.TryParse(partes[0], out int id) || id <= 0)
                    continue;

                registros.Add(new Registro { Id = id, Contenido = partes[1] });
            }

            return registros;
        }

        private void GuardarTxt(string rutaArchivo, List<Registro> registros)
        {
            IEnumerable<string> lines = registros.OrderBy(r => r.Id)
                .Select(r => $"{r.Id}|{r.Contenido}");

            File.WriteAllLines(rutaArchivo, lines, Encoding.UTF8);
        }

        // ==================== CSV ====================
        private List<Registro> LeerCsv(string rutaArchivo)
        {
            List<Registro> registros = new();

            if (!File.Exists(rutaArchivo))
                return registros;

            string[] lineas = File.ReadAllLines(rutaArchivo, Encoding.UTF8);

            foreach (string linea in lineas.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(linea))
                    continue;

                string[] campos = ParseCsvLine(linea);

                if (campos.Length < 2)
                    continue;

                if (!int.TryParse(LimpiarComillasCsv(campos[0]), out int id) || id <= 0)
                    continue;

                registros.Add(new Registro
                {
                    Id = id,
                    Contenido = LimpiarComillasCsv(campos[1])
                });
            }

            return registros;
        }

        private void GuardarCsv(string rutaArchivo, List<Registro> registros)
        {
            List<string> lineas = new() { "Id,Contenido" };

            foreach (Registro registro in registros.OrderBy(r => r.Id))
            {
                lineas.Add($"{registro.Id},{EscapeCsv(registro.Contenido)}");
            }

            File.WriteAllLines(rutaArchivo, lineas, Encoding.UTF8);
        }

        // ==================== JSON ====================
        private List<Registro> LeerJson(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo))
                return new List<Registro>();

            string json = File.ReadAllText(rutaArchivo, Encoding.UTF8);

            if (string.IsNullOrWhiteSpace(json))
                return new List<Registro>();

            List<Registro>? registros = JsonSerializer.Deserialize<List<Registro>>(json);

            return registros?.Where(r => r.Id > 0).ToList() ?? new List<Registro>();
        }

        private void GuardarJson(string rutaArchivo, List<Registro> registros)
        {
            JsonSerializerOptions options = new() { WriteIndented = true };

            string json = JsonSerializer.Serialize(registros.OrderBy(r => r.Id), options);

            File.WriteAllText(rutaArchivo, json, Encoding.UTF8);
        }

        // ==================== XML ====================
        private List<Registro> LeerXml(string rutaArchivo)
        {
            List<Registro> registros = new();

            if (!File.Exists(rutaArchivo))
                return registros;

            XDocument doc = XDocument.Load(rutaArchivo);

            IEnumerable<Registro> result = doc.Root?
                .Elements("Registro")
                .Select(x => new Registro
                {
                    Id = (int?)x.Element("Id") ?? 0,
                    Contenido = (string?)x.Element("Contenido") ?? string.Empty
                })
                .Where(r => r.Id > 0) ?? Enumerable.Empty<Registro>();

            return result.ToList();
        }

        private void GuardarXml(string rutaArchivo, List<Registro> registros)
        {
            XDocument doc = new(
                new XElement("Registros",
                    registros.OrderBy(r => r.Id).Select(r =>
                        new XElement("Registro",
                            new XElement("Id", r.Id),
                            new XElement("Contenido", r.Contenido)))));

            doc.Save(rutaArchivo);
        }

        // ==================== CREAR ====================
        public void CrearArchivo(string rutaArchivo, DataGridView dgvDatos)
        {
            List<Registro> registros = ObtenerRegistrosDesdeGrid(dgvDatos);
            GuardarRegistros(rutaArchivo, registros);

            ArchivoActual = rutaArchivo;

            MessageBox.Show("Archivo creado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ==================== ABRIR ====================
        public void AbrirArchivo(string rutaArchivo, DataGridView dgvDatos)
        {
            ArchivoActual = rutaArchivo;

            List<Registro> registros = LeerRegistros(rutaArchivo);

            CargarRegistrosEnGrid(registros, dgvDatos);

            MessageBox.Show("Archivo cargado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ==================== MODIFICAR ====================
        public void ModificarArchivo(DataGridView dgvDatos)
        {
            if (string.IsNullOrWhiteSpace(ArchivoActual) || !File.Exists(ArchivoActual))
            {
                MessageBox.Show("Primero abra o cree un archivo.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<Registro> registros = ObtenerRegistrosDesdeGrid(dgvDatos);
            GuardarRegistros(ArchivoActual, registros);

            MessageBox.Show("Archivo actualizado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ==================== ELIMINAR CONTENIDO ====================
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

        // ==================== ELIMINAR ARCHIVO ====================
        public void EliminarArchivo(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo))
            {
                MessageBox.Show("El archivo no existe.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            File.Delete(rutaArchivo);
            MessageBox.Show("Archivo eliminado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ==================== COPIAR ====================
        public void CopiarArchivo(string rutaOrigen, string rutaDestino)
        {
            File.Copy(rutaOrigen, rutaDestino, true);
            MessageBox.Show("Archivo copiado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ==================== MOVER ====================
        public void MoverArchivo(string rutaOrigen, string rutaDestino)
        {
            File.Move(rutaOrigen, rutaDestino, true);
            MessageBox.Show("Archivo movido exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ==================== PROPIEDADES ====================
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
        }
    }
}
