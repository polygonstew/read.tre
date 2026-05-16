<div align="center">

```
  ┌─ tree-writer
  └─ folder -> .tre
```

![C#](https://img.shields.io/badge/c%23-.NET-D97706?style=flat-square)
![status](https://img.shields.io/badge/status-works_on_my_machine-1F2937?style=flat-square)

</div>

---

Walks a directory and dumps its structure to a `.tre` file. Companion to my [create.tre](https://github.com/) VS Code extension which goes the other direction.

## usage

```
path to scan (blank = current): C:\projects\demo
wrote demo.tre
```

Output is named after the scanned folder and lands next to the exe.

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

- skips hidden folders (`.git`, `.vs`)
- `bin`/`obj` are NOT skipped — edit Program.cs to add

## build

```
dotnet run
```