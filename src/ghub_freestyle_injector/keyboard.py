"""Keyboard key-code mapping used by Logitech G HUB per-key freestyle effects."""

from __future__ import annotations

from collections.abc import Iterable


ALL_CODES = [
    *range(4, 30),      # A-Z
    *range(30, 40),     # 1-0
    40, 41, 42, 43, 44, # Enter, Esc, Backspace, Tab, Space
    45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, # US punctuation row
    *range(57, 70),     # CapsLock, F1-F12
    73, 74, 75, 76, 77, 78, # Ins/Home/PgUp/Del/End/PgDn
    *range(79, 83),     # arrows
    *range(224, 232),   # modifiers
]

KEY_CODES = {
    **{chr(ord("A") + i): 4 + i for i in range(26)},
    **{str(i): 29 + i for i in range(1, 10)},
    "0": 39,
    "ENTER": 40,
    "ESC": 41,
    "ESCAPE": 41,
    "BACKSPACE": 42,
    "TAB": 43,
    "SPACE": 44,
    "ESPAÇO": 44,
    "MINUS": 45,
    "EQUAL": 46,
    "LBRACKET": 47,
    "RBRACKET": 48,
    "BACKSLASH": 49,
    "SEMICOLON": 51,
    "QUOTE": 52,
    "GRAVE": 53,
    "COMMA": 54,
    "DOT": 55,
    "PERIOD": 55,
    "SLASH": 56,
    "CAPS": 57,
    "CAPSLOCK": 57,
    "INS": 73,
    "INSERT": 73,
    "HOME": 74,
    "PGUP": 75,
    "PAGEUP": 75,
    "DEL": 76,
    "DELETE": 76,
    "END": 77,
    "PGDN": 78,
    "PAGEDOWN": 78,
    "RIGHT": 79,
    "LEFT": 80,
    "DOWN": 81,
    "UP": 82,
    "LCTRL": 224,
    "CTRL": 224,
    "CONTROL": 224,
    "LSHIFT": 225,
    "SHIFT": 225,
    "LALT": 226,
    "ALT": 226,
    "LWIN": 227,
    "WIN": 227,
    "RCTRL": 228,
    "RSHIFT": 229,
    "RALT": 230,
    "FN": 231,
}

for i in range(1, 13):
    KEY_CODES[f"F{i}"] = 57 + i

ZONES = {
    "letters": list(range(4, 30)),
    "letter_keys": list(range(4, 30)),
    "function_row": list(range(58, 70)),
    "f-row": list(range(58, 70)),
    "modifiers": list(range(224, 232)),
    "modifier_keys": list(range(224, 232)),
    "number_row": list(range(30, 40)),
    "numbers": list(range(30, 40)),
    "arrows": [79, 80, 81, 82],
    "navigation": [73, 74, 75, 76, 77, 78],
    "wasd": [KEY_CODES[key] for key in "WASD"],
}


def normalize_key_name(name: str) -> str:
    return name.strip().upper().replace(" ", "_").replace("-", "_")


def codes_for_key(name: str) -> list[int]:
    normalized = normalize_key_name(name)
    if normalized not in KEY_CODES:
        raise KeyError(f"tecla desconhecida: {name}")
    return [KEY_CODES[normalized]]


def codes_for_zone(name: str) -> list[int]:
    normalized = normalize_key_name(name).lower()
    if normalized not in ZONES:
        raise KeyError(f"zona desconhecida: {name}")
    return ZONES[normalized]


def apply_codes(color_map: dict[str, dict[str, str]], codes: Iterable[int], color: str) -> None:
    for code in codes:
        color_map[str(code)] = {"hex": color.lower()}
