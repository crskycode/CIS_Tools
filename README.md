# CIS Tools

This toolkit is designed for modifying games developed with the CIS engine.

## Resource Packs

Resource pack files for this engine are named `data`, `data2`, `data3`, etc., without an extension.

### Extracting Files from a Resource Pack

Run the following command:
```shell
ArcTool -e -in data -out data_extract
```

Parameter Description:
- `-e`: Extracts files from the resource pack.
- `-in`: Specifies the resource pack filename.
- `-out`: Specifies the directory where extracted items will be stored.

### Creating a Resource Pack

Run the following command:
```shell
ArcTool -c -in index.json -out data_create
```

Parameter Description:
- `-c`: Creates a resource pack.
- `-in`: Specifies the folder containing files you wish to add to the resource pack.
- `-out`: Specifies the output directory.

## Scripts

Script files for this engine are `script.dat`.

### Disassembling Scripts

Run the following command:
```shell
ScriptTool -d -in script.dat -icp shift_jis -out output.txt
```

Parameter Description:
- `-d`: Disassembles scripts.
- `-in`: Specifies the script filename.
- `-icp`: Specifies the encoding of text within the script file, which is usually `Shift_JIS`.
- `-out`: Specifies the output filename.

### Extracting Text from Scripts

Run the following command:
```shell
ScriptTool -e -in script.dat -icp shift_jis -out output.txt
```

Parameter Description:
- `-e`: Extracts text.
- `-in`: Specifies the script filename.
- `-icp`: Specifies the encoding of text within the script file, which is usually `Shift_JIS`.
- `-out`: Specifies the output filename.

### Importing Text into Scripts

Currently not supported, use Hook to replace strings.

---

**Note:** This toolkit has been tested on a limited number of games only.
