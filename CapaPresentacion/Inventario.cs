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
    public partial class Inventario : Form
    {
        public Inventario()
        {
            InitializeComponent();
        }

        private void Inventario_Load(object sender, EventArgs e)
        {
            // Cargar datos iniciales en el DataGridView
            CargarDatos();

            // Configurar ComboBox
            cbobusqueda.Items.Add("Código");
            cbobusqueda.Items.Add("Nombre");
            cbobusqueda.SelectedIndex = 0; // Seleccionar un ítem por defecto si lo deseas
        }

        private void CargarDatos(string filtro = "", string busqueda = "")
        {
            // Cadena de conexión a la base de datos
            string connectionString = "Data Source=adn-script\\SQLEXPRESS;Initial Catalog=DBSISTEMA_INVENTARIO;User ID=sa;Password=Local;";

            // Consulta SQL para obtener los datos de inventario con el filtro aplicado
            string query = "SELECT Codigo AS 'Código del Producto', Nombre AS 'Nombre del Producto', Stock AS 'Cantidad en Stock' FROM PRODUCTO WHERE Estado = 1";

            // Agregar el filtro a la consulta si existe
            if (!string.IsNullOrEmpty(filtro))
            {
                query += filtro;
            }

            // Crear un DataTable para almacenar los datos
            DataTable dataTable = new DataTable();

            try
            {
                // Conectar a la base de datos y llenar el DataTable
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);

                    // Agregar el parámetro si se proporciona un término de búsqueda
                    if (!string.IsNullOrEmpty(busqueda))
                    {
                        command.Parameters.AddWithValue("@busqueda", $"%{busqueda}%");
                    }

                    SqlDataAdapter dataAdapter = new SqlDataAdapter(command);
                    dataAdapter.Fill(dataTable);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los datos: {ex.Message}");
            }

            // Asignar el DataTable al DataGridView
            dgvdata.DataSource = dataTable;
        }


        private void btnbuscar_Click(object sender, EventArgs e)
        {
            string filtro = "";
            string busqueda = txtbusqueda.Text.Trim();

            // Verificar si el ComboBox tiene una selección válida
            if (cbobusqueda.SelectedItem == null)
            {
                MessageBox.Show("Por favor, seleccione un campo de búsqueda.");
                return;
            }

            // Obtener el campo de búsqueda seleccionado
            string campoBusqueda = cbobusqueda.SelectedItem.ToString();

            // Construir el filtro basado en el campo de búsqueda y el texto ingresado
            if (!string.IsNullOrEmpty(busqueda))
            {
                switch (campoBusqueda)
                {
                    case "Código":
                        filtro = " AND Codigo LIKE @busqueda";
                        break;
                    case "Nombre":
                        filtro = " AND Nombre LIKE @busqueda";
                        break;
                    default:
                        MessageBox.Show("Campo de búsqueda no válido.");
                        return; // Salir del método para evitar la búsqueda con un campo no válido
                }
            }

            // Llamar al método para llenar la tabla con el filtro aplicado
            CargarDatos(filtro, busqueda);
        }

    }
}
