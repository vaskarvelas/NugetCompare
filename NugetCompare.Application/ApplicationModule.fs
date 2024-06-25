module CompareNugets

open System.IO
open System.Xml.Linq

type PackageInfo = { Name: string; Version: string; ProjectFile: string }

let rec FindAllCsprojFiles path =
    Directory.EnumerateFiles(path, "*.csproj", SearchOption.AllDirectories)

let ExtractPackageReferences (basePath: string) (csprojPath: string) =
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

let CreatePackageMap (packages: seq<PackageInfo>) =
    packages
    |> Seq.groupBy (fun pkg -> (pkg.Name, pkg.ProjectFile))
    |> Seq.map (fun ((name, projectFile), pkgs) -> ((name, projectFile), pkgs |> Seq.head))
    |> Map.ofSeq

let FormatComparison packageMap1 packageMap2 =
    let keys1 = packageMap1 |> Map.toSeq |> Seq.map fst
    let keys2 = packageMap2 |> Map.toSeq |> Seq.map fst

    let commonPackageKeys = Set.intersect (Set.ofSeq keys1) (Set.ofSeq keys2)
    commonPackageKeys
    |> Seq.map (fun packageKey ->
        let package1 = Map.find packageKey packageMap1
        let package2 = Map.find packageKey packageMap2
        if package1.Version <> package2.Version then
            sprintf "%s %s (%s) | %s %s (%s)" package1.Name package1.Version package1.ProjectFile package2.Name package2.Version package2.ProjectFile
        else
            ""
    )
    |> Seq.filter (fun x -> x <> "")
    |> String.concat "\n"

let FormatComparisonCsv packageMap1 packageMap2 =
    let keys1 = packageMap1 |> Map.toSeq |> Seq.map fst
    let keys2 = packageMap2 |> Map.toSeq |> Seq.map fst

    let commonPackageKeys = Set.intersect (Set.ofSeq keys1) (Set.ofSeq keys2)
    let comparisons =
        commonPackageKeys
        |> Seq.choose (fun packageKey ->
            let package1 = Map.find packageKey packageMap1
            let package2 = Map.find packageKey packageMap2
            if package1.Version <> package2.Version then
                Some (package1.ProjectFile, package1.Name, package1.Version, package2.Version, package2.Name, package2.ProjectFile)
            else
                None
        )
    comparisons

let ToCsvFormat (comparisons: seq<string * string * string * string * string * string>) =
    let csvLines = 
        comparisons 
        |> Seq.map (fun (projectFile1, name1, version1, version2, name2, projectFile2) ->
            sprintf "\"%s\",\"%s\",\"%s\",\"%s\",\"%s\",\"%s\"" projectFile1 name1 version1 version2 name2 projectFile2)
    String.concat "\n" csvLines

