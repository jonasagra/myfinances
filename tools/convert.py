"""Gera D:/Projetos/myfinances/src a partir de _decomp: renomeia Blackshuffle -> MyFinances,
troca o roxo pelo verde e aplica o icone novo. Pode ser rodado de novo (recria src/)."""
import os, re, shutil, sys

ROOT = r"D:\Projetos\myfinances"
SRC_IN = os.path.join(ROOT, "_decomp")
OUT = os.path.join(ROOT, "src")
BRAND = os.path.join(ROOT, "branding")

# Valores que NAO podem mudar (compatibilidade com dados existentes)
PROTECT = [
    '"Blackshuffle.chave-mestra.v1"u8',   # entropia DPAPI do key.dat -> mudar = perder acesso aos dados
]
# Dentro do zip de backup o banco se chama blackshuffle.db; manter para restaurar backups antigos
PROTECT_IN_FILE = {"BackupService.cs": ['"blackshuffle.db"']}

# Roxo -> verde (escala emerald)
COLORS = {
    "#7C3AED": "#059669",  # Violet       -> verde principal
    "#8B5CF6": "#10B981",  # VioletHover
    "#6D28D9": "#047857",  # VioletPress
    "#C4B5FD": "#6EE7B7",  # VioletLight
    "#A78BFA": "#047857",  # categoria padrao lilas
}
COLOR_FILES = {"theme.xaml", "Category.cs", "Bootstrap.cs"}

LOGO = '''<DataTemplate x:Key="Logo">
    <Viewbox>
      <Canvas Width="512" Height="512">
        <Ellipse Canvas.Left="16" Canvas.Top="16" Width="480" Height="480" Fill="{DynamicResource AccentPressBrush}" />
        <Ellipse Canvas.Left="44" Canvas.Top="44" Width="424" Height="424" Fill="{DynamicResource AccentBrush}" />
        <Path Stroke="#FFFFFFFF" StrokeThickness="30" StrokeStartLineCap="Round" StrokeEndLineCap="Round" Data="M256,104 V408" />
        <Path Stroke="#FFFFFFFF" StrokeThickness="44" StrokeStartLineCap="Round" StrokeEndLineCap="Round" StrokeLineJoin="Round" Data="M330,178 C312,152 286,142 256,142 C214,142 184,166 184,202 C184,244 220,256 256,264 C298,273 334,288 334,330 C334,368 300,392 256,392 C220,392 192,378 172,350" />
      </Canvas>
    </Viewbox>
  </DataTemplate>'''

def rename(s: str) -> str:
    return (s.replace("Blackshuffle", "MyFinances")
             .replace("blackshuffle", "myfinances")
             .replace("BLACKSHUFFLE", "MYFINANCES"))

def convert_text(name: str, text: str) -> str:
    guards = PROTECT + PROTECT_IN_FILE.get(name, [])
    tokens = {}
    for i, g in enumerate(guards):
        t = f"@@KEEP{i}@@"
        tokens[t] = g
        text = text.replace(g, t)
    text = rename(text)
    # extensao do backup portatil
    text = text.replace("|*.bsbak", "|*.mfbak;*.bsbak").replace(".bsbak", ".mfbak")
    text = text.replace("*.mfbak;*.mfbak", "*.mfbak;*.bsbak")
    if name in COLOR_FILES:
        for a, b in COLORS.items():
            text = re.sub(re.escape(a), b, text, flags=re.I)
        text = text.replace("Violet", "Accent")
    elif name.endswith(".xaml"):
        text = text.replace("Violet", "Accent")
    elif name.endswith(".cs"):
        text = re.sub(r'(["\x27])Violet', r"\1Accent", text)
    if name == "FinanceViewModel.cs":  # inicializador de objeto desmontado pelo descompilador
        text = text.replace(
            "EntryRow entryRow = new EntryRow(entry.Id, entry.Description, entry.Amount, entry.Group, Fixed: false, OriginName(entry.Origin));\n"
            "\t\t\tClip c = _clip;\n"
            "\t\t\tentryRow.IsCut = (object)c != null && c.Cut && c.Ids.Contains(entry.Id);\n"
            "\t\t\tentryRow.IsAdjustment = entry.Origin == EntryOrigin.Adjustment;\n"
            "\t\t\tentryRow.Recurring = FixedEntries.IsFixed(entry);\n",
            "Clip c = _clip;\n"
            "\t\t\tEntryRow entryRow = new EntryRow(entry.Id, entry.Description, entry.Amount, entry.Group, Fixed: false, OriginName(entry.Origin))\n"
            "\t\t\t{\n"
            "\t\t\t\tIsCut = (object)c != null && c.Cut && c.Ids.Contains(entry.Id),\n"
            "\t\t\t\tIsAdjustment = entry.Origin == EntryOrigin.Adjustment,\n"
            "\t\t\t\tRecurring = FixedEntries.IsFixed(entry)\n"
            "\t\t\t};\n")
    if name == "icons.xaml":  # logo do menu: setas de shuffle -> moeda com cifrao
        text = re.sub(r'[ \t]*<PathGeometry x:Key="Icon\.Shuffle"[^>]*/>\r?\n', '', text)
        text = re.sub(r'<DataTemplate x:Key="Logo">.*?</DataTemplate>', LOGO, text, flags=re.S)
    # codigo de regex gerado ja vem descompilado: tirar o atributo evita gerar de novo
    text = re.sub(r'^[ \t]*\[GeneratedRegex\(.*\n', '', text, flags=re.M)
    for t, g in tokens.items():
        text = text.replace(t, g)
    return text

