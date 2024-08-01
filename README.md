# dwml_cs - MSOffice OMML to LaTeX conversion library.
A C# port of dwml library by xiilei (https://github.com/xiilei/dwml).

#### Example input:

![equation](https://github.com/user-attachments/assets/c6e8f9ef-1cac-41d7-81e0-6374ef3dbb26)

#### Example output:
```latex
2\pi \int_{a}^{b}{y\sqrt{1+(f^{\prime}(x))^{2}}} dx
```

# Usage:
```csharp
using mxd.Dwml;

var xmldoc = new XmlDocument();
xmldoc.Load("example.xml");

var latex_str = MLConverter.Convert(xmldoc.DocumentElement); // DocumentElement is expected to be a m:oMath node.
Console.WriteLine($"LaTex: '{latex_str}'");
```
