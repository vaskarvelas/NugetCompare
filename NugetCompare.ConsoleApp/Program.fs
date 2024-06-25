open System
open System.IO // Import System.IO namespace for file operations

[<EntryPoint>]
let main args =
    let argv =
    #if DEBUG
        [|"C:\\Users\\vkarvelas\\Repositories\\Integrated.Dias_develop" ; "C:\\Users\\vkarvelas\\Repositories\\Integrated.Dias" ; "./output"|]
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
            if not (Directory.Exists(outputDir)) then 
                Directory.CreateDirectory(outputDir) |> ignore

            // Clear or create the output file
            File.WriteAllText(comparisonOutputFile, "")

            // After calling findAllCsprojFiles for each solution
            let csprojFilesSolution1 = CompareNugets.FindAllCsprojFiles solutionDir1
            let csprojFilesSolution2 = CompareNugets.FindAllCsprojFiles solutionDir2

            let packagesSolution1 = csprojFilesSolution1 |> Seq.collect (CompareNugets.ExtractPackageReferences solutionDir1) // |> Seq.distinct
            let packagesSolution2 = csprojFilesSolution2 |> Seq.collect (CompareNugets.ExtractPackageReferences solutionDir2) // |> Seq.distinct

            printfn "csproj files 1: %A" csprojFilesSolution1

            let packageMap1 = CompareNugets.CreatePackageMap packagesSolution1
            let packageMap2 = CompareNugets.CreatePackageMap packagesSolution2

           #if DEBUG
            //printfn "package map 1 %A " packageMap1
           #endif

            let comparisonContent = CompareNugets.FormatComparison packageMap1 packageMap2

            let directory1Info = DirectoryInfo(solutionDir1)
            let solutionDir1 = directory1Info.Name 

            let directory2Info = DirectoryInfo(solutionDir2)
            let solutionDir2 = directory2Info.Name 

            // Write the formatted comparison to the output file
            File.AppendAllText(comparisonOutputFile, sprintf "Package Comparison between %s and %s:\n%s\n" solutionDir1 solutionDir2 comparisonContent)

            let comparisonContent = CompareNugets.FormatComparisonCsv packageMap1 packageMap2
            let csvContent = CompareNugets.ToCsvFormat comparisonContent

            let headers = sprintf "\"%s Project File\",\"%s Name\",\"%s Version\",\"%s Version\",\"%s Name\",\"%s Project File\"" solutionDir1 solutionDir1 solutionDir1 solutionDir2 solutionDir2 solutionDir2
            let fullCsv = headers + "\n" + csvContent
            File.WriteAllText((Path.GetFileNameWithoutExtension comparisonOutputFile) + ".csv", fullCsv)

            // Print a message indicating that the comparison is complete
            printfn "Package comparison complete. Differences are in %s and in " comparisonOutputFile

            0 // Return zero to indicate success
            with
            | :? UnauthorizedAccessException as ex ->
                printfn "Error: Unauthorized access - %s" ex.Message
                1
            | ex ->
                printfn "An unexpected error occurred: %s" ex.Message
                1
