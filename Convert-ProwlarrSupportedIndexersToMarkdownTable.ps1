#Requires -Module FormatMarkdownTable

<#
    .SYNOPSIS
        Name: Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1
        The purpose of this script is to export a markdown table for the wiki of the available indexers
    .DESCRIPTION
        Grabs build number and available indexers from a local or remotely installed Prowlarr instance
    .NOTES
        This script has been tested on Windows PowerShell 5.1 and PowerShell 7.1.3
    .EXAMPLE
    PS> .\Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1 -Commit 1.1.1.1 -Build "test" -AppAPIKey "asjdhfjashdf89787asdfsad87676" -AppBaseURL http://prowlarr:9696 -OutputFile "supported-indexers.md"
    .EXAMPLE
    PS> .\Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1 -Commit 1.1.1.1 -Build "test" -AppAPIKey "asjdhfjashdf89787asdfsad87676" -AppBaseURL http://prowlarr:9696
    .EXAMPLE
    PS> .\Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1 -Commit 1.1.1.1 -Build "test" -AppAPIKey "asjdhfjashdf89787asdfsad87676" -OutputFile "supported-indexers.md"
    .EXAMPLE
    PS> .\Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1 -Commit 1.1.1.1 -AppAPIKey "asjdhfjashdf89787asdfsad87676"
#>

param (
    [Parameter(Mandatory, Position = 1)]
    [string]
    $Commit,
    [Parameter(Position = 2)]
    [string]
    $Build,
    [Parameter(Mandatory, Position = 3)]
    [string]
    $AppAPIKey,
    [Parameter(Position = 4)]
    [System.IO.FileInfo]
    $OutputFile = ".$([System.IO.Path]::DirectorySeparatorChar)supported-indexers.md",
    [Parameter(Position = 5)]
    [uri]
    $AppBaseURL = 'http://localhost:9696'
),

## Gather Inputs & Variables
### User Inputs
#### Convert Params to match vars
$app_baseUrl = $AppBaseURL
$app_apikey = $AppAPIKey
####

## Start Variables
### Application Details
$app_api_version = 'v1'
$app_api_path = '/api/'
$app_api_endpoint_version = '/system/status'
$app_api_endpoint_indexer = '/indexer/schema'
$headers = @{'X-Api-Key' = $app_apikey }
#### Github App Info
$gh_app_org = 'Prowlarr'
$gh_app_repo = 'Prowlarr'
#$gh_app_branch = 'develop'
## Disabled; API used for manual usage only
### Wiki Details
$wiki_link = 'https://wiki.servarr.com'
$wiki_app_path = '/prowlarr'
$wiki_page = 'supported-indexers'
$wiki_bookmark = '#'
### Page Formating
$wiki_1newline = "`r`n"
$wiki_2newline = "`r`n`r`n"
#$wiki_encoding = 'utf8'
### Github Details
$gh_web = 'https://github.com'
$gh_web_commit = 'commit/'
#$gh_baseurl = 'https://api.github.com'
#$gh_api_path = '/'
#$gh_api_type = 'repos/'
#$gh_api_branches = 'branches/'
## Disabled; GH API used for manual usage only
## End Variables
Write-Host 'Variables and Inputs Imported'

## Build Parameters
### App
$api_url = ($app_baseUrl.ToString().TrimEnd('/')) + $app_api_path + $app_api_version
$version_url = $api_url + $app_api_endpoint_version
$indexer_url = $api_url + $app_api_endpoint_indexer
### Github
$gh_repo_org = $gh_app_org + '/' + $gh_app_repo + '/'
#$gh_url = ($gh_baseUrl.ToString().TrimEnd('/')) + $gh_api_path + $gh_api_type + $gh_repo_org + $gh_api_branches + $gh_app_branch
## Disabled; API used for manual usage only
## 
### Wiki
$wiki_infolink = ($wiki_link.ToString().TrimEnd('/')) + $wiki_app_path + '/' + $wiki_page + $wiki_bookmark
$wiki_commiturl = ($gh_web.ToString().TrimEnd('/')) + '/' + $gh_repo_org + $gh_web_commit
Write-Host 'Parameters Built'

## Invoke Requests
#Write-Host 'Getting Github Commit Info'
#$github_req = Invoke-WebRequest -Uri $gh_url -ContentType 'application/json' -Method Get
#Write-Host 'Got Github Commit Info'
## Disabled; API used for manual usage only
Write-Host 'Getting App Version'
$version_req = Invoke-WebRequest -Uri $version_url -Headers $headers -ContentType 'application/json' -Method Get
Write-Host 'Got App Version'
Write-Host 'Getting Indexer Data'
$indexer_req = Invoke-WebRequest -Uri $indexer_url -Headers $headers -ContentType 'application/json' -Method Get
Write-Host 'Got Indexer Data'

## Convert to Objects
Write-Host 'Converting Responses to Objects'
#$github_obj = $github_req.Content | ConvertFrom-Json
## Disabled; API used for manual usage only
$version_obj = $version_req.Content | ConvertFrom-Json
$indexer_obj = $indexer_req.Content | ConvertFrom-Json
$indexer_name_exp = { IF ($_.IndexerUrls) { '[' + $_.name + '](' + ($_.IndexerUrls[0]) + ')' + '{#' + $_.infoLink.Replace($wiki_infolink.ToString(), '') + '}' } Else { $_.name + '{#' + $_.infoLink.Replace($wiki_infolink.ToString(), '') + '}' } }

