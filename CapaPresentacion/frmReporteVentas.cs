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

    public partial class frmReporteVentas : Form
    {
        private string connectionString = "Data Source=Rosibell-PC\\SQLEXPRESS;Initial Catalog=DBSISTEMA_INVENTARIO; Integrated Security=True;";

        public frmReporteVentas()
        {
            InitializeComponent();
        }

        private void frmReporteVentas_Load(object sender, EventArgs e)
        {
            foreach (DataGridViewColumn columna in dgvdata.Columns)
            {
                cbobusqueda.Items.Add(new { Valor = columna.Name, Texto = columna.HeaderText });
            }
            cbobusqueda.DisplayMember = "Texto";
            cbobusqueda.ValueMember = "Valor";
            cbobusqueda.SelectedIndex = 0;
        }

        private void btnbuscarreporte_Click(object sender, EventArgs e)
        {
            string fechainicio = txtfechainicio.Value.Date.ToString("yyyy-MM-dd");
            string fechafin = txtfechafin.Value.Date.AddDays(1).AddTicks(-1).ToString("yyyy-MM-dd HH:mm:ss");

            // Ajusta la consulta SQL según la estructura de la base de datos
            string query = "SELECT v.FechaRegistro AS RegistrationDate, " +
                           "v.TipoDocumento AS DocumentType, " +
                           "v.NumeroDocumento AS DocumentNumber, " +
                           "v.MontoTotal AS TotalAmount, " +
                           "v.IdUsuario AS UsuarioRegistro, " + // Columna añadida
                           "v.DocumentoCliente AS ClientDocument, " +
                           "v.NombreCliente AS ClientName, " +
                           "p.Codigo AS ProductCode, " +
                           "p.Nombre AS ProductName, " +
                           "c.Descripcion AS Category, " +
                           "dv.PrecioVenta AS RentalPrice, " +
                           "dv.Cantidad AS Quantity, " +
                           "dv.SubTotal AS SubTotal " +
                           "FROM VENTA v " +
                           "INNER JOIN DETALLE_VENTA dv ON v.IdVenta = dv.IdVenta " +
                           "INNER JOIN PRODUCTO p ON dv.IdProducto = p.Codigo " +
                           "INNER JOIN CATEGORIA c ON p.IdCategoria = c.IdCategoria " +
                           "WHERE v.FechaRegistro BETWEEN @fechainicio AND @fechafin";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@fechainicio", fechainicio);
                cmd.Parameters.AddWithValue("@fechafin", fechafin);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                dgvdata.Rows.Clear();

                while (reader.Read())
                {
                    dgvdata.Rows.Add(new object[]
                    {
                reader["RegistrationDate"],
                reader["DocumentType"],
                reader["DocumentNumber"],
                reader["TotalAmount"],
                reader["UsuarioRegistro"], // Columna añadida
                reader["ClientDocument"],
                reader["ClientName"],
                reader["ProductCode"],
                reader["ProductName"],
                reader["Category"],
                reader["RentalPrice"],
                reader["Quantity"],
                reader["SubTotal"]
                    });
                }
            }
        }



        private void btnbuscar_Click(object sender, EventArgs e)
        {
            string columnaFiltro = ((dynamic)cbobusqueda.SelectedItem).Valor.ToString();

            if (dgvdata.Rows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvdata.Rows)
                {
                    if (row.Cells[columnaFiltro].Value != null &&
                        row.Cells[columnaFiltro].Value.ToString().Trim().ToUpper().Contains(txtbusqueda.Text.Trim().ToUpper()))
                    {
                        row.Visible = true;
                    }
                    else
                    {
                        row.Visible = false;
                    }
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
                    dt.Rows.Add(new object[]
                    {
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
                    row.Cells[12].Value?.ToString()
                    });
                }
            }

            SaveFileDialog savefile = new SaveFileDialog
            {
                FileName = $"ReporteVentas_{DateTime.Now:ddMMyyyyHHmmss}.xlsx",
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
    }

}
