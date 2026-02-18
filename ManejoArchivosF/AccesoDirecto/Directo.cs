using System.Windows.Forms;

namespace ManejoArchivosF.AccesoDirecto
{
    /// <summary>
    /// Organización de Acceso Directo REAL.
    /// Usa función hash (id % totalSlots) + sondeo lineal + FileStream.Seek()
    /// para leer y escribir cualquier registro en O(1) sin recorrer el archivo.
    ///
    /// Formato: archivo binario .dat con registros de tamaño fijo (256 bytes).
    /// Caso de uso: Catálogo de productos de ferretería — acceso por código de artículo.
    /// </summary>
    public class Directo
    {
        // ── Estado ────────────────────────────────────────────────────────────
        public string ArchivoActual { get; private set; } = string.Empty;

        private HashFileManager? _hash = null;

        // ── Clase pública Registro (mantiene compatibilidad con Form1) ─────────
        public class Registro
        {
            public int Id { get; set; }
            public string Contenido { get; set; } = string.Empty;
        }

        // ── Configurar grids ──────────────────────────────────────────────────
        public void ConfigurarDataGridViews(DataGridView dgvDatos, DataGridView dgvPropiedades)
        {
            dgvDatos.AllowUserToAddRows = true;
            dgvDatos.AllowUserToDeleteRows = false;
            dgvDatos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDatos.Columns.Clear();
            dgvDatos.Columns.Add("Id", "Código de producto");
            dgvDatos.Columns.Add("Contenido", "Descripción | Precio | Stock");
            dgvDatos.Columns.Add("Slot", "Slot hash");
            dgvDatos.Columns["Slot"]!.ReadOnly = true;

            dgvPropiedades.AllowUserToAddRows = false;
            dgvPropiedades.AllowUserToDeleteRows = false;
            dgvPropiedades.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPropiedades.Columns.Clear();
            dgvPropiedades.Columns.Add("Propiedad", "Propiedad");
            dgvPropiedades.Columns.Add("Valor", "Valor");
        }

        // ── Obtener registros desde el grid ───────────────────────────────────
        public List<Registro> ObtenerRegistrosDesdeGrid(DataGridView grid)
        {
            List<Registro> registros = new();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                if (!int.TryParse(row.Cells[0].Value?.ToString(), out int id) || id <= 0) continue;
                string contenido = row.Cells[1].Value?.ToString()?.Trim() ?? string.Empty;
                registros.Add(new Registro { Id = id, Contenido = contenido });
            }
            return registros.OrderBy(r => r.Id).ToList();
        }

        // ── Refrescar grid ────────────────────────────────────────────────────
        private void RefrescarGrid(DataGridView grid)
        {
            grid.Rows.Clear();
            if (_hash == null) return;

            foreach (RegistroDirecto r in _hash.ReadAll())
            {
                var (_, slot) = _hash.Search(r.Id);
                // El grid puede tener 2 columnas (Form1 original) o 3 (con Slot)
                if (grid.Columns.Count >= 3)
                    grid.Rows.Add(r.Id, r.Contenido, slot);
                else
                    grid.Rows.Add(r.Id, r.Contenido);
            }
        }

