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

native = (ROOT / "Native~/WebViewBridge.cpp").read_text(encoding="utf-8")
interop = (ROOT / "Runtime/WebViewNative.cs").read_text(encoding="utf-8")
exports = set(re.findall(r"__cdecl (W2U_\w+)\s*\(", native))
imports = set(re.findall(r"extern\s+\w+\s+(W2U_\w+)\s*\(", interop))
assert len(exports) >= 15 and exports == imports, (
    f"C ABI mismatch: C++ only {exports - imports}; C# only {imports - exports}"
)
print("PASS: UPM package manifest, asmdefs and native C ABI method names")
