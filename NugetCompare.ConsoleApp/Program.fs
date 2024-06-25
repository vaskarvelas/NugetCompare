open System
open System.IO

[<EntryPoint>]
let main args =
    let argv =
    #if DEBUG
    // your code
        args
    #else
        args
    #endif
    if argv.Length < 3 then
        printfn "Usage: <application-name> <solutionDir1> <solutionDir2> <outputFolder>"
        1
    else
        try
            let solutionDir1 = argv.[0]
            let solutionDir2 = argv.[1]
            let comparisonOutputFile = argv.[2]

            // Ensure the output directory exists
            let outputDir = Path.GetDirectoryName(comparisonOutputFile)
            if not (String.IsNullOrWhiteSpace(outputDir) && Directory.Exists(outputDir)) then 
                Directory.CreateDirectory(outputDir) |> ignore

            // Clear or create the output file
            File.WriteAllText(comparisonOutputFile, "")

            // Find all .csproj files for each solution
            let csprojFilesSolution1 = CompareNugets.FindAllCsprojFiles solutionDir1
            let csprojFilesSolution2 = CompareNugets.FindAllCsprojFiles solutionDir2

            let packagesSolution1 = csprojFilesSolution1 |> Seq.collect (CompareNugets.ExtractPackageReferences solutionDir1)
            let packagesSolution2 = csprojFilesSolution2 |> Seq.collect (CompareNugets.ExtractPackageReferences solutionDir2)

            // Debugging information
            printfn "csproj files 1: %A" csprojFilesSolution1
            printfn "Packages in Solution 1: %A" packagesSolution1
            printfn "csproj files 2: %A" csprojFilesSolution2
            printfn "Packages in Solution 2: %A" packagesSolution2

            let packageMap1 = CompareNugets.CreatePackageMap packagesSolution1
            let packageMap2 = CompareNugets.CreatePackageMap packagesSolution2

            // Debugging information
            printfn "Package Map 1: %A" packageMap1
            printfn "Package Map 2: %A" packageMap2

            let comparisonContent = CompareNugets.FormatComparison packageMap1 packageMap2
            printfn "Comparison Content (Text): %s" comparisonContent

            let comparisonContentCsv = CompareNugets.FormatComparisonCsv packageMap1 packageMap2
            printfn "Comparison Content (CSV): %A" comparisonContentCsv

            let csvContent = CompareNugets.ToCsvFormat comparisonContentCsv
            printfn "CSV Content: %s" csvContent

            let directory1Info = DirectoryInfo(solutionDir1)
            let solutionDir1 = directory1Info.Name 

            let directory2Info = DirectoryInfo(solutionDir2)
            let solutionDir2 = directory2Info.Name 

            // Write the formatted comparison to the output file
            if not (String.IsNullOrEmpty(comparisonContent)) then
                File.AppendAllText(comparisonOutputFile, sprintf "Package Comparison between %s and %s:\n%s\n" solutionDir1 solutionDir2 comparisonContent)
            else
                printfn "No differences found to write to the output file."

            let headers = sprintf "\"%s Project File\",\"%s Name\",\"%s Version\",\"%s Version\",\"%s Name\",\"%s Project File\"" solutionDir1 solutionDir1 solutionDir1 solutionDir2 solutionDir2 solutionDir2
            let fullCsv = headers + "\n" + csvContent
            let csvFileName = Path.Combine(Path.GetDirectoryName(comparisonOutputFile), (Path.GetFileNameWithoutExtension comparisonOutputFile) + ".csv")
            if not (String.IsNullOrEmpty(csvContent)) then
                File.WriteAllText(csvFileName, fullCsv)
            else
                printfn "No differences found to write to the CSV file."

            // Print a message indicating that the comparison is complete
            printfn "Package comparison complete. Differences are in %s and in %s" comparisonOutputFile csvFileName

            0
        with
        | :? UnauthorizedAccessException as ex ->
            printfn "Error: Unauthorized access - %s" ex.Message
            1
        | ex ->
            printfn "An unexpected error occurred: %s" ex.Message
            1

