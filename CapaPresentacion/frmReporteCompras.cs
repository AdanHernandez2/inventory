using CapaPresentacion.Utilidades;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapaPresentacion
{

    public partial class frmReporteCompras : Form
    {
        private string connectionString = "Data Source=Rosibell-PC\\SQLEXPRESS;Initial Catalog=DBSISTEMA_INVENTARIO; Integrated Security=True;";

        public frmReporteCompras()
        {
            InitializeComponent();
        }

        private void frmReporteCompras_Load(object sender, EventArgs e)
        {
            CargarProveedores();
            CargarBusquedaColumnas();
        }

        private void CargarProveedores()
        {
            string query = "SELECT Documento, RazonSocial FROM PROVEEDOR WHERE Estado = 1";
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                cboproveedor.Items.Add(new OpcionCombo() { Valor = "0", Texto = "TODOS" });
                while (reader.Read())
                {
                    cboproveedor.Items.Add(new OpcionCombo() { Valor = reader["Documento"].ToString(), Texto = reader["RazonSocial"].ToString() });
                }
                cboproveedor.DisplayMember = "Texto";
                cboproveedor.ValueMember = "Valor";
                cboproveedor.SelectedIndex = 0;
            }
        }

        private void CargarBusquedaColumnas()
        {
            foreach (DataGridViewColumn columna in dgvdata.Columns)
            {
                cbobusqueda.Items.Add(new OpcionCombo() { Valor = columna.Name, Texto = columna.HeaderText });
            }
            cbobusqueda.DisplayMember = "Texto";
            cbobusqueda.ValueMember = "Valor";
            cbobusqueda.SelectedIndex = 0;
        }

        private void btnbuscarresultado_Click(object sender, EventArgs e)
        {
            string idProveedor = ((OpcionCombo)cboproveedor.SelectedItem).Valor.ToString();
            string fechaInicio = txtfechainicio.Value.ToString("yyyy-MM-dd");
            string fechaFin = txtfechafin.Value.Date.AddDays(1).AddTicks(-1).ToString("yyyy-MM-dd HH:mm:ss");

            string query = @"
        SELECT c.FechaRegistro, c.TipoDocumento, c.NumeroDocumento, c.MontoTotal, u.NombreCompleto AS UsuarioRegistro, 
               p.Documento AS DocumentoProveedor, p.RazonSocial, dc.IdProducto AS CodigoProducto, pr.Nombre AS NombreProducto, 
               ca.Descripcion AS Categoria, dc.PrecioCompra, dc.PrecioVenta, dc.Cantidad, dc.MontoTotal AS SubTotal
        FROM COMPRA c
        INNER JOIN USUARIO u ON c.IdUsuario = u.Documento
        INNER JOIN PROVEEDOR p ON c.IdProveedor = p.Documento
        INNER JOIN DETALLE_COMPRA dc ON c.IdCompra = dc.IdCompra
        INNER JOIN PRODUCTO pr ON dc.IdProducto = pr.Codigo
        INNER JOIN CATEGORIA ca ON pr.IdCategoria = ca.IdCategoria
        WHERE c.FechaRegistro BETWEEN @FechaInicio AND @FechaFin
        AND (@IdProveedor = '0' OR c.IdProveedor = @IdProveedor)";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                cmd.Parameters.AddWithValue("@FechaFin", fechaFin);
                cmd.Parameters.AddWithValue("@IdProveedor", idProveedor);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                dgvdata.Rows.Clear();

                while (reader.Read())
                {
                    dgvdata.Rows.Add(new object[] {
                    reader["FechaRegistro"],
                    reader["TipoDocumento"],
                    reader["NumeroDocumento"],
                    reader["MontoTotal"],
                    reader["UsuarioRegistro"],
                    reader["DocumentoProveedor"],
                    reader["RazonSocial"],
                    reader["CodigoProducto"],
                    reader["NombreProducto"],
                    reader["Categoria"],
                    reader["PrecioCompra"],
                    reader["PrecioVenta"],
                    reader["Cantidad"],
                    reader["SubTotal"]
                });
                }
            }
        }

        private void btnexportar_Click(object sender, EventArgs e)
        {
            if (dgvdata.Rows.Count < 1)
            {
                MessageBox.Show("No hay registros para exportar", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            DataTable dt = new DataTable();

            foreach (DataGridViewColumn columna in dgvdata.Columns)
            {
                dt.Columns.Add(columna.HeaderText, typeof(string));
            }

            foreach (DataGridViewRow row in dgvdata.Rows)
            {
                if (row.Visible)
                {
                    dt.Rows.Add(new object[] {
                    row.Cells[0].Value?.ToString(),
                    row.Cells[1].Value?.ToString(),
                    row.Cells[2].Value?.ToString(),
                    row.Cells[3].Value?.ToString(),
                    row.Cells[4].Value?.ToString(),
                    row.Cells[5].Value?.ToString(),
                    row.Cells[6].Value?.ToString(),
                    row.Cells[7].Value?.ToString(),
                    row.Cells[8].Value?.ToString(),
                    row.Cells[9].Value?.ToString(),
                    row.Cells[10].Value?.ToString(),
                    row.Cells[11].Value?.ToString(),
                    row.Cells[12].Value?.ToString(),
                    row.Cells[13].Value?.ToString()
                });
                }
            }

            SaveFileDialog savefile = new SaveFileDialog
            {
                FileName = $"ReporteCompras_{DateTime.Now:ddMMyyyyHHmmss}.xlsx",
                Filter = "Excel Files | *.xlsx"
            };

            if (savefile.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (XLWorkbook wb = new XLWorkbook())
                    {
                        var hoja = wb.Worksheets.Add(dt, "Informe");
                        hoja.ColumnsUsed().AdjustToContents();
                        wb.SaveAs(savefile.FileName);
                    }
                    MessageBox.Show("Reporte Generado", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch
                {
                    MessageBox.Show("Error al generar reporte", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void btnbuscar_Click(object sender, EventArgs e)
        {
            string columnaFiltro = ((OpcionCombo)cbobusqueda.SelectedItem).Valor.ToString();

            if (dgvdata.Rows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvdata.Rows)
                {
                    if (row.Cells[columnaFiltro]?.Value?.ToString().Trim().ToUpper().Contains(txtbusqueda.Text.Trim().ToUpper()) == true)
                        row.Visible = true;
                    else
                        row.Visible = false;
                }
            }
        }

        private void btnlimpiarbuscador_Click(object sender, EventArgs e)
        {
            txtbusqueda.Text = "";
            foreach (DataGridViewRow row in dgvdata.Rows)
            {
                row.Visible = true;
            }
        }
    }

    public class OpcionCombo
    {
        public object Valor { get; set; }
        public string Texto { get; set; }
    }


}