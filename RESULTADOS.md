# Resultados del Taller 3

## 1. Exploración del tutorial

### 1.1 MostrarRecursos

Cambios: cambié `LIMIT 2` por `LIMIT 5` y agregué una segunda consulta
(`SELECT COUNT(*) FROM Products`) con el mismo comando, después de liberar el lector.

Resultado: salieron 5 nombres en vez de 2 y la segunda consulta devolvió 77.

Estados de la conexión: Closed antes de Open, Open después de Open y Open
después de liberar el lector.

Explicación: liberar el lector no cierra la conexión, solo libera el lector.
La segunda consulta funciona porque el primer lector ya se liberó, con el lector
abierto, esa conexión no podría ejecutar otra consulta. El `using` de arriba del
archivo solo importa nombres de clases; el `using` dentro del método libera un
objeto al terminar su bloque, así que no cumplen la misma función.

### 1.2 MostrarParametros

Cambios: no modifiqué el código; probé las entradas 1, 9999, 0 y abc.

Resultados:
- 1: Producto encontrado: Chai.
- 9999: No existe un producto con ese ID.
- 0: El ID debe ser un número entero mayor que cero.
- abc: El ID debe ser un número entero mayor que cero.

Explicación: 1 y 9999 pasan la validación y ejecutan la consulta con el
parámetro @id; 9999 no encuentra fila y ExecuteScalar devuelve null. 0 y abc
se rechazan antes de enviar SQL por el int.TryParse y la condición idProducto <= 0.

### 1.3 MostrarTiposYNulos

Cambios: cambié el valor del parámetro @id de 5 a 1 y luego a 99999.

Resultados:
- @id = 1: Chai con precio 18,00, más las dos filas simuladas.
- @id = 99999: solo las dos filas simuladas; la fila real desapareció.

Explicación: con 99999 el WHERE no encuentra producto, así que no sale su fila.
Las filas simuladas siguen porque vienen de los UNION ALL. El precio 0,00 es un
valor conocido; "sin registrar" es NULL y se detecta con IsDBNull antes de leer.

### 1.4 MostrarErrorConsulta

Cambios: cambié TablaInexistenteDemoAdoNet por Products en la consulta.

Resultados:
- Original: error 1146, "Table 'northwind.tablainexistentedemoadonet' doesn't
  exist", y la conexión seguía funcionando.
- Con Products: "La tabla existe; no se reprodujo el fallo esperado".

Explicación: con Products la consulta es válida, no se lanza excepción y el
catch no se activa. En el original, el catch con when solo atrapa el código
1146 (tabla inexistente).

### 1.5 MostrarErrorRestriccion

Cambios: antes del segundo ExecuteNonQuery agregué
`comando.Parameters["@id"].Value = 2;`.

Resultados:
- Original: error 1062 por clave duplicada, atrapado por el catch.
- Con ID 2: "Primera inserción correcta: ID 1." y después
  "Sesión de prueba cerrada; tabla temporal eliminada." sin ningún error.

Explicación: con dos claves distintas no se viola la clave primaria, no se lanza
excepción y el catch no se activa. El catch con when solo atrapa el código 1062.

### 1.6 Error de consulta frente a error de conexión

En 4.1 la conexión ya estaba abierta y falló la consulta (código 1146), y la
conexión siguió funcionando. En 4.2 falló la conexión (código 1042) antes de
enviar cualquier SQL, así que esa nunca llegó a abrirse.

### Tutorial 5: MostrarTransacciones

Cambios: cambié los dos importes de TransferirSaldo de 20m a 10m y ajusté
los mensajes que anuncian el importe.

Resultados:
- Estado inicial: cuenta 1 = 100,00 y cuenta 2 = 50,00.
- Caso A (Commit): 90,00 y 60,00.
- Caso B (Rollback): siguen en 90,00 y 60,00.

Explicación: en el caso A el descuento y el abono se confirmaron juntos con
Commit. En el caso B el descuento se ejecutó, pero la inserción duplicada
falló (error 1062) antes del abono y el Rollback deshizo ese descuento. Por eso
los saldos quedan como estaban después del Commit del caso A.

## 2. Casos de prueba

Todas las pruebas se hicieron sobre la misma base, sin modificarla. En los listados largos dejo solo algunas filas y marco el recorte con `...`.

### Caso 1: precio máximo 20 y stock mínimo 10

```
Precio máximo por unidad: 20
Existencias mínimas: 10
33 | Geitost | 2,50 | 112 | 500 g
13 | Konbu | 6,00 | 24 | 2 kg box
52 | Filo Mix | 7,00 | 38 | 16 - 2 kg boxes
...
57 | Ravioli Angelo | 19,50 | 36 | 24 - 250 g pkgs.
49 | Maxilaku | 20,00 | 10 | 24 - 50 g pkgs.
Total según COUNT(*): 32 (filas mostradas: 32)
```

Salieron 32 filas, todas con precio menor o igual a 20 y stock mayor o igual a 10, y el conteo coincide con el listado.

### Caso 2: precio máximo 0 y stock mínimo 0

