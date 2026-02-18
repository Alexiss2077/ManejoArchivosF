namespace ManejoArchivosF
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        // ── Controles globales ──────────────────────────────────────
        private TabControl tabControl;
        private TabPage tabSecuencial, tabDirecto, tabIndexado;

        // ── Tab Secuencial ──────────────────────────────────────────
        private SplitContainer splitSecuencial;
        private GroupBox grpNuevaEntrada, grpArchLog, grpPropLog;
        private TextBox txtFechaBit, txtTurno, txtCajero, txtMonto, txtDescBit;
        private Button btnAgregarTransaccion, btnCrearBitacora, btnAbrirBitacora;
        private Button btnGuardarBitacora, btnEliminarBitacora, btnCopiarBitacora;
        private Button btnMoverBitacora, btnPropBitacora;
        private Label lblArchBit;
        private DataGridView dgvBitacora, dgvPropBitacora;

        // ── Tab AccesoDirecto ───────────────────────────────────────
        private SplitContainer splitDirecto;
        private GroupBox grpArchInv, grpAcciones;
        private Button btnNuevoInv, btnAbrirInv, btnGuardarInv;
        private Button btnEliminarInv, btnCopiarInv, btnMoverInv, btnPropInv;
        private Button btnEliminarFila;
        private Label lblArchInv;
        private DataGridView dgvInventario, dgvPropInventario;

        // ── Tab Indexado ────────────────────────────────────────────
        private SplitContainer splitIndexado;
        private GroupBox grpDatosEmp, grpArchIdx, grpBusqueda;
        private TextBox txtEmpId, txtEmpNombre, txtEmpEdad, txtEmpEmail;
        private TextBox txtBuscarId;
        private ComboBox cmbFormatoIdx;
        private Button btnBuscarEmp, btnInsertarEmp, btnModificarEmp, btnEliminarEmp;
        private Button btnCrearDir, btnCargarDir, btnReorganizar;
        private Label lblArchDir, lblResultado;
        private DataGridView dgvEmpleados, dgvIndice;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            AutoScaleMode = AutoScaleMode.Font;
            Text = "Sistema de Manejo de Archivos — Secuencial | Directo | Indexado";
            ClientSize = new Size(1100, 680);
            MinimumSize = new Size(900, 600);
            Font = new Font("Segoe UI", 9F);

            tabControl = new TabControl { Dock = DockStyle.Fill };
            Controls.Add(tabControl);

            tabSecuencial = new TabPage("📋  Bitácora de Caja  (Secuencial)");
            tabDirecto = new TabPage("📦  Catálogo de Productos  (Acceso Directo)");
            tabIndexado = new TabPage("👥  Directorio de Empleados  (Indexado)");

            tabControl.TabPages.Add(tabSecuencial);
            tabControl.TabPages.Add(tabDirecto);
            tabControl.TabPages.Add(tabIndexado);

            BuildTabSecuencial();
            BuildTabDirecto();
            BuildTabIndexado();
        }

        // ══════════════════════════════════════════════════════════
        //  TAB 1 — BITÁCORA DE CAJA (SECUENCIAL)
        private void BuildTabSecuencial()
        {
            splitSecuencial = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 310, Panel1MinSize = 290 };
            tabSecuencial.Controls.Add(splitSecuencial);

            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };
            splitSecuencial.Panel1.Controls.Add(pnlLeft);

            var lblProblema = new Label
            {
                Text = "Problema: Una tienda necesita registrar todas las transacciones de caja en orden cronológico para auditoría. El archivo secuencial es ideal porque las entradas solo se agregan al final y se leen de corrido.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.TopLeft,
                AutoSize = false,
                Width = 285,
                Height = 100,
                Top = 4,
                Left = 4
            };
            pnlLeft.Controls.Add(lblProblema);

            grpNuevaEntrada = new GroupBox { Text = "Nueva Transacción", Left = 4, Top = 110, Width = 285, Height = 220 };
            pnlLeft.Controls.Add(grpNuevaEntrada);

            int y = 22;
            AddLabelTextBox(grpNuevaEntrada, "Fecha/Hora:", ref y, out txtFechaBit, readOnly: true, defVal: DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            AddLabelTextBox(grpNuevaEntrada, "Turno:", ref y, out txtTurno, defVal: "Mañana");
            AddLabelTextBox(grpNuevaEntrada, "Cajero:", ref y, out txtCajero);
            AddLabelTextBox(grpNuevaEntrada, "Monto $:", ref y, out txtMonto);
            AddLabelTextBox(grpNuevaEntrada, "Concepto:", ref y, out txtDescBit);

            btnAgregarTransaccion = new Button
            {
                Text = "➕ Registrar Transacción",
                Left = 10,
                Top = y,
                Width = 255,
                Height = 30,
                BackColor = Color.FromArgb(76, 153, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAgregarTransaccion.Click += btnAgregarTransaccion_Click;
            grpNuevaEntrada.Controls.Add(btnAgregarTransaccion);

            grpArchLog = new GroupBox { Text = "Archivo de Bitácora (.txt)", Left = 4, Top = 318, Width = 285, Height = 195 };
            pnlLeft.Controls.Add(grpArchLog);

            lblArchBit = new Label { Text = "Sin archivo", ForeColor = Color.Gray, Left = 10, Top = 20, Width = 265, AutoSize = false, Font = new Font("Segoe UI", 8F, FontStyle.Italic) };
            grpArchLog.Controls.Add(lblArchBit);

            int by = 40;
            AddButton(grpArchLog, "📄 Nueva Bitácora", ref by, out btnCrearBitacora, Color.FromArgb(0, 102, 204), btnCrearBitacora_Click);
            AddButton(grpArchLog, "📂 Abrir Bitácora", ref by, out btnAbrirBitacora, Color.FromArgb(0, 122, 204), btnAbrirBitacora_Click);
            AddButton(grpArchLog, "💾 Guardar Cambios", ref by, out btnGuardarBitacora, Color.FromArgb(0, 153, 76), btnGuardarBitacora_Click);

            var pnlBtnsLog = new Panel { Left = 8, Top = by, Width = 265, Height = 65 };
            grpArchLog.Controls.Add(pnlBtnsLog);

            btnEliminarBitacora = MiniBtn("🗑 Eliminar", Color.FromArgb(180, 50, 50), btnEliminarBitacora_Click);
            btnCopiarBitacora = MiniBtn("📋 Copiar", Color.FromArgb(80, 80, 160), btnCopiarBitacora_Click);
            btnMoverBitacora = MiniBtn("✂ Mover", Color.FromArgb(120, 80, 0), btnMoverBitacora_Click);
            btnPropBitacora = MiniBtn("ℹ Propiedades", Color.FromArgb(60, 100, 120), btnPropBitacora_Click);

            btnEliminarBitacora.Left = 0; btnEliminarBitacora.Top = 0;
            btnCopiarBitacora.Left = 66; btnCopiarBitacora.Top = 0;
            btnMoverBitacora.Left = 133; btnMoverBitacora.Top = 0;
            btnPropBitacora.Left = 0; btnPropBitacora.Top = 32; btnPropBitacora.Width = 130;

            pnlBtnsLog.Controls.AddRange(new Control[] { btnEliminarBitacora, btnCopiarBitacora, btnMoverBitacora, btnPropBitacora });

            var pnlRight = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                Padding = new Padding(6),
                RowStyles = { new RowStyle(SizeType.Percent, 65), new RowStyle(SizeType.Percent, 35) }
            };
            splitSecuencial.Panel2.Controls.Add(pnlRight);

            var lblBit = new Label { Text = "Transacciones registradas (lectura secuencial):", Dock = DockStyle.Top, Height = 20, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            dgvBitacora = BuildDgv();
            dgvBitacora.AllowUserToAddRows = false;
            dgvBitacora.AllowUserToDeleteRows = false;
            dgvBitacora.Columns.Add("Entrada", "Transacción");

            var pBit = new Panel { Dock = DockStyle.Fill };
            pBit.Controls.Add(dgvBitacora);
            pBit.Controls.Add(lblBit);
            pnlRight.Controls.Add(pBit, 0, 0);

            grpPropLog = new GroupBox { Text = "Propiedades del archivo", Dock = DockStyle.Fill };
            dgvPropBitacora = BuildPropDgv();
            dgvPropBitacora.Dock = DockStyle.Fill;
            grpPropLog.Controls.Add(dgvPropBitacora);
            pnlRight.Controls.Add(grpPropLog, 0, 1);
        }

        // ══════════════════════════════════════════════════════════
        //  TAB 2 — CATÁLOGO DE PRODUCTOS (ACCESO DIRECTO)
        private void BuildTabDirecto()
        {
            splitDirecto = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 290, Panel1MinSize = 270 };
            tabDirecto.Controls.Add(splitDirecto);

            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };
            splitDirecto.Panel1.Controls.Add(pnlLeft);

            var lblProblema = new Label
            {
                Text = "Problema: Una ferretería necesita un catálogo de productos donde pueda saltar directamente a cualquier artículo por su ID sin recorrer el archivo completo. Usa un archivo binario .dat con hash + FileStream.Seek().",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.DimGray,
                AutoSize = false,
                Width = 270,
                Height = 100,
                Top = 4,
                Left = 4
            };
            pnlLeft.Controls.Add(lblProblema);

            grpArchInv = new GroupBox { Text = "Gestión de Catálogo (.dat)", Left = 4, Top = 110, Width = 270, Height = 225 };
            pnlLeft.Controls.Add(grpArchInv);

            lblArchInv = new Label { Text = "Sin archivo", ForeColor = Color.Gray, Left = 8, Top = 20, Width = 250, AutoSize = false, Font = new Font("Segoe UI", 8F, FontStyle.Italic) };
            grpArchInv.Controls.Add(lblArchInv);

            int by = 40;
            AddButton(grpArchInv, "📄 Nuevo Catálogo", ref by, out btnNuevoInv, Color.FromArgb(0, 102, 204), btnNuevoInv_Click);
            AddButton(grpArchInv, "📂 Abrir Catálogo", ref by, out btnAbrirInv, Color.FromArgb(0, 122, 204), btnAbrirInv_Click);
            AddButton(grpArchInv, "💾 Guardar Cambios", ref by, out btnGuardarInv, Color.FromArgb(0, 153, 76), btnGuardarInv_Click);

            var pnlBtnsInv = new Panel { Left = 8, Top = by, Width = 250, Height = 65 };
            grpArchInv.Controls.Add(pnlBtnsInv);

            btnEliminarInv = MiniBtn("🗑 Eliminar", Color.FromArgb(180, 50, 50), btnEliminarInv_Click);
            btnCopiarInv = MiniBtn("📋 Copiar", Color.FromArgb(80, 80, 160), btnCopiarInv_Click);
            btnMoverInv = MiniBtn("✂ Mover", Color.FromArgb(120, 80, 0), btnMoverInv_Click);
            btnPropInv = MiniBtn("ℹ Propiedades", Color.FromArgb(60, 100, 120), btnPropInv_Click);
            btnPropInv.Width = 130;

            btnEliminarInv.Left = 0; btnEliminarInv.Top = 0;
            btnCopiarInv.Left = 66; btnCopiarInv.Top = 0;
            btnMoverInv.Left = 133; btnMoverInv.Top = 0;
            btnPropInv.Left = 0; btnPropInv.Top = 32;

            pnlBtnsInv.Controls.AddRange(new Control[] { btnEliminarInv, btnCopiarInv, btnMoverInv, btnPropInv });

            grpAcciones = new GroupBox { Text = "Fila seleccionada", Left = 4, Top = 343, Width = 270, Height = 55 };
            pnlLeft.Controls.Add(grpAcciones);

            int ba = 20;
            AddButton(grpAcciones, "🗑 Eliminar fila seleccionada", ref ba, out btnEliminarFila, Color.FromArgb(180, 50, 50), btnEliminarFila_Click);

            var pnlRight = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                Padding = new Padding(6),
                RowStyles = { new RowStyle(SizeType.Percent, 65), new RowStyle(SizeType.Percent, 35) }
            };
            splitDirecto.Panel2.Controls.Add(pnlRight);

            var lblInv = new Label { Text = "Catálogo de productos (Id = código, Contenido = descripción|precio|stock):", Dock = DockStyle.Top, Height = 20, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            dgvInventario = BuildDgv();
            dgvInventario.Dock = DockStyle.Fill;
            dgvInventario.Columns.Add("Id", "Código de producto");
            dgvInventario.Columns.Add("Contenido", "Descripción | Precio | Stock");

            var pInv = new Panel { Dock = DockStyle.Fill };
            pInv.Controls.Add(dgvInventario);
            pInv.Controls.Add(lblInv);
            pnlRight.Controls.Add(pInv, 0, 0);

            var grpPropInv = new GroupBox { Text = "Propiedades del archivo", Dock = DockStyle.Fill };
            dgvPropInventario = BuildPropDgv();
            dgvPropInventario.Dock = DockStyle.Fill;
            grpPropInv.Controls.Add(dgvPropInventario);
            pnlRight.Controls.Add(grpPropInv, 0, 1);
        }

        // ══════════════════════════════════════════════════════════
        //  TAB 3 — DIRECTORIO DE EMPLEADOS (INDEXADO)
        private void BuildTabIndexado()
        {
            splitIndexado = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 310, Panel1MinSize = 290 };
            tabIndexado.Controls.Add(splitIndexado);

            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };
            splitIndexado.Panel1.Controls.Add(pnlLeft);

            var lblProblema = new Label
            {
                Text = "Problema: RRHH necesita acceso O(1) a cualquier empleado por ID. El archivo indexado mantiene un índice separado para búsquedas instantáneas, soporta baja lógica y reorganización física.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.DimGray,
                AutoSize = false,
                Width = 285,
                Height = 100,
                Top = 4,
                Left = 4
            };
            pnlLeft.Controls.Add(lblProblema);

            var grpFmt = new GroupBox { Text = "Formato", Left = 4, Top = 110, Width = 285, Height = 50 };
            pnlLeft.Controls.Add(grpFmt);
            cmbFormatoIdx = new ComboBox { Left = 10, Top = 18, Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFormatoIdx.Items.AddRange(new object[] { "TXT", "CSV", "XML" });
            cmbFormatoIdx.SelectedIndex = 0;
            grpFmt.Controls.Add(cmbFormatoIdx);

            grpBusqueda = new GroupBox { Text = "Búsqueda por ID (acceso directo vía índice)", Left = 4, Top = 166, Width = 285, Height = 60 };
            pnlLeft.Controls.Add(grpBusqueda);

            txtBuscarId = new TextBox { Left = 10, Top = 24, Width = 140, PlaceholderText = "ID del empleado..." };
            grpBusqueda.Controls.Add(txtBuscarId);
            btnBuscarEmp = new Button
            {
                Text = "🔍 Buscar",
                Left = 158,
                Top = 22,
                Width = 115,
                Height = 26,
                BackColor = Color.FromArgb(0, 102, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnBuscarEmp.Click += btnBuscarEmp_Click;
            grpBusqueda.Controls.Add(btnBuscarEmp);

            grpDatosEmp = new GroupBox { Text = "Datos del Empleado", Left = 4, Top = 234, Width = 285, Height = 200 };
            pnlLeft.Controls.Add(grpDatosEmp);

            int y = 22;
            AddLabelTextBox(grpDatosEmp, "ID:", ref y, out txtEmpId, defVal: "");
            AddLabelTextBox(grpDatosEmp, "Nombre:", ref y, out txtEmpNombre, defVal: "");
            AddLabelTextBox(grpDatosEmp, "Edad:", ref y, out txtEmpEdad, defVal: "");
            AddLabelTextBox(grpDatosEmp, "Email:", ref y, out txtEmpEmail, defVal: "");

            var pnlCRUD = new Panel { Left = 8, Top = y, Width = 265, Height = 32 };
            grpDatosEmp.Controls.Add(pnlCRUD);

            btnInsertarEmp = MiniBtn("➕ Insertar", Color.FromArgb(0, 153, 76), btnInsertarEmp_Click);
            btnModificarEmp = MiniBtn("✏ Modificar", Color.FromArgb(0, 122, 204), btnModificarEmp_Click);
            btnEliminarEmp = MiniBtn("🗑 Eliminar", Color.FromArgb(180, 50, 50), btnEliminarEmp_Click);

            btnInsertarEmp.Left = 0; btnInsertarEmp.Top = 0;
            btnModificarEmp.Left = 88; btnModificarEmp.Top = 0;
            btnEliminarEmp.Left = 176; btnEliminarEmp.Top = 0;

            pnlCRUD.Controls.AddRange(new Control[] { btnInsertarEmp, btnModificarEmp, btnEliminarEmp });

            lblResultado = new Label
            {
                Text = "",
                ForeColor = Color.DarkGreen,
                Left = 8,
                Top = y + 36,
                Width = 265,
                AutoSize = false,
                Height = 20,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            grpDatosEmp.Controls.Add(lblResultado);

            grpArchIdx = new GroupBox { Text = "Gestión de Archivo Indexado", Left = 4, Top = 442, Width = 285, Height = 160 };
            pnlLeft.Controls.Add(grpArchIdx);

            lblArchDir = new Label { Text = "Sin archivo", ForeColor = Color.Gray, Left = 8, Top = 20, Width = 265, AutoSize = false, Font = new Font("Segoe UI", 8F, FontStyle.Italic) };
            grpArchIdx.Controls.Add(lblArchDir);

            int ba = 40;
            AddButton(grpArchIdx, "📄 Crear Directorio", ref ba, out btnCrearDir, Color.FromArgb(0, 102, 204), btnCrearDir_Click);
            AddButton(grpArchIdx, "📂 Cargar Directorio", ref ba, out btnCargarDir, Color.FromArgb(0, 122, 204), btnCargarDir_Click);
            AddButton(grpArchIdx, "🔧 Reorganizar (purgar eliminados)", ref ba, out btnReorganizar, Color.FromArgb(130, 80, 0), btnReorganizar_Click);

            var pnlRight = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                Padding = new Padding(6),
                RowStyles = { new RowStyle(SizeType.Percent, 65), new RowStyle(SizeType.Percent, 35) }
            };
            splitIndexado.Panel2.Controls.Add(pnlRight);

            var lblEmp = new Label { Text = "Empleados activos:", Dock = DockStyle.Top, Height = 20, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            dgvEmpleados = BuildDgv();
            dgvEmpleados.Dock = DockStyle.Fill;
            dgvEmpleados.AllowUserToAddRows = false;
            dgvEmpleados.AllowUserToDeleteRows = false;
            dgvEmpleados.Columns.Add("ID", "ID");
            dgvEmpleados.Columns.Add("Nombre", "Nombre");
            dgvEmpleados.Columns.Add("Edad", "Edad");
            dgvEmpleados.Columns.Add("Email", "Email");
            dgvEmpleados.CellClick += dgvEmpleados_CellClick;

            var pEmp = new Panel { Dock = DockStyle.Fill };
            pEmp.Controls.Add(dgvEmpleados);
            pEmp.Controls.Add(lblEmp);
            pnlRight.Controls.Add(pEmp, 0, 0);

            var grpIdx = new GroupBox { Text = "Índice (clave → posición en archivo)", Dock = DockStyle.Fill };
            dgvIndice = BuildPropDgv();
            dgvIndice.Columns.Clear();
            dgvIndice.Columns.Add("Clave", "ID (Clave)");
            dgvIndice.Columns.Add("Posicion", "Posición");
            dgvIndice.Columns.Add("Estado", "Estado");
            dgvIndice.Dock = DockStyle.Fill;
            grpIdx.Controls.Add(dgvIndice);
            pnlRight.Controls.Add(grpIdx, 0, 1);
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        private void AddLabelTextBox(GroupBox parent, string label, ref int y, out TextBox txt,
                                     bool readOnly = false, string defVal = "")
        {
            var lbl = new Label { Text = label, Left = 8, Top = y + 2, Width = 72, AutoSize = false };
            txt = new TextBox { Left = 84, Top = y, Width = 185, ReadOnly = readOnly, Text = defVal };
            parent.Controls.Add(lbl);
            parent.Controls.Add(txt);
            y += 28;
        }

        private void AddButton(GroupBox parent, string text, ref int y, out Button btn,
                               Color color, EventHandler handler)
        {
            btn = new Button
            {
                Text = text,
                Left = 8,
                Top = y,
                Width = 260,
                Height = 28,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btn.Click += handler;
            parent.Controls.Add(btn);
            y += 32;
        }

        private Button MiniBtn(string text, Color color, EventHandler handler)
        {
            var btn = new Button
            {
                Text = text,
                Width = 82,
                Height = 28,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btn.Click += handler;
            return btn;
        }

        private DataGridView BuildDgv()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(245, 248, 252) }
            };
        }

        private DataGridView BuildPropDgv()
        {
            var dgv = BuildDgv();
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.Columns.Add("Propiedad", "Propiedad");
            dgv.Columns.Add("Valor", "Valor");
            return dgv;
        }
    }
}