## Determine Commit from GH
#Write-Host 'Determining Commit'
#$commit = $github_obj | Select-Object -ExpandProperty 'commit' | Select-Object -ExpandProperty 'sha' | Out-String
#$commit = $commit -replace "`n |`r", ''
## Disabled; API used for manual usage only
Write-Host "Commit is $commit"


## Determine Version (Build)
Write-Host 'Determining Build'
$build = $version_obj | Select-Object -ExpandProperty 'version' | Out-String
$build = $build -replace "`n|`r", ''
Write-Host "Build is $build"

Write-Host 'Ingesting Indexer Data'
## Get Indexer Data
$indexer_tbl_obj = $indexer_obj | Sort-Object -Property 'name'

## Build Table Fields
Write-Host 'Building Indexer Tables'
### Public Usenet (Disabled)
#Write-Host 'Building: Usenet - Public'
#$tbl_PubUse= $indexer_tbl_obj | Where-Object {($_.privacy -eq "public") -and ($_.protocol -eq "usenet")} | `
#         Select-Object @{Name = 'Indexer'; Expression =$indexer_name_exp}, `
#         @{Name = 'Language'; Expression = {$_.language}}, `
#         @{Name = 'Description'; Expression = {$_.description}}

### Private Usenet
Write-Host 'Building: Usenet - Private'
$tbl_PrvUse = $indexer_tbl_obj | Where-Object { ($_.privacy -CIn 'private' -and $_.protocol -eq 'usenet') } | `
    Select-Object @{Name = 'Indexer'; Expression = $indexer_name_exp }, `
@{Name = 'Language'; Expression = { $_.language } }, `
@{Name = 'Description'; Expression = { $_.description } }

### Public Torrents
Write-Host 'Building: Torrents - Public'
$tbl_PubTor = $indexer_tbl_obj | Where-Object { ($_.privacy -eq 'public') -and ($_.protocol -eq 'torrent') } | `
    Select-Object @{Name = 'Indexer'; Expression = $indexer_name_exp }, `
@{Name = 'Language'; Expression = { $_.language } }, `
@{Name = 'Description'; Expression = { $_.description } }
        
### Private Torrents
Write-Host 'Building: Torrents - Private'
$tbl_PrvTor = $indexer_tbl_obj | Where-Object { ($_.privacy -CIn 'private' -and $_.protocol -eq 'torrent') } | `
    Select-Object @{Name = 'Indexer'; Expression = $indexer_name_exp }, `
@{Name = 'Language'; Expression = { $_.language } }, `
@{Name = 'Description'; Expression = { $_.description } }         

## Convert Data to Markdown Table
#$tbl_fmt_PubUse= ConvertTo-Markdown($tbl_PubUse)
$tbl_fmt_PubUse = 'None'
$tbl_fmt_PrvUse = $tbl_PrvUse | Format-MarkdownTableTableStyle Indexer, Description, Language -HideStandardOutput -ShowMarkdown -DoNotCopyToClipboard
$tbl_fmt_PubTor = $tbl_PubTor | Format-MarkdownTableTableStyle Indexer, Description, Language -HideStandardOutput -ShowMarkdown -DoNotCopyToClipboard 
$tbl_fmt_PrvTor = $tbl_PrvTor | Format-MarkdownTableTableStyle Indexer, Description, Language -HideStandardOutput -ShowMarkdown -DoNotCopyToClipboard
Write-Host 'Builds Converted to Markdown Tables'

## Page Header Info
$wiki_page_start = $wiki_1newline + "- Supported Indexers as of Build ``" + $build + "`` / [Commit: " + $commit + '](' + $wiki_commiturl + $commit + ')'
Write-Host 'Page Header Built'

## Build Page Pieces'
$tbl_fmt_tor = $wiki_1newline + '## Torrents' + $wiki_2newline + '### Public' + $wiki_2newline + $tbl_fmt_PubTor + $wiki_2newline + '### Private' + $wiki_2newline + $tbl_fmt_PrvTor + $wiki_2newline
$tbl_fmt_use = '## Usenet' + $wiki_2newline + '### Public' + $wiki_2newline + $tbl_fmt_PubUse + $wiki_2newline + '### Private' + $wiki_2newline + $tbl_fmt_PrvUse
Write-Host 'Wiki Markdown Tables Built'
$date = [DateTime]::UtcNow.ToString('o')
$mdHeader = 
"---
title: Prowlarr Supported Indexers
description: Indexers currently named as supported in the current nightly build of Prowlarr. Other indexers are available via either Generic Newznab or Generic Torznab.
published: true
date: $date
tags: prowlarr, indexers
editor: markdown
dateCreated: $date
---"
Write-Host 'Wiki Page pieces built'
## Build and output Page
$wiki_page_file = $mdHeader + $wiki_2newline + $wiki_page_start + $wiki_1newline + $tbl_fmt_tor + $tbl_fmt_use
Write-Host 'Wiki Page Built'
#$wiki_page_file | Out-File $OutputFile -Encoding $wiki_encoding
[IO.File]::WriteAllLines(($OutputFile | Resolve-Path), $wiki_page_file)
Write-Host 'Wiki Page Output'