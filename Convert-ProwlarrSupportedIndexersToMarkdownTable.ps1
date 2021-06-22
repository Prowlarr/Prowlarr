#Requires -Module FormatMarkdownTable

<#
    .SYNOPSIS
        Name: Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1
        The purpose of this script is to export a markdown table for the wiki of the available indexers

    .DESCRIPTION
        Grabs available indexers from a locally installed Prowlarr instance

    .NOTES
        This script has been tested on Windows PowerShell 5.1 and PowerShell 7.1.3

    .EXAMPLE
    PS> .\Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1 -Commit 1.1.1.1 -Build "test" -APIKey "asjdhfjashdf89787asdfsad87676" -BaseURL http://prowlarr:9696 -OutputFile "supported-indexers.md"
    
    .EXAMPLE
    PS> .\Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1 -Commit 1.1.1.1 -Build "test" -APIKey "asjdhfjashdf89787asdfsad87676" -BaseURL http://prowlarr:9696

    .EXAMPLE
    PS> .\Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1 -Commit 1.1.1.1 -Build "test" -APIKey "asjdhfjashdf89787asdfsad87676" -OutputFile "supported-indexers.md"

    .EXAMPLE
    PS> .\Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1 -Commit 1.1.1.1 -Build "test" -APIKey "asjdhfjashdf89787asdfsad87676"
#>

param (
    [Parameter(
        Mandatory, 
        Position = 1
    )]
    [string]
    $Commit,
    [Parameter(
        Mandatory, 
        Position = 2
    )]
    [string]
    $Build,
    [Parameter(
        Mandatory,
        Position = 3
    )]
    [string]
    $APIKey,
    [Parameter(
        Position = 4
    )]
    [System.IO.FileInfo]
    $OutputFile = ".$([System.IO.Path]::DirectorySeparatorChar)supported-indexers.md",
    [Parameter(
        Position = 5
    )]
    [uri]
    $BaseURL = "http://localhost:9696"
)

$url=($BaseURL.ToString().TrimEnd("/"))+'/api/v1/Indexer/schema'
$headers=@{'X-Api-Key'=$APIKey}
$R= Invoke-WebRequest -Uri $url -Headers $headers -ContentType 'application/json' -Method Get
$exp_indexername= { IF ($_.baseUrl) {'['+$_.name+']('+$_.baseUrl+')'+'{#'+$_.infoLink.Replace('https://wiki.servarr.com/prowlarr/supported-indexers#','')+'}'} Else {$_.name+'{#'+$_.infoLink.Replace('https://wiki.servarr.com/prowlarr/supported-indexers#','')+'}'}}
$indexerobject= $R.Content | ConvertFrom-Json
$tblobj= $indexerobject | Sort-Object -Property 'name'

#$tbl_PubUse= $tblobj | Where-Object {($_.privacy -eq "public") -and ($_.protocol -eq "usenet")} | `
#         Select-Object @{Name = 'Indexer'; Expression =$exp_indexername}, `
#         @{Name = 'Language'; Expression = {$_.language}}, `
#         @{Name = 'Description'; Expression = {$_.description}}

$tbl_PrvUse= $tblobj | Where-Object {($_.privacy -CIn "private" -and $_.protocol -eq "usenet")} | `
         Select-Object @{Name = 'Indexer'; Expression =$exp_indexername}, `
         @{Name = 'Language'; Expression = {$_.language}}, `
         @{Name = 'Description'; Expression = {$_.description}}

$tbl_PubTor= $tblobj | Where-Object {($_.privacy -eq "public") -and ($_.protocol -eq "torrent")} | `
         Select-Object @{Name = 'Indexer'; Expression =$exp_indexername}, `
         @{Name = 'Language'; Expression = {$_.language}}, `
         @{Name = 'Description'; Expression = {$_.description}}
        
        
$tbl_PrvTor= $tblobj | Where-Object {($_.privacy -CIn "private" -and $_.protocol -eq "torrent")} | `
         Select-Object @{Name = 'Indexer'; Expression =$exp_indexername}, `
         @{Name = 'Language'; Expression = {$_.language}}, `
         @{Name = 'Description'; Expression = {$_.description}}         

#$fmttablePubUse= ConvertTo-Markdown($tbl_PubUse)
$fmttablePubUse= "None"
$fmttablePrvUse= $tbl_PrvUse | Format-MarkdownTableTableStyle Indexer, Description, Language -HideStandardOutput -ShowMarkdown -DoNotCopyToClipboard
$fmttablePubTor= $tbl_PubTor | Format-MarkdownTableTableStyle Indexer, Description, Language -HideStandardOutput -ShowMarkdown -DoNotCopyToClipboard 
$fmttablePrvTor= $tbl_PrvTor | Format-MarkdownTableTableStyle Indexer, Description, Language -HideStandardOutput -ShowMarkdown -DoNotCopyToClipboard

$prefix= "`n"+"- Supported Indexers as of Build ``" + $build + "`` / [Commit: " + $commit + "](https://github.com/Prowlarr/Prowlarr/commit/" + $commit + ")"

$fmttabletor= "`r`n"+"## Torrents" + "`r`n`r`n"  + "### Public" + "`r`n`r`n" + $fmttablePubTor + "`r`n`r`n" + "### Private" + "`r`n`r`n" + $fmttablePrvTor + "`r`n`r`n"
$fmttableuse= "## Usenet" + "`r`n`r`n" + "### Public" + "`r`n`r`n" + $fmttablePubUse + "`r`n`r`n" + "### Private" + "`r`n`r`n" + $fmttablePrvUse
$date = [DateTime]::UtcNow.ToString("o")
$mdHeader = "---
title: Prowlarr Supported Indexers
description: Indexers currently named as supported in the current nightly build of Prowlarr. Other indexers are available via either Generic Newznab or Generic Torznab. 
published: true
date: $date
tags: prowlarr, indexers
editor: markdown
dateCreated: $date
---"

$fmttableall= $mdHeader + "`n" + $prefix + "`n" + $fmttabletor + $fmttableuse
$fmttableall | Out-File $OutputFile
