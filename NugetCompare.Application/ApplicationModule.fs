module CompareNugets

open System.IO // Import System.IO namespace for file operations
open System.Xml.Linq // Import System.Xml.Linq for XML processing

type PackageInfo = { Name: string; Version: string; ProjectFile: string }

// Define a recursive function to find all .csproj files in a given path
let rec FindAllCsprojFiles path =
    // Search for .csproj files in all directories starting from the given path
    Directory.EnumerateFiles(path, "*.csproj", SearchOption.AllDirectories)

let ExtractPackageReferences (basePath:string) (csprojPath: string) =
    let relativePath = csprojPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar)
    let doc = XDocument.Load(csprojPath)
    doc.Descendants("PackageReference")
    |> Seq.map (fun el ->
        let name = 
            el.Attribute(XName.Get("Include")) 
            |> fun a -> if a <> null then a.Value else "N/A"
        let version = 
            el.Attribute(XName.Get("Version")) 
            |> fun a -> if a <> null then a.Value else "N/A"
        { Name = name; Version = version; ProjectFile = relativePath })
    |> Seq.toList

// Create a map of package names to package details for each solution
let CreatePackageMap (packages: seq<PackageInfo>) =
    packages
    |> Seq.groupBy (fun pkg -> pkg.Name, pkg.Version)
    |> Seq.map (fun (name, pkgs) -> (name, Seq.head pkgs))
    |> Map.ofSeq

// Function to compare and format the package information
let FormatComparison packageMap1 packageMap2 =
    let keys1 = packageMap1 |> Map.toSeq |> Seq.map fst
    let keys2 = packageMap2 |> Map.toSeq |> Seq.map fst

    let commonPackageNames = Set.intersect (Set.ofSeq keys1) (Set.ofSeq keys2)
    commonPackageNames
    |> Seq.map (fun packageName ->
        let package1 = Map.tryFind packageName packageMap1
        let package2 = Map.tryFind packageName packageMap2
        match (package1, package2) with
        | (Some pkg1, Some pkg2) when pkg1.Version <> pkg2.Version -> sprintf "%s %s (%s) | %s %s (%s)" pkg1.Name pkg1.Version pkg1.ProjectFile pkg2.Name pkg2.Version pkg2.ProjectFile
        | _ -> "" // This case should not occur for common packages
    )
    |> Seq.filter (fun x -> x <> "")
    |> String.concat "\n"

let FormatComparisonCsv packageMap1 packageMap2 =
    let keys1 = packageMap1 |> Map.toSeq |> Seq.map fst
    let keys2 = packageMap2 |> Map.toSeq |> Seq.map fst

    let commonPackageNames = Set.intersect (Set.ofSeq keys1) (Set.ofSeq keys2)
    let comparisons =
        commonPackageNames
        |> Seq.map (fun packageName ->
            let package1 = Map.tryFind packageName packageMap1
            let package2 = Map.tryFind packageName packageMap2
            match (package1, package2) with
            | (Some pkg1, Some pkg2) when pkg1.Version <> pkg2.Version -> 
                Some (pkg1.Name, pkg1.Version, pkg1.ProjectFile, pkg2.Name, pkg2.Version, pkg2.ProjectFile)
            | _ -> None // This case should not occur for common packages
        )
        |> Seq.choose id
    comparisons

let ToCsvFormat (comparisons: seq<string * string * string * string * string * string>) =
    let csvLines = 
        comparisons 
        |> Seq.map (fun (name1, version1, projectFile1, name2, version2, projectFile2) ->
            sprintf "\"%s\",\"%s\",\"%s\",\"%s\",\"%s\",\"%s\"" projectFile1 name1 version1 version2 name2 projectFile2)
    String.concat "\n" csvLines

