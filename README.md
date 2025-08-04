For this **test release**, where we may have instability and still lack of refinement, I recommend using a semantic **pre-release** version like `0.1.0`

---

## Installation

Download the binary for your system from [releases on GitHub](https://github.com/serene1491/SaLang/releases):

- Linux/macOS:  
  ```bash
  wget -O salang https://github.com/serene1491/SaLang/releases/download/0.1.0/salang
  chmod +x salang
  sudo mv salang /usr/local/bin/sal
  ```

* Windows (PowerShell):

  ```powershell
  Invoke-WebRequest -Uri "https://github.com/serene1491/SaLang/releases/download/0.1.0/salang.exe" `
    -OutFile "$env:USERPROFILE\sal.exe"
  [Environment]::SetEnvironmentVariable("Path", `
    "$env:USERPROFILE;$env:Path",[EnvironmentVariableTarget]::User)
  ```

> After that, calling `sal` from anywhere should work.

---

## Use

### REPL interactivo

```bash
sal
> var x = 10
[finished] << 0
> x + 20
[finished] << 0
>            # ENTER vazio sai do REPL
```

### Run file script

```bash
sal path/to/your_script.sal
```

* If `path` is relative, try three resolutions in order:

  1. Current working directory
  2. Executable directory (`sa.exe` or `sal`)
  3. Working directory again (fallback)

### Modules via `require`

Inside a `.sal` script:

```sal
require("util") as util
var result = util.someFunction(42)
```

The interpreter first looks in `modules/` next to the script, then in `modules/` in the CWD.
---

## Resources

* **Lexer**: tokenization of identifiers, numbers, escaped strings, operators and symbols.
* **Parser**:

  * Declarations: `var`, `local`, `function`, `unsafe function`, `return`, `if/then/elseif/not`, `while do/end`, `for … in … do/end`.
  * Expressions: function calls, table access (`obj.prop`, `{ k = v }`), literals.
  * Binary and unary precedence (`#` para `len`).
* **Runtime**:

  * Values (`Number`, `Bool`, `String`, `Table`, `Nil`, `Error`).
  * Basic built-in functions (`sum`, `sub`, `mul`, `div`, `len`, `require`, `print`, `error`, etc).
  * Global environment and stacking scopes for calls (stack trace).
* **Errors log**:

  * Syntax and semantic errors with `TraceFrame` containing (production, file, line, column).
  * Handling IO errors when loading modules.
* **Internals**:

  * `Interpreter.Main` decide between **REPL mode** and **file mode** (`args[0]`).
  * Separate methods:

    * `RunRepl()`: loop `ReadLine` → `Execute` → result.
    * `RunFile()`: `ResolveFilePath` → `File.ReadAllText` → `Execute`.
  * Utility function `ResolveFilePath` does `~` expansion, resolves absolute vs. relative to CWD and .exe directory.

---

## Development

* **Pasta `/Lexing`** contains `Lexer.cs`, `TokenType.cs`, `SyntaxToken.cs`.
* **Pasta `/Parsing`** contains `Parser.cs`, `SyntaxResult.cs`, AST nodes.
* **Pasta `/Runtime`** contains the main `Interpreter`, built-ins and `Environment`.

---

## License

MIT © 2025 \[serene1491\]

---

## 0.1.0

- First public beta release.
- Basic REPL implementation, file execution, modules, and error diagnosis.
- Support for arithmetic and a bit of logical (just declare true and false so as not to lie)
