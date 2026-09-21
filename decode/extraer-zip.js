const fs = require("fs");
const path = require("path");

const jsonPath = path.join(__dirname, "templates.json");
const zipPath = path.join(__dirname, "templates_830081407.zip");

try {
    // Leer archivo
    const contenido = fs.readFileSync(jsonPath, "utf8");

    console.log("Leyendo:", jsonPath);
    console.log("Primeros caracteres:", contenido.substring(0, 30));

    // Parsear JSON
    const data = JSON.parse(contenido);

    console.log("JSON válido.");
    console.log("NIT:", data.nit);
    console.log("Archivo:", data.fileName);
    console.log("Tipo:", data.contentType);
    console.log("Archivos:", data.fileCount);

    // Decodificar Base64
    const buffer = Buffer.from(data.contentBase64, "base64");

    console.log("Bytes obtenidos:", buffer.length);

    // Validar ZIP
    if (buffer[0] !== 0x50 || buffer[1] !== 0x4B) {
        throw new Error("El contenido no es un ZIP válido.");
    }

    // Guardar ZIP
    fs.writeFileSync(zipPath, buffer);

    console.log("\n✅ ZIP creado correctamente:");
    console.log(zipPath);

} catch (error) {
    console.error("\n❌ Error:");
    console.error(error.message);
}