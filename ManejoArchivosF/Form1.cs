using ManejoArchivosF.AccesoDirecto;
using ManejoArchivosF.ArchivoSecuencial;
using ManejoArchivosF.Indexado;

namespace ManejoArchivosF
{
    public partial class Form1 : Form
    {
        //Instancias de negocio para cada organización. El indexado s
        private readonly Secuencial _secuencial = new();
        private readonly Directo _directo = new();
        private IArchivoIndexado? _indexado;

        public Form1()
        {
            InitializeComponent();
            ConfigurarFormulario();
        }

        private void ConfigurarFormulario()
        {
            // Las columnas de cada DataGridView ya se configuran dentro de los métodos
            // BuildTab* del Designer y se pone el  reloj.
            var timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += (s, e) =>
            {
                if (!IsDisposed && txtFechaBit != null)
                    txtFechaBit.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            };
            timer.Start();
        }

       
        //  TAB 1 — BITÁCORA DE CAJA (SECUENCIAL)

        private void btnAgregarTransaccion_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCajero.Text) ||
                string.IsNullOrWhiteSpace(txtMonto.Text) ||
                string.IsNullOrWhiteSpace(txtDescBit.Text))
            {
                MessageBox.Show("Complete todos los campos antes de registrar.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string entrada = $"[{txtFechaBit.Text}] Turno:{txtTurno.Text} | Cajero:{txtCajero.Text} | " +
                             $"Monto:${txtMonto.Text} | Concepto:{txtDescBit.Text}";

            dgvBitacora.Rows.Add(entrada);

            // Si hay archivo activo, guardar automáticamente (comportamiento secuencial)
            if (!string.IsNullOrWhiteSpace(_secuencial.ArchivoActual))
                _secuencial.ModificarArchivo(_secuencial.ArchivoActual, dgvBitacora);

            // Limpiar campos
            txtCajero.Clear();
            txtMonto.Clear();
            txtDescBit.Clear();
            txtCajero.Focus();

            // Scroll al final (lectura secuencial)
            if (dgvBitacora.Rows.Count > 0)
                dgvBitacora.FirstDisplayedScrollingRowIndex = dgvBitacora.Rows.Count - 1;
        }

        private void btnCrearBitacora_Click(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Crear archivo de bitácora",
                Filter = "Archivo de texto (*.txt)|*.txt",
                FileName = $"bitacora_{DateTime.Now:yyyyMMdd}"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            // Si el grid está vacío agregar encabezado
            if (dgvBitacora.Rows.Count == 0 || dgvBitacora.Rows.Cast<DataGridViewRow>().All(r => r.IsNewRow))
            {
                dgvBitacora.Rows.Add($"=== BITÁCORA DE CAJA — Apertura: {DateTime.Now:dd/MM/yyyy HH:mm} ===");
            }

            _secuencial.CrearArchivo(dlg.FileName, dgvBitacora);
            lblArchBit.Text = Path.GetFileName(dlg.FileName);
            lblArchBit.ForeColor = Color.DarkGreen;
        }

        private void btnAbrirBitacora_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Abrir bitácora",
                Filter = "Archivo de texto (*.txt)|*.txt|Todos (*.*)|*.*"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            _secuencial.AbrirArchivo(dlg.FileName, dgvBitacora);
            lblArchBit.Text = Path.GetFileName(dlg.FileName);
            lblArchBit.ForeColor = Color.DarkGreen;
            _secuencial.CargarPropiedades(dlg.FileName, dgvPropBitacora);
        }

        private void btnGuardarBitacora_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_secuencial.ArchivoActual))
            {
                MessageBox.Show("Primero cree o abra una bitácora.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _secuencial.ModificarArchivo(_secuencial.ArchivoActual, dgvBitacora);
            _secuencial.CargarPropiedades(_secuencial.ArchivoActual, dgvPropBitacora);
        }

        private void btnEliminarBitacora_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_secuencial.ArchivoActual)) return;

            if (MessageBox.Show($"¿Eliminar permanentemente '{Path.GetFileName(_secuencial.ArchivoActual)}'?",
                    "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            _secuencial.EliminarArchivo(_secuencial.ArchivoActual);
            dgvBitacora.Rows.Clear();
            dgvPropBitacora.Rows.Clear();
            lblArchBit.Text = "Sin archivo";
            lblArchBit.ForeColor = Color.Gray;
        }

        private void btnCopiarBitacora_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_secuencial.ArchivoActual)) return;

            using var dlg = new SaveFileDialog
            {
                Title = "Copiar bitácora a...",
                Filter = "Archivo de texto (*.txt)|*.txt",
                FileName = "copia_" + Path.GetFileName(_secuencial.ArchivoActual)
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;
            _secuencial.CopiarArchivo(_secuencial.ArchivoActual, dlg.FileName);
        }

        private void btnMoverBitacora_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_secuencial.ArchivoActual)) return;

            using var dlg = new SaveFileDialog
            {
                Title = "Mover bitácora a...",
                Filter = "Archivo de texto (*.txt)|*.txt",
                FileName = Path.GetFileName(_secuencial.ArchivoActual)
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;
            _secuencial.MoverArchivo(_secuencial.ArchivoActual, dlg.FileName);
            lblArchBit.Text = Path.GetFileName(dlg.FileName);
        }

        private void btnPropBitacora_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_secuencial.ArchivoActual) || !File.Exists(_secuencial.ArchivoActual))
            {
                MessageBox.Show("No hay archivo activo.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _secuencial.CargarPropiedades(_secuencial.ArchivoActual, dgvPropBitacora);
        }




        //  TAB 2 — CATÁLOGO DE PRODUCTOS (ACCESO DIRECTO)

        //private string ExtensionSeleccionada()
        //{
        //    if (rbCsv.Checked) return ".csv";
        //    if (rbJson.Checked) return ".json";
        //    if (rbXml.Checked) return ".xml";
        //    return ".txt";
        //}

        //private string FiltroArchivo()
        //{
        //    if (rbCsv.Checked) return "CSV (*.csv)|*.csv";
        //    if (rbJson.Checked) return "JSON (*.json)|*.json";
        //    if (rbXml.Checked) return "XML (*.xml)|*.xml";
        //    return "Texto (*.txt)|*.txt";
        //}

        private void btnNuevoInv_Click(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Crear catálogo de productos (acceso directo)",
                Filter = "Catálogo de acceso directo (*.dat)|*.dat",
                FileName = "catalogo_productos"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            // Agregar datos de ejemplo si el grid está vacío
            if (dgvInventario.Rows.Cast<DataGridViewRow>().All(r => r.IsNewRow))
            {
                dgvInventario.Rows.Add(1, "Martillo carpintero|85.50|20");
                dgvInventario.Rows.Add(2, "Taladro 500W|1250.00|8");
                dgvInventario.Rows.Add(3, "Tornillos 6x50mm (caja)|45.00|150");
            }

            _directo.CrearArchivo(dlg.FileName, dgvInventario);
            lblArchInv.Text = Path.GetFileName(dlg.FileName);
            lblArchInv.ForeColor = Color.DarkGreen;
        }



        private void btnAbrirInv_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Abrir catálogo de acceso directo",
                Filter = "Catálogo de acceso directo (*.dat)|*.dat"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            _directo.AbrirArchivo(dlg.FileName, dgvInventario);
            lblArchInv.Text = Path.GetFileName(dlg.FileName);
            lblArchInv.ForeColor = Color.DarkGreen;
            _directo.VerPropiedades(dlg.FileName, dgvPropInventario);
        }



        private void btnGuardarInv_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_directo.ArchivoActual))
            {
                MessageBox.Show("Primero cree o abra un catálogo.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _directo.ModificarArchivo(dgvInventario);
            _directo.VerPropiedades(_directo.ArchivoActual, dgvPropInventario);
        }

        private void btnEliminarInv_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_directo.ArchivoActual)) return;

            if (MessageBox.Show($"¿Eliminar permanentemente '{Path.GetFileName(_directo.ArchivoActual)}'?",
                    "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            _directo.EliminarArchivo(_directo.ArchivoActual);
            dgvInventario.Rows.Clear();
            dgvPropInventario.Rows.Clear();
            lblArchInv.Text = "Sin archivo";
            lblArchInv.ForeColor = Color.Gray;
        }


        private void btnCopiarInv_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_directo.ArchivoActual)) return;

            using var dlg = new SaveFileDialog
            {
                Title = "Copiar catálogo a...",
                Filter = "Catálogo de acceso directo (*.dat)|*.dat",
                FileName = "copia_" + Path.GetFileName(_directo.ArchivoActual)
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;
            _directo.CopiarArchivo(_directo.ArchivoActual, dlg.FileName);
        }




        private void btnMoverInv_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_directo.ArchivoActual)) return;

            using var dlg = new SaveFileDialog
            {
                Title = "Mover catálogo a...",
                Filter = "Catálogo de acceso directo (*.dat)|*.dat",
                FileName = Path.GetFileName(_directo.ArchivoActual)
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;
            _directo.MoverArchivo(_directo.ArchivoActual, dlg.FileName);
            lblArchInv.Text = Path.GetFileName(dlg.FileName);
        }



        private void btnPropInv_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_directo.ArchivoActual) || !File.Exists(_directo.ArchivoActual))
            {
                MessageBox.Show("No hay archivo activo.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _directo.VerPropiedades(_directo.ArchivoActual, dgvPropInventario);
        }

        private void btnEliminarFila_Click(object? sender, EventArgs e)
        {
            _directo.EliminarContenido(dgvInventario);
            if (!string.IsNullOrWhiteSpace(_directo.ArchivoActual))
                _directo.VerPropiedades(_directo.ArchivoActual, dgvPropInventario);
        }



        //
        //  TAB 3 — DIRECTORIO DE EMPLEADOS (INDEXADO)
        private IArchivoIndexado CrearImplementacion(string formato) =>
            formato switch
            {
                "CSV" => new IndexadoCSV(),
                "XML" => new IndexadoXML(),
                _ => new IndexadoTXT()
            };

        private void btnCrearDir_Click(object? sender, EventArgs e)
        {
            string fmt = cmbFormatoIdx.SelectedItem!.ToString()!;

            string filtro = fmt switch
            {
                "CSV" => "CSV (*.csv)|*.csv",
                "XML" => "XML (*.xml)|*.xml",
                _ => "Texto (*.txt)|*.txt"
            };

            using var dlg = new SaveFileDialog
            {
                Title = "Crear directorio de empleados",
                Filter = filtro,
                FileName = "empleados"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                _indexado = CrearImplementacion(fmt);

                // Datos de ejemplo
                var registros = new List<Registro>
                {
                    new() { ID = "E001", Nombre = "Ana García",    Edad = 32, Email = "ana.garcia@empresa.com",    Activo = true },
                    new() { ID = "E002", Nombre = "Luis Ramírez",  Edad = 28, Email = "luis.ramirez@empresa.com",  Activo = true },
                    new() { ID = "E003", Nombre = "María Torres",  Edad = 45, Email = "maria.torres@empresa.com",  Activo = true },
                    new() { ID = "E004", Nombre = "Carlos Vega",   Edad = 35, Email = "carlos.vega@empresa.com",   Activo = true },
                    new() { ID = "E005", Nombre = "Sofía Mendoza", Edad = 29, Email = "sofia.mendoza@empresa.com", Activo = true }
                };

                _indexado.CrearArchivo(dlg.FileName, registros);

                lblArchDir.Text = Path.GetFileName(dlg.FileName);
                lblArchDir.ForeColor = Color.DarkGreen;

                RefrescarEmpleados();
                RefrescarIndice();

                MessageBox.Show($"Directorio creado con {registros.Count} empleados de ejemplo.\n" +
                                $"Se generó también el archivo de índice: {Path.GetFileName(dlg.FileName)}.idx.{fmt.ToLower()}",
                    "Directorio creado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCargarDir_Click(object? sender, EventArgs e)
        {
            string fmt = cmbFormatoIdx.SelectedItem!.ToString()!;

            using var dlg = new OpenFileDialog
            {
                Title = "Cargar directorio de empleados",
                Filter = "Todos los formatos|*.txt;*.csv;*.xml|Texto (*.txt)|*.txt|CSV (*.csv)|*.csv|XML (*.xml)|*.xml"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                // Detectar formato por extensión
                string ext = Path.GetExtension(dlg.FileName).TrimStart('.').ToUpper();
                _indexado = CrearImplementacion(ext);
                _indexado.CargarArchivo(dlg.FileName);

                lblArchDir.Text = Path.GetFileName(dlg.FileName);
                lblArchDir.ForeColor = Color.DarkGreen;

                RefrescarEmpleados();
                RefrescarIndice();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBuscarEmp_Click(object? sender, EventArgs e)
        {
            if (_indexado == null)
            {
                MessageBox.Show("Primero cree o cargue un directorio.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string id = txtBuscarId.Text.Trim();
            if (string.IsNullOrWhiteSpace(id)) return;

            Registro? reg = _indexado.BuscarRegistro(id);

            if (reg == null)
            {
                lblResultado.Text = $"❌ Empleado '{id}' no encontrado.";
                lblResultado.ForeColor = Color.Red;
                return;
            }

            // Llenar formulario
            txtEmpId.Text = reg.ID;
            txtEmpNombre.Text = reg.Nombre;
            txtEmpEdad.Text = reg.Edad.ToString();
            txtEmpEmail.Text = reg.Email;

            lblResultado.Text = $"✅ Empleado '{id}' encontrado vía índice.";
            lblResultado.ForeColor = Color.DarkGreen;

            // Seleccionar fila en grid
            foreach (DataGridViewRow row in dgvEmpleados.Rows)
            {
                if (row.Cells[0].Value?.ToString() == id)
                {
                    row.Selected = true;
                    dgvEmpleados.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }
        }

        private void btnInsertarEmp_Click(object? sender, EventArgs e)
        {
            if (_indexado == null)
            {
                MessageBox.Show("Primero cree o cargue un directorio.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidarCamposEmp(out Registro reg)) return;

            try
            {
                _indexado.InsertarRegistro(reg);
                lblResultado.Text = $"✅ Empleado '{reg.ID}' insertado.";
                lblResultado.ForeColor = Color.DarkGreen;
                RefrescarEmpleados();
                RefrescarIndice();
                LimpiarCamposEmp();
            }
            catch (Exception ex)
            {
                lblResultado.Text = $"❌ {ex.Message}";
                lblResultado.ForeColor = Color.Red;
            }
        }

        private void btnModificarEmp_Click(object? sender, EventArgs e)
        {
            if (_indexado == null) return;
            if (!ValidarCamposEmp(out Registro reg)) return;

            try
            {
                _indexado.ModificarRegistro(reg.ID, reg);
                lblResultado.Text = $"✅ Empleado '{reg.ID}' modificado.";
                lblResultado.ForeColor = Color.DarkGreen;
                RefrescarEmpleados();
                RefrescarIndice();
            }
            catch (Exception ex)
            {
                lblResultado.Text = $"❌ {ex.Message}";
                lblResultado.ForeColor = Color.Red;
            }
        }

        private void btnEliminarEmp_Click(object? sender, EventArgs e)
        {
            if (_indexado == null) return;

            string id = txtEmpId.Text.Trim();
            if (string.IsNullOrWhiteSpace(id)) return;

            if (MessageBox.Show($"¿Dar de baja lógicamente al empleado '{id}'?\n(El registro permanece en el archivo marcado como inactivo.)",
                    "Confirmar baja", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            bool ok = _indexado.EliminarRegistro(id);
            lblResultado.Text = ok
                ? $"✅ Empleado '{id}' dado de baja (eliminación lógica)."
                : $"❌ Empleado '{id}' no encontrado o ya inactivo.";
            lblResultado.ForeColor = ok ? Color.DarkGreen : Color.Red;

            RefrescarEmpleados();
            RefrescarIndice();
            LimpiarCamposEmp();
        }

        private void btnReorganizar_Click(object? sender, EventArgs e)
        {
            if (_indexado == null) return;

            int eliminados = _indexado.Reorganizar();
            lblResultado.Text = $"🔧 Reorganización completada. Se purgaron {eliminados} registro(s) del archivo físico.";
            lblResultado.ForeColor = Color.DarkBlue;

            RefrescarEmpleados();
            RefrescarIndice();
        }

        private void dgvEmpleados_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvEmpleados.Rows[e.RowIndex];
            txtEmpId.Text = row.Cells["ID"].Value?.ToString() ?? "";
            txtEmpNombre.Text = row.Cells["Nombre"].Value?.ToString() ?? "";
            txtEmpEdad.Text = row.Cells["Edad"].Value?.ToString() ?? "";
            txtEmpEmail.Text = row.Cells["Email"].Value?.ToString() ?? "";
        }



        
        //  HELPERS INDEXADO
        private void RefrescarEmpleados()
        {
            if (_indexado == null) return;
            dgvEmpleados.Rows.Clear();
            foreach (var r in _indexado.LeerTodosLosRegistros())
                dgvEmpleados.Rows.Add(r.ID, r.Nombre, r.Edad, r.Email);
        }

        private void RefrescarIndice()
        {
            if (_indexado == null) return;
            dgvIndice.Rows.Clear();
            foreach (var kv in _indexado.ObtenerIndice())
            {
                dgvIndice.Rows.Add(
                    kv.Value.Clave,
                    kv.Value.Posicion,
                    kv.Value.Activo ? "✅ Activo" : "❌ Eliminado lógico"
                );
            }
        }

        private bool ValidarCamposEmp(out Registro reg)
        {
            reg = new Registro();

            if (string.IsNullOrWhiteSpace(txtEmpId.Text))
            {
                MessageBox.Show("El campo ID es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!int.TryParse(txtEmpEdad.Text, out int edad) || edad <= 0 || edad > 120)
            {
                MessageBox.Show("Ingrese una edad válida (número entre 1 y 120).", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            reg.ID = txtEmpId.Text.Trim();
            reg.Nombre = txtEmpNombre.Text.Trim();
            reg.Edad = edad;
            reg.Email = txtEmpEmail.Text.Trim();
            reg.Activo = true;

            return true;
        }

        private void LimpiarCamposEmp()
        {
            txtEmpId.Clear();
            txtEmpNombre.Clear();
            txtEmpEdad.Clear();
            txtEmpEmail.Clear();
        }
    }
}