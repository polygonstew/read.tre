<div align="center">

```
  ┌─ read.tre
  └─ folder <-> .tre
```

![C#](https://img.shields.io/badge/c%23-.NET-D97706?style=flat-square)
![status](https://img.shields.io/badge/status-works_on_my_machine-1F2937?style=flat-square)

</div>

---
## download

Grab the latest `tre.exe` from [releases](https://github.com/polygonstew/read.tre/releases/latest), drop it in a folder on PATH.
Bidirectional CLI for the `.tre` format. Companion to my [create.tre](https://github.com/polygonstew/create.tre) VS Code extension.(in dev)

OR

## install via 

# scoop
scoop install https://raw.githubusercontent.com/polygonstew/read.tre/main/read.tre.json
# winget
winget install polygonstew.read.tre  <--IN REVIEW-->

## usage

```
tre .                  # current dir -> <foldername>.tre
tre <folder>           # that folder -> <foldername>.tre
tre <file.tre>         # build folder structure from a .tre
tre --hidden <folder>  # include hidden folders (.git, .vscode, etc)
tre -h                 # help
```

The reverse mode also reads Windows `tree /F /A` output, so this works:

```
tree /F /A > sample.txt
tre sample.txt
```

## format

Simple 2-space indent. Folders end with `/`. Files don't.

```
demo/
  readme.md
  src/
    main.py
    utils/
      helper.py
  tests/
    test_main.py
```

## notes

- output lands in the current working directory
- skips folders starting with `.` unless `--hidden` is passed
- `bin`/`obj` are NOT skipped — edit Program.cs to add
- materialize creates empty files (names only, no content)
- no overwrite protection — if the target folder exists, it gets merged

## encoding gotcha

If you redirect `tree /F` through PowerShell's `>`, older versions write UTF-16 without a BOM and the parser misreads it. Workaround:

```
tree /F /A | Out-File -Encoding utf8 sample.txt
```

cmd's `>` writes ANSI/UTF-8 and works fine.

## build

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Drop the exe somewhere on PATH (`C:\tools\` works) and call it from any terminal.