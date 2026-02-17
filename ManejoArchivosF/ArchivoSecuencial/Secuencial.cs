using System;
using System.IO;
using System.Windows.Forms;

namespace ManejoArchivosF.ArchivoSecuencial
{
    public class Secuencial
    {
        public string ArchivoActual { get; private set; } = "";

        // ==================== CREAR ARCHIVO ====================
        public void CrearArchivo(string rutaArchivo, DataGridView dgvDatos)
        {
            try
            {
                if (dgvDatos.Rows.Count == 0 || string.IsNullOrWhiteSpace(dgvDatos.Rows[0].Cells[0].Value?.ToString()))
                {
                    MessageBox.Show("Por favor, escriba algo antes de crear el archivo.",
                        "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (StreamWriter writer = new StreamWriter(rutaArchivo))
                {
                    foreach (DataGridViewRow row in dgvDatos.Rows)
                    {
                        if (!row.IsNewRow && row.Cells[0].Value != null)
                        {
                            writer.WriteLine(row.Cells[0].Value.ToString());
                        }
                    }
                }

                ArchivoActual = rutaArchivo;
                MessageBox.Show("Archivo creado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear el archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== ABRIR ARCHIVO ====================
        public void AbrirArchivo(string rutaArchivo, DataGridView dgvDatos)
        {
            try
            {
                ArchivoActual = rutaArchivo;

                dgvDatos.Rows.Clear();

                using (StreamReader reader = new StreamReader(rutaArchivo))
                {
                    string linea;

                    while ((linea = reader.ReadLine()) != null)
                    {
                        dgvDatos.Rows.Add(linea);
                    }
                }

                MessageBox.Show("Archivo abierto exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir el archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== MODIFICAR ARCHIVO ====================
        public void ModificarArchivo(string rutaArchivo, DataGridView dgvDatos)
        {
            try
            {
                if (string.IsNullOrEmpty(rutaArchivo))
                {
                    MessageBox.Show("No hay archivo seleccionado.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string archivoTemporal = rutaArchivo + ".tmp";

                using (StreamWriter writer = new StreamWriter(archivoTemporal))
                {
                    foreach (DataGridViewRow row in dgvDatos.Rows)
                    {
                        if (!row.IsNewRow && row.Cells[0].Value != null)
                        {
                            writer.WriteLine(row.Cells[0].Value.ToString());
                        }
                    }
                }

                if (File.Exists(rutaArchivo))
                {
                    File.Delete(rutaArchivo);
                }

                File.Move(archivoTemporal, rutaArchivo);

                ArchivoActual = rutaArchivo;

                MessageBox.Show("Archivo modificado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al modificar el archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== COPIAR ARCHIVO ====================
        public void CopiarArchivo(string rutaOrigen, string rutaDestino)
        {
            try
            {
                if (File.Exists(rutaDestino))
                {
                    File.Delete(rutaDestino);
                }

                File.Copy(rutaOrigen, rutaDestino);

                MessageBox.Show("Archivo copiado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al copiar el archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== MOVER ARCHIVO ====================
        public void MoverArchivo(string rutaOrigen, string rutaDestino)
        {
            try
            {
                if (File.Exists(rutaDestino))
                {
                    File.Delete(rutaDestino);
                }

                File.Move(rutaOrigen, rutaDestino);

                MessageBox.Show("Archivo movido exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al mover el archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== ELIMINAR ARCHIVO ====================
        public void EliminarArchivo(string rutaArchivo)
        {
            try
            {
                if (File.Exists(rutaArchivo))
                {
                    File.Delete(rutaArchivo);
                    MessageBox.Show("Archivo eliminado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("El archivo no existe.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar el archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== VER PROPIEDADES ====================
        public void CargarPropiedades(string rutaArchivo, DataGridView dgvPropiedades)
        {
            try
            {
                FileInfo info = new FileInfo(rutaArchivo);

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
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar propiedades: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