PKG = {
    "MyFinances.Data": [("Microsoft.EntityFrameworkCore.Sqlite", "10.0.13"), ("ClosedXML", "0.105.1"),
                        ("PdfPig", "0.1.16"), ("System.Security.Cryptography.ProtectedData", "10.0.0")],
    "MyFinances": [("QRCoder", "1.8.1")],
}
PROJREF = {"MyFinances.Data": ["MyFinances.Core"], "MyFinances": ["MyFinances.Core", "MyFinances.Data"]}

def fix_csproj(proj: str, text: str) -> str:
    text = re.sub(r"\s*<ItemGroup>\s*(<Reference Include=\"[^\"]+\">\s*<HintPath>[^<]+</HintPath>\s*</Reference>\s*)+</ItemGroup>", "", text)
    items = "".join(f'\n    <PackageReference Include="{n}" Version="{v}" />' for n, v in PKG.get(proj, []))
    items += "".join(f'\n    <ProjectReference Include="..\\{p}\\{p}.csproj" />' for p in PROJREF.get(proj, []))
    extra = "\n  <PropertyGroup>\n    <Nullable>enable</Nullable>\n    <NoWarn>$(NoWarn);CS8600;CS8601;CS8602;CS8603;CS8604;CS8618;CS8619;CS8620;CS8625;CS8629;CS8714</NoWarn>\n  </PropertyGroup>"
    if proj == "MyFinances":
        extra += ("\n  <PropertyGroup>\n    <Product>MyFinances</Product>\n    <Version>1.0.0</Version>\n  </PropertyGroup>")
    block = (f"\n  <ItemGroup>{items}\n  </ItemGroup>" if items else "") + extra
    return text.replace("</Project>", block + "\n</Project>")

if os.path.exists(OUT):  # esvazia sem apagar a pasta (o editor pode estar com ela aberta)
    for e in os.listdir(OUT):
        p = os.path.join(OUT, e)
        shutil.rmtree(p) if os.path.isdir(p) else os.remove(p)
for dirpath, _, files in os.walk(SRC_IN):
    rel = os.path.relpath(dirpath, SRC_IN)
    outdir = os.path.join(OUT, rename(rel)) if rel != "." else OUT
    os.makedirs(outdir, exist_ok=True)
    for f in files:
        src = os.path.join(dirpath, f)
        dst = os.path.join(outdir, rename(f))
        if f.endswith((".cs", ".xaml", ".csproj", ".manifest")):
            text = open(src, encoding="utf-8-sig").read()
            text = convert_text(f, text)
            if f.endswith(".csproj"):
                text = fix_csproj(os.path.splitext(rename(f))[0], text)
            open(dst, "w", encoding="utf-8").write(text)
        else:
            shutil.copy2(src, dst)

app = os.path.join(OUT, "MyFinances")
shutil.copy2(os.path.join(BRAND, "myfinances.ico"), os.path.join(app, "app.ico"))
shutil.copy2(os.path.join(BRAND, "myfinances.ico"), os.path.join(app, "resources", "myfinances.ico"))
shutil.copy2(os.path.join(BRAND, "myfinances-icon-256.png"), os.path.join(app, "resources", "logo.png"))

with open(os.path.join(OUT, "MyFinances.slnx"), "w", encoding="utf-8") as fh:
    fh.write('<Solution>\n  <Project Path="MyFinances.Core/MyFinances.Core.csproj" />\n'
             '  <Project Path="MyFinances.Data/MyFinances.Data.csproj" />\n'
             '  <Project Path="MyFinances/MyFinances.csproj" />\n</Solution>\n')

left = []
for dp, _, fs in os.walk(OUT):
    for f in fs:
        if f.endswith((".cs", ".xaml", ".csproj")):
            for n, line in enumerate(open(os.path.join(dp, f), encoding="utf-8"), 1):
                if re.search(r"(?i)blackshuffle|bsbak|7C3AED|8B5CF6|6D28D9|C4B5FD", line):
                    left.append(f"{os.path.relpath(os.path.join(dp, f), OUT)}:{n}: {line.strip()[:140]}")
print("Restantes (devem ser so os protegidos/compat):")
print("\n".join(left))