```
Precio máximo por unidad: 0
Existencias mínimas: 0
No hay productos que cumplan los filtros
Total según COUNT(*): 0 (filas mostradas: 0)
```

El catálogo no tiene productos con precio 0, así que no hay coincidencias. El programa muestra el mensaje y sigue funcionando.

### Caso 3: precio máximo 1000 y stock mínimo 10000

```
Precio máximo por unidad: 1000
Existencias mínimas: 10000
No hay productos que cumplan los filtros
Total según COUNT(*): 0 (filas mostradas: 0)
```

Ningún producto tiene 10000 unidades. Sale el mensaje de ausencia de coincidencias y no un mensaje de error de MySQL, porque la consulta se ejecutó bien y solo devolvió cero filas.

### Caso 4: precio o stock negativo

```
Precio máximo por unidad: -5
El valor no puede ser negativo. Intenta de nuevo.
...
Existencias mínimas: -1
El valor no puede ser negativo. Intenta de nuevo.
```

Los dos valores se rechazaron antes de enviar la consulta, y el programa volvió a pedir el dato.

### Caso 5: texto en cada entrada numérica

```
Precio máximo por unidad: abc
Entrada no numérica. Intenta de nuevo.
...
Existencias mínimas: abc
Debe ser un número entero. Intenta de nuevo.
```

En ambas entradas el programa informó el problema y no se cerró.

### Caso 6: stock con decimales

```
Existencias mínimas: 2,5
Debe ser un número entero. Intenta de nuevo.
```

Se rechazó porque el stock se lee como `int`.

### Caso 7: límites inclusivos

Elegí Maxilaku (ProductID 49), que tiene precio 20,00 y stock 10. Con precio máximo 20 y stock mínimo 10 aparece en el listado, en la última fila:

```
49 | Maxilaku | 20,00 | 10 | 24 - 50 g pkgs.
```

Esto confirma que los dos límites incluyen el valor exacto (`<=` y `>=`).

### Caso 8: presentación NULL

Cambié temporalmente `QuantityPerUnit` por `CAST(NULL AS CHAR) AS QuantityPerUnit` en la consulta, sin tocar filas de la base.

```
33 | Geitost | 2,50 | 112 | Presentación sin registrar
13 | Konbu | 6,00 | 24 | Presentación sin registrar
52 | Filo Mix | 7,00 | 38 | Presentación sin registrar
...
49 | Maxilaku | 20,00 | 10 | Presentación sin registrar
Total según COUNT(*): 32 (filas mostradas: 32)
```

Las 32 filas muestran el texto de reemplazo y el conteo no cambia. Después restauré la consulta normal.

### Caso 9: nombre con coincidencias

Fragmento ingresado: `ch`

```
1 | Chai | 18,00 | 39
2 | Chang | 19,00 | 17
39 | Chartreuse verte | 18,00 | 69
4 | Chef Anton's Cajun Seasoning | 22,00 | 53
48 | Chocolade | 12,75 | 15
56 | Gnocchi di nonna Alice | 38,00 | 21
26 | Gumbär Gummibärchen | 31,23 | 15
41 | Jack's New England Clam Chowder | 9,65 | 85
55 | Pâté chinois | 24,00 | 115
12 | Queso Manchego La Pastora | 38,00 | 86
34 | Sasquatch Ale | 14,00 | 111
27 | Schoggi Schokolade | 43,90 | 49
19 | Teatime Chocolate Biscuits | 9,20 | 25
```

Salieron 13 productos, todos con "ch" en el nombre y ordenados alfabéticamente por nombre. La consulta filtra `Discontinued = 0`, así que solo salen productos activos. MySQL no distingue mayúsculas y minúsculas en este `LIKE`, por eso aparece "Clam Chowder", donde la coincidencia es con "Ch" en mayúscula.

### Caso 10: nombre sin coincidencias

```
Fragmento del nombre del producto: zzzzsincoincidencias999
No hay productos activos que coincidan con el nombre
```

Sale el mensaje de ausencia de coincidencias y no se trata como error.

### Caso 11: nombre vacío

```
Fragmento del nombre del producto:
El texto no puede estar vacío. Intenta de nuevo.
Fragmento del nombre del producto:
El texto no puede estar vacío. Intenta de nuevo.
Fragmento del nombre del producto:
El texto no puede estar vacío. Intenta de nuevo.
```

Tanto presionar Enter sin escribir como escribir solo espacios se rechazaron antes de ejecutar SQL.

## 3. Orden ascendente y descendente

Con precio máximo 20 y stock mínimo 10:

- **Ascendente (`ORDER BY UnitPrice ASC, ProductID ASC`):** Geitost (2,50) sale primero y Maxilaku (20,00) al final.
- **Descendente (`ORDER BY UnitPrice DESC, ProductID ASC`):** Maxilaku (20,00) sale primero y Geitost (2,50) al final.

En los dos casos salen los mismos 32 productos y el conteo es 32. Cambió solo su posición, no cuáles se encuentran. Los productos con el mismo precio conservan el orden por `ProductID` ascendente (por ejemplo, con 18,00 salen el 1, 35, 39 y 76 en ambos listados). Para la entrega restauré el orden ascendente.

## 4. Explicación


