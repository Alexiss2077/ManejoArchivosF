using System.IO;
using System.Windows.Forms;

namespace ManejoArchivosF.ArchivoSecuencial
{
    /// <summary>
    /// Organización Secuencial: las entradas se leen y escriben en orden estricto.
    /// Caso de uso: Bitácora de transacciones de caja se agregan al final y se leen completamente para generar reportes. No se requiere acceso directo a registros específicos, solo agregar y leer secuencialmente.
    /// </summary>
    public class Secuencial
    {
        public string ArchivoActual { get; private set; } = "";

        // CREAR ARCHIVO
        public void CrearArchivo(string rutaArchivo, DataGridView dgvDatos)
        {
            try
            {
                var lineas = ObtenerLineas(dgvDatos);

                if (lineas.Count == 0)
                {
                    MessageBox.Show("Agregue al menos una transacción antes de crear el archivo.",
                        "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using StreamWriter writer = new(rutaArchivo);
                foreach (string linea in lineas)
                    writer.WriteLine(linea);

                ArchivoActual = rutaArchivo;
                MessageBox.Show("Bitácora creada exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //ABRIR ARCHIVO 
        public void AbrirArchivo(string rutaArchivo, DataGridView dgvDatos)
        {
            try
            {
                ArchivoActual = rutaArchivo;
                dgvDatos.Rows.Clear();

                using StreamReader reader = new(rutaArchivo);
                string? linea;
                while ((linea = reader.ReadLine()) != null)
                    dgvDatos.Rows.Add(linea);

                MessageBox.Show("Bitácora abierta correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //MODIFICAR ARCHIVO
        /// <summary>Reescribe el archivo con el contenido actual del grid (acceso secuencial).</summary>
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
                var lineas = ObtenerLineas(dgvDatos);

                using (StreamWriter writer = new(archivoTemporal))
                    foreach (string linea in lineas)
                        writer.WriteLine(linea);

                if (File.Exists(rutaArchivo)) File.Delete(rutaArchivo);
                File.Move(archivoTemporal, rutaArchivo);

                ArchivoActual = rutaArchivo;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //COPIAR
        public void CopiarArchivo(string rutaOrigen, string rutaDestino)
        {
            try
            {
                if (File.Exists(rutaDestino)) File.Delete(rutaDestino);
                File.Copy(rutaOrigen, rutaDestino);
                MessageBox.Show("Bitácora copiada exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al copiar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //mOVER
        public void MoverArchivo(string rutaOrigen, string rutaDestino)
        {
            try
            {
                if (File.Exists(rutaDestino)) File.Delete(rutaDestino);
                File.Move(rutaOrigen, rutaDestino);
                ArchivoActual = rutaDestino;
                MessageBox.Show("Bitácora movida exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al mover: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //ELLIMINAR 
        public void EliminarArchivo(string rutaArchivo)
        {

            try
            {
                if (File.Exists(rutaArchivo))
                {
                    File.Delete(rutaArchivo);
                    ArchivoActual = "";
                    MessageBox.Show("Bitácora eliminada.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("El archivo no existe.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //PROPIEDADES
        public void CargarPropiedades(string rutaArchivo, DataGridView dgvPropiedades)
        {
            try
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
                dgvPropiedades.Rows.Add("Ubicación completa", info.FullName);
                dgvPropiedades.Rows.Add("Carpeta contenedora", info.DirectoryName ?? "");
                dgvPropiedades.Rows.Add("Líneas totales", File.ReadAllLines(rutaArchivo).Length.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar propiedades: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //HELPER PRIVADO 
        private static List<string> ObtenerLineas(DataGridView grid)
        {
            var lineas = new List<string>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                string? valor = row.Cells[0].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(valor))
                    lineas.Add(valor);
            }
            return lineas;
        }
    }
}