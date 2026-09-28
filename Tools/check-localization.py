"""Static localization check (no Unity needed).

    python Tools/check-localization.py [paths...]

With no paths it scans every runtime script under Assets/CottonCircuit/Scripts and every
UXML under Assets/Resources/UI. It fails (exit 1) when:
  * a C# string literal outside comments and Inspector attributes contains Hangul,
  * Strings.Get / Strings.Format uses a literal key that strings.tsv does not define,
  * a UXML text attribute contains Hangul but the element has no LocalizedText binding,
  * a LocalizedText key is missing from strings.tsv,
  * strings.tsv is malformed (header, 3 cells, duplicate keys).
"""
import pathlib, re, sys
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[1]
TABLE = ROOT / "Assets/Resources/Localization/strings.tsv"
HANGUL = re.compile("[가-힣]")
ATTRIBUTE_LINE = re.compile(r"^\s*\[(Header|Tooltip|InspectorName)\(")
LITERAL = re.compile(r'@"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\])*"')
KEY_USE = re.compile(r'Strings\.(?:Get|Format)\(\s*"([^"]+)"')


def load_keys(problems):
    lines = TABLE.read_text(encoding="utf-8").replace("\r\n", "\n").split("\n")
    if lines[0].lstrip("﻿") != "key\tko\ten":
        problems.append(f"{TABLE}: bad header")
    keys = set()
    for number, line in enumerate(lines[1:], start=2):
        if not line:
            continue
        cells = line.split("\t")
        if len(cells) != 3:
            problems.append(f"{TABLE}:{number}: needs 3 cells")
            continue
        if cells[0] in keys:
            problems.append(f"{TABLE}:{number}: duplicate key {cells[0]}")
        keys.add(cells[0])
    return keys


def strip_comments(source):
    out, i, n = [], 0, len(source)
    while i < n:
        if source.startswith("//", i):
            j = source.find("\n", i)
            i = n if j < 0 else j
        elif source.startswith("/*", i):
            j = source.find("*/", i + 2)
            i = n if j < 0 else j + 2
        elif source[i] == '"' or source.startswith('@"', i) or source.startswith('$"', i):
            match = LITERAL.match(source, i)
            if not match:
                out.append(source[i]); i += 1; continue
            out.append(match.group(0)); i = match.end()
        elif source[i] == "'":
            j = i + 1
            while j < n and source[j] != "'":
                j += 2 if source[j] == "\\" else 1
            out.append(source[i:j + 1]); i = j + 1
        else:
            out.append(source[i]); i += 1
    return "".join(out)


def check_cs(path, keys, problems):
    source = strip_comments(path.read_text(encoding="utf-8-sig"))
    for number, line in enumerate(source.split("\n"), start=1):
        if ATTRIBUTE_LINE.match(line):
            continue
        for literal in LITERAL.findall(line):
            if HANGUL.search(literal):
                problems.append(f"{path.relative_to(ROOT)}:{number}: Hangul literal {literal}")
        for key in KEY_USE.findall(line):
            if key not in keys:
                problems.append(f"{path.relative_to(ROOT)}:{number}: unknown key {key}")


def check_uxml(path, keys, problems):
    tree = ET.parse(path)
    for element in tree.iter():
        bindings = [b for child in element if child.tag.split("}")[-1] == "Bindings" for b in child]
        localized = [b for b in bindings if b.tag.split("}")[-1] == "CottonCircuit.LocalizedText"]
        for binding in localized:
            key = binding.get("key")
            if key not in keys:
                problems.append(f"{path.relative_to(ROOT)}: unknown LocalizedText key {key}")
        text = element.get("text") or ""
        if HANGUL.search(text) and not any(b.get("property") == "text" for b in localized):
            problems.append(f"{path.relative_to(ROOT)}: <{element.get('name') or element.tag.split('}')[-1]}> text '{text}' has no LocalizedText binding")


def main(arguments):
    problems = []
    keys = load_keys(problems)
    paths = [ROOT / p for p in arguments] if arguments else \
        sorted((ROOT / "Assets/CottonCircuit/Scripts").rglob("*.cs")) + sorted((ROOT / "Assets/Resources/UI").glob("*.uxml"))
    for path in paths:
        if path.suffix == ".cs":
            check_cs(path, keys, problems)
        elif path.suffix == ".uxml":
            check_uxml(path, keys, problems)
    for problem in problems:
        print(problem)
    print(f"{len(problems)} localization problem(s) in {len(paths)} file(s)")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