        // ── CREAR archivo .dat ────────────────────────────────────────────────
        public void CrearArchivo(string rutaArchivo, DataGridView dgvDatos)
        {
            try
            {
                // Pedir número de slots al usuario
                int slots = PedirSlots();
                if (slots <= 0) return;

                HashFileManager.CreateFile(rutaArchivo, slots);
                _hash = new HashFileManager(rutaArchivo);
                ArchivoActual = rutaArchivo;

                // Insertar los registros que ya había en el grid
                int insertados = 0, errores = 0;
                foreach (Registro reg in ObtenerRegistrosDesdeGrid(dgvDatos))
                {
                    int resultado = _hash.Insert(reg.Id, reg.Contenido);
                    if (resultado >= 0) insertados++;
                    else errores++;
                }

                RefrescarGrid(dgvDatos);

                MessageBox.Show(
                    $"Catálogo creado con {slots} slots.\n" +
                    $"Registros insertados: {insertados}" +
                    (errores > 0 ? $"\nErrores (IDs duplicados o archivo lleno): {errores}" : ""),
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear el catálogo: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── ABRIR archivo .dat ────────────────────────────────────────────────
        public void AbrirArchivo(string rutaArchivo, DataGridView dgvDatos)
        {
            try
            {
                var (valid, _) = HashFileManager.ReadHeader(rutaArchivo);
                if (!valid)
                {
                    MessageBox.Show("El archivo seleccionado no es un catálogo de acceso directo (.dat) válido.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _hash = new HashFileManager(rutaArchivo);
                ArchivoActual = rutaArchivo;
                RefrescarGrid(dgvDatos);

                MessageBox.Show(
                    $"Catálogo cargado correctamente.\n" +
                    $"Slots totales: {_hash.TotalSlots}\n" +
                    $"Registros encontrados: {dgvDatos.Rows.Count}",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir el catálogo: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── GUARDAR CAMBIOS (insertar o actualizar según corresponda) ─────────
        public void ModificarArchivo(DataGridView dgvDatos)
        {
            if (!VerificarArchivoAbierto()) return;

            int insertados = 0, actualizados = 0, errores = 0;

            try
            {
                foreach (DataGridViewRow row in dgvDatos.Rows)
                {
                    if (row.IsNewRow) continue;
                    if (!int.TryParse(row.Cells[0].Value?.ToString(), out int id) || id <= 0)
                    { errores++; continue; }

                    string contenido = row.Cells[1].Value?.ToString()?.Trim() ?? string.Empty;

                    var (existente, _) = _hash!.Search(id);
                    if (existente == null)
                    {
                        int slot = _hash.Insert(id, contenido);
                        if (slot >= 0) insertados++;
                        else errores++;
                    }
                    else
                    {
                        if (_hash.Update(id, contenido)) actualizados++;
                        else errores++;
                    }
                }

                RefrescarGrid(dgvDatos);
                MessageBox.Show(
                    $"Insertados: {insertados}\nActualizados: {actualizados}" +
                    (errores > 0 ? $"\nErrores: {errores}" : ""),
                    "Catálogo actualizado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar cambios: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── ELIMINAR FILA seleccionada (tombstone en disco) ───────────────────
        public void EliminarContenido(DataGridView dgvDatos)
        {
            if (!VerificarArchivoAbierto()) return;

            if (dgvDatos.CurrentRow == null || dgvDatos.CurrentRow.IsNewRow)
            {
                MessageBox.Show("Seleccione una fila para eliminar.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(dgvDatos.CurrentRow.Cells[0].Value?.ToString(), out int id) || id <= 0)
            {
                MessageBox.Show("El Id de la fila seleccionada no es válido.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_hash!.Delete(id))
            {
                RefrescarGrid(dgvDatos);
                MessageBox.Show(
                    $"Producto Id={id} eliminado.\n(Solo se marcó el slot como ELIMINADO en disco — tombstone.)",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"No se encontró el producto con Id={id}.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ── ELIMINAR archivo físico ───────────────────────────────────────────
        public void EliminarArchivo(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo))
            {
                MessageBox.Show("El archivo no existe.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            File.Delete(rutaArchivo);
            if (ArchivoActual == rutaArchivo) { _hash = null; ArchivoActual = string.Empty; }
            MessageBox.Show("Catálogo eliminado exitosamente.", "Éxito",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── COPIAR archivo ────────────────────────────────────────────────────
        public void CopiarArchivo(string rutaOrigen, string rutaDestino)
        {
            File.Copy(rutaOrigen, rutaDestino, true);
            MessageBox.Show("Catálogo copiado exitosamente.", "Éxito",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── MOVER archivo ─────────────────────────────────────────────────────
        public void MoverArchivo(string rutaOrigen, string rutaDestino)
        {
            File.Move(rutaOrigen, rutaDestino, true);
            if (ArchivoActual == rutaOrigen)
            {
                ArchivoActual = rutaDestino;
                _hash = new HashFileManager(rutaDestino);
            }
            MessageBox.Show("Catálogo movido exitosamente.", "Éxito",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── VER PROPIEDADES ───────────────────────────────────────────────────
        public void VerPropiedades(string rutaArchivo, DataGridView dgvPropiedades)
        {
            FileInfo info = new(rutaArchivo);
            dgvPropiedades.Rows.Clear();

            // Propiedades del sistema de archivos
            dgvPropiedades.Rows.Add("Nombre", info.Name);
            dgvPropiedades.Rows.Add("Tamaño", FormatBytes(info.Length));
            dgvPropiedades.Rows.Add("Extensión", info.Extension);
            dgvPropiedades.Rows.Add("Fecha de creación", info.CreationTime.ToString());
            dgvPropiedades.Rows.Add("Último acceso", info.LastAccessTime.ToString());
            dgvPropiedades.Rows.Add("Última modificación", info.LastWriteTime.ToString());
            dgvPropiedades.Rows.Add("Atributos", info.Attributes.ToString());
            dgvPropiedades.Rows.Add("Ubicación", info.FullName);
            dgvPropiedades.Rows.Add("Carpeta contenedora", info.DirectoryName);

            // Propiedades del esquema hash
            var (valid, slots) = HashFileManager.ReadHeader(rutaArchivo);
            if (valid)
            {
                var hfm = new HashFileManager(rutaArchivo);
                var (ocu, del, vac) = hfm.GetStats();

                dgvPropiedades.Rows.Add("─── Hash ───────────", "──────────────────");
                dgvPropiedades.Rows.Add("Total de slots", slots.ToString());
                dgvPropiedades.Rows.Add("Tamaño por registro", "256 bytes (fijo)");
                dgvPropiedades.Rows.Add("Registros ocupados", ocu.ToString());
                dgvPropiedades.Rows.Add("Registros eliminados", del.ToString());
                dgvPropiedades.Rows.Add("Slots vacíos", vac.ToString());
                dgvPropiedades.Rows.Add("Factor de carga", $"{(double)ocu / slots:P1}");
                dgvPropiedades.Rows.Add("Función hash", "id % totalSlots");
                dgvPropiedades.Rows.Add("Manejo colisiones", "Sondeo lineal");
                dgvPropiedades.Rows.Add("Total de productos", ocu.ToString());
            }
        }

        // ── Helpers privados ──────────────────────────────────────────────────

        private bool VerificarArchivoAbierto()
        {
            if (_hash != null && File.Exists(ArchivoActual)) return true;
            MessageBox.Show("Primero cree o abra un catálogo (.dat).", "Advertencia",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        /// <summary>Muestra un mini diálogo para pedir el número de slots.</summary>
        private static int PedirSlots()
        {
            using Form dlg = new()
            {
                Text = "Configurar catálogo de acceso directo",
                Width = 360,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lbl = new Label
            {
                Text = "Total de slots (número primo recomendado):",
                Left = 10,
                Top = 14,
                Width = 320,
                AutoSize = false
            };
            var num = new NumericUpDown
            {
                Left = 10,
                Top = 38,
                Width = 110,
                Minimum = 7,
                Maximum = 9973,
                Value = 101
            };
            var lblInfo = new Label
            {
                Text = "Sugeridos: 101, 251, 503, 1009, 2003",
                Left = 130,
                Top = 41,
                Width = 210,
                ForeColor = System.Drawing.Color.DimGray,
                Font = new System.Drawing.Font("Segoe UI", 8F)
            };
            var ok = new Button { Text = "Crear", DialogResult = DialogResult.OK, Left = 10, Top = 78, Width = 80 };
            var can = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Left = 100, Top = 78, Width = 80 };

            dlg.Controls.AddRange(new System.Windows.Forms.Control[] { lbl, num, lblInfo, ok, can });
            dlg.AcceptButton = ok;
            dlg.CancelButton = can;

            return dlg.ShowDialog() == DialogResult.OK ? (int)num.Value : -1;
        }

        private static string FormatBytes(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / (1024.0 * 1024):F2} MB"
        };
    }
}