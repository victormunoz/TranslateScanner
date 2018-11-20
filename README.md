# Translate Scanner

Looks for translations in source code (recursively in all folders) and generates .json or .po translation files.
The translation is made using [Google Translator](https://translate.google.com).
The text to search in the source code is like `__("Hello")`.

An example output file in json format:


    {
	    "Contact address is required": "Se requiere una dirección de contacto",
	    "you bought ": "has comprado ",
    }


And in po format:


    msgid "Contact address is required"
    msgstr "Se requiere una dirección de contacto"

    msgid "you bought "
    msgstr "has comprado "

## Contact

Victor Muñoz
victor.munoz@newronia.com
