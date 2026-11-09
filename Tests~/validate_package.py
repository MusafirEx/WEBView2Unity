"""Static checks for the Git UPM package; independent of Unity and Windows."""
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parent.parent
package = json.loads((ROOT / "package.json").read_text(encoding="utf-8"))
assert package["name"] == "com.musafirex.webview2unity"
assert package["unity"] == "2022.3"
assert package["version"] == "0.1.0"

for filename in (
    "Runtime/MusafirEx.WEBView2Unity.asmdef",
    "Editor/MusafirEx.WEBView2Unity.Editor.asmdef",
    "Tests/Editor/MusafirEx.WEBView2Unity.Editor.Tests.asmdef",
):
    data = json.loads((ROOT / filename).read_text(encoding="utf-8"))
    assert data["name"].startswith("MusafirEx.WEBView2Unity")

# All Unity-imported assets inside immutable Git packages need committed .meta files.
all_unity_assets = [
    Path("package.json"), Path("README.md"), Path("CHANGELOG.md"),
    Path("Editor"), Path("Runtime"), Path("Tests"), Path("Tests/Editor"),
    *Path("Editor").rglob("*.cs"), *Path("Editor").rglob("*.asmdef"),
    *Path("Runtime").rglob("*.cs"), *Path("Runtime").rglob("*.asmdef"),
    *Path("Tests").rglob("*.cs"), *Path("Tests").rglob("*.asmdef"),
]
guids = set()
for asset in all_unity_assets:
    meta = ROOT / (str(asset) + ".meta")
    assert meta.exists(), f"Missing immutable Git package .meta: {asset}"
    data = meta.read_text(encoding="utf-8")
    match = re.search(r"(?m)^guid: ([0-9a-f]{32})$", data)
    assert match, f"Invalid metadata GUID: {meta}"
    assert match.group(1) not in guids, f"Duplicate Unity GUID: {match.group(1)}"
    guids.add(match.group(1))
    if (ROOT / asset).is_dir():
        assert "folderAsset: yes" in data, f"Missing folderAsset in {meta}"

native = (ROOT / "Native~/WebViewBridge.cpp").read_text(encoding="utf-8")
interop = (ROOT / "Runtime/WebViewNative.cs").read_text(encoding="utf-8")
exports = set(re.findall(r"__cdecl (W2U_\w+)\s*\(", native))
imports = set(re.findall(r"extern\s+\w+\s+(W2U_\w+)\s*\(", interop))
assert len(exports) >= 15 and exports == imports, (
    f"C ABI mismatch: C++ only {exports - imports}; C# only {imports - exports}"
)
print(f"PASS: UPM manifest, asmdefs, {len(guids)} metadata GUIDs and native C ABI")
