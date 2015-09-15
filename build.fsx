#I @"packages/Octokit.0.15.0/lib/net45"
#r "Octokit.dll"
#r @"packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Git
open Octokit
open System

let version = "0.0.1"

// change to your account
let user = "forki"
let pw = "PASSWORD"
let gitOwner = "forki"
let gitRepo = "TestUpload"

// commit and push to repo
StageAll ""
Git.Commit.Commit "" (sprintf "Bump version to %s" version)
Branches.pushBranch "" "origin" (Information.getBranchName "")

Branches.tag "" version
Branches.pushTag "" "origin" version   

type Draft =
    { Client : GitHubClient
      Owner : string
      Project : string
      DraftRelease : Release }

let createClient user password =
    async {
        let github = new GitHubClient(new ProductHeaderValue("FAKE"))
        github.Credentials <- Credentials(user, password)
        return github
    }

let private createDraft owner project version prerelease (notes:seq<string>) (client : Async<GitHubClient>) =
    async {
        let! client' = client
        let data = new NewRelease(version)
        data.Name <- version
        data.Body <- String.Join(Environment.NewLine, notes)
        data.Draft <- true
        data.Prerelease <- prerelease
        let! draft = Async.AwaitTask <| client'.Release.Create(owner, project, data)
        let draftWord = if data.Draft then " draft" else ""
        printfn "Created%s release id %d" draftWord draft.Id
        return {
            Client = client'
            Owner = owner
            Project = project
            DraftRelease = draft }
    }

let uploadFile fileName (draft : Async<Draft>) =
    async {
        let! draft' = draft
        let fi = IO.FileInfo(fileName)
        let archiveContents = IO.File.OpenRead(fi.FullName)
        let assetUpload = new ReleaseAssetUpload(fi.Name,"application/octet-stream",archiveContents,Nullable<TimeSpan>())
        let! asset = Async.AwaitTask <| draft'.Client.Release.UploadAsset(draft'.DraftRelease, assetUpload)
        printfn "Uploaded %s" asset.Name
        return draft'
    }


let releaseDraft (draft : Async<Draft>) =
    async {
        let! draft' = draft
        let update = draft'.DraftRelease.ToUpdate()
        update.Draft <- Nullable<bool>(false)
        let! released = Async.AwaitTask <| draft'.Client.Release.Edit(draft'.Owner, draft'.Project, draft'.DraftRelease.Id, update)
        printfn "Released %d on github" released.Id
    }

// release on github
createClient user pw
|> createDraft gitOwner gitRepo version false ["Some text"]
|> uploadFile "./build.fsx"
|> releaseDraft
|> Async.RunSynchronously