using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

class ArchivoFAT
{
    public string Nombre { get; set; }
    public string RutaArchivoInicial { get; set; }
    public bool Papelera { get; set; } = false;
    public int TamanoTotal { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
    public DateTime? FechaEliminacion { get; set; }

    public ArchivoFAT(string nombre)
    {
        Nombre = nombre;
        RutaArchivoInicial = $"{nombre}_0.json";
        FechaCreacion = DateTime.Now;
        FechaModificacion = DateTime.Now;
    }
}

class BloqueDatos
{
    public string Datos { get; set; }
    public string SiguienteArchivo { get; set; }
    public bool EOF { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        const int TAMANO_BLOQUE = 20;
        string fatFilePath = "fat_table.json";
        List<ArchivoFAT> fatTable = new List<ArchivoFAT>();

        // Cargar la tabla FAT si existe
        if (File.Exists(fatFilePath))
        {
            string json = File.ReadAllText(fatFilePath);
            fatTable = JsonConvert.DeserializeObject<List<ArchivoFAT>>(json) ?? new List<ArchivoFAT>();
        }

        while (true)
        {
            Console.WriteLine("1. Crear archivo");
            Console.WriteLine("2. Listar archivos");
            Console.WriteLine("3. Abrir archivo");
            Console.WriteLine("4. Modificar archivo");
            Console.WriteLine("5. Eliminar archivo");
            Console.WriteLine("6. Recuperar archivo");
            Console.WriteLine("7. Salir");
            Console.Write("Seleccione una opción: ");
            string opcion = Console.ReadLine();

            if (opcion == "1")  // Crear archivo
            {
                Console.Write("Ingrese el nombre del archivo: ");
                string nombre = Console.ReadLine();
                Console.Write("Ingrese los datos del archivo: ");
                string datos = Console.ReadLine();

                var archivoFAT = new ArchivoFAT(nombre);
                fatTable.Add(archivoFAT);

                string rutaArchivo = archivoFAT.RutaArchivoInicial;
                int inicio = 0;
                string siguienteArchivo = null;

                // Guardar datos en bloques de 20 caracteres
                while (inicio < datos.Length)
                {
                    int longitud = Math.Min(TAMANO_BLOQUE, datos.Length - inicio);
                    string bloqueDatos = datos.Substring(inicio, longitud);

                    var bloque = new BloqueDatos
                    {
                        Datos = bloqueDatos,
                        SiguienteArchivo = siguienteArchivo,
                        EOF = inicio + longitud >= datos.Length
                    };

                    string rutaActual = Path.Combine("data", rutaArchivo);
                    File.WriteAllText(rutaActual, JsonConvert.SerializeObject(bloque));

                    siguienteArchivo = $"{Path.GetFileNameWithoutExtension(rutaArchivo)}_{(inicio / TAMANO_BLOQUE) + 1}.json";
                    rutaArchivo = siguienteArchivo;
                    inicio += longitud;
                }

                archivoFAT.TamanoTotal = datos.Length;

                // Guardar la tabla FAT
                File.WriteAllText(fatFilePath, JsonConvert.SerializeObject(fatTable));
                Console.WriteLine($"Archivo {nombre} creado con éxito.");
            }
            else if (opcion == "2")  // Listar archivos
            {
                int index = 1;
                foreach (var archivo in fatTable)
                {
                    if (!archivo.Papelera)
                    {
                        Console.WriteLine($"{index}. {archivo.Nombre} - {archivo.TamanoTotal} caracteres - Creado: {archivo.FechaCreacion} - Modificado: {archivo.FechaModificacion}");
                        index++;
                    }
                }
            }
            else if (opcion == "3")  // Abrir archivo
            {
                Console.Write("Ingrese el nombre del archivo a abrir: ");
                string nombreAbrir = Console.ReadLine();

                var archivoFAT = fatTable.Find(a => a.Nombre == nombreAbrir);
                if (archivoFAT != null && !archivoFAT.Papelera)
                {
                    Console.WriteLine($"Nombre: {archivoFAT.Nombre} - Tamaño: {archivoFAT.TamanoTotal} caracteres");
                    Console.WriteLine($"Creado: {archivoFAT.FechaCreacion} - Modificado: {archivoFAT.FechaModificacion}");

                    // Leer y mostrar contenido
                    string rutaArchivo = archivoFAT.RutaArchivoInicial;
                    string contenido = "";
                    string rutaActual = Path.Combine("data", rutaArchivo);

                    while (File.Exists(rutaActual))
                    {
                        string json = File.ReadAllText(rutaActual);
                        var bloque = JsonConvert.DeserializeObject<BloqueDatos>(json);
                        contenido += bloque.Datos;

                        if (bloque.EOF) break;
                        rutaActual = Path.Combine("data", bloque.SiguienteArchivo);
                    }

                    Console.WriteLine("Contenido: " + contenido);
                }
                else
                {
                    Console.WriteLine("Archivo no encontrado o está en la papelera.");
                }
            }
            else if (opcion == "4")  // Modificar archivo
            {
                Console.Write("Ingrese el nombre del archivo a modificar: ");
                string nombreModificar = Console.ReadLine();

                var archivoFAT = fatTable.Find(a => a.Nombre == nombreModificar);
                if (archivoFAT != null && !archivoFAT.Papelera)
                {
                    // Mostrar contenido actual
                    string rutaArchivo = archivoFAT.RutaArchivoInicial;
                    string contenido = "";
                    string rutaActual = Path.Combine("data", rutaArchivo);

                    while (File.Exists(rutaActual))
                    {
                        string json = File.ReadAllText(rutaActual);
                        var bloque = JsonConvert.DeserializeObject<BloqueDatos>(json);
                        contenido += bloque.Datos;

                        if (bloque.EOF) break;
                        rutaActual = Path.Combine("data", bloque.SiguienteArchivo);
                    }

                    Console.WriteLine("Contenido actual: " + contenido);
                    Console.WriteLine("Ingrese el nuevo contenido (teclee ESC para finalizar):");
                    string nuevosDatos = "";

                    while (true)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Escape) break;
                        nuevosDatos += key.KeyChar;
                    }

                    // Confirmar modificación
                    Console.WriteLine("\n¿Desea guardar los cambios? (S/N)");
                    if (Console.ReadLine().ToUpper() == "S")
                    {
                        // Eliminar archivos anteriores
                        rutaArchivo = archivoFAT.RutaArchivoInicial;
                        rutaActual = Path.Combine("data", rutaArchivo);
                        while (File.Exists(rutaActual))
                        {
                            File.Delete(rutaActual);
                            var bloque = JsonConvert.DeserializeObject<BloqueDatos>(File.ReadAllText(rutaActual));
                            if (bloque.EOF) break;
                            rutaActual = Path.Combine("data", bloque.SiguienteArchivo);
                        }

                        // Guardar nuevos datos en bloques de 20 caracteres
                        archivoFAT.FechaModificacion = DateTime.Now;
                        archivoFAT.TamanoTotal = nuevosDatos.Length;
                        string siguienteArchivo = null;
                        rutaArchivo = archivoFAT.RutaArchivoInicial;
                        int inicio = 0;

                        while (inicio < nuevosDatos.Length)
                        {
                            int longitud = Math.Min(TAMANO_BLOQUE, nuevosDatos.Length - inicio);
                            string bloqueDatos = nuevosDatos.Substring(inicio, longitud);

                            var bloque = new BloqueDatos
                            {
                                Datos = bloqueDatos,
                                SiguienteArchivo = siguienteArchivo,
                                EOF = inicio + longitud >= nuevosDatos.Length
                            };

                            string rutaNueva = Path.Combine("data", rutaArchivo);
                            File.WriteAllText(rutaNueva, JsonConvert.SerializeObject(bloque));

                            siguienteArchivo = $"{Path.GetFileNameWithoutExtension(rutaArchivo)}_{(inicio / TAMANO_BLOQUE) + 1}.json";
                            rutaArchivo = siguienteArchivo;
                            inicio += longitud;
                        }

                        // Guardar la tabla FAT actualizada
                        File.WriteAllText(fatFilePath, JsonConvert.SerializeObject(fatTable));
                        Console.WriteLine($"Archivo {nombreModificar} modificado con éxito.");
                    }
                }
                else
                {
                    Console.WriteLine("Archivo no encontrado o está en la papelera.");
                }
            }
            else if (opcion == "5")  // Eliminar archivo
            {
                Console.Write("Ingrese el nombre del archivo a eliminar: ");
                string nombreEliminar = Console.ReadLine();

                var archivoFAT = fatTable.Find(a => a.Nombre == nombreEliminar);
                if (archivoFAT != null && !archivoFAT.Papelera)
                {
                    archivoFAT.Papelera = true;
                    archivoFAT.FechaEliminacion = DateTime.Now;

                    // Guardar la tabla FAT
                    File.WriteAllText(fatFilePath, JsonConvert.SerializeObject(fatTable));
                    Console.WriteLine($"El archivo {nombreEliminar} ha sido movido a la papelera.");
                }
                else
                {
                    Console.WriteLine("Archivo no encontrado o ya está en la papelera.");
                }
            }
            else if (opcion == "6")  // Recuperar archivo
            {
                int index = 1;
                foreach (var archivo in fatTable)
                {
                    if (archivo.Papelera)
                    {
                        Console.WriteLine($"{index}. {archivo.Nombre} -
