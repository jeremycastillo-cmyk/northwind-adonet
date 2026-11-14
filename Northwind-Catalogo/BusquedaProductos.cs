// Autor: Jeremy Thorp
// Descripción: Taller 3 de Programación III. Búsquedas de productos activos
// con ADO.NET (parámetros, ExecuteReader y ExecuteScalar) sobre Northwind.

using System;
using System.Globalization;
using MySql.Data.MySqlClient; // si tu proyecto usa MySqlConnector, cambia por: using MySqlConnector;

namespace Northwind.Catalogo
{
    public static class BusquedaProductos
    {
        // Consulta 1: productos activos por precio máximo y existencias mínimas
        private const string SqlBusqueda =
            "SELECT ProductID, ProductName, UnitPrice, UnitsInStock, QuantityPerUnit " +
            "FROM Products " +
            "WHERE UnitPrice <= @precioMax AND UnitsInStock >= @stockMin AND Discontinued = 0 " +
            "ORDER BY UnitPrice ASC, ProductID ASC";

        // Consulta 2: mismos filtros, solo el conteo
        private const string SqlConteo =
            "SELECT COUNT(*) " +
            "FROM Products " +
            "WHERE UnitPrice <= @precioMax AND UnitsInStock >= @stockMin AND Discontinued = 0";

        // Consulta 3: productos activos cuyo nombre contiene el fragmento
        private const string SqlPorNombre =
            "SELECT ProductID, ProductName, UnitPrice, UnitsInStock " +
            "FROM Products " +
            "WHERE Discontinued = 0 AND ProductName LIKE @patron " +
            "ORDER BY ProductName ASC, ProductID ASC";

        public static void BuscarPorPrecioYStock(MySqlConnection conexion)
        {
            Console.WriteLine("\n=== Búsqueda por precio máximo y existencias mínimas ===");

            decimal precioMax = PedirDecimalNoNegativo("Precio máximo por unidad: ");
            int stockMin = PedirEnteroNoNegativo("Existencias mínimas: ");

            int filasMostradas = 0;

            try
            {
                using (var comando = new MySqlCommand(SqlBusqueda, conexion))
                {
                    comando.Parameters.AddWithValue("@precioMax", precioMax);
                    comando.Parameters.AddWithValue("@stockMin", stockMin);

                    using (MySqlDataReader lector = comando.ExecuteReader())
                    {
                        int ordID = lector.GetOrdinal("ProductID");
                        int ordNombre = lector.GetOrdinal("ProductName");
                        int ordPrecio = lector.GetOrdinal("UnitPrice");
                        int ordStock = lector.GetOrdinal("UnitsInStock");
                        int ordPresentacion = lector.GetOrdinal("QuantityPerUnit");

                        while (lector.Read())
                        {
                            int id = Convert.ToInt32(lector.GetValue(ordID));
                            string nombre = lector.GetString(ordNombre);
                            decimal precio = lector.GetDecimal(ordPrecio);
                            int stock = Convert.ToInt32(lector.GetValue(ordStock));
                            string presentacion = lector.IsDBNull(ordPresentacion)
                                ? "Presentación sin registrar"
                                : lector.GetString(ordPresentacion);

                            Console.WriteLine(
                                $"{id} | {nombre} | {precio:F2} | {stock} | {presentacion}");
                            filasMostradas++;
                        }
                    } // el lector se libera aquí, antes del conteo
                }

                if (filasMostradas == 0)
                {
                    Console.WriteLine("No hay productos que cumplan los filtros");
                }

                // El conteo se ejecuta después de liberar el lector anterior
                using (var comandoConteo = new MySqlCommand(SqlConteo, conexion))
                {
                    comandoConteo.Parameters.AddWithValue("@precioMax", precioMax);
                    comandoConteo.Parameters.AddWithValue("@stockMin", stockMin);

                    int total = Convert.ToInt32(comandoConteo.ExecuteScalar());
                    Console.WriteLine($"Total según COUNT(*): {total} (filas mostradas: {filasMostradas})");
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Error de MySQL ({ex.Number}): {ex.Message}");
            }
        }

        public static void BuscarPorNombre(MySqlConnection conexion)
        {
            Console.WriteLine("\n=== Búsqueda por parte del nombre ===");

            string fragmento = PedirTextoNoVacio("Fragmento del nombre del producto: ");
            string patron = "%" + fragmento + "%";

            int filasMostradas = 0;

            try
            {
                using (var comando = new MySqlCommand(SqlPorNombre, conexion))
                {
                    comando.Parameters.AddWithValue("@patron", patron);

                    using (MySqlDataReader lector = comando.ExecuteReader())
                    {
                        int ordID = lector.GetOrdinal("ProductID");
                        int ordNombre = lector.GetOrdinal("ProductName");
                        int ordPrecio = lector.GetOrdinal("UnitPrice");
                        int ordStock = lector.GetOrdinal("UnitsInStock");

                        while (lector.Read())
                        {
                            int id = Convert.ToInt32(lector.GetValue(ordID));
                            string nombre = lector.GetString(ordNombre);
                            decimal precio = lector.GetDecimal(ordPrecio);
                            int stock = Convert.ToInt32(lector.GetValue(ordStock));

                            Console.WriteLine($"{id} | {nombre} | {precio:F2} | {stock}");
                            filasMostradas++;
                        }
                    }
                }

                if (filasMostradas == 0)
                {
                    Console.WriteLine("No hay productos activos que coincidan con el nombre");
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Error de MySQL ({ex.Number}): {ex.Message}");
            }
        }

        // ---------- Validación de entradas (antes de ejecutar SQL) ----------

        private static decimal PedirDecimalNoNegativo(string mensaje)
        {
            while (true)
            {
                Console.Write(mensaje);
                string? entrada = Console.ReadLine();

                if (!decimal.TryParse(entrada,
                        NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                        CultureInfo.CurrentCulture, out decimal valor))
                {
                    Console.WriteLine("Entrada no numérica. Intenta de nuevo.");
                    continue;
                }
                if (valor < 0)
                {
                    Console.WriteLine("El valor no puede ser negativo. Intenta de nuevo.");
                    continue;
                }
                return valor;
            }
        }

        private static int PedirEnteroNoNegativo(string mensaje)
        {
            while (true)
            {
                Console.Write(mensaje);
                string entrada = Console.ReadLine();

                // NumberStyles.Integer no acepta decimales, así que "5.5" se rechaza
                if (!int.TryParse(entrada, NumberStyles.Integer, CultureInfo.CurrentCulture, out int valor))
                {
                    Console.WriteLine("Debe ser un número entero. Intenta de nuevo.");
                    continue;
                }
                if (valor < 0)
                {
                    Console.WriteLine("El valor no puede ser negativo. Intenta de nuevo.");
                    continue;
                }
                return valor;
            }
        }

        private static string PedirTextoNoVacio(string mensaje)
        {
            while (true)
            {
                Console.Write(mensaje);
                string texto = (Console.ReadLine() ?? string.Empty).Trim();

                if (texto.Length == 0)
                {
                    Console.WriteLine("El texto no puede estar vacío. Intenta de nuevo.");
                    continue;
                }
                return texto;
            }
        }
    }
}
