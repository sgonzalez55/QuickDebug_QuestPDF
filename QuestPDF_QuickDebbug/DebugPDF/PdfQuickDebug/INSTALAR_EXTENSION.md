# 🔧 INSTALAR EXTENSIÓN C# PARA DEBUGGING

## ⚠️ IMPORTANTE: Sin esta extensión NO funcionan los breakpoints

Para que funcione F5 y los puntos de interrupción (breakpoints) en VS Code:

### Paso 1: Instalar C# Dev Kit

1. Abre VS Code
2. Presiona `Ctrl+Shift+X` (o click en el ícono de extensiones 📦)
3. En el buscador escribe: **C# Dev Kit**
4. Click en "Install" en la extensión de Microsoft
5. **Reinicia VS Code** (cerrar y abrir de nuevo)

### Paso 2: Verificar instalación

1. Abre `Program.cs`
2. Haz click en el número de línea 74 (debe aparecer un punto rojo 🔴)
3. Presiona `F5`
4. El programa debe detenerse en la línea 74

Si se detiene = ✅ **TODO OK**

Si no se detiene = ❌ Reinicia VS Code

---

## 🚀 ALTERNATIVA RÁPIDA (Si no quieres instalar)

Usa la terminal:

```bash
dotnet run
```

Y agrega `Console.WriteLine` para ver datos:

```csharp
Console.WriteLine($"Cliente: {model.Customer.Name}");
```

---

## ✅ DESPUÉS DE INSTALAR

Ya puedes usar:
- **F5** = Run with debugging
- **F10** = Step over (siguiente línea)
- **F11** = Step into (entrar a función)
- **Breakpoints** = Click en número de línea

🎉 ¡Listo para debugear!
