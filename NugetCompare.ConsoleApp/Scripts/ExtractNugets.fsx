open System.IO

let fullPath = @"C:/Users/vkarvelas/Repositories/IntegratedPaypal"
let directoryInfo = DirectoryInfo(fullPath)
let lastPart = directoryInfo.Name

printfn "Last part of the path: %s" lastPart
