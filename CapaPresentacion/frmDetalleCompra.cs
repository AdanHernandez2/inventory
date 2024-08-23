using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapaPresentacion
{
    public partial class frmDetalleCompra : Form
    {
        private string connectionString = "Data Source=adn-script\\SQLEXPRESS;Initial Catalog=DBSISTEMA_INVENTARIO;User ID=sa;Password=Local;";

        public frmDetalleCompra()
        {
            InitializeComponent();
        }

        private void btnbuscar_Click(object sender, EventArgs e)
        {
            string queryCompra = "SELECT c.IdCompra, c.NumeroDocumento, c.FechaRegistro, c.TipoDocumento, " +
                      "u.NombreCompleto, p.Documento AS DocumentoProveedor, p.RazonSocial " +
                      "FROM COMPRA c " +
                      "INNER JOIN USUARIO u ON c.IdUsuario = u.Documento " +
                      "INNER JOIN PROVEEDOR p ON c.IdProveedor = p.Documento " +
                      "WHERE c.NumeroDocumento = @NumeroDocumento";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(queryCompra, conn);
                cmd.Parameters.AddWithValue("@NumeroDocumento", txtbusqueda.Text);
                conn.Open();

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    txtnumerodocumento.Text = reader["NumeroDocumento"].ToString();
                    txtfecha.Text = Convert.ToDateTime(reader["FechaRegistro"]).ToString("yyyy-MM-dd");
                    txttipodocumento.Text = reader["TipoDocumento"].ToString();
                    txtusuario.Text = reader["NombreCompleto"].ToString();
                    txtdocproveedor.Text = reader["DocumentoProveedor"].ToString();
                    txtnombreproveedor.Text = reader["RazonSocial"].ToString();

                    // Asegúrate de que IdCompra esté presente en el lector
                    LoadCompraDetails(Convert.ToInt32(reader["IdCompra"]));
                }

                reader.Close();
            }
        }

        private void LoadCompraDetails(int idCompra)
        {
            string queryDetalles = "SELECT dc.*, p.Nombre FROM DETALLE_COMPRA dc " +
                                   "INNER JOIN PRODUCTO p ON dc.IdProducto = p.Codigo " +
                                   "WHERE dc.IdCompra = @IdCompra";

            dgvdata.Rows.Clear();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(queryDetalles, conn);
                cmd.Parameters.AddWithValue("@IdCompra", idCompra);
                conn.Open();

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    dgvdata.Rows.Add(new object[]
                    {
                    reader["Nombre"].ToString(),
                    reader["PrecioCompra"].ToString(),
                    reader["Cantidad"].ToString(),
                    reader["MontoTotal"].ToString()
                    });
                }

                reader.Close();
            }

            string queryTotal = "SELECT ISNULL(SUM(MontoTotal), 0) FROM DETALLE_COMPRA WHERE IdCompra = @IdCompra";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(queryTotal, conn);
                cmd.Parameters.AddWithValue("@IdCompra", idCompra);
                conn.Open();

                txtmontototal.Text = cmd.ExecuteScalar().ToString();
            }
        }

        private void btnborrar_Click(object sender, EventArgs e)
        {
            txtfecha.Text = "";
            txttipodocumento.Text = "";
            txtusuario.Text = "";
            txtdocproveedor.Text = "";
            txtnombreproveedor.Text = "";

            dgvdata.Rows.Clear();
            txtmontototal.Text = "0.00";
        }

        private void btndescargar_Click(object sender, EventArgs e)
        {
            if (txttipodocumento.Text == "")
            {
                MessageBox.Show("No se encontraron resultados", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            string Texto_Html = Properties.Resources.PlantillaCompra;
            string queryNegocio = "SELECT * FROM [NEGOCIO]"; // Asume que tienes una tabla para los datos del negocio

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(queryNegocio, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    Texto_Html = Texto_Html.Replace("@nombrenegocio", reader["Nombre"].ToString().ToUpper());
                    Texto_Html = Texto_Html.Replace("@docnegocio", reader["RIF"].ToString());
                    Texto_Html = Texto_Html.Replace("@direcnegocio", reader["Direccion"].ToString());
                }

                reader.Close();
            }

            Texto_Html = Texto_Html.Replace("@tipodocumento", txttipodocumento.Text.ToUpper());
            Texto_Html = Texto_Html.Replace("@numerodocumento", txtnumerodocumento.Text);
            Texto_Html = Texto_Html.Replace("@docproveedor", txtdocproveedor.Text);
            Texto_Html = Texto_Html.Replace("@nombreproveedor", txtnombreproveedor.Text);
            Texto_Html = Texto_Html.Replace("@fecharegistro", txtfecha.Text);
            Texto_Html = Texto_Html.Replace("@usuarioregistro", txtusuario.Text);

            string filas = string.Empty;
            foreach (DataGridViewRow row in dgvdata.Rows)
            {
                if (row.IsNewRow) continue;

                filas += "<tr>";
                filas += "<td>" + row.Cells["Producto"].Value.ToString() + "</td>";
                filas += "<td>" + row.Cells["PrecioCompra"].Value.ToString() + "</td>";
                filas += "<td>" + row.Cells["Cantidad"].Value.ToString() + "</td>";
                filas += "<td>" + row.Cells["SubTotal"].Value.ToString() + "</td>";
                filas += "</tr>";
            }
            Texto_Html = Texto_Html.Replace("@filas", filas);
            Texto_Html = Texto_Html.Replace("@montototal", txtmontototal.Text);

            SaveFileDialog savefile = new SaveFileDialog
            {
                FileName = $"Compra_{txtnumerodocumento.Text}.pdf",
                Filter = "Pdf Files|*.pdf"
            };

            if (savefile.ShowDialog() == DialogResult.OK)
            {
                using (FileStream stream = new FileStream(savefile.FileName, FileMode.Create))
                {
                    Document pdfDoc = new Document(PageSize.A4, 25, 25, 25, 25);
                    PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                    pdfDoc.Open();

                    byte[] byteImage = null;

                    // Asume que tienes una columna Logo en tu tabla de negocio
                    string queryLogo = "SELECT Logo FROM [NEGOCIO]";
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        SqlCommand cmd = new SqlCommand(queryLogo, conn);
                        conn.Open();
                        byteImage = (byte[])cmd.ExecuteScalar();
                        conn.Close();
                    }

                    if (byteImage != null)
                    {
                        iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(byteImage);
                        img.ScaleToFit(60, 60);
                        img.Alignment = iTextSharp.text.Image.UNDERLYING;
                        img.SetAbsolutePosition(pdfDoc.Left, pdfDoc.GetTop(51));
                        pdfDoc.Add(img);
                    }

                    using (StringReader sr = new StringReader(Texto_Html))
                    {
                        XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, sr);
                    }

                    pdfDoc.Close();
                    stream.Close();
                    MessageBox.Show("Documento Generado", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void frmDetalleCompra_Load(object sender, EventArgs e)
        {
            // Inicialización del formulario si es necesario
        }
    }
}